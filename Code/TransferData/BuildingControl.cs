// <copyright file="BuildingControl.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using System.IO;
    using AlgernonCommons;
    using ColossalFramework;

    /// <summary>
    /// Static class to control building service limits.
    /// </summary>
    internal static class BuildingControl
    {
        // Dictionary of building settings.  Key is building ID as lower 16 bits with transfer reason as upper 8 bits and mask as next upper 8 bits.
        private const uint NewOutgoingMask = 0x00800000;
        internal static Dictionary<uint, BuildingRecord> s_buildingRecords = new Dictionary<uint, BuildingRecord>();

        // Flag to indicate loading of an older data version.
        private static bool oldDataVersion = false;

        /// <summary>
        /// Building restriction flags.
        /// </summary>
        internal enum RestrictionFlags : uint
        {
            /// <summary>
            /// No building restrictions.
            /// </summary>
            None = 0x00,

            /// <summary>
            /// Distric restrictions are enabled for this building.
            /// </summary>
            DistrictEnabled = 0x01,

            /// <summary>
            /// 'Automatically allow same district' setting is DISABLED for this building.
            /// </summary>
            BlockSameDistrict = 0x02,

            /// <summary>
            /// Outside connections are blocked for this building.
            /// </summary>
            BlockOutsideConnection = 0x04,

            /// <summary>
            /// Building restrictions are enabled for this building (LEGACY).
            /// </summary>
            BuildingEnabled = 0x08,

            /// <summary>
            /// Same-district preference is enabled for this building.
            /// </summary>
            PreferSameDistrict = 0x10,
        }

        /// <summary>
        /// Checks to see if the specified building has an entry for the given transfer.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>True if a custom record exists, false otherwise.</returns>
        internal static bool HasRecord(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason) =>
            s_buildingRecords.ContainsKey(CalculateEntryKey(buildingID, incoming, transferReason));

        /// <summary>
        /// Calculates the building dictionary key for the given parameters.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="incoming">True if this transfer is incoming, false otherwise.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>Building dictionary key.</returns>
        internal static uint CalculateEntryKey(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason)
        {
            // Base key is reason as top 8 bits and building ID as lower 16 bits.
            uint key = buildingID | (uint)((byte)transferReason << 24);
            if (!incoming)
            {
                // Bit 23 is the outgoing mask.
                key |= NewOutgoingMask;
            }

            return key;
        }

        /// <summary>
        /// Adds a district to a building's list of permitted districts using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">Building ID to apply to.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="districtID">District ID to add.</param>
        internal static void AddDistrict(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, int districtID)
        {
            // Calculate entry ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // See if we've already got an entry for this building; if not, create one.
            if (!s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Init buildingRecord.
                s_buildingRecords.Add(buildingRecordID, new BuildingRecord
                {
                    Flags = RestrictionFlags.None,
                    Districts = new HashSet<int> { districtID },
                });
            }
            else
            {
                // Existing entry for this building.
                // Create district hashset if it doesn't already exist.
                if (buildingRecord.Districts == null)
                {
                    buildingRecord.Districts = new HashSet<int> { districtID };
                    s_buildingRecords[buildingRecordID] = buildingRecord;
                }
                else
                {
                    // Existing hasheset - add district.
                    s_buildingRecords[buildingRecordID].Districts.Add(districtID);
                }
            }
        }

        /// <summary>
        /// Removes a district from a building's list of permitted districts.
        /// </summary>
        /// <param name="buildingID">Building ID to remove from.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="districtID">District ID to remove.</param>
        internal static void RemoveDistrict(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, int districtID)
        {
            Logging.Message("attempting to remove district ", districtID, " from ", buildingID);

            // Calculate building record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // See if we've got an entry for this building.
            if (s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                if (buildingRecord.Districts != null)
                {
                    // Got an entry - remove district from hasshset.
                    buildingRecord.Districts.Remove(districtID);

                    // If no further entries left in hashset, clear it totally.
                    if (buildingRecord.Districts.Count == 0)
                    {
                        buildingRecord.Districts = null;
                        s_buildingRecords[buildingRecordID] = buildingRecord;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a building to a building's list of permitted building using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">Building ID to apply to.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="newBuilding">Building ID to add.</param>
        internal static void AddBuilding(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, ushort newBuilding)
        {
            // Calculate building record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // See if we've already got an entry for this building; if not, create one.
            if (!s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Init buildingRecord.
                s_buildingRecords.Add(buildingRecordID, new BuildingRecord
                {
                    Flags = RestrictionFlags.None,
                    Buildings = new HashSet<uint> { newBuilding },
                });
            }
            else
            {
                // Existing entry for this building.
                // Create district hashset if it doesn't already exist.
                if (buildingRecord.Buildings == null)
                {
                    buildingRecord.Buildings = new HashSet<uint> { newBuilding };
                    s_buildingRecords[buildingRecordID] = buildingRecord;
                }
                else
                {
                    // Existing hasheset - add district.
                    s_buildingRecords[buildingRecordID].Buildings.Add(newBuilding);
                }
            }
        }

        /// <summary>
        /// Removes a building from a building's list of permitted buildings.
        /// </summary>
        /// <param name="buildingID">Building ID to remove from.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="removeBuilding">Building ID to remove.</param>
        internal static void RemoveBuilding(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, ushort removeBuilding)
        {
            // Calculate building record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // See if we've got an entry for this building.
            if (s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                if (buildingRecord.Buildings != null)
                {
                    // Got an entry - remove district from hasshset.
                    buildingRecord.Buildings.Remove(removeBuilding);

                    // If no further entries left in hashset, clear it totally.
                    if (buildingRecord.Buildings.Count == 0)
                    {
                        buildingRecord.Buildings = null;
                        s_buildingRecords[buildingRecordID] = buildingRecord;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current list of districts attached to a building as a HashSet.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>Hashset of districts attached to the building, null if none.</returns>
        internal static HashSet<int> GetDistricts(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason)
        {
            // Calculate building record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // See if we've got an entry for this building.
            if (s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Got an entry - return it.
                return buildingRecord.Districts;
            }

            // If we got here, no entry was found; return null.
            return null;
        }

        /// <summary>
        /// Gets the current list of buildings attached to a building as a HashSet.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>Hashset of buildings attached to the building, null if none.</returns>
        internal static HashSet<uint> GetBuildings(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason)
        {
            // Calculate building record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // See if we've got an entry for this building.
            if (s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Got an entry - return it.
                return buildingRecord.Buildings;
            }

            // If we got here, no entry was found; return null.
            return null;
        }

        /// <summary>
        /// Sets the district restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="status">Enabled status to set.</param>
        internal static void SetDistrictEnabled(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, bool status) =>
            SetFlag(buildingID, incoming, transferReason, status, RestrictionFlags.DistrictEnabled);

        /// <summary>
        /// Sets the same-district status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="status">Enabled status to set.</param>
        internal static void SetSameDistrict(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, bool status) =>
            SetFlag(buildingID, incoming, transferReason, status, RestrictionFlags.BlockSameDistrict);

        /// <summary>
        /// Sets the prefer-same-district status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="status">Enabled status to set.</param>
        internal static void SetPreferSameDistrict(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, bool status) =>
            SetFlag(buildingID, incoming, transferReason, status, RestrictionFlags.PreferSameDistrict);

        /// <summary>
        /// Sets the outside connection status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="status">Enabled status to set.</param>
        internal static void SetOutsideConnection(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, bool status) =>
            SetFlag(buildingID, incoming, transferReason, status, RestrictionFlags.BlockOutsideConnection);

        /// <summary>
        /// Sets the building restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="status">Enabled status to set.</param>
        internal static void SetBuildingEnabled(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, bool status) =>
            SetFlag(buildingID, incoming, transferReason, status, RestrictionFlags.BuildingEnabled);

        /// <summary>
        /// Returns the current district restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>True if building incoming restrictions are enabled, false otherwise.</returns>
        internal static bool GetDistrictEnabled(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason) =>
            GetFlags(buildingID, incoming, transferReason, RestrictionFlags.DistrictEnabled);

        /// <summary>
        /// Returns the current same-district status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>True if building is limited to same-district-only, false otherwise.</returns>
        internal static bool GetSameDistrict(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason) =>
            GetFlags(buildingID, incoming, transferReason, RestrictionFlags.BlockSameDistrict);

        /// <summary>
        /// Returns the current prefer-same-district status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>True if building is limited to same-district-only, false otherwise.</returns>
        internal static bool GetPreferSameDistrict(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason) =>
            GetFlags(buildingID, incoming, transferReason, RestrictionFlags.PreferSameDistrict);

        /// <summary>
        /// Returns the current outdise connection status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>True if building incoming restrictions are enabled, false otherwise.</returns>
        internal static bool GetOutsideConnection(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason) =>
            GetFlags(buildingID, incoming, transferReason, RestrictionFlags.BlockOutsideConnection);

        /// <summary>
        /// Returns the current building restrictions enabled status of the given building record.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <returns>True if building incoming restrictions are enabled, false otherwise.</returns>
        internal static bool GetBuildingEnabled(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason) =>
            GetFlags(buildingID, incoming, transferReason, RestrictionFlags.BuildingEnabled);

        /// <summary>
        /// Updates the record for the given building with the provided building record data.
        /// </summary>
        /// <param name="buildingID">Building ID to update.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="recordData">Record data to update to.</param>
        internal static void UpdateRecord(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, ref BuildingRecord recordData)
        {
            // Copy record data.
            BuildingRecord newRecord = recordData;

            // Calculate target record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // Does the provided building record have any flags?
            bool isEmpty = recordData.Flags == 0;

            // If the record isn't empty, copy district and building buffers to new HashSets, otherwise the old ones will be shared.
            if (!isEmpty)
            {
                if (newRecord.Districts != null)
                {
                    newRecord.Districts = new HashSet<int>(newRecord.Districts);
                }

                if (newRecord.Buildings != null)
                {
                    newRecord.Buildings = new HashSet<uint>(newRecord.Buildings);
                }
            }

            // Do we already have an entry for this building?
            if (s_buildingRecords.ContainsKey(buildingRecordID))
            {
                // Yes - is the new entry empty?
                if (isEmpty)
                {
                    // Yes - remove existing record.
                    s_buildingRecords.Remove(buildingRecordID);
                }
                else
                {
                    // Not empty replace existing entry with the new one.
                    s_buildingRecords[buildingRecordID] = newRecord;
                }
            }
            else if (!isEmpty)
            {
                // No - create new entry if the provided data wasn't empty.
                s_buildingRecords.Add(buildingRecordID, newRecord);
            }
        }

        /// <summary>
        /// Deletes all records relating to the specified building.
        /// </summary>
        /// <param name="buildingID">Building ID to delete.</param>
        internal static void ReleaseBuilding(uint buildingID)
        {
            // Iterate through each transfer type and find all records..
            for (int i = 0; i < TransferManager.TRANSFER_REASON_COUNT; ++i)
            {
                // Remove incomung and outgoiing entries.
                uint key = (uint)(i << 24) | buildingID;
                s_buildingRecords.Remove(key);
                s_buildingRecords.Remove(key | NewOutgoingMask);
            }

            // Then, iterate through all records and remove this reference from all building lists.
            foreach (KeyValuePair<uint, BuildingRecord> entry in s_buildingRecords)
            {
                if (entry.Value.Buildings != null)
                {
                    entry.Value.Buildings.Remove(buildingID);
                }
            }
        }

        /// <summary>
        /// Serializes savegame data.
        /// </summary>
        /// <param name="writer">Binary writer instance to serialize to.</param>
        internal static void Serialize(BinaryWriter writer)
        {
            Logging.Message("serializing building data");

            // Write length of dictionary.
            writer.Write(s_buildingRecords.Count);

            // Serialise each building entry.
            foreach (KeyValuePair<uint, BuildingRecord> entry in s_buildingRecords)
            {
                // Local reference.
                BuildingRecord buildingRecord = entry.Value;

                // Serialize key and simple fields.
                writer.Write(entry.Key);
                writer.Write((uint)buildingRecord.Flags);

                // Serialize district entries for this building.
                int districtCount = buildingRecord.Districts?.Count ?? 0;
                writer.Write(districtCount);

                // Don't write anything further if count is zero.
                if (districtCount > 0)
                {
                    foreach (int district in buildingRecord.Districts)
                    {
                        writer.Write(district);
                    }
                }

                // Serialize building entries for this building.
                int buildingCount = buildingRecord.Buildings?.Count ?? 0;
                writer.Write(buildingCount);

                // Don't write anything further if count is zero.
                if (buildingCount > 0)
                {
                    foreach (uint building in buildingRecord.Buildings)
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
        /// <param name="reader">Reader to deserialize from.</param>
        /// <param name="dataVersion">Data version.</param>
        internal static void Deserialize(BinaryReader reader, int dataVersion)
        {
            Logging.Message("deserializing building data");

            // Local reference.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            // Clear dictionary.
            s_buildingRecords.Clear();

            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                uint key = reader.ReadUInt32();

                BuildingRecord buildingRecord;

                // Check older data versions.
                if (dataVersion <= 4)
                {
                    oldDataVersion = true;
                }

                // Legacy dataversion.
                if (dataVersion <= 2)
                {
                    // Discard old recordnumber byte.
                    byte nextRecord = reader.ReadByte();

                    buildingRecord = new BuildingRecord
                    {
                        Flags = (RestrictionFlags)reader.ReadByte(),
                    };

                    // Convert key to new format.
                    ushort oldBuildingID = (ushort)(key & 0xFFFF);
                    bool isIncoming = (key & 0x80000000) == 0;
                    Logging.Message("found old building entry for building ", oldBuildingID, " with record mask ", (key & 0xFF000000) >> 24);
                    TransferManager.TransferReason transferReason = (TransferManager.TransferReason)reader.ReadInt32();
                    key = CalculateEntryKey(oldBuildingID, isIncoming, transferReason);
                }
                else
                {
                    // New dataversion.
                    buildingRecord = new BuildingRecord
                    {
                        Flags = (RestrictionFlags)reader.ReadUInt32(),
                    };
                }

                Logging.Message("read flags ", buildingRecord.Flags, " for entry ", key);

                // Deserialize district entries for this building.
                int districtCount = reader.ReadInt32();

                Logging.Message("district count is ", districtCount);

                // If serialized count is zero, there's nothing further to deserialize.
                if (districtCount > 0)
                {
                    // Create new hashset and populate with deserialized data.
                    buildingRecord.Districts = new HashSet<int>();
                    for (int j = 0; j < districtCount; ++j)
                    {
                        buildingRecord.Districts.Add(reader.ReadInt32());
                    }

                    // Validate district list before continuing.
                    TransferDataUtils.ValidateDistricts(buildingRecord.Districts);
                }

                // Deserialize building entries for this building.
                int buildingCount = reader.ReadInt32();

                Logging.Message("building count is ", buildingCount);

                // If serialized count is zero, there's nothing further to deserialize.
                if (buildingCount > 0)
                {
                    // Create new hashset and populate with deserialized data.
                    buildingRecord.Buildings = new HashSet<uint>();
                    for (int j = 0; j < buildingCount; ++j)
                    {
                        buildingRecord.Buildings.Add(reader.ReadUInt32());
                    }
                }

                // Extract details from key.
                TransferManager.TransferReason reason = (TransferManager.TransferReason)((key & 0xFF000000) >> 24);
                bool incoming = (key & NewOutgoingMask) == 0;
                ushort buildingID = (ushort)(key & 0x0000FFFF);

                // Fix for invalidly recorded transfers.
                if (!incoming)
                {
                    if (reason == TransferManager.TransferReason.Garbage ||
                        reason == TransferManager.TransferReason.Crime ||
                        reason == TransferManager.TransferReason.Dead ||
                        reason == TransferManager.TransferReason.Sick ||
                        reason == TransferManager.TransferReason.Sick2 ||
                        reason == TransferManager.TransferReason.Fire ||
                        reason == TransferManager.TransferReason.Fire2 ||
                        reason == TransferManager.TransferReason.Mail)
                    {
                        Logging.Message("invalid incoming state for ", reason, "; correcting");
                        key = CalculateEntryKey(buildingID, true, reason);
                    }
                }

                // Drop any entries for invalid buildings.
                if ((buildingBuffer[buildingID].m_flags & Building.Flags.Created) == 0)
                {
                    Logging.Message("dropping entry for invalid building ", buildingID);
                    continue;
                }

                // Drop any empty entries.
                if (buildingRecord.Flags == 0 && buildingRecord.Buildings.Count == 0 && buildingRecord.Districts.Count == 0)
                {
                    Logging.Message("dropping empty entry for building ", buildingID);
                    continue;
                }

                // Add completed entry to dictionary.
                if (!s_buildingRecords.ContainsKey(key))
                {
                    s_buildingRecords.Add(key, buildingRecord);
                    Logging.Message("read entry for building ", buildingID, " incoming ", (key & NewOutgoingMask) == 0, " and reason ", reason);
                }
                else
                {
                    Logging.Error("duplicate buildingRecord key for building ", buildingID, " incoming ", (key & 0x00FF0000) == 0, " and reason ", reason);
                }
            }
        }

        /// <summary>
        /// Performs any required correction of older save data.
        /// </summary>
        internal static void ConvertLegacyRecords()
        {
            // Don't do anything if we didn't load an old data version.
            if (!oldDataVersion)
            {
                return;
            }

            Logging.KeyMessage("Converting legacy records");

            // Local reference.
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            // List of candidate entries.
            List<uint> candidateEntries = new List<uint>();

            // Iterate through building record dictionary looking for eligible records.
            foreach (KeyValuePair<uint, BuildingRecord> entry in s_buildingRecords)
            {
                // Interested in any records with the 'None' or 'Oil' transfer reasons.
                TransferManager.TransferReason reason = (TransferManager.TransferReason)((entry.Key & 0xFF000000) >> 24);
                if (reason == TransferManager.TransferReason.None | reason == TransferManager.TransferReason.Oil)
                {
                    candidateEntries.Add(entry.Key);
                }
            }

            // Now, iterate through each candidate.
            foreach (uint oldEntry in candidateEntries)
            {
                // Extract data from key.
                ushort buildingID = (ushort)(oldEntry & 0x0000FFFF);
                bool isIncoming = (oldEntry & NewOutgoingMask) == 0;
                TransferManager.TransferReason transferReason = (TransferManager.TransferReason)((oldEntry & 0xFF000000) >> 24);

                // Local references.
                BuildingInfo buildingInfo = buildingBuffer[buildingID].Info;
                BuildingAI buildingAI = buildingInfo.m_buildingAI;

                // New transfer reason.
                TransferManager.TransferReason newReason = TransferManager.TransferReason.None;

                if (transferReason == TransferManager.TransferReason.None)
                {
                    // Incoming or outgoing?
                    if (isIncoming)
                    {
                        // Incoming transfer updates from TransferReason.None.
                        if (buildingAI is HospitalAI)
                        {
                            newReason = TransferManager.TransferReason.Sick;
                        }
                        else if (buildingAI is CemeteryAI)
                        {
                            newReason = TransferManager.TransferReason.Dead;
                        }
                        else if (buildingAI is HelicopterDepotAI && buildingInfo.m_class.m_service == ItemClass.Service.HealthCare)
                        {
                            newReason = TransferManager.TransferReason.Sick2;
                        }
                    }
                    else
                    {
                        // Outgoing transfer updates from TransferReason.None.
                        if (buildingAI is ExtractingFacilityAI extractingAI)
                        {
                            newReason = extractingAI.m_outputResource;
                        }
                        else if (buildingAI is ProcessingFacilityAI processingAI)
                        {
                            newReason = processingAI.m_outputResource;
                        }
                    }
                }
                else if (transferReason == TransferManager.TransferReason.Oil && buildingAI is HeatingPlantAI heatingAI)
                {
                    // Conversion for records for boiler stations from oil to petrol.
                    newReason = heatingAI.m_resourceType;
                }

                // Did we end up with an updated reason?
                if (newReason != TransferManager.TransferReason.None)
                {
                    // Yes - remove old record and add new replacement.
                    Logging.Message("converting old TransferType.None ", isIncoming ? "incoming" : "outgoing", " record for building ", buildingID, " to new record with transferType ", newReason);
                    BuildingRecord oldRecord = s_buildingRecords[oldEntry];
                    s_buildingRecords.Remove(oldEntry);

                    // Calculate new key and check that it's unique.
                    uint newEntry = CalculateEntryKey(buildingID, isIncoming, newReason);
                    if (s_buildingRecords.ContainsKey(newEntry))
                    {
                        // Duplicate key - log and drop.
                        Logging.Error("duplicate new key for building ", buildingID, "; skipping");
                    }
                    else
                    {
                        // Add new entry.
                        s_buildingRecords.Add(CalculateEntryKey(buildingID, isIncoming, newReason), oldRecord);
                    }
                }
                else
                {
                    // No change made.
                    Logging.Message("leaving building ID ", buildingID, " with ", isIncoming ? "incoming " : "outgoing ", transferReason);
                }
            }
        }

        /// <summary>
        /// Sets or clears the specified flag for the given building instance using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="status">True to set flag, false to clear.</param>
        /// <param name="flag">Flag to set/clear.</param>
        private static void SetFlag(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, bool status, RestrictionFlags flag)
        {
            // Calculate building record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // Try to get existing entry.
            bool hasEntry = s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord);

            // Setting or clearing?
            if (status)
            {
                // Setting a flag - do we have an existing entry?
                if (hasEntry)
                {
                    // Add flag to existing entry.
                    buildingRecord.Flags |= flag;
                    s_buildingRecords[buildingRecordID] = buildingRecord;
                }
                else
                {
                    // No record for building in dictionary - add one.
                    s_buildingRecords.Add(buildingRecordID, new BuildingRecord
                    {
                        Flags = flag,
                    });
                }
            }
            else if (hasEntry)
            {
                // Clearing a flag - only bother if we've got an existing entry.
                // Get updated flags.
                RestrictionFlags updatedFlags = buildingRecord.Flags & ~flag;

                // If no flags remaining, remove entire dictionary entry.
                if (updatedFlags == RestrictionFlags.None)
                {
                    s_buildingRecords.Remove(buildingRecordID);
                }
                else
                {
                    // Update existing entry.
                    buildingRecord.Flags = updatedFlags;
                    s_buildingRecords[buildingRecordID] = buildingRecord;
                }
            }
        }

        /// <summary>
        /// Returns the current status of the given flag for the given building using the specified dictionary.
        /// </summary>
        /// <param name="buildingID">ID of building to check.</param>
        /// <param name="incoming">True if this is an incoming transfer record, false if outgoing.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="flag">Flag to check.</param>
        /// <returns>True if flag is set, false otherwise.</returns>S
        private static bool GetFlags(ushort buildingID, bool incoming, TransferManager.TransferReason transferReason, RestrictionFlags flag)
        {
            // Calculate building record ID.
            uint buildingRecordID = CalculateEntryKey(buildingID, incoming, transferReason);

            // See if we've got an entry for this building.
            if (s_buildingRecords.TryGetValue(buildingRecordID, out BuildingRecord buildingRecord))
            {
                // Entry found - return same-district flag status.
                return (buildingRecord.Flags & flag) != RestrictionFlags.None;
            }
            else
            {
                // No dictionary entry, therefore no same-district setting.
                return false;
            }
        }

        /// <summary>
        /// Per- building restriction data.
        /// </summary>
        public struct BuildingRecord
        {
            /// <summary>
            /// Building restriction flags.
            /// </summary>
            public RestrictionFlags Flags;

            /// <summary>
            /// List of permitted districts for this building.
            /// </summary>
            public HashSet<int> Districts;

            /// <summary>
            /// List of permitted buildings for this building.
            /// </summary>
            public HashSet<uint> Buildings;
        }
    }
}