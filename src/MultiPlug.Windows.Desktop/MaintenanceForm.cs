using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiPlug.Windows.Desktop
{
    public partial class MaintenanceForm : Form
    {


        public MaintenanceForm( string theIPAddress)
        {
            InitializeComponent();

            IPAddressTextBox.Text = theIPAddress;
        }

        private SshCommand[] SendCommand(string[] theCommands, bool WaitForResult = true  )
        {
            SshCommand[] Response = new SshCommand[theCommands.Length];

            Task<SshCommand>[] Tasks = new Task<SshCommand>[theCommands.Length];

            var client = new SshClient(IPAddressTextBox.Text, UserNameTextBox.Text, PasswordTextBox.Text);

            try
            {
                client.Connect();
            }
            catch (System.Net.Sockets.SocketException theException)
            {
                AppendTextOutputWindow(theException.Message);
                    
            }
            catch(Renci.SshNet.Common.SshConnectionException theException)
            {
                AppendTextOutputWindow(theException.Message);
            }
            catch(Renci.SshNet.Common.SshAuthenticationException theException)
            {
                AppendTextOutputWindow(theException.Message);
            }

            if(client.IsConnected)
            {
                for ( int i = 0; i < theCommands.Length; i++)
                {
                    int index = i;

                    Tasks[index] = Task.Run(() =>
                    {

                        try
                        {
                            return client.RunCommand(theCommands[index]);
                        }
                        catch( Exception ex)
                        {
                            AppendTextOutputWindow(ex.Message);
                            return null;
                        }
                    });
                }
                if(WaitForResult)
                {
                    if( ! Task.WaitAll(Tasks, 5000) )
                    {
                        return new SshCommand[0];
                    }

                    try
                    {
                        client.Disconnect();
                    }
                    catch (ObjectDisposedException theException)
                    {
                        AppendTextOutputWindow(theException.Message);
                    }

                    for (int i = 0; i < theCommands.Length; i++)
                    {
                        Response[i] = Tasks[i].Result;
                    }

                    client.Dispose();
                }
                else
                {
                    Task.Delay(3000).ContinueWith(T =>
                    {
                        client.Dispose();
                    });

                    return new SshCommand[0];
                }

            }
            else
            {
                return new SshCommand[0];
            }

            return Response;
        }

        private static object m_Lock = new object();

        private void AppendTextOutputWindow( string theOutput )
        {
            lock (m_Lock)
            {
                Invoke((MethodInvoker)delegate
                {
                    OutputWindow.AppendText(theOutput + Environment.NewLine);
                });
            }        
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            var Response = SendCommand(new string[] { "systemctl is-enabled multiplug", "systemctl is-active multiplug" });

            if (Response.Any())
            {
                if (Response.Any(r => r.Error == string.Empty))
                {
                    AppendTextOutputWindow("Connected to " + IPAddressTextBox.Text);

                    RebootButton.Text = "Reboot";
                    RebootButton.Enabled = true;
                    ShutdownButton.Text = "Shutdown";
                    ShutdownButton.Enabled = true;
                    UploadButton.Text = "Upload";
                    UploadButton.Enabled = true;
                }

                if (Response[0].Result.TrimEnd().Equals("enabled", StringComparison.OrdinalIgnoreCase))
                {
                    SetStartOnBootButton(true);
                }
                else
                {
                    SetStartOnBootButton(false);
                }

                if (Response[1].Result.TrimEnd().Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    SetStartStopButton(true);
                }
                else
                {
                    SetStartStopButton(false);
                }
            }
        }

        private bool StartOnBoot = false;

        private void SetStartOnBootButton(bool theStartOnBoot)
        {
            StartOnBoot = theStartOnBoot;
            StartOnBootButton.Enabled = true;

            if ( theStartOnBoot)
            {
                AppendTextOutputWindow("MultiPlug is set to start on boot");
                StartOnBootButton.Text = "Disable";
            }
            else
            {
                AppendTextOutputWindow("MultiPlug is NOT set to start on boot");
                StartOnBootButton.Text = "Enable";
            }
        }

        private bool StartStop = false;

        private void SetStartStopButton(bool isStarted)
        {
            StartStop = isStarted;
            StartStopButton.Enabled = true;

            if (isStarted)
            {
                AppendTextOutputWindow("MultiPlug is running");
                StartStopButton.Text = "Stop";
            }
            else
            {
                AppendTextOutputWindow("MultiPlug is stopped");
                StartStopButton.Text = "Start";
            }
        }

        private void StartOnBootButton_Click(object sender, EventArgs e)
        {
            if (StartOnBoot)
            {
                SendCommand(new string[] { "sudo systemctl disable multiplug" });

            }
            else
            {
                SendCommand(new string[] { "sudo systemctl enable multiplug" });
            }

            SetStartOnBootButton( ! StartOnBoot );
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (StartStop)
            {
                SendCommand(new string[] { "sudo systemctl stop multiplug" });

            }
            else
            {
                SendCommand(new string[] { "sudo systemctl start multiplug" });
            }

            SetStartStopButton( ! StartStop);
        }

        private void RebootButton_Click(object sender, EventArgs e)
        {
            AppendTextOutputWindow("Reboot command sent");
            SendCommand(new string[] { "sudo reboot" }, false);
        }


        private void ShutdownButton_Click(object sender, EventArgs e)
        {
            AppendTextOutputWindow("Shutdown command sent");
            SendCommand(new string[] { "sudo halt" }, false);
        }

        private void UploadButton_Click(object sender, EventArgs e)
        {
            if(FileLocationTextBox.Text == string.Empty)
            {
                return;
            }

            AppendTextOutputWindow("Extracting files...");
            var ExtractDirectory = Desktop.Update.Package.Extract(FileLocationTextBox.Text);

            if( ExtractDirectory == string.Empty)
            {
                AppendTextOutputWindow("Extract Failed.");
                return;
            }

            string PackageName = Desktop.Update.Package.GetPackageName(ExtractDirectory);

            if( PackageName == string.Empty)
            {
                return;
            }

            bool isCore = false;


            if( PackageName.StartsWith("multiplug.core", StringComparison.OrdinalIgnoreCase))
            {
                SendCommand(new string[] { "sudo systemctl stop multiplug" });
                SetStartStopButton(false);
                isCore = true;
            }




            string HomeDirectory = Desktop.Update.Package.GetHomeDirectory(ExtractDirectory);

            var connectionInfo = new ConnectionInfo(IPAddressTextBox.Text, 22,
                                        UserNameTextBox.Text,
                                        new PasswordAuthenticationMethod(UserNameTextBox.Text, PasswordTextBox.Text));
            using (var client = new SftpClient(connectionInfo))
            {
                client.Connect();

                var UpdateHome = (isCore)? "/usr/local/bin/multiplug/" : "/usr/local/bin/multiplug/updates/";
                var PackageHome = (isCore) ? UpdateHome : UpdateHome + PackageName + "/";

                // Work around for Updates folder Permission Denied
                if ( ! Desktop.Update.Package.DirectoryExistsOrCreate(client, UpdateHome) )
                {
                    if( Desktop.Update.Package.DirectoryDeleteCreate(client, UpdateHome) )
                    {
                        if ( ! Desktop.Update.Package.DirectoryExistsOrCreate(client, UpdateHome) )
                        {
                            AppendTextOutputWindow("Permission Denied to " + UpdateHome);
                            return;
                        }
                    }
                }
                // Work around for Updates folder Permission Denied

                if( !Desktop.Update.Package.DirectoryExistsOrCreate(client, PackageHome))
                {
                    AppendTextOutputWindow("Permission Denied to create " + PackageHome);
                    return;
                }

                if( Desktop.Update.Package.UploadDirectory(client, HomeDirectory, PackageHome) )
                {
                    AppendTextOutputWindow("Package uploaded. " + PackageName);
                    if (isCore)
                    {
                        SendCommand(new string[] { "sudo systemctl start multiplug" });
                        SetStartStopButton(true);
                    }

                }
                else
                {
                    AppendTextOutputWindow("Package uploaded FAILED." + PackageName);
                }

                try
                {
                    Directory.Delete(ExtractDirectory, true);
                }
                catch
                { }
            }
        }




        private void BrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Title = "Browse Text Files",

                CheckFileExists = true,
                CheckPathExists = true,

                DefaultExt = "nupkg",
                Filter = "Nupkg files (*.nupkg)|*.nupkg",
                FilterIndex = 2,
                RestoreDirectory = true,

            //    ReadOnlyChecked = true,
           //     ShowReadOnly = true
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileLocationTextBox.Text = openFileDialog1.FileName;
            }
        }
    }
}
