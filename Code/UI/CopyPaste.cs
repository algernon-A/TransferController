// <copyright file="CopyPaste.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;

    /// <summary>
    /// Handles copying and pasting of building settings.
    /// </summary>
    public static class CopyPaste
    {
        // Copy buffer.
        private static bool s_isCopied = false;
        private static int s_bufferSize;
        private static BuildingControl.BuildingRecord[] s_copyBuffer = new BuildingControl.BuildingRecord[BuildingPanel.MaxTransfers];
        private static bool[] s_copyIncoming = new bool[BuildingPanel.MaxTransfers];
        private static TransferManager.TransferReason[] s_copyReason = new TransferManager.TransferReason[BuildingPanel.MaxTransfers];

        // Copy buffer - warehouse extensions.
        private static bool s_isWarehouse = false;
        private static WarehouseRecord s_warehouseRecord;

        // Prevent heap allocations every time we copy.
        private static TransferStruct[] transferBuffer = new TransferStruct[BuildingPanel.MaxTransfers];

        /// <summary>
        /// Copies TC data from the given building to the copy buffer.
        /// </summary>
        /// <param name="buildingID">Source building ID.</param>
        /// <param name="buildingInfo">Source building info.</param>
        internal static void Copy(ushort buildingID, BuildingInfo buildingInfo)
        {
            Logging.Message("copying from building ", buildingID);

            // Saftey checks.
            if (buildingID == 0 || buildingInfo == null)
            {
                Logging.Error("invalid parameter passed to CopyPaste.Copy");
                return;
            }

            // Number of records to copy - make sure there's at least one before proceeding.
            int length = TransferDataUtils.BuildingEligibility(buildingID, buildingInfo, transferBuffer);
            s_bufferSize = length;

            // Make sure there's at least one tranfer before proceeding.
            if (length > 0)
            {
                // Clear copied flag (it will be set later if valid data was copied).
                s_isCopied = false;

                // Copy warehouse settings, if any.
                s_isWarehouse = buildingInfo.m_buildingAI is WarehouseAI && WarehouseControl.TryGetRecord(buildingID, out s_warehouseRecord);

                // Copy records from source building to buffer.
                for (int i = 0; i < length; ++i)
                {
                    // Calculate building record ID.
                    uint buildingRecordID = BuildingControl.CalculateEntryKey(buildingID, transferBuffer[i].isIncoming, transferBuffer[i].reason);

                    // Set data.
                    s_copyIncoming[i] = transferBuffer[i].isIncoming;
                    s_copyReason[i] = transferBuffer[i].reason;

                    // Try to get valid entry, outputting to the copy buffer.
                    if (BuildingControl.s_buildingRecords.TryGetValue(buildingRecordID, out s_copyBuffer[i]))
                    {
                        // Copy district and building buffers to new HashSets, otherwise the old ones will be shared.
                        if (s_copyBuffer[i].Districts != null)
                        {
                            s_copyBuffer[i].Districts = new HashSet<int>(s_copyBuffer[i].Districts);
                        }

                        if (s_copyBuffer[i].Buildings != null)
                        {
                            s_copyBuffer[i].Buildings = new HashSet<uint>(s_copyBuffer[i].Buildings);
                        }

                        s_isCopied = true;
                    }
                    else
                    {
                        // If no valid entry for this record, clear the buffer entry.
                        s_copyBuffer[i] = default;
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to paste TC data from the copy buffer to the given building.
        /// </summary>
        /// <param name="buildingID">Source building ID.</param>
        internal static bool Paste(ushort buildingID) => Paste(buildingID, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info);


        /// <summary>
        /// Attempts to paste TC data from the copy buffer to the given building.
        /// </summary>
        /// <param name="buildingID">Source building ID</param>
        /// <param name="buildingInfo">Source building info</param>
        /// <returns>True if copy was successful, false otherwise</returns>
        internal static bool Paste(ushort buildingID, BuildingInfo buildingInfo)
        {
            // Don't do anything if there's no active copy data.
            if (!s_isCopied)
            {
                return false;
            }

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
            if (length != s_bufferSize)
            {
                Logging.Message("copy-paste buffer size mismatch");
                return false;
            }

            // Check for record type (incoming/outoging) match between buffer and target.
            for (int i = 0; i < length; ++i)
            {
                if (transferBuffer[i].isIncoming != s_copyIncoming[i])
                {
                    Logging.Message("copy-paste direction mismatch");
                    return false;
                }
            }

            // All checks passed - copy records from buffer to building.
            for (int i = 0; i < length; ++i)
            {
                // Create new building entry from copied data and update dictionary.
                BuildingControl.BuildingRecord newRecord = new BuildingControl.BuildingRecord
                {
                    Flags = s_copyBuffer[i].Flags,
                    Districts = s_copyBuffer[i].Districts,
                    Buildings = s_copyBuffer[i].Buildings,
                };

                // Apply pasted data.
                BuildingControl.UpdateRecord(buildingID, s_copyIncoming[i], transferBuffer[i].reason, ref newRecord);
            }

            // Paste warehouse info, if applicable.
            if (buildingInfo.m_buildingAI is WarehouseAI && s_isWarehouse)
            {
                WarehouseControl.UpdateRecord(buildingID, s_warehouseRecord);
            }

            // If we got here, then pasting was successful.
            return true;
        }
    }
}
