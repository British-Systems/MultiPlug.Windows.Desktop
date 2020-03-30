using System;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using MultiPlug.Windows.Desktop.Models;
using Newtonsoft.Json;

namespace MultiPlug.Windows.Desktop.Service
{
    public class DiscoveryDescriptionLookup
    {
        readonly string m_Url;
        readonly string m_IpAddress;


        public event EventHandler<DataGridRow> Resolved;
        public event EventHandler<string> Errored;

        public DiscoveryDescriptionLookup(string theIpAddress, string theUrl)
        {
            m_Url = theUrl;
            m_IpAddress = theIpAddress;
        }

        public void Lookup()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(m_Url.Replace("ssdp", "environment"));
                request.Accept = "application/json";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    var serializer = new JsonSerializer();
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    using (var jsonTextReader = new JsonTextReader(sr))
                    {
                        EnvironmentModel Model = (EnvironmentModel)serializer.Deserialize(jsonTextReader, typeof(EnvironmentModel));

                        Resolved?.Invoke(this, new DataGridRow() { Name = Model.machineName, Url = "http://"+ m_IpAddress + "/", IpAddress = m_IpAddress, Location = Model.location });
                    }
                }
            }
            catch( WebException )
            {
                LookupLegacy();
            }
            catch (Exception)
            {
                Errored?.Invoke(this, m_Url);
            }
        }

        public void LookupLegacy()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(m_Url);
                request.Accept = "application/xml";

                using (WebResponse response = request.GetResponse())
                {

                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);

                    XmlSerializer serializer = new XmlSerializer(typeof(DiscoveryDeviceDescriptionModel));

                    DiscoveryDeviceDescriptionModel deserialized = null;
                    try
                    {
                        deserialized = (DiscoveryDeviceDescriptionModel)serializer.Deserialize(reader);
                    }
                    catch (InvalidOperationException)
                    {

                    }

                    if (deserialized == null)
                    {
                        return;
                    }

                    Resolved?.Invoke(this, new DataGridRow() { Name = deserialized.Device.FriendlyName, Url = deserialized.Device.PresentationURL, IpAddress = m_IpAddress, Location = "Unknown" });

                }
            }
            catch (Exception)
            {
                Errored?.Invoke(this, m_Url);
            }
        }
    }
}
