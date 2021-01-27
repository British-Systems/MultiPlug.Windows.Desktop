using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MultiPlug.Windows.Desktop.Models;
using MultiPlug.Windows.Desktop.Properties;

namespace MultiPlug.Windows.Desktop
{
    public partial class DiscoveryForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private ContextMenu m_TrayMenu;
        private NotifyIcon m_TrayIcon;

        private Service.Discovery m_DiscoveryService;
        private DataGridModel m_DataGridModel = new DataGridModel();

        private bool m_HiddenOnStartup = true;

        private MenuItem m_DiscoveredMenuItem;

        private bool WindowStateMaximized = false;
        private int WindowStateNormalHeight;
        private Point WindowStateNormalLocation;

        private System.Timers.Timer m_RefreshTimer = new System.Timers.Timer();

        public DiscoveryForm()
        {
            InitializeComponent();

            WindowStateNormalHeight = Height;

            m_TrayMenu = new ContextMenu();

            MenuItem MenuItem = new MenuItem();
            MenuItem.Break = false;
            MenuItem.Text = "Show";
            MenuItem.Click += ShowButton_Click;
            m_TrayMenu.MenuItems.Add(MenuItem);

            m_DiscoveredMenuItem = new MenuItem();
            m_DiscoveredMenuItem.Break = false;
            m_DiscoveredMenuItem.Text = "Discovered";
            m_TrayMenu.MenuItems.Add(m_DiscoveredMenuItem);

            m_TrayMenu.MenuItems.Add("Exit", OnExit);

            m_TrayIcon = new NotifyIcon();
            m_TrayIcon.MouseClick += ShowButton_MouseClick;
            m_TrayIcon.Text = "MultiPlug Desktop";
            m_TrayIcon.Icon = Resources.multiplug;
            m_TrayIcon.ContextMenu = m_TrayMenu;
            m_TrayIcon.Visible = true;

            DataGridModelBindingSource.Add(m_DataGridModel);

            m_DiscoveryService = new Service.Discovery();
            m_DiscoveryService.Resolved += OnDeviceDiscovered;

            Load += Form_Load;

            m_RefreshTimer.Elapsed += OnRefreshTimerElapsed;

            m_RefreshTimer.Interval = 25000;    // Refresh Every 25 Seconds.
        }

        private void OnRefreshTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            m_DiscoveryService.Stop();

            BeginInvoke((MethodInvoker)delegate
            {
                m_DataGridModel.Devices.Clear();
            });

            BeginInvoke((MethodInvoker)delegate
            {
                m_DiscoveredMenuItem.MenuItems.Clear();
            });

            m_DiscoveryService.Start();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (m_HiddenOnStartup)
            {
                m_HiddenOnStartup = false;    // Future actions will be processed normally.
                base.SetVisibleCore(false);
            }
            else
            {
                base.SetVisibleCore(value);
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            m_TrayIcon.Visible = false;
            Application.Exit();
        }

