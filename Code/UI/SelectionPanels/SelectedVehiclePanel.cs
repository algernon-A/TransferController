using System.Linq;
using System.Collections.Generic;


namespace TransferController
{
    /// <summary>
    /// Selected vehicle panel.
    /// </summary>
    internal class SelectedVehiclePanel : VehicleSelectionPanel
    {
        /// <summary>
        /// Populates the list.
        /// </summary>
        protected override void PopulateList()
        {
            List<VehicleItem> items = new List<VehicleItem>();
            List<VehicleInfo> buildingVehicles = VehicleControl.GetVehicles(ParentPanel.CurrentBuilding, ParentPanel.TransferReason);
            if (buildingVehicles != null)
            {
                foreach (VehicleInfo vehicle in buildingVehicles)
                {
                    items.Add(new VehicleItem(vehicle));
                }
            }

            vehicleList.rowsData = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.name).ToArray(),
                m_size = items.Count
            };
        }
    }
}