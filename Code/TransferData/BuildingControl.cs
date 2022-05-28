using System.IO;
using System.Collections.Generic;


namespace TransferController
{
    /// <summary>
    /// Static class to control building service limits.
    /// </summary>
    internal static class BuildingControl
    {
        /// <summary>
        /// Building restriction flags.
        /// </summary>
        internal enum RestrictionFlags : uint
        {
            None = 0x00,
            DistrictEnabled = 0x01,
            BlockSameDistrict = 0x02,
            BlockOutsideConnection = 0x04,
            BuildingEnabled = 0x08
        }

        internal const byte IncomingMask = 0x00;
        internal const byte OutgoingMask = 0x80;
        internal const byte NextRecordMask = 0x7F;


        /// <summary>
        /// Per- building restriction data.
        /// </summary>
        public struct BuildingRecord
        {
            public byte nextRecord;
            public RestrictionFlags flags;
            public TransferManager.TransferReason reason;
            public HashSet<int> districts;
            public HashSet<uint> buildings;
        }


        // Dictionary of building settings.  Key is building ID as lower 24 bits with building record ID as upper 8 bits.
        internal readonly static Dictionary<uint, BuildingRecord> buildingRecords = new Dictionary<uint, BuildingRecord>();


        /// <summary>
        /// Adds a district to a building's list of permitted districts using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">Building ID to apply to</param>
        /// <param name="recordID">Building record number</param>
        /// <param name="districtID">District ID to add</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <param name="nextRecordMask">Next record mask</param>
        internal static void AddDistrict(uint buildingID, byte recordID, int districtID, TransferManager.TransferReason transferReason, byte nextRecordMask)
        {
            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);
            Logging.Message("adding district ", districtID, " to districts for record ", buildingRecordID);

            // See if we've already got an entry for this building; if not, create one.
            if (!buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Init buildingRecord.
                buildingRecords.Add(buildingRecordID, new BuildingRecord
                {
                    nextRecord = nextRecordMask,
                    flags = RestrictionFlags.None,
                    reason = transferReason,
                    districts = new HashSet<int> { districtID }
                });
            }
            else
            {
                // Existing entry for this building.
                // Create district hashset if it doesn't already exist.
                if (buildingRecord.districts == null)
                {
                    buildingRecord.districts = new HashSet<int> { districtID };
                    buildingRecords[buildingRecordID] = buildingRecord;
                }
                else
                {
                    // Existing hasheset - add district.
                    buildingRecords[buildingRecordID].districts.Add(districtID);
                }
            }
        }


        /// <summary>
        /// Removes a district from a building's list of permitted districts.
        /// </summary>
        /// <param name="buildingID">Building ID to remove from</param>
        /// <param name="recordID">Building record number to remove from</param>
        /// <param name="districtID">District ID to remove</param>
        internal static void RemoveDistrict(uint buildingID, byte recordID, int districtID)
        {
            Logging.Message("attempting to remove district ", districtID, " from ", buildingID);

            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);

