using LocoVPN.Core;

namespace LocoVPN;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var controller = new AppController();

        using (var loading = new LoadingForm(controller))
        {
            var result = loading.ShowDialog();
            if (result != DialogResult.OK)
                return; // user closed the loading screen - exit without opening MainForm
        }

        Application.Run(new MainForm(controller));
    }
}
