using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        private SshCommand[] SendCommand(params string[] theCommands)
        {
            SshCommand[] Response = new SshCommand[theCommands.Length];

            Task<SshCommand>[] Tasks = new Task<SshCommand>[theCommands.Length];

            using (var client = new SshClient(IPAddressTextBox.Text, UserNameTextBox.Text, PasswordTextBox.Text))
            {
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

                    Task.WaitAll(Tasks);

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
                }
                else
                {
                    return new SshCommand[0];
                }
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
            var Response = SendCommand("systemctl is-enabled multiplug", "systemctl is-active multiplug");

            if (Response.Any())
            {
                if (Response.Any(r => r.Error == string.Empty))
                {
                    AppendTextOutputWindow("Connected to " + IPAddressTextBox.Text);

                    RebootButton.Text = "Reboot";
                    RebootButton.Enabled = true;
                    ShutdownButton.Text = "Shutdown";
                    ShutdownButton.Enabled = true;
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

        private void SetStartStopButton(bool theStartStop)
        {
            StartStop = theStartStop;
            StartStopButton.Enabled = true;

            if (theStartStop)
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
                SendCommand("sudo systemctl disable multiplug");

            }
            else
            {
                SendCommand("sudo systemctl enable multiplug");
            }

            SetStartOnBootButton( ! StartOnBoot );
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (StartStop)
            {
                SendCommand("sudo systemctl stop multiplug");

            }
            else
            {
                SendCommand("sudo systemctl start multiplug");
            }

            SetStartStopButton( ! StartStop);
        }

        private void RebootButton_Click(object sender, EventArgs e)
        {
            SendCommand("sudo reboot");
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }

        private void ShutdownButton_Click(object sender, EventArgs e)
        {
            SendCommand("sudo halt");
        }
    }
}
