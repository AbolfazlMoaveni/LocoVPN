using LocoVPN.Core;

namespace LocoVPN;

public partial class LoadingForm : Form
{
    public AppController Controller { get; }

    private readonly CancellationTokenSource _cts = new();

    public LoadingForm(AppController controller)
    {
        Controller = controller;
        InitializeComponent();

        Controller.Log += OnLog;
        Controller.ProgressChanged += OnProgress;

        Load += LoadingForm_Load;
        FormClosed += (_, _) =>
        {
            Controller.Log -= OnLog;
            Controller.ProgressChanged -= OnProgress;
        };
    }

    private async void LoadingForm_Load(object? sender, EventArgs e)
    {
        try
        {
            Controller.RecoverFromCrash();
            await Controller.FetchAndFilterAsync(_cts.Token);
            DialogResult = DialogResult.OK;
        }
        catch (OperationCanceledException)
        {
            // User clicked the close button - just exit without opening MainForm.
            DialogResult = DialogResult.Cancel;
        }
        catch (Exception ex)
        {
            // Don't strand the user on the loading screen - surface the error, then still
            // continue to MainForm (it will just start with an empty/partial server list).
            MessageBox.Show(this, $"Something went wrong while preparing servers:\n{ex.Message}",
                "LocoVPN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.OK;
        }
        finally
        {
            Close();
        }
    }

    private void OnLog(string message)
    {
        if (InvokeRequired) { Invoke(() => OnLog(message)); return; }
        lblStatus.Content = message;
    }

    private void OnProgress(int percent)
    {
        if (InvokeRequired) { Invoke(() => OnProgress(percent)); return; }

        // NOTE: assumes cuiCircleProgressBar exposes a 0-100 "Value" property (the common
        // WinForms progress-control convention). If your installed CuoreUI version names
        // this differently, update this one line - everything else is unaffected.
        circleProgress.ProgressValue = Math.Clamp(percent, 0, 100);
    }

    private void BtnClose_Click(object? sender, EventArgs e)
    {
        _cts.Cancel();
    }
}
