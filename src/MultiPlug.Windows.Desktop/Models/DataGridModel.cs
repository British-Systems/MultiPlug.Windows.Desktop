
namespace MultiPlug.Windows.Desktop.Models
{
    public class DataGridModel
    {
        private System.ComponentModel.BindingList<DataGridRow> m_DevicesCollection = new System.ComponentModel.BindingList<DataGridRow>();
        
        public DataGridModel()
        {
        }

        public System.ComponentModel.BindingList<DataGridRow> Devices
        {
            get { return m_DevicesCollection; }
            set { m_DevicesCollection = value; }
        }
    }
}
