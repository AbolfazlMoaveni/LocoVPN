using CuoreUI.Controls;

namespace LocoVPN;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private cuiButton btnExit = null!;
    private PictureBox picLogo = null!;
    private cuiLabel lblTitle = null!;
    private cuiLabel lblSelectServer = null!;
    private cuiComboBox comboServers = null!;
    private cuiButton btnConnectToggle = null!;
    private cuiButton btnRefresh = null!;
    private TextBox txtLog = null!;

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
        btnExit = new cuiButton();
        picLogo = new PictureBox();
        lblTitle = new cuiLabel();
        lblSelectServer = new cuiLabel();
        comboServers = new cuiComboBox();
        btnConnectToggle = new cuiButton();
        btnRefresh = new cuiButton();
        txtLog = new TextBox();
        lblSelectedServer = new cuiLabel();
        lblConnectionState = new cuiLabel();
        pnlStatusBar = new cuiPanel();
        lblConnectCaption = new cuiLabel();
        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        pnlStatusBar.SuspendLayout();
        SuspendLayout();
        // 
        // btnExit
        // 
        btnExit.CheckButton = false;
        btnExit.Checked = false;
        btnExit.CheckedBackground = Color.FromArgb(255, 106, 0);
        btnExit.CheckedForeColor = Color.White;
        btnExit.CheckedImageTint = Color.White;
        btnExit.CheckedOutline = Color.FromArgb(255, 106, 0);
        btnExit.Content = "×";
        btnExit.DialogResult = DialogResult.None;
        btnExit.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        btnExit.ForeColor = Color.White;
        btnExit.HoverBackground = Color.FromArgb(50, 255, 255, 255);
        btnExit.HoverForeColor = Color.White;
        btnExit.HoverImageTint = Color.White;
        btnExit.HoverOutline = Color.FromArgb(32, 128, 128, 128);
        btnExit.Image = null;
        btnExit.ImageAutoCenter = true;
        btnExit.ImageExpand = new Point(0, 0);
        btnExit.ImageOffset = new Point(0, 0);
        btnExit.Location = new Point(307, 16);
        btnExit.Name = "btnExit";
        btnExit.NormalBackground = Color.FromArgb(30, 255, 255, 255);
        btnExit.NormalForeColor = Color.White;
        btnExit.NormalImageTint = Color.White;
        btnExit.NormalOutline = Color.FromArgb(64, 128, 128, 128);
        btnExit.OutlineThickness = 0F;
        btnExit.PressedBackground = Color.FromArgb(70, 255, 255, 255);
        btnExit.PressedForeColor = Color.White;
        btnExit.PressedImageTint = Color.White;
        btnExit.PressedOutline = Color.FromArgb(64, 128, 128, 128);
        btnExit.Rounding = new Padding(16);
        btnExit.Size = new Size(32, 32);
        btnExit.TabIndex = 0;
        btnExit.TextAlignment = StringAlignment.Center;
        btnExit.TextOffset = new Point(0, 0);
        btnExit.Click += BtnExit_Click;
        // 
        // picLogo
        // 
        picLogo.BackColor = Color.Transparent;
        picLogo.BackgroundImage = Properties.Resources.logo2;
        picLogo.BackgroundImageLayout = ImageLayout.Stretch;
        picLogo.Location = new Point(20, 16);
        picLogo.Name = "picLogo";
        picLogo.Size = new Size(40, 40);
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.TabIndex = 1;
        picLogo.TabStop = false;
        // 
        // lblTitle
        // 
        lblTitle.Content = "LocoVPN";
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.HorizontalAlignment = StringAlignment.Center;
        lblTitle.Location = new Point(68, 24);
        lblTitle.Margin = new Padding(4, 3, 4, 3);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(200, 28);
        lblTitle.TabIndex = 2;
        lblTitle.VerticalAlignment = StringAlignment.Near;
        // 
        // lblSelectServer
        // 
        lblSelectServer.Content = "Select\\ Server";
        lblSelectServer.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblSelectServer.ForeColor = Color.FromArgb(220, 225, 235);
        lblSelectServer.HorizontalAlignment = StringAlignment.Center;
        lblSelectServer.Location = new Point(24, 257);
        lblSelectServer.Margin = new Padding(4, 3, 4, 3);
        lblSelectServer.Name = "lblSelectServer";
        lblSelectServer.Size = new Size(300, 24);
        lblSelectServer.TabIndex = 3;
        lblSelectServer.VerticalAlignment = StringAlignment.Near;
        // 
        // comboServers
        // 
        comboServers.BackgroundColor = Color.FromArgb(24, 30, 44);
        comboServers.ButtonCursor = Cursors.Arrow;
        comboServers.ButtonHoverBackground = Color.FromArgb(192, 255, 106, 0);
        comboServers.ButtonHoverOutline = Color.Empty;
        comboServers.ButtonNormalBackground = Color.FromArgb(255, 106, 0);
        comboServers.ButtonNormalOutline = Color.Empty;
        comboServers.ButtonPressedBackground = Color.FromArgb(255, 106, 0);
        comboServers.ButtonPressedOutline = Color.Empty;
        comboServers.DropDownBackgroundColor = Color.FromArgb(24, 30, 44);
        comboServers.DropDownOutlineColor = Color.FromArgb(30, 255, 255, 255);
        comboServers.ExpandArrowColor = Color.Gray;
        comboServers.ForeColor = Color.White;
        comboServers.Location = new Point(13, 299);
        comboServers.Margin = new Padding(4, 3, 4, 3);
        comboServers.Name = "comboServers";
        comboServers.NoSelectionDropdownText = "Empty";
        comboServers.NoSelectionText = "No servers yet";
        comboServers.OutlineColor = Color.FromArgb(60, 255, 255, 255);
        comboServers.OutlineThickness = 1F;
        comboServers.Rounding = 8;
        comboServers.Size = new Size(326, 44);
        comboServers.TabIndex = 4;
        comboServers.SelectedIndexChanged += ComboServers_SelectedIndexChanged;
        // 
        // btnConnectToggle
        // 
        btnConnectToggle.CheckButton = false;
        btnConnectToggle.Checked = false;
        btnConnectToggle.CheckedBackground = Color.FromArgb(255, 106, 0);
        btnConnectToggle.CheckedForeColor = Color.White;
        btnConnectToggle.CheckedImageTint = Color.White;
        btnConnectToggle.CheckedOutline = Color.FromArgb(255, 106, 0);
        btnConnectToggle.Content = "Connect";
        btnConnectToggle.DialogResult = DialogResult.None;
        btnConnectToggle.Font = new Font("Impact", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        btnConnectToggle.ForeColor = Color.White;
        btnConnectToggle.HoverBackground = Color.FromArgb(240, 95, 65);
        btnConnectToggle.HoverForeColor = Color.White;
        btnConnectToggle.HoverImageTint = Color.White;
        btnConnectToggle.HoverOutline = Color.FromArgb(32, 128, 128, 128);
        btnConnectToggle.Image = null;
        btnConnectToggle.ImageAutoCenter = true;
        btnConnectToggle.ImageExpand = new Point(0, 0);
        btnConnectToggle.ImageOffset = new Point(0, 0);
        btnConnectToggle.Location = new Point(126, 385);
        btnConnectToggle.Name = "btnConnectToggle";
        btnConnectToggle.NormalBackground = Color.FromArgb(230, 80, 50);
        btnConnectToggle.NormalForeColor = Color.White;
        btnConnectToggle.NormalImageTint = Color.White;
        btnConnectToggle.NormalOutline = Color.FromArgb(64, 128, 128, 128);
        btnConnectToggle.OutlineThickness = 0F;
        btnConnectToggle.PressedBackground = Color.FromArgb(200, 65, 40);
        btnConnectToggle.PressedForeColor = Color.White;
        btnConnectToggle.PressedImageTint = Color.White;
        btnConnectToggle.PressedOutline = Color.FromArgb(64, 128, 128, 128);
        btnConnectToggle.Rounding = new Padding(50);
        btnConnectToggle.Size = new Size(100, 100);
        btnConnectToggle.TabIndex = 6;
        btnConnectToggle.TextAlignment = StringAlignment.Center;
        btnConnectToggle.TextOffset = new Point(0, 0);
        btnConnectToggle.Click += BtnConnectToggle_Click;
        // 
        // btnRefresh
        // 
        btnRefresh.CheckButton = false;
        btnRefresh.Checked = false;
        btnRefresh.CheckedBackground = Color.FromArgb(255, 106, 0);
        btnRefresh.CheckedForeColor = Color.White;
        btnRefresh.CheckedImageTint = Color.White;
        btnRefresh.CheckedOutline = Color.FromArgb(255, 106, 0);
        btnRefresh.Content = "Refresh Server List";
        btnRefresh.DialogResult = DialogResult.None;
        btnRefresh.Font = new Font("Segoe UI", 9F);
        btnRefresh.ForeColor = Color.FromArgb(150, 160, 180);
        btnRefresh.HoverBackground = Color.FromArgb(20, 255, 255, 255);
        btnRefresh.HoverForeColor = Color.White;
        btnRefresh.HoverImageTint = Color.White;
        btnRefresh.HoverOutline = Color.FromArgb(32, 128, 128, 128);
        btnRefresh.Image = null;
        btnRefresh.ImageAutoCenter = true;
        btnRefresh.ImageExpand = new Point(0, 0);
        btnRefresh.ImageOffset = new Point(0, 0);
        btnRefresh.Location = new Point(12, 349);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.NormalBackground = Color.Transparent;
        btnRefresh.NormalForeColor = Color.FromArgb(150, 160, 180);
        btnRefresh.NormalImageTint = Color.White;
        btnRefresh.NormalOutline = Color.FromArgb(64, 128, 128, 128);
        btnRefresh.OutlineThickness = 0F;
        btnRefresh.PressedBackground = Color.FromArgb(35, 255, 255, 255);
        btnRefresh.PressedForeColor = Color.FromArgb(32, 32, 32);
        btnRefresh.PressedImageTint = Color.White;
        btnRefresh.PressedOutline = Color.FromArgb(64, 128, 128, 128);
        btnRefresh.Rounding = new Padding(8);
        btnRefresh.Size = new Size(160, 30);
        btnRefresh.TabIndex = 5;
        btnRefresh.TextAlignment = StringAlignment.Center;
        btnRefresh.TextOffset = new Point(0, 0);
        btnRefresh.Click += BtnRefresh_Click;
        // 
        // txtLog
        // 
        txtLog.BackColor = Color.FromArgb(20, 25, 36);
        txtLog.BorderStyle = BorderStyle.None;
        txtLog.ForeColor = Color.FromArgb(150, 160, 180);
        txtLog.Location = new Point(24, 81);
        txtLog.Multiline = true;
        txtLog.Name = "txtLog";
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(315, 160);
        txtLog.TabIndex = 9;
        // 
        // lblSelectedServer
        // 
        lblSelectedServer.Content = "";
        lblSelectedServer.Font = new Font("Segoe UI", 9F);
        lblSelectedServer.ForeColor = Color.FromArgb(200, 205, 215);
        lblSelectedServer.HorizontalAlignment = StringAlignment.Center;
        lblSelectedServer.Location = new Point(140, 0);
        lblSelectedServer.Margin = new Padding(4, 3, 4, 3);
        lblSelectedServer.Name = "lblSelectedServer";
        lblSelectedServer.Size = new Size(147, 44);
        lblSelectedServer.TabIndex = 1;
        lblSelectedServer.VerticalAlignment = StringAlignment.Near;
        // 
        // lblConnectionState
        // 
        lblConnectionState.Content = "●\\ Disconnected";
        lblConnectionState.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblConnectionState.ForeColor = Color.FromArgb(230, 100, 100);
        lblConnectionState.HorizontalAlignment = StringAlignment.Center;
        lblConnectionState.Location = new Point(16, 0);
        lblConnectionState.Margin = new Padding(4, 3, 4, 3);
        lblConnectionState.Name = "lblConnectionState";
        lblConnectionState.Size = new Size(116, 44);
        lblConnectionState.TabIndex = 0;
        lblConnectionState.VerticalAlignment = StringAlignment.Near;
        // 
        // pnlStatusBar
        // 
        pnlStatusBar.BackColor = Color.FromArgb(24, 30, 44);
        pnlStatusBar.Controls.Add(lblConnectionState);
        pnlStatusBar.Controls.Add(lblSelectedServer);
        pnlStatusBar.Location = new Point(12, 530);
        pnlStatusBar.Name = "pnlStatusBar";
        pnlStatusBar.OutlineThickness = 1F;
        pnlStatusBar.PanelColor = Color.FromArgb(255, 106, 0);
        pnlStatusBar.PanelOutlineColor = Color.FromArgb(255, 106, 0);
        pnlStatusBar.Rounding = new Padding(22);
        pnlStatusBar.Size = new Size(327, 44);
        pnlStatusBar.TabIndex = 8;
        // 
        // lblConnectCaption
        // 
        lblConnectCaption.Content = "Connect";
        lblConnectCaption.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblConnectCaption.ForeColor = Color.White;
        lblConnectCaption.HorizontalAlignment = StringAlignment.Center;
        lblConnectCaption.Location = new Point(103, 491);
        lblConnectCaption.Margin = new Padding(4, 3, 4, 3);
        lblConnectCaption.Name = "lblConnectCaption";
        lblConnectCaption.Size = new Size(143, 24);
        lblConnectCaption.TabIndex = 7;
        lblConnectCaption.VerticalAlignment = StringAlignment.Near;
        // 
        // MainForm
        // 
        BackColor = Color.FromArgb(30, 40, 48);
        ClientSize = new Size(400, 600);
        Controls.Add(pnlStatusBar);
        Controls.Add(lblConnectCaption);
        Controls.Add(btnConnectToggle);
        Controls.Add(btnRefresh);
        Controls.Add(comboServers);
        Controls.Add(lblSelectServer);
        Controls.Add(txtLog);
        Controls.Add(lblTitle);
        Controls.Add(picLogo);
        Controls.Add(btnExit);
        Font = new Font("Cascadia Code SemiBold", 9F, FontStyle.Bold);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "LocoVPN";
        FormClosing += MainForm_FormClosing;
        ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
        pnlStatusBar.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private cuiLabel lblSelectedServer;
    private cuiLabel lblConnectionState;
    private cuiPanel pnlStatusBar;
    private cuiLabel lblConnectCaption;
}
