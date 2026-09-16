using CuoreUI.Controls;

namespace LocoVPN;

partial class LoadingForm
{
    private System.ComponentModel.IContainer components = null;

    private cuiButton btnClose = null!;
    private cuiLabel lblTitle = null!;
    private PictureBox picLogo = null!;
    private cuiCircleProgressBar circleProgress = null!;
    private cuiLabel lblStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        btnClose = new cuiButton();
        lblTitle = new cuiLabel();
        picLogo = new PictureBox();
        circleProgress = new cuiCircleProgressBar();
        lblStatus = new cuiLabel();

        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        SuspendLayout();

        // Form itself
        BackColor = Color.FromArgb(13, 18, 28);
        ClientSize = new Size(420, 560);
        FormBorderStyle = FormBorderStyle.None;
        Name = "LoadingForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "LocoVPN";

        // btnClose - small circular "x" in the top-right corner
        btnClose.Content = "×";
        btnClose.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnClose.Location = new Point(376, 16);
        btnClose.Name = "btnClose";
        btnClose.Rounding = new Padding(16);
        btnClose.Size = new Size(32, 32);
        btnClose.NormalBackground = Color.FromArgb(30, 255, 255, 255);
        btnClose.HoverBackground = Color.FromArgb(50, 255, 255, 255);
        btnClose.PressedBackground = Color.FromArgb(70, 255, 255, 255);
        btnClose.NormalForeColor = Color.White;
        btnClose.HoverForeColor = Color.White;
        btnClose.PressedForeColor = Color.White;
        btnClose.OutlineThickness = 0;
        btnClose.TabIndex = 0;
        btnClose.Click += BtnClose_Click;

        // picLogo - drop your logo.png in Assets\ to replace the placeholder
        picLogo.Location = new Point(160, 90);
        picLogo.Name = "picLogo";
        picLogo.Size = new Size(100, 100);
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.TabIndex = 1;
        picLogo.TabStop = false;
        picLogo.BackColor = Color.Transparent;

        // lblTitle
        lblTitle.Content = "LocoVPN";
        lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.Location = new Point(0, 200);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(420, 40);
        lblTitle.TabIndex = 2;

        // circleProgress
        circleProgress.Location = new Point(160, 280);
        circleProgress.Name = "circleProgress";
        circleProgress.Size = new Size(100, 100);
        circleProgress.TabIndex = 3;
        // NOTE: exact CuoreUI property names for value/color can vary slightly by version -
        // if these don't match in your installed version, adjust via the Properties panel
        // (look for something like Value/Maximum and a primary/track color pair).

        // lblStatus
        lblStatus.Content = "Finding the best servers for you...";
        lblStatus.Font = new Font("Segoe UI", 10F);
        lblStatus.ForeColor = Color.FromArgb(180, 190, 205);
        lblStatus.Location = new Point(20, 410);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(380, 40);
        lblStatus.TabIndex = 4;

        // LoadingForm
        Controls.Add(lblStatus);
        Controls.Add(circleProgress);
        Controls.Add(lblTitle);
        Controls.Add(picLogo);
        Controls.Add(btnClose);

        ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
        ResumeLayout(false);
    }
}
