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
        }

        List<string> Found = new List<string>();

        private void OnNewDiscovery(IEnumerable<BeaconLocation> theBeacons)
        {
            foreach (var beacon in theBeacons)
            {
                string IpAdress = beacon.Address.Address.ToString();

                var Search = Found.FirstOrDefault(d => d == beacon.Location);

                if( Search == null )
                {
                    Found.Add(beacon.Location);

                    var LookupWorker = new DiscoveryDescriptionLookup(IpAdress, beacon.Location);

                    LookupWorker.Resolved += Lookup_Resolved;
                    LookupWorker.Errored += Lookup_Errored;

                    var Thread = new Thread(LookupWorker.Lookup) { IsBackground = true };

                    Thread.Start();
                }

            }
        }

        private void Lookup_Errored(object sender, string theUrl)
        {
            Found.Remove(theUrl);
        }

        private void Lookup_Resolved(object sender, DataGridRow theNewDeviceRow)
        {
            Resolved?.Invoke(this, theNewDeviceRow);
        }
    }
}
