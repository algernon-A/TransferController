using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TransferController
{
    public static class CopyPaste
    {

        internal static ushort BuildingTemplate = 0;

        internal static TransferStruct[] Transfers = new TransferStruct[4];

        /// <summary>
        /// Check if can copy between buildings
        /// </summary>
        /// <param name="source_building">building to copy from (source building)</param>
        /// <param name="destination_building">building to copy to (destination building)</param>
        internal static bool CanCopy(ushort source_building, ushort destination_building)
        {
            List<byte> source_building_list = new List<byte>();
            List<byte> destination_building_list = new List<byte>();

            var source_building_transfers = new TransferStruct[4];
            var destination_building_transfers = new TransferStruct[4];

            var source_building_number = TransferDataUtils.BuildingEligibility(source_building, Singleton<BuildingManager>.instance.m_buildings.m_buffer[source_building].Info, source_building_transfers);
            var destination_building_number = TransferDataUtils.BuildingEligibility(destination_building, Singleton<BuildingManager>.instance.m_buildings.m_buffer[destination_building].Info, destination_building_transfers);
            if(source_building_number != destination_building_number)
            {
                return false;
            }
            foreach (var record in source_building_transfers) 
            {
                source_building_list.Add(record.recordNumber);
            }
            foreach (var record in destination_building_transfers)
            {
                destination_building_list.Add(record.recordNumber);
            }
            bool equals = source_building_list.OrderBy(a => a).SequenceEqual(destination_building_list.OrderBy(a => a));
            if (!equals)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Copy policy between buildings.
        /// </summary>
        /// <param name="building">Building to copy to (destination building)</param>
        /// <param name="transfers">Building transfers (destination building)</param>
        internal static bool CopyPolicyTo(ushort building, TransferStruct[] transfers)
        {
            try
            {
                BuildingControl.DeleteEntry(building);

                for (int i = 0; i < transfers.Length; ++i)
                {
                    BuildingControl.SetDistrictEnabled(building, transfers[i].recordNumber, BuildingControl.GetDistrictEnabled(BuildingTemplate, Transfers[i].recordNumber), transfers[i].reason, transfers[i].nextRecord);

                    BuildingControl.SetSameDistrict(building, transfers[i].recordNumber, BuildingControl.GetSameDistrict(BuildingTemplate, Transfers[i].recordNumber), transfers[i].reason, transfers[i].nextRecord);

                    BuildingControl.SetOutsideConnection(building, transfers[i].recordNumber, BuildingControl.GetOutsideConnection(BuildingTemplate, Transfers[i].recordNumber), transfers[i].reason, transfers[i].nextRecord);

                    var DistrictsServed = BuildingControl.GetDistricts(BuildingTemplate, transfers[i].recordNumber);
                    if (DistrictsServed != null)
                    {
                        foreach (var districtPark in DistrictsServed)
                        {
                            BuildingControl.AddDistrict(building, transfers[i].recordNumber, districtPark, transfers[i].reason, transfers[i].nextRecord);
                        }
                    }
                }
 
                return true;
            }
            catch (Exception ex)
            {
                Logging.Error(ex);
                return false;
            }
            
        }
    }
}
