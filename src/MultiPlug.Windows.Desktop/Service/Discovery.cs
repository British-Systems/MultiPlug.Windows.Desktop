using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MultiPlug.Discovery;
using MultiPlug.Windows.Desktop.Models;

namespace MultiPlug.Windows.Desktop.Service
{
    public class Discovery
    {
        private Probe m_Probe;
        public event EventHandler<DataGridRow> Resolved;

        public Discovery()
        {
        }
        public void Start()
        {
            m_Probe = new Probe(BeaconModel.Types.Service, "MultiPlug", "1");
            m_Probe.BeaconsUpdated += OnNewDiscovery;
            m_Probe.Start();
        }

        public void Stop()
        {
            m_Probe.Stop();
            lock (m_Lock)
            {
                Found.Clear();
            }
        }

        List<string> Found = new List<string>();

        private static object m_Lock = new object();

        private void OnNewDiscovery(IEnumerable<BeaconLocation> theBeacons)
        {
            lock (m_Lock)
            {

                foreach (var beacon in theBeacons)
                {
                    string IpAdress = beacon.Address.Address.ToString();

                    var Search = Found.FirstOrDefault(d => d == beacon.Location);

                    if (Search == null)
                    {
                        Found.Add(beacon.Location);

                        var LookupWorker = new DiscoveryDescriptionLookup(beacon.Address, beacon.Location);

                        LookupWorker.Resolved += Lookup_Resolved;
                        LookupWorker.Errored += Lookup_Errored;

                        var Thread = new Thread(LookupWorker.Lookup) { IsBackground = true };

                        Thread.Start();
                    }

                }
            }
        }

        private void Lookup_Errored(object sender, string theUrl)
        {
            lock (m_Lock)
            {
                Found.Remove(theUrl);
            }
        }

        private void Lookup_Resolved(object sender, DataGridRow theNewDeviceRow)
        {
            Resolved?.Invoke(this, theNewDeviceRow);
        }
    }
}