        private void OnDeviceDiscovered(object sender, DataGridRow e)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                m_DataGridModel.Devices.Add(e);
            });

            DataGridView.ClearSelection();

            MenuItem MenuItem = new MenuItem();
            MenuItem.Break = false;
            MenuItem.Text = e.Name;
            MenuItem.Tag = e.Url;
            MenuItem.Click += OnDiscoveredMenuItem_Click;
            MenuItem.Enabled = true;
            m_DiscoveredMenuItem.MenuItems.Add(MenuItem);
        }

        private void Form_Load(object sender, EventArgs e)
        {
            this.MaximumSize = new Size(this.Size.Width, Screen.PrimaryScreen.WorkingArea.Height);
        }

        private string m_ClickedIP = string.Empty;

        private void OnCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex != 4)
            {
                m_ClickedIP = DataGridView.Rows[e.RowIndex].Cells[2].Value.ToString();
            }
        }

        private void OnDiscoveredMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem MenuItem = (MenuItem)sender;

            ProcessStartInfo sInfo = new ProcessStartInfo(MenuItem.Tag.ToString());
            Process.Start(sInfo);
        }

        private void OnCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex != 4)
            {
                ProcessStartInfo sInfo = new ProcessStartInfo(DataGridView.Rows[e.RowIndex].Cells[3].Value.ToString());
                Process.Start(sInfo);
                HideForm();
            }
        }

        private void OnCellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridView.Rows[e.RowIndex].Selected = true;
            }
        }

        private void OnCellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridView.Rows[e.RowIndex].Selected = false;
            }
        }

        private void MaximizeButton_Click(object sender, EventArgs e)
        {
            if (WindowStateMaximized)
            {
                WindowStateMaximized = false;
                SetMaximizeButtonImage();

                // If the form hasn't been moved from the X cord, put it 
                // back to where it was, if it has, just change the height and leave it where it is
                if(WindowStateNormalLocation.X == this.Location.X)
                {
                    this.Location = WindowStateNormalLocation;
                }

                this.Height = WindowStateNormalHeight;
            }
            else
            {
                WindowStateMaximized = true;
                SetMaximizeButtonImage();

                Screen Screen = Screen.FromControl(this);

                this.Height = Screen.WorkingArea.Height;

                WindowStateNormalLocation = this.Location;
                this.Location = new Point(Location.X, 0);
            }

        }

        private void OnResized(object sender, EventArgs e)
        {
            SetMaximizeButtonImage();
        }

        private void SetMaximizeButtonImage()
        {
            MaximizeButton.Image = (WindowStateMaximized) ? Resources.minimize : Resources.maximize;
        }

        private void HideButton_Click(object sender, EventArgs e)
        {
            HideForm();
        }

        private void HideForm()
        {
            m_RefreshTimer.Stop();
            ShowInTaskbar = false;
            Hide();

            // Hack to prevent Button BackColor being Red on Show.
            this.TopPanel.Controls.Remove(this.HideButton);
            this.HideButton.Dispose();
            this.HideButton = new System.Windows.Forms.Button();
            this.HideButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HideButton.FlatAppearance.BorderSize = 0;
            this.HideButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(201)))), ((int)(((byte)(18)))), ((int)(((byte)(48)))));
            this.HideButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.HideButton.Image = global::MultiPlug.Windows.Desktop.Properties.Resources.cross;
            this.HideButton.Location = new System.Drawing.Point(540, 0);
            this.HideButton.Margin = new System.Windows.Forms.Padding(0);
            this.HideButton.Name = "HideButton";
            this.HideButton.Size = new System.Drawing.Size(45, 31);
            this.HideButton.TabIndex = 2;
            this.HideButton.UseVisualStyleBackColor = true;
            this.HideButton.Click += new System.EventHandler(this.HideButton_Click);
            this.TopPanel.Controls.Add(this.HideButton);

            m_DiscoveryService.Stop();
        }

        private void ShowButton_MouseClick(object sender, MouseEventArgs theEventArgs)
        {
            // Hack to fix double firing of ShowButton_Click by the NotifyIcon (m_TrayIcon) Click Event
            // https://stackoverflow.com/questions/45336951/clicking-a-contextmenuitem-in-a-notifyicon-context-menu-calls-the-notifyicon-cli
            if (((MouseEventArgs)theEventArgs).Button != MouseButtons.Left)
            {
                return;
            }

            ShowButton_Click(sender, theEventArgs);
        }

        private void ShowButton_Click(object sender, EventArgs theEventArgs)
        {
            Show();
            BeginInvoke((MethodInvoker)delegate
            {
                m_DiscoveredMenuItem.MenuItems.Clear();
            });

            BeginInvoke((MethodInvoker)delegate
            {
                m_DataGridModel.Devices.Clear();
            });

            ShowInTaskbar = true;
            m_DiscoveryService.Start();
            m_RefreshTimer.Start();
        }

        private void TopPanel_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void DiscoveryForm_Move(object sender, System.EventArgs e)
        {
            MaximizedBounds = new Rectangle( Location.X, 0, Location.X + Width, Screen.PrimaryScreen.WorkingArea.Height);
        }

        private void MaintenanceButton_Click(object sender, EventArgs e)
        {
            var MaintenanceForm = new MaintenanceForm( m_ClickedIP );
            MaintenanceForm.Show();
        }
    }
}
