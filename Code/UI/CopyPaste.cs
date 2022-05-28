using System.Collections.Generic;
using ColossalFramework;


namespace TransferController
{
    /// <summary>
    /// Handles copying and pasting of building settings.
    /// </summary>
    public static class CopyPaste
    {
        // Copy buffer.
        private static int bufferSize;
        private static BuildingControl.BuildingRecord[] copyBuffer = new BuildingControl.BuildingRecord[BuildingInfoPanel.MaxTransfers];
        private static byte[] copyRecordNumbers = new byte[BuildingInfoPanel.MaxTransfers];

        // Prevent heap allocations every time we copy.
        private static TransferStruct[] transferBuffer = new TransferStruct[BuildingInfoPanel.MaxTransfers];


        /// <summary>
        /// Copies TC data from the given building to the copy buffer.
        /// </summary>
        /// <param name="buildingID">Source building ID</param>
        /// <param name="buildingInfo">Source building info</param>
        internal static void Copy(ushort buildingID, BuildingInfo buildingInfo)
        {
            Logging.Message("copying from building ", buildingID);

            // Saftey checks.
            if (buildingID == 0 || buildingInfo == null)
            {
                Logging.Error("invalid parameter passed to CopyPaste.Copy");
                return;
            }

            // Number of records to copy.
            int length = TransferDataUtils.BuildingEligibility(buildingID, buildingInfo, transferBuffer);
            bufferSize = length;

            // Copy records from source building to buffer.
            for (int i = 0; i < length; ++i)
            {
                // Calculate building record ID.
                uint mask = (uint)transferBuffer[i].recordNumber << 24;
                uint buildingRecordID = (uint)(buildingID | mask);

                // Try to get valid entry, outputting to the copy buffer.
                if (BuildingControl.buildingRecords.TryGetValue(buildingRecordID, out copyBuffer[i]))
                {
                    // Set record mask.
                    copyRecordNumbers[i] = transferBuffer[i].recordNumber;

                    // Copy district and building buffers to new HashSets, otherwise the old ones will be shared.
                    if (copyBuffer[i].districts != null)
                    {
                        copyBuffer[i].districts = new HashSet<int>(copyBuffer[i].districts);
                    }
                    if (copyBuffer[i].buildings != null)
                    {
                        copyBuffer[i].buildings = new HashSet<uint>(copyBuffer[i].buildings);
                    }
                }
                else
                {
                    // If no valid entry for this record, clear the buffer entry.
                    copyBuffer[i] = default;
                    copyRecordNumbers[i] = 0;
                }
            }
        }



        /// <summary>
        /// Attempts to paste TC data from the copy buffer to the given building.
        /// </summary>
        /// <param name="buildingID">Source building ID</param>
        internal static bool Paste(ushort buildingID) => Paste(buildingID, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info);


        /// <summary>
        /// Attempts to paste TC data from the copy buffer to the given building.
        /// </summary>
        /// <param name="buildingID">Source building ID</param>
        /// <param name="buildingInfo">Source building info</param>
        internal static bool Paste(ushort buildingID, BuildingInfo buildingInfo)
        {
            Logging.Message("pasting to building ", buildingID);

            // Saftey checks.
            if (buildingID == 0 || buildingInfo == null)
            {
                Logging.Error("invalid parameter passed to CopyPaste.Paste");
                return false;
            }

            // Determine length of target building transfer buffer.
            int length = TransferDataUtils.BuildingEligibility(buildingID, buildingInfo, transferBuffer);

            // Check for a length match between buffer and target.
            if (length != bufferSize)
            {
                Logging.Message("copy-paste buffer size mismatch");
                return false;
            }

            // Check for record type (incoming/outoging) match between buffer and target.
            for (int i = 0; i < length; ++i)
            {
                if (transferBuffer[i].recordNumber != copyRecordNumbers[i])
                {
                    Logging.Message("copy-paste record type mismatch");
                    return false;
                }
            }

            // All checks passed - copy records from buffer to building.
            for (int i = 0; i < length; ++i)
            {
                // New building entry.
                BuildingControl.BuildingRecord newRecord = new BuildingControl.BuildingRecord
                {
                    nextRecord = transferBuffer[i].nextRecord,
                    flags = copyBuffer[i].flags,
                    reason = transferBuffer[i].reason
                };

                // Copy district and building buffers to new HashSets, otherwise the old ones will be shared.
                if (copyBuffer[i].districts != null)
                {
                    newRecord.districts = new HashSet<int>(copyBuffer[i].districts);
                }
                if (copyBuffer[i].buildings != null)
                {
                    newRecord.buildings = new HashSet<uint>(copyBuffer[i].buildings);
                }

                // Calculate target record ID.
                uint mask = (uint)transferBuffer[i].recordNumber << 24;
                uint buildingRecordID = (uint)(buildingID | mask);

                // Do we already have an entry for this building?
                if (BuildingControl.buildingRecords.ContainsKey(buildingRecordID))
                {
                    // Yes - replace existing entry with the new one.
                    BuildingControl.buildingRecords[buildingRecordID] = newRecord;
                }
                else
                {
                    // No - create new entry.
                    BuildingControl.buildingRecords.Add(buildingRecordID, newRecord);
                }
            }

            // If we got here, then pasting was successful.
            return true;
        }
    }
}
