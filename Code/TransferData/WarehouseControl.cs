// <copyright file="WarehouseControl.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Static class to control additional warehouse functions.
    /// </summary>
    [HarmonyPatch]
    internal static class WarehouseControl
    {
        // Dictionary of warehouse settings.
        private static readonly Dictionary<uint, WarehouseRecord> WarehouseRecords = new Dictionary<uint, WarehouseRecord>();

        /// <summary>
        /// Warehouse data flags.
        /// </summary>
        public enum WarehouseFlags : ushort
        {
            /// <summary>
            /// No flags.
            /// </summary>
            None = 0,

            /// <summary>
            /// Reserve vehicles for unique factories.
            /// </summary>
            ReserveUnique = 0x01,

            /// <summary>
            /// Reserve vehicles for outside connections.
            /// </summary>
            ReserveOutside = 0x02,

            /// <summary>
            /// Reserve vehicles for intra-city delivery only (no outside connections).
            /// </summary>
            ReserveCity = 0x04,

            /// <summary>
            /// All reserve flags set.
            /// </summary>
            AllReserveFlags = ReserveUnique | ReserveOutside | ReserveCity,
        }

        /// <summary>
        /// Harmony reverse patch for CommonBuildingAI.CalculateOwnVehicles to access protected method of original instance.
        /// </summary>
        /// <param name="instance">Object instance.</param>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="data">Building data reference.</param>
        /// <param name="material">Transfer material.</param>
        /// <param name="count">Vehicle count.</param>
        /// <param name="cargo">Cargo load.</param>
        /// <param name="capacity">Cargo capacity.</param>
        /// <param name="outside">Number of vehicles in use.</param>
        /// <exception cref="NotImplementedException">Harmony patch not applied.</exception>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(CommonBuildingAI), "CalculateOwnVehicles")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CalculateOwnVehicles(
            CommonBuildingAI instance,
            ushort buildingID,
            ref Building data,
            TransferManager.TransferReason material,
            ref int count,
            ref int cargo,
            ref int capacity,
            ref int outside)
        {
            string message = "CalculateOwnVehicles reverse Harmony patch wasn't applied";
            Logging.Error(message, instance, buildingID, data, material, count, cargo, capacity, outside);
            throw new NotImplementedException(message);
        }

        /// <summary>
        /// Checks to see if the specified building has a warehouse record.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <returns>True if a custom record exists, false otherwise.</returns>
        internal static bool HasRecord(ushort buildingID) => WarehouseRecords.ContainsKey(buildingID);

        /// <summary>
        /// Attempts to retrieve a warehouse record from the dictionary.
        /// </summary>
        /// <param name="buildingID">Warehouse building ID to retrieve.</param>
        /// <param name="warehouseRecord">Warehouse data record.</param>
        /// <returns>True if a record was sucessfully retrieved, false otherwise.</returns>
        internal static bool TryGetRecord(ushort buildingID, out WarehouseRecord warehouseRecord) => WarehouseRecords.TryGetValue(buildingID, out warehouseRecord);

        /// <summary>
        /// Checks to see if the specified warehouse has available vehicles for dispatch to serve the proposed transfer after allowing for quotas.
        /// </summary>
        /// <param name="warehouseAI">Warehouse building AI reference.</param>
        /// <param name="warehouseID">Warehouse building ID.</param>
        /// <param name="warehouseData">Warehouse building data record.</param>
        /// <param name="material">Transfer material.</param>
        /// <param name="otherAI">AI reference for the other building in the transfer.</param>
        /// <returns>True if transfer permitted, false otherwise.</returns>
        internal static bool CheckVehicleQuota(BuildingAI warehouseAI, ushort warehouseID, ref Building warehouseData, TransferManager.TransferReason material, BuildingAI otherAI)
        {
            // Check to see if there's an entry for this warehouse.
            if (WarehouseRecords.TryGetValue(warehouseID, out WarehouseRecord warehouseRecord))
            {
                // Entry found - determine if a quota needs to be reserved.
                if (((warehouseRecord.Flags & WarehouseFlags.ReserveUnique) != 0 && !(otherAI is UniqueFactoryAI)) ||
                    ((warehouseRecord.Flags & WarehouseFlags.ReserveOutside) != 0 && !(otherAI is OutsideConnectionAI)) ||
                    (((warehouseRecord.Flags & WarehouseFlags.ReserveCity) != 0) && (otherAI is OutsideConnectionAI)))
                {
                    if (warehouseAI is WarehouseAI thisWarehouseAI)
                    {
                        // Retrieve reserved vehicle count - don't bother checking if no vehicles are reserved.
                        byte reservedVehicles = GetReservedVehicles(warehouseID);
                        if (reservedVehicles != 0)
                        {
                            // Calculate non-reserved vehicle count quota and compare to in-use vehicle count.
                            int quota = (int)(thisWarehouseAI.m_truckCount - reservedVehicles);
                            int vehicleCount = 0, cargoLoad = 0, cargoCapacity = 0, inUse = 0;
                            CalculateOwnVehicles(thisWarehouseAI, warehouseID, ref warehouseData, material, ref vehicleCount, ref cargoLoad, ref cargoCapacity, ref inUse);

                            // Permit transfer if we've got available (non-in-use) vehicles less than the maximum quota permitted.
                            return inUse < quota;
                        }
                    }
                }
            }

            // Default is to return true (transfer permitted).
            return true;
        }

        /// <summary>
        /// Sets the warehouse to reserve vehicles for unique factories.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID.</param>
        internal static void SetReserveUnique(uint buildingID) => SetReserveFlag(buildingID, WarehouseFlags.ReserveUnique);

        /// <summary>
        /// Sets the warehouse to reserve vehicles for outside connections.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID.</param>
        internal static void SetReserveOutside(uint buildingID) => SetReserveFlag(buildingID, WarehouseFlags.ReserveOutside);

        /// <summary>
        /// Sets the warehouse to reserve vehicles for the city.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID.</param>
        internal static void SetReserveCity(uint buildingID) => SetReserveFlag(buildingID, WarehouseFlags.ReserveCity);

        /// <summary>
        /// Clears the reserved vehicle state for the specified warehouse.
        /// </summary>
        /// <param name="buildingID">Warehouse builidng ID.</param>
        internal static void ClearReserve(uint buildingID)
        {
            // Don't do anything if there's no current record.
            if (WarehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord))
            {
                // Calculate updated flags.
                warehouseRecord.Flags &= ~WarehouseFlags.AllReserveFlags;

                // Remove entry if no other data remains.
                if (warehouseRecord.Flags == 0 && warehouseRecord.Priority == 0)
                {
                    WarehouseRecords.Remove(buildingID);
                }
                else
                {
                    // Some valid data remains; update the record with our changes.
                    warehouseRecord.ReserveVehicles = 0;
                    WarehouseRecords[buildingID] = warehouseRecord;
                }
            }
        }

        /// <summary>
        /// Checks if the given warehouse is set to reserve vehicles for unique factories.
        /// </summary>
        /// <param name="buildingID">Building ID of warehouse to check.</param>
        /// <returns>True if the warehouse is set to reserve vehicles for unique factories, false otherwise.</returns>
        internal static bool GetReserveUnique(uint buildingID) => GetFlags(buildingID, WarehouseFlags.ReserveUnique);

        /// <summary>
        /// Checks if the given warehouse is set to reserve vehicles for outside connections.
        /// </summary>
        /// <param name="buildingID">Building ID of warehouse to check.</param>
        /// <returns>True if the warehouse is set to reserve vehicles for outside connections, false otherwise.</returns>
        internal static bool GetReserveOutside(uint buildingID) => GetFlags(buildingID, WarehouseFlags.ReserveOutside);

        /// <summary>
        /// Checks if the given warehouse is set to reserve vehicles for the city.
        /// </summary>
        /// <param name="buildingID">Building ID of warehouse to check.</param>
        /// <returns>True if the warehouse is set to reserve vehicles for local deliveries, false otherwise.</returns>
        internal static bool GetReserveCity(uint buildingID) => GetFlags(buildingID, WarehouseFlags.ReserveCity);

        /// <summary>
        /// Returns the current reserved vehicle count for the specified warehouse.
        /// </summary>
        /// <param name="buildingID">Warehouse building ID.</param>
        /// <returns>Number of reserved vehicles.</returns>
        internal static byte GetReservedVehicles(uint buildingID)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return 0;
            }

            // See if we've got an entry for this building.
            if (buildingID != 0 && WarehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord))
            {
                // Entry found - return same-district flag status.
                return warehouseRecord.ReserveVehicles;
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
        /// <param name="buildingID">Warehouse building ID.</param>
        /// <param name="vehicles">Number of vehicles to reserve.</param>
        internal static void SetReservedVehicles(uint buildingID, byte vehicles)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return;
            }

            // Try to get existing entry.
            bool hasEntry = WarehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord);

            // Are we reserving any vehicles?
            if (vehicles != 0)
            {
                // Reserving vehicles - do we have an existing entry?
                if (hasEntry)
                {
                    // Add flag to existing entry.
                    warehouseRecord.ReserveVehicles = vehicles;
                    WarehouseRecords[buildingID] = warehouseRecord;
                }
                else
                {
                    // No record for building in dictionary - add one.
                    WarehouseRecords.Add(buildingID, new WarehouseRecord
                    {
                        ReserveVehicles = vehicles,
                    });
                }
            }
            else if (hasEntry)
            {
                // Setting reserved vehicles to zero and there's an existing entry for this warehouse (just do nothing if no existing entry).
                // If no other data either, remove entire dictionary entry.
                if (warehouseRecord.Flags == 0 && warehouseRecord.ReserveVehicles == 0 && warehouseRecord.Priority == 0)
                {
                    WarehouseRecords.Remove(buildingID);
                }
                else
                {
                    // Update existing entry.
                    warehouseRecord.ReserveVehicles = vehicles;
                    WarehouseRecords[buildingID] = warehouseRecord;
                }
            }
        }

        /// <summary>
        /// Updates the record for the given warehouse with the provided warehouse record data.
        /// </summary>
        /// <param name="buildingID">Warehouse building ID to update.</param>
        /// <param name="recordData">Warehouse ecord data to update to.</param>
        internal static void UpdateRecord(ushort buildingID,  WarehouseRecord recordData)
        {
            // Does the provided building record have any flags?
            bool isEmpty = recordData.Flags == 0;

            // Do we already have an entry for this building?
            if (WarehouseRecords.ContainsKey(buildingID))
            {
                // Yes - is the new entry empty?
                if (isEmpty)
                {
                    // Yes - remove existing record.
                    WarehouseRecords.Remove(buildingID);
                }
                else
                {
                    // Not empty replace existing entry with the new one.
                    WarehouseRecords[buildingID] = recordData;
                }
            }
            else if (!isEmpty)
            {
                // No - create new entry if the provided data wasn't empty.
                WarehouseRecords.Add(buildingID, recordData);
            }
        }

        /// <summary>
        /// Serializes warehouse data.
        /// </summary>
        /// <param name="writer">Binary writer instance to serialize to.</param>
        internal static void Serialize(BinaryWriter writer)
        {
            Logging.Message("serializing warehouse data");

            // Write length of dictionary.
            writer.Write(WarehouseRecords.Count);

            // Serialise each building entry.
            foreach (KeyValuePair<uint, WarehouseRecord> entry in WarehouseRecords)
            {
                // Local reference.
                WarehouseRecord warehouseRecord = entry.Value;

                // Serialize key and simple fields.
                writer.Write(entry.Key);
                writer.Write((ushort)warehouseRecord.Flags);
                writer.Write(warehouseRecord.ReserveVehicles);
                writer.Write(warehouseRecord.Priority);

                Logging.Message("wrote entry ", entry.Key);
            }
        }

        /// <summary>
        /// Deserializes savegame data.
        /// </summary>
        /// <param name="reader">Binary reader instance to deserialize from.</param>
        internal static void Deserialize(BinaryReader reader)
        {
            Logging.Message("deserializing warehouse data");

            // Clear dictionary.
            WarehouseRecords.Clear();

            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                uint key = reader.ReadUInt32();

                // Deserialize basic building record fields.
                WarehouseRecord warehouseRecord = new WarehouseRecord
                {
                    Flags = (WarehouseFlags)reader.ReadInt16(),
                    ReserveVehicles = reader.ReadByte(),
                    Priority = reader.ReadByte(),
                };

                // Add completed entry to dictionary.
                WarehouseRecords.Add(key, warehouseRecord);
                Logging.Message("read entry ", key);
            }
        }

        /// <summary>
        /// Sets or clears the specified flags for the given warehouse building.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="status">True to set flags, false to clear.</param>
        /// <param name="flags">Flags to set/clear.</param>
        private static void SetFlags(uint buildingID, bool status, WarehouseFlags flags)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return;
            }

            // Try to get existing entry.
            bool hasEntry = WarehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord);

            // Setting or clearing?
            if (status)
            {
                // Setting a flag - do we have an existing entry?
                if (hasEntry)
                {
                    // Add flag to existing entry.
                    warehouseRecord.Flags |= flags;
                    WarehouseRecords[buildingID] = warehouseRecord;
                }
                else
                {
                    // No record for building in dictionary - add one.
                    WarehouseRecords.Add(buildingID, new WarehouseRecord
                    {
                        Flags = flags,
                    });
                }
            }
            else if (hasEntry)
            {
                // Clearing a flag - only bother if we've got an existing entry.
                // Get updated flags.
                WarehouseFlags updatedFlags = warehouseRecord.Flags & ~flags;

                // If no flags remaining, remove entire dictionary entry if there's no other data either.
                if (updatedFlags == 0 && warehouseRecord.ReserveVehicles == 0 && warehouseRecord.Priority == 0)
                {
                    WarehouseRecords.Remove(buildingID);
                }
                else
                {
                    // Update existing entry.
                    warehouseRecord.Flags = updatedFlags;
                    WarehouseRecords[buildingID] = warehouseRecord;
                }
            }
        }

        /// <summary>
        /// Returns the current status of the given flags for the given warehouse building.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="flags">Flags to check.</param>
        /// <returns>True if ANY of the specified flags is set, false otherwise.</returns>
        private static bool GetFlags(uint buildingID, WarehouseFlags flags)
        {
            // Don't do anything if no valid building is set.
            if (buildingID == 0)
            {
                return false;
            }

            // See if we've got an entry for this building.
            if (WarehouseRecords.TryGetValue(buildingID, out WarehouseRecord warehouseRecord))
            {
                // Entry found - return same-district flag status.
                return (warehouseRecord.Flags & flags) != 0;
            }
            else
            {
                // No dictionary entry, therefore no same-district setting.
                return false;
            }
        }

        /// <summary>
        /// Enables the given reserved flag for the given building.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="flag">Flag to enable.</param>
        private static void SetReserveFlag(uint buildingID, WarehouseFlags flag)
        {
            // Clear all other reserve flags first.
            SetFlags(buildingID, false, WarehouseFlags.AllReserveFlags);

            // Set this flag.
            SetFlags(buildingID, true, flag);

            // Ensure minimum vehicle count of one.
            if (GetReservedVehicles(buildingID) == 0)
            {
                SetReservedVehicles(buildingID, 1);
            }
        }

        /// <summary>
        /// Warehouse data record.
        /// </summary>
        public struct WarehouseRecord
        {
            /// <summary>
            /// Warehouse flags.
            /// </summary>
            public WarehouseFlags Flags;

            /// <summary>
            /// Reserved vehicles count.
            /// </summary>
            public byte ReserveVehicles;

            /// <summary>
            /// Custom warehouse priority.
            /// </summary>
            public byte Priority;
        }
    }
}