using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;


namespace TransferController
{
    /// <summary>
    /// Warehouse data flags.
    /// </summary>
    public enum WarehouseFlags : ushort
    {
        None = 0,
        ReserveUnique = 0x01,
        ReserveOutside = 0x02,
        ReserveCity = 0x04,
        AllReserveFlags = ReserveUnique | ReserveOutside | ReserveCity
    }


    /// <summary>
    /// Warehouse data record.
    /// </summary>
    public struct WarehouseRecord
    {
        public WarehouseFlags flags;
        public byte reserveVehicles;
        public byte priority;
    }


    [HarmonyPatch]
    internal static class WarehouseControl
    {
        /// Dictionary of warehouse settings.
        private readonly static Dictionary<uint, WarehouseRecord> warehouseRecords = new Dictionary<uint, WarehouseRecord>();


        /// <summary>
        /// Harmony reverse patch for CommonBuildingAI.CalculateOwnVehicles to access protected method of original instance.
        /// </summary>
        /// <param name="instance">Object instance</param>
        /// <param name="citizenID">ID of this citizen (for game method)</param>
        /// <param name="data">Citizen data (for game method)</param>
        [HarmonyReversePatch]
        [HarmonyPatch((typeof(CommonBuildingAI)), "CalculateOwnVehicles")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CalculateOwnVehicles(CommonBuildingAI instance, ushort buildingID, ref Building data, TransferManager.TransferReason material, ref int count, ref int cargo, ref int capacity, ref int outside)
        {
            string message = "CalculateOwnVehicles reverse Harmony patch wasn't applied";
            Logging.Error(message, instance, buildingID, data, material, count, cargo, capacity, outside);
            throw new NotImplementedException(message);
        }


        /// <summary>
        /// Checks to see if the specified warehouse has available vehicles for dispatch to serve the proposed transfer after allowing for quotas.
        /// </summary>
        /// <param name="warehouseAI">Warehouse AI reference</param>
        /// <param name="warehouseID">Warehouse building ID</param>
        /// <param name="warehouseData">Warehouse building data record</param>
        /// <param name="material">Transfer material</param>
        /// <param name="otherAI">AI reference for the other building in the transfer</param>
        /// <returns>True if transfer permitted, false otherwise</returns>
        internal static bool CheckVehicleQuota(WarehouseAI warehouseAI, ushort warehouseID, ref Building warehouseData, TransferManager.TransferReason material, BuildingAI otherAI)
        {
            // Check to see if there's an entry for this warehouse.
            if (warehouseRecords.TryGetValue(warehouseID, out WarehouseRecord warehouseRecord))
            {
                // Entry found - determine if a quota needs to be reserved.
                if (((warehouseRecord.flags & WarehouseFlags.ReserveUnique) != 0 && !(otherAI is UniqueFactoryAI)) ||
                    ((warehouseRecord.flags & WarehouseFlags.ReserveOutside) != 0 && !(otherAI is OutsideConnectionAI)) ||
                    ((warehouseRecord.flags & WarehouseFlags.ReserveCity) != 0) && (otherAI is OutsideConnectionAI))
                {
                    // Retrieve reserved vehicle count - don't bother checking if no vehicles are reserved.
                    byte reservedVehicles = GetReservedVehicles(warehouseID);
                    if (reservedVehicles != 0)
                    {
                        // Calculate non-reserved vehicle count quota and compare to in-use vehicle count.
                        int quota = (int)(warehouseAI.m_truckCount - reservedVehicles);
                        int vehicleCount = 0, cargoLoad = 0, cargoCapacity = 0, inUse = 0;
                        CalculateOwnVehicles(warehouseAI, warehouseID, ref warehouseData, material, ref vehicleCount, ref cargoLoad, ref cargoCapacity, ref inUse);
                        
                        // Permit transfer if we've got available (non-in-use) vehicles less than the maximum quota permitted.
                        return inUse < quota;
                    }
                }
            }

            // Default is to return true (transfer permitted).
            return true;
        }


        /// <summary>
        /// Sets the warehouse to reserve vehicles for unique factories.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID</param>
        internal static void SetReserveUnique(uint buildingID)
        {
            SetFlags(buildingID, false, WarehouseFlags.AllReserveFlags);
            SetFlags(buildingID, true, WarehouseFlags.ReserveUnique);
        }


        /// <summary>
        /// Sets the warehouse to reserve vehicles for outside connections.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID</param>
        internal static void SetReserveOutside(uint buildingID)
        {
            SetFlags(buildingID, false, WarehouseFlags.AllReserveFlags);
            SetFlags(buildingID, true, WarehouseFlags.ReserveOutside);
        }


        /// <summary>
        /// Sets the warehouse to reserve vehicles for the city.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID</param>
        internal static void SetReserveCity(uint buildingID)
        {
            SetFlags(buildingID, false, WarehouseFlags.AllReserveFlags);
            SetFlags(buildingID, true, WarehouseFlags.ReserveCity);
        }


        /// <summary>
        /// Clears the reserved vehicle state for the specified warehouse.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID</param>
        internal static void ClearReserve(uint buildingID) => SetFlags(buildingID, false, WarehouseFlags.AllReserveFlags);


        /// <summary>
        /// Checks if the given warehouse is set to reserve vehicles for unique factories.
        /// </summary>
        /// <param name="buildingID">Building ID of warehouse to check</param>
        /// <returns>True if the warehouse is set to reserve vehicles for unique factories, false otherwise</returns>
        internal static bool GetReserveUnique(uint buildingID) => GetFlags(buildingID, WarehouseFlags.ReserveUnique);


        /// <summary>
        /// Checks if the given warehouse is set to reserve vehicles for outside connections.
        /// </summary>
        /// <param name="buildingID">Building ID of warehouse to check</param>
        /// <returns>True if the warehouse is set to reserve vehicles for outside connections, false otherwise</returns>
        internal static bool GetReserveOutside(uint buildingID) => GetFlags(buildingID, WarehouseFlags.ReserveOutside);


        /// <summary>
        /// Checks if the given warehouse is set to reserve vehicles for the city.
        /// </summary>
        /// <param name="buildingID">Building ID of warehouse to check</param>
        /// <returns>True if the warehouse is set to reserve vehicles for local deliveries, false otherwise</returns>
        internal static bool GetReserveCity(uint buildingID) => GetFlags(buildingID, WarehouseFlags.ReserveCity);


        /// <summary>
        /// Returns the current reserved vehicle count for the specified warehouse.
        /// </summary>
        /// <param name="buildingID">Warehouse building ID</param>
        /// <returns>Number of reserved vehicles</returns>
        internal static byte GetReservedVehicles(uint buildingID)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return 0;
            }

