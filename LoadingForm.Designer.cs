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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoadingForm));
        btnClose = new cuiButton();
        lblTitle = new cuiLabel();
        picLogo = new PictureBox();
        circleProgress = new cuiCircleProgressBar();
        lblStatus = new cuiLabel();
        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        SuspendLayout();
        // 
        // btnClose
        // 
        btnClose.CheckButton = false;
        btnClose.Checked = false;
        btnClose.CheckedBackground = Color.FromArgb(255, 106, 0);
        btnClose.CheckedForeColor = Color.White;
        btnClose.CheckedImageTint = Color.White;
        btnClose.CheckedOutline = Color.FromArgb(255, 106, 0);
        btnClose.Content = "×";
        btnClose.DialogResult = DialogResult.None;
        btnClose.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnClose.ForeColor = Color.White;
        btnClose.HoverBackground = Color.FromArgb(50, 255, 255, 255);
        btnClose.HoverForeColor = Color.White;
        btnClose.HoverImageTint = Color.White;
        btnClose.HoverOutline = Color.FromArgb(32, 128, 128, 128);
        btnClose.Image = null;
        btnClose.ImageAutoCenter = true;
        btnClose.ImageExpand = new Point(0, 0);
        btnClose.ImageOffset = new Point(0, 0);
        btnClose.Location = new Point(376, 16);
        btnClose.Name = "btnClose";
        btnClose.NormalBackground = Color.FromArgb(30, 255, 255, 255);
        btnClose.NormalForeColor = Color.White;
        btnClose.NormalImageTint = Color.White;
        btnClose.NormalOutline = Color.FromArgb(64, 128, 128, 128);
        btnClose.OutlineThickness = 0F;
        btnClose.PressedBackground = Color.FromArgb(70, 255, 255, 255);
        btnClose.PressedForeColor = Color.White;
        btnClose.PressedImageTint = Color.White;
        btnClose.PressedOutline = Color.FromArgb(64, 128, 128, 128);
        btnClose.Rounding = new Padding(16);
        btnClose.Size = new Size(32, 32);
        btnClose.TabIndex = 0;
        btnClose.TextAlignment = StringAlignment.Center;
        btnClose.TextOffset = new Point(0, 0);
        btnClose.Click += BtnClose_Click;
        // 
        // lblTitle
        // 
        lblTitle.Content = "LocoVPN";
        lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.HorizontalAlignment = StringAlignment.Center;
        lblTitle.Location = new Point(0, 200);
        lblTitle.Margin = new Padding(4, 3, 4, 3);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(420, 40);
        lblTitle.TabIndex = 2;
        lblTitle.VerticalAlignment = StringAlignment.Near;
        // 
        // picLogo
        // 
        picLogo.BackColor = Color.Transparent;
        picLogo.BackgroundImage = (Image)resources.GetObject("picLogo.BackgroundImage");
        picLogo.BackgroundImageLayout = ImageLayout.Stretch;
        picLogo.Location = new Point(160, 90);
        picLogo.Name = "picLogo";
        picLogo.Size = new Size(100, 100);
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.TabIndex = 1;
        picLogo.TabStop = false;
        // 
        // circleProgress
        // 
        circleProgress.BorderWidth = 12;
        circleProgress.Location = new Point(160, 280);
        circleProgress.MaximumValue = 100;
        circleProgress.MinimumSize = new Size(24, 24);
        circleProgress.MinimumValue = 0;
        circleProgress.Name = "circleProgress";
        circleProgress.NormalColor = Color.FromArgb(64, 128, 128, 128);
        circleProgress.ProgressColor = Color.FromArgb(255, 106, 0);
        circleProgress.ProgressValue = 50;
        circleProgress.RoundedEnds = true;
        circleProgress.Size = new Size(100, 100);
        circleProgress.TabIndex = 3;
        // 
        // lblStatus
        // 
        lblStatus.Content = "Finding\\ the\\ best\\ servers\\ for\\ you\\.\\.\\.";
        lblStatus.Font = new Font("Segoe UI", 10F);
        lblStatus.ForeColor = Color.FromArgb(180, 190, 205);
        lblStatus.HorizontalAlignment = StringAlignment.Center;
        lblStatus.Location = new Point(20, 410);
        lblStatus.Margin = new Padding(4, 3, 4, 3);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(380, 40);
        lblStatus.TabIndex = 4;
        lblStatus.VerticalAlignment = StringAlignment.Near;
        // 
        // LoadingForm
        // 
        BackColor = Color.FromArgb(30, 40, 48);
        ClientSize = new Size(420, 560);
        Controls.Add(lblStatus);
        Controls.Add(circleProgress);
        Controls.Add(lblTitle);
        Controls.Add(picLogo);
        Controls.Add(btnClose);
        FormBorderStyle = FormBorderStyle.None;
        Name = "LoadingForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "LocoVPN";
        ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
        ResumeLayout(false);
    }
}