            // See if we've got an entry for this building.
            if (buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                if (buildingRecord.districts != null)
                {
                    // Got an entry - remove district from hasshset.
                    buildingRecord.districts.Remove(districtID);

                    // If no further entries left in hashset, clear it totally.
                    if (buildingRecord.districts.Count == 0)
                    {
                        buildingRecord.districts = null;
                        buildingRecords[buildingRecordID] = buildingRecord;
                    }
                }
            }
        }


        /// <summary>
        /// Adds a building to a building's list of permitted building using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">Building ID to apply to</param>
        /// <param name="recordID">Building record number</param>
        /// <param name="newBuilding">Building ID to add</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <param name="nextRecordMask">Next record mask</param>
        internal static void AddBuilding(uint buildingID, byte recordID, ushort newBuilding, TransferManager.TransferReason transferReason, byte nextRecordMask)
        {
            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);
            Logging.Message("adding building ", newBuilding, " to buildings for record ", buildingRecordID);

            // See if we've already got an entry for this building; if not, create one.
            if (!buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Init buildingRecord.
                buildingRecords.Add(buildingRecordID, new BuildingRecord
                {
                    nextRecord = nextRecordMask,
                    flags = RestrictionFlags.None,
                    reason = transferReason,
                    buildings = new HashSet<uint> { newBuilding }
                });
            }
            else
            {
                // Existing entry for this building.
                // Create district hashset if it doesn't already exist.
                if (buildingRecord.buildings == null)
                {
                    buildingRecord.buildings = new HashSet<uint> { newBuilding };
                    buildingRecords[buildingRecordID] = buildingRecord;
                }
                else
                {
                    // Existing hasheset - add district.
                    buildingRecords[buildingRecordID].buildings.Add(newBuilding);
                }
            }
        }


        /// <summary>
        /// Removes a building from a building's list of permitted buildings.
        /// </summary>
        /// <param name="buildingID">Building ID to remove from</param>
        /// <param name="recordID">Building record number to remove from</param>
        /// <param name="removeBuilding">Building ID to remove</param>
        internal static void RemoveBuilding(uint buildingID, byte recordID, uint removeBuilding)
        {
            Logging.Message("attempting to remove building ", removeBuilding, " from ", buildingID);

            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);

            // See if we've got an entry for this building.
            if (buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                if (buildingRecord.buildings != null)
                {
                    // Got an entry - remove district from hasshset.
                    buildingRecord.buildings.Remove(removeBuilding);

                    // If no further entries left in hashset, clear it totally.
                    if (buildingRecord.buildings.Count == 0)
                    {
                        buildingRecord.buildings = null;
                        buildingRecords[buildingRecordID] = buildingRecord;
                    }
                }
            }
        }


        /// <summary>
        /// Gets the current list of districts attached to a building as a HashSet.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="recordID">Building record number to check</param>
        /// <returns>Hashset of districts attached to the building, null if none</returns>
        internal static HashSet<int> GetDistricts(uint buildingID, byte recordID)
        {
            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);

            // See if we've got an entry for this building.
            if (buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Got an entry - return it.
                return buildingRecord.districts;
            }

            // If we got here, no entry was found; return null.
            return null;
        }


        /// <summary>
        /// Gets the current list of buildings attached to a building as a HashSet.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="recordID">Building record number to check</param>
        /// <returns>Hashset of districts attached to the building, null if none</returns>
        internal static HashSet<uint> GetBuildings(uint buildingID, byte recordID)
        {
            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);

            // See if we've got an entry for this building.
            if (buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Got an entry - return it.
                return buildingRecord.buildings;
            }

            // If we got here, no entry was found; return null.
            return null;
        }


        /// <summary>
        /// Sets the district restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="recordID">Building record number to set</param>
        /// <param name="status">Enabled status to set</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <param name="nextRecordMask">Next record mask</param>
        internal static void SetDistrictEnabled(uint buildingID, byte recordID, bool status, TransferManager.TransferReason transferReason, byte nextRecordMask) => SetFlag(buildingID, recordID, status, RestrictionFlags.DistrictEnabled, transferReason, nextRecordMask);


        /// <summary>
        /// Sets the same-district status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to set</param>
        /// <param name="recordID">Building record number to set</param>
        /// <param name="status">Same-district status to set</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <param name="nextRecordMask">Next record mask</param>
        internal static void SetSameDistrict(uint buildingID, byte recordID, bool status, TransferManager.TransferReason transferReason, byte nextRecordMask) => SetFlag(buildingID, recordID, status, RestrictionFlags.BlockSameDistrict, transferReason, nextRecordMask);


        /// <summary>
        /// Sets the outside connection status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to set</param>
        /// <param name="recordID">Building record number to set</param>
        /// <param name="status">Same-district status to set</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <param name="nextRecordMask">Next record mask</param>
        internal static void SetOutsideConnection(uint buildingID, byte recordID, bool status, TransferManager.TransferReason transferReason, byte nextRecordMask) => SetFlag(buildingID, recordID, status, RestrictionFlags.BlockOutsideConnection, transferReason, nextRecordMask);



        /// <summary>
        /// Sets the building restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="recordID">Building record number to set</param>
        /// <param name="status">Enabled status to set</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <param name="nextRecordMask">Next record mask</param>
        internal static void SetBuildingEnabled(uint buildingID, byte recordID, bool status, TransferManager.TransferReason transferReason, byte nextRecordMask) => SetFlag(buildingID, recordID, status, RestrictionFlags.BuildingEnabled, transferReason, nextRecordMask);


        /// <summary>
        /// Returns the current district restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="recordID">Building record number to check</param>
        /// <returns>True if building incoming restrictions are enabled, false otherwise</returns>
        internal static bool GetDistrictEnabled(uint buildingID, byte recordID) => GetFlags(buildingID, recordID, RestrictionFlags.DistrictEnabled);


        /// <summary>
        /// Returns the current same-district status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="recordID">Building record number to check</param>
        /// <returns>True if building is limited to same-district-only, false otherwise</returns>
        internal static bool GetSameDistrict(uint buildingID, byte recordID) => GetFlags(buildingID, recordID, RestrictionFlags.BlockSameDistrict);


        /// <summary>
        /// Returns the current outdise connection status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="recordID">Building record number to check</param>
        /// <returns>True if building incoming restrictions are enabled, false otherwise</returns>
        internal static bool GetOutsideConnection(uint buildingID, byte recordID) => GetFlags(buildingID, recordID, RestrictionFlags.BlockOutsideConnection);


        /// <summary>
        /// Returns the current building restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="recordID">Building record number to check</param>
        /// <returns>True if building incoming restrictions are enabled, false otherwise</returns>
        internal static bool GetBuildingEnabled(uint buildingID, byte recordID) => GetFlags(buildingID, recordID, RestrictionFlags.BuildingEnabled);


        /// <summary>
        /// Updates the record for the given building with the provided building record data.
        /// </summary>
        /// <param name="buildingID">Building ID to update</param>
        /// <param name="recordID">Record ID to update</param>
        /// <param name="recordData">Record data to update to</param>
        internal static void UpdateRecord(ushort buildingID, byte recordID, ref BuildingRecord recordData)
        {
            // Copy record data.
            BuildingRecord newRecord = recordData;

            // Calculate target record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);

            // Does the provided building record have any flags?
            bool isEmpty = recordData.flags == 0;

            // If the record isn't empty, copy district and building buffers to new HashSets, otherwise the old ones will be shared.
            if (!isEmpty)
            {
                if (newRecord.districts != null)
                {
                    newRecord.districts = new HashSet<int>(newRecord.districts);
                }
                if (newRecord.buildings != null)
                {
                    newRecord.buildings = new HashSet<uint>(newRecord.buildings);
                }
            }

            // Do we already have an entry for this building?
            if (buildingRecords.ContainsKey(buildingRecordID))
            {
                // Yes - is the new entry empty?
                if (isEmpty)
                {
                    // Yes - remove existing record.
                    buildingRecords.Remove(buildingRecordID);
                }
                else
                {
                    // Not empty replace existing entry with the new one.
                    buildingRecords[buildingRecordID] = newRecord;
                }
            }
            else if (!isEmpty)
            {
                // No - create new entry if the provided data wasn't empty.
                buildingRecords.Add(buildingRecordID, newRecord);
            }
        }


        /// <summary>
        /// Deletes all records relating to the specified building.
        /// </summary>
        /// <param name="buildingID">Building ID to delete</param>
        internal static void ReleaseBuilding(uint buildingID)
        {
            const uint BuildingIncrement = 0x01 << 24;

            // Remove all incoming records for this building.
            uint recordID = (uint)(buildingID | IncomingMask);
            for (int i = 0; i < BuildingInfoPanel.MaxTransfers; ++i)
            {
                buildingRecords.Remove(recordID);
                recordID += BuildingIncrement;
            }

            // Remove all outgoing records for this building.
            recordID = (uint)(buildingID | OutgoingMask);
            for (int i = 0; i < BuildingInfoPanel.MaxTransfers; ++i)
            {
                buildingRecords.Remove(recordID);
                recordID += BuildingIncrement;
            }

            // Then, iterate through all records and remove this reference from all building lists.
            foreach (KeyValuePair<uint, BuildingRecord> entry in buildingRecords)
            {
                if (entry.Value.buildings != null)
                {
                    entry.Value.buildings.Remove(buildingID);
                }
            }
        }


        /// <summary>
        /// Serializes savegame data.
        /// </summary>
        /// <param name="stream">Binary writer instance to serialize to</param>
        internal static void Serialize(BinaryWriter writer)
        {
            Logging.Message("serializing building data");

            // Write length of dictionary.
            writer.Write(buildingRecords.Count);

            // Serialise each building entry.
            foreach (KeyValuePair<uint, BuildingRecord> entry in buildingRecords)
            {
                // Local reference.
                BuildingRecord buildingRecord = entry.Value;

                // Serialize key and simple fields.
                writer.Write(entry.Key);
                writer.Write(buildingRecord.nextRecord);
                writer.Write((byte)buildingRecord.flags);
                writer.Write((int)buildingRecord.reason);

                // Serialize district entries for this building.
                int districtCount = buildingRecord.districts?.Count ?? 0;
                writer.Write(districtCount);

                // Don't write anything further if count is zero.
                if (districtCount > 0)
                {
                    foreach (int district in buildingRecord.districts)
                    {
                        writer.Write(district);
                    }
                }

                // Serialize building entries for this building.
                int buildingCount = buildingRecord.buildings?.Count ?? 0;
                writer.Write(buildingCount);

                // Don't write anything further if count is zero.
                if (buildingCount > 0)
                {
                    foreach (uint building in buildingRecord.buildings)
                    {
                        writer.Write(building);
                    }
                }

                Logging.Message("wrote entry ", entry.Key);
            }
        }


        /// <summary>
        /// Deserializes savegame data.
        /// </summary>
        /// <param name="stream">Data memory stream to deserialize from</param>
        internal static void Deserialize(BinaryReader reader)
        {
            Logging.Message("deserializing building data");

            // Clear dictionary.
            buildingRecords.Clear();

            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                uint key = reader.ReadUInt32();

                // Deserialize basic building record fields.
                BuildingRecord buildingRecord = new BuildingRecord
                {
                    nextRecord = reader.ReadByte(),
                    flags = (RestrictionFlags)reader.ReadByte(),
                    reason = (TransferManager.TransferReason)reader.ReadInt32()
                };

                Logging.Message("read flags for entry ", key);

                // Deserialize district entries for this building.
                int districtCount = reader.ReadInt32();

                Logging.Message("district count is ", districtCount);

                // If serialized count is zero, there's nothing further to deserialize.
                if (districtCount > 0)
                {
                    // Create new hashset and populate with deserialized data.
                    buildingRecord.districts = new HashSet<int>();
                    for (int j = 0; j < districtCount; ++j)
                    {
                        buildingRecord.districts.Add(reader.ReadInt32());
                    }

                    // Validate district list before continuing.
                    TransferDataUtils.ValidateDistricts(buildingRecord.districts);
                }

                // Deserialize building entries for this building.
                int buildingCount = reader.ReadInt32();

                Logging.Message("building count is ", buildingCount);

                // If serialized count is zero, there's nothing further to deserialize.
                if (buildingCount > 0)
                {
                    // Create new hashset and populate with deserialized data.
                    buildingRecord.buildings = new HashSet<uint>();
                    for (int j = 0; j < buildingCount; ++j)
                    {
                        buildingRecord.buildings.Add(reader.ReadUInt32());
                    }
                }

                // Add completed entry to dictionary.
                buildingRecords.Add(key, buildingRecord);
                Logging.Message("read entry ", key);
            }
        }


        /// <summary>
        /// Sets or clears the specified flag for the given building instance using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="recordID">Building record number to set</param>
        /// <param name="status">True to set flag, false to clear</param>
        /// <param name="flag">Flag to set/clear</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <param name="nextRecordMask">Next record mask</param>
        private static void SetFlag(uint buildingID, byte recordID, bool status, RestrictionFlags flag, TransferManager.TransferReason transferReason, byte nextRecordMask)
        {
            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);

            // Try to get existing entry.
            bool hasEntry = buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord);

            // Setting or clearing?
            if (status)
            {
                // Setting a flag - do we have an existing entry?
                if (hasEntry)
                {
                    // Add flag to existing entry.
                    buildingRecord.flags |= flag;
                    buildingRecords[buildingRecordID] = buildingRecord;
                }
                else
                {
                    // No record for building in dictionary - add one.
                    buildingRecords.Add(buildingRecordID, new BuildingRecord
                    {
                        nextRecord = nextRecordMask,
                        flags = flag,
                        reason = transferReason
                    });
                }
            }
            else if (hasEntry)
            {
                // Clearing a flag - only bother if we've got an existing entry.
                // Get updated flags.
                RestrictionFlags updatedFlags = buildingRecord.flags & ~flag;

                // If no flags remaining, remove entire dictionary entry.
                if (updatedFlags == RestrictionFlags.None)
                {
                    buildingRecords.Remove(buildingRecordID);
                }
                else
                {
                    // Update existing entry.
                    buildingRecord.flags = updatedFlags;
                    buildingRecords[buildingRecordID] = buildingRecord;
                }
            }
        }


        /// <summary>
        /// Returns the current status of the given flag for the given building using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="recordID">Building record number to check</param>
        /// <param name="flag">Flag to check</param>
        /// <returns>True if flag is set, false otherwise</returns>
        private static bool GetFlags(uint buildingID, byte recordID, RestrictionFlags flag)
        {
            // Calculate building record ID.
            uint mask = (uint)recordID << 24;
            uint buildingRecordID = (uint)(buildingID | mask);

            // See if we've got an entry for this building.
            if (buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Entry found - return same-district flag status.
                return (buildingRecord.flags & flag) != RestrictionFlags.None;
            }
            else
            {
                // No dictionary entry, therefore no same-district setting.
                return false;
            }
        }
    }
}