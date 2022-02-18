using System;
using System.Collections.Generic;


namespace TransferController
{
    /// <summary>
    /// Transfer log entry struct.
    /// </summary>
    public struct LogEntry
    {
        /// <summary>
        /// Transfer blocking reason enum/
        /// </summary>
        public enum BlockReason : ushort
        {
            None = 0,
            IncomingDistrict = 0x01,
            OutgoingDistrict = 0x02
        }

        public TransferManager.TransferReason reason;
        public bool incoming;
        public uint inBuilding;
        public uint outBuilding;
        public bool allowed;
        public BlockReason blockedReason;
    }


    /// <summary>
    /// Static class to manage tranfser log.
    /// </summary>
    internal static class TransferLogging
    {
        /// Log is a circular buffer of logentries.
        internal static LogEntry[] log = new LogEntry[65536];
        internal static ushort logIndex = 0;


        /// <summary>
        /// Add a new logging entry.
        /// </summary>
        /// <param name="reason">Transfer reason</param>
        /// <param name="incoming">True if the offer is incoming, false otherwise</param>
        /// <param name="inBuilding">Incoming building ID</param>
        /// <param name="outBuilding">Outgoing building ID</param>
        /// <param name="allowed">True if the transfer was allowed, false if blocked</param>
        /// <param name="blockedReason">Reason for transfer being blocked</param>
        internal static void AddEntry(TransferManager.TransferReason reason, bool incoming, uint inBuilding, uint outBuilding, bool allowed, LogEntry.BlockReason blockedReason)
        {
            // Add new log entry with provided data and increment log index pointer.
            log[logIndex++] = new LogEntry
            {
                reason = reason,
                incoming = incoming,
                inBuilding = inBuilding,
                outBuilding = outBuilding,
                allowed = allowed,
                blockedReason = blockedReason
            };
        }


        /// <summary>
        /// Returns a list of strings representing the current log, applying specified filters.
        /// </summary>
        /// </summary>
        /// <param name="buildingID">Building ID to match (0 for none)</param>
        /// <param name="showBlocked">True to show blocked transfers</param>
        /// <param name="showAllowed">True to show allowed transfers</param>
        /// <param name="showIn">True to show incoming transfers</param>
        /// <param name="showOut">True to show outgoing transfers</param>
        /// <returns>List of strings representing the current log, filtered by parameters</returns>
        internal static List<string> EntryList(ushort buildingID, bool showBlocked, bool showAllowed, bool showIn, bool showOut)
        {
            List<string> returnList = new List<string>(log.Length);

            // Iterate through log starting at current position and wrapping around.
            for (ushort i = (ushort)(logIndex + 1); i != logIndex; ++i)
            {
                // Apply filters.
                LogEntry thisEntry = log[i];
                if ((thisEntry.inBuilding != 0 || thisEntry.outBuilding != 0)
                    && (buildingID == 0 | thisEntry.inBuilding == buildingID | thisEntry.outBuilding == buildingID)
                    && ((showBlocked && !thisEntry.allowed) | (showAllowed && thisEntry.allowed))
                    && ((showIn && thisEntry.incoming) | (showOut && !thisEntry.incoming)))
                {
                    returnList.Add(String.Format("{0} {1}: {2}-{3}: {4} {5}", thisEntry.reason, thisEntry.incoming ? "In" : "Out", thisEntry.inBuilding, thisEntry.outBuilding, thisEntry.allowed ? "Allow" : "Block", thisEntry.blockedReason));
                }
            }

            return returnList;
        }
    }
}