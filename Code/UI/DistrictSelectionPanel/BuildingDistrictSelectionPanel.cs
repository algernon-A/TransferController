using System.Linq;
using System.Collections.Generic;


namespace TransferController
{
    /// <summary>
    /// District selection panel main class.
    /// </summary>
    internal class BuildingDistrictSelectionPanel : DistrictSelectionPanel
    {
        /// <summary>
        /// HashSet of relevant district settings for this panel type.
        /// </summary>
        private HashSet<int> DistrictSettingsList => ServiceLimits.GetBuildingDistricts((parent as TransferPanel).CurrentBuilding, (parent as TransferPanel).RecordNumber);


        /// <summary>
        /// Populates the list.
        /// </summary>
        protected override void PopulateList()
        {

            // Get district hashset for this building.
            HashSet<int> hashSet = DistrictSettingsList;

            // If no district hashset was recovered, clear list and selection and exit.
            if (hashSet == null)
            {
                Logging.Message("no district settings retrieved");

                districtList.rowsData = new FastList<object>
                {
                    m_buffer = new object[0],
                    m_size = 0
                };
                districtList.selectedIndex = -1;
                return;
            }

            // Recreate UI district list from hashset.
            districtList.rowsData = new FastList<object>
            {
                m_buffer = hashSet.Select(x => (object)x).ToArray(),
                m_size = hashSet.Count
            };
        }
    }
}