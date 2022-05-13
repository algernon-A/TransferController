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
        public byte priorityIn, priorityOut;
        public ushort inBuilding, outBuilding;
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
		/// <param name="priorityIn">Incoming offer priority</param
		/// <param name="priorityOut">Outgoing offer priority</param
        /// <param name="inBuilding">Incoming building ID</param>
        /// <param name="outBuilding">Outgoing building ID</param>
        /// <param name="allowed">True if the transfer was allowed, false if blocked</param>
        /// <param name="blockedReason">Reason for transfer being blocked</param>
        internal static void AddEntry(TransferManager.TransferReason reason, bool incoming, byte priorityIn, byte priorityOut, ushort inBuilding, ushort outBuilding, bool allowed, LogEntry.BlockReason blockedReason)
        {
            // Add new log entry with provided data and increment log index pointer.
            log[logIndex++] = new LogEntry
            {
                reason = reason,
                incoming = incoming,
                priorityIn = priorityIn,
                priorityOut = priorityOut,
                inBuilding = inBuilding,
                outBuilding = outBuilding,
                allowed = allowed,
                blockedReason = blockedReason
            };
        }


        /// <summary>
        /// Returns a list of OfferData instances representing the current log, applying specified filters.
        /// </summary>
        /// </summary>
        /// <param name="buildingID">Building ID to match (0 for none)</param>
        /// <param name="showBlocked">True to show blocked transfers</param>
        /// <param name="showAllowed">True to show allowed transfers</param>
        /// <param name="showIn">True to show incoming transfers</param>
        /// <param name="showOut">True to show outgoing transfers</param>
        /// <returns>List of OfferData instances representing the current log, filtered by parameters</returns>
        internal static List<OfferData> EntryList(ushort buildingID, bool showBlocked, bool showAllowed, bool showIn, bool showOut)
        {
            List<OfferData> returnList = new List<OfferData>(log.Length);

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
                    bool thisBuildingIn = thisEntry.inBuilding == buildingID;
                    string inString = "In";
                    string outString = "Out";
                    if (thisEntry.incoming)
                    {
                        inString += "*";
                    }
                    else
                    {
                        outString += "*";
                    }

                    returnList.Add(new OfferData(String.Format("{0} {1} {2}-{3}: {4}-{5}: {6}",
                        thisEntry.reason,
                        thisBuildingIn ? inString : outString,
                        thisEntry.priorityIn,
                        thisEntry.priorityOut,
                        thisEntry.inBuilding,
                        thisEntry.outBuilding,
                        thisEntry.allowed ? "Allowed" : "Blocked - " + thisEntry.blockedReason),
                        thisBuildingIn ? thisEntry.outBuilding : thisEntry.inBuilding));
                }
            }

            return returnList;
        }
    }
}