            // See if we've got an entry for this building.
            if (buildingID != 0 && warehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord))
            {
                // Entry found - return same-district flag status.
                return warehouseRecord.reserveVehicles;
            }
            else
            {
                // No dictionary entry, therefore no reserved vehicles.
                return 0;
            }
        }


        /// <summary>
        /// Sets the reserved vehicle count for the specified warehouse.
        /// </summary>
        /// <param name="buildingID">Warehouse building ID</param>
        /// <param name="vehicles">Number of vehicles to reserve</param>
        internal static void SetReservedVehicles(uint buildingID, byte vehicles)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return;
            }

            // Try to get existing entry.
            bool hasEntry = warehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord);

            // Are we reserving any vehicles?
            if (vehicles != 0)
            {
                // Reserving vehicles - do we have an existing entry?
                if (hasEntry)
                {
                    // Add flag to existing entry.
                    warehouseRecord.reserveVehicles = vehicles;
                    warehouseRecords[buildingID] = warehouseRecord;
                }
                else
                {
                    // No record for building in dictionary - add one.
                    warehouseRecords.Add(buildingID, new WarehouseRecord
                    {
                        reserveVehicles = vehicles
                    });
                }
            }
            else if (hasEntry)
            {
                // Setting reserved vehicles to zero and there's an existing entry for this warehouse (just do nothing if no existing entry).
                // If no other data either, remove entire dictionary entry.
                if (warehouseRecord.reserveVehicles == 0 && warehouseRecord.priority == 0)
                {
                    warehouseRecords.Remove(buildingID);
                }
                else
                {
                    // Update existing entry.
                    warehouseRecord.reserveVehicles = vehicles;
                    warehouseRecords[buildingID] = warehouseRecord;
                }
            }
        }


        /// <summary>
        /// Serializes warehouse data.
        /// </summary>
        /// <param name="stream">Binary writer instance to serialize to</param>
        internal static void Serialize(BinaryWriter writer)
        {
            Logging.Message("serializing warehouse data");

            // Write length of dictionary.
            writer.Write(warehouseRecords.Count);

            // Serialise each building entry.
            foreach (KeyValuePair<uint, WarehouseRecord> entry in warehouseRecords)
            {
                // Local reference.
                WarehouseRecord warehouseRecord = entry.Value;

                // Serialize key and simple fields.
                writer.Write(entry.Key);
                writer.Write((ushort)warehouseRecord.flags);
                writer.Write(warehouseRecord.reserveVehicles);
                writer.Write(warehouseRecord.priority);

                Logging.Message("wrote entry ", entry.Key);
            }
        }


        /// <summary>
        /// Deserializes savegame data.
        /// </summary>
        /// <param name="stream">Data memory stream to deserialize from</param>
        internal static void Deserialize(BinaryReader reader)
        {
            Logging.Message("deserializing warehouse data");

            // Clear dictionary.
            warehouseRecords.Clear();

            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                uint key = reader.ReadUInt32();

                // Deserialize basic building record fields.
                WarehouseRecord warehouseRecord = new WarehouseRecord
                {
                    flags = (WarehouseFlags)reader.ReadInt16(),
                    reserveVehicles = reader.ReadByte(),
                    priority = reader.ReadByte()
                };

                // Add completed entry to dictionary.
                warehouseRecords.Add(key, warehouseRecord);
                Logging.Message("read entry ", key);
            }
        }


        /// <summary>
        /// Sets or clears the specified flags for the given warehouse building.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="status">True to set flags, false to clear</param>
        /// <param name="flags">Flags to set/clear</param>
        private static void SetFlags(uint buildingID, bool status, WarehouseFlags flags)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return;
            }

            // Try to get existing entry.
            bool hasEntry = warehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord);

            // Setting or clearing?
            if (status)
            {
                // Setting a flag - do we have an existing entry?
                if (hasEntry)
                {
                    // Add flag to existing entry.
                    warehouseRecord.flags |= flags;
                    warehouseRecords[buildingID] = warehouseRecord;
                }
                else
                {
                    // No record for building in dictionary - add one.
                    warehouseRecords.Add(buildingID, new WarehouseRecord
                    {
                        flags = flags
                    });
                }
            }
            else if (hasEntry)
            {
                // Clearing a flag - only bother if we've got an existing entry.
                // Get updated flags.
                WarehouseFlags updatedFlags = warehouseRecord.flags & ~flags;

                // If no flags remaining, remove entire dictionary entry if there's no other data either.
                if (updatedFlags == 0 && warehouseRecord.reserveVehicles == 0 && warehouseRecord.priority == 0)
                {
                    warehouseRecords.Remove(buildingID);
                }
                else
                {
                    // Update existing entry.
                    warehouseRecord.flags = updatedFlags;
                    warehouseRecords[buildingID] = warehouseRecord;
                }
            }
        }


        /// <summary>
        /// Returns the current status of the given flags for the given warehouse building.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="flag">Flags to check</param>
        /// <returns>True if ANY of the specified flags is set, false otherwise</returns>
        private static bool GetFlags(uint buildingID, WarehouseFlags flags)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return false;
            }

            // See if we've got an entry for this building.
            if (warehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord))
            {
                // Entry found - return same-district flag status.
                return (warehouseRecord.flags & flags) != 0;
            }
            else
            {
                // No dictionary entry, therefore no same-district setting.
                return false;
            }
        }
    }
}