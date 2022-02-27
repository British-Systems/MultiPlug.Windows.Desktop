using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MultiPlug.Windows.Desktop.Models;
using Newtonsoft.Json;
using System.Reflection;

namespace MultiPlug.Windows.Desktop.Update
{
    internal class LatestVersionLookup
    {
        private int m_Retrys = 5;
        private bool m_UserPrompt = false;

        internal LatestVersionModel LatestRelease { get; set; } = new LatestVersionModel();

        internal LatestVersionLookup()
        {
            // using System.Net;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons

            Task.Run(() => Lookup());
        }

        private void Lookup()
        {
            m_Retrys--;
            try
            {
                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("https://windows.multiplug.app/version.json");

                using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
                {
                    JsonSerializer JsonSerializer = new JsonSerializer();
                    using (StreamReader StreamReader = new StreamReader(Response.GetResponseStream()))
                    using (var jsonTextReader = new JsonTextReader(StreamReader))
                    {
                        LatestRelease = (LatestVersionModel)JsonSerializer.Deserialize(jsonTextReader, typeof(LatestVersionModel));
                    }
                }
            }
            catch (WebException)
            {
                if(m_Retrys != 0)  // Retry 5 times
                {
                    Task.Delay(60000).Wait(); // Wait 1 minute before trying again.
                    Task.Run(() => Lookup());
                }
 
            }
            catch (Exception)
            {
            }

            if( ! string.IsNullOrEmpty(LatestRelease.version))
            {
                Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

                string AssemblyVersion = ExecutingAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version.Substring(0,5);

                if(AssemblyVersion != LatestRelease.version)
                {
                    m_UserPrompt = true;
                }
            }
        }

        internal bool ShouldDisplayUpdatePrompt()
        {
            bool Result = m_UserPrompt;
            m_UserPrompt = false;
            return Result;
        }
    }
}
