using MultiPlug.Windows.Desktop.Models;
using System;
using System.Threading;
using System.Windows.Forms;

namespace MultiPlug.Windows.Desktop
{
    static class Program
    {
        private static string appGuid = "desktopa-4888-4bec-bfdf-multiplug358";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DiscoveryForm());
            }
        }
    }
}
