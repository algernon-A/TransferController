using System.Collections.Generic;


namespace TransferController
{
    /// <summary>
    /// Match status code.
    /// </summary>
    public enum MatchStatus : byte
    {
        None = 0,
        Blocked,
        PathFailure,
        NoVehicle,
        Eligible,
        Selected
    }


    /// <summary>
    /// Transfer log entry struct.
    /// </summary>
    public struct LogEntry
    {
        public TransferManager.TransferReason reason;
        public bool incoming;
        public byte priorityIn, priorityOut;
        public ushort inBuilding, outBuilding;
        public MatchStatus status;
        public bool inExcluded, outExcluded;
        public UnityEngine.Vector3 incomingPos, outgoingPos;
    }


    /// <summary>
    /// Static class to manage tranfser log.
    /// </summary>
    internal static class TransferLogging
    {
        /// Log is a circular buffer of logentries.
        internal static LogEntry[] log = new LogEntry[131072];
        internal static uint logIndex = 0;


        /// <summary>
        /// Add a new logging entry.
        /// </summary>
        /// <param name="reason">Transfer reason</param>
        /// <param name="incoming">True if the offer is incoming, false otherwise</param>
        /// <param name="priorityIn">Incoming offer priority</param
        /// <param name="priorityOut">Outgoing offer priority</param
        /// <param name="inBuilding">Incoming building ID</param>
        /// <param name="outBuilding">Outgoing building ID</param>
        /// <param name="status">Match status</param>
        /// <param name="inExcluded">Incoming offer excluded flag</param>
        /// <param name="outExcluded">Outgoing offer excluded flag</param>
        /// <param name="inPos">Incoming offer position</param>
        /// <param name="outPos">Outgoing offer position</param>
        internal static void AddEntry(TransferManager.TransferReason reason, bool incoming, int priorityIn, int priorityOut, ushort inBuilding, ushort outBuilding, MatchStatus status, bool inExcluded, bool outExcluded, UnityEngine.Vector3 inPos, UnityEngine.Vector3 outPos)
        {
            // Add new log entry with provided data and increment log index pointer.
            log[logIndex++] = new LogEntry
            {
                reason = reason,
                incoming = incoming,
                priorityIn = (byte)priorityIn,
                priorityOut = (byte)priorityOut,
                inBuilding = inBuilding,
                outBuilding = outBuilding,
                status = status,
                inExcluded = inExcluded,
                outExcluded = outExcluded,
                incomingPos = inPos,
                outgoingPos = outPos
            };

            // Reset log index if we've reached the end of the buffer.
            if (logIndex >= log.Length)
            {
                logIndex = 0;
                Logging.Message("logging buffer wrapped");
            }
        }


        /// <summary>
        /// Returns a list of MatchData instances representing the current log, applying specified filters.
        /// </summary>
        /// <param name="buildingID">Building ID to match (0 for none)</param>
        /// <param name="showBlocked">True to show blocked transfers</param>
        /// <param name="showPathFail">True to show transfers blocked due to recent pathfinding fail</param>
        /// <param name="showNoVehicles">True to show transfers with no available vehicles</param>
        /// <param name="showEligibile">True to show eligible transfers</param>
        /// <param name="showSelected">True to show selected transfers</param>
        /// <param name="showIn">True to show incoming transfers</param>
        /// <param name="showOut">True to show outgoing transfers</param>
        /// <returns>List of MatchData instances representing the current log, filtered by parameters</returns>
        internal static List<MatchData> EntryList(ushort buildingID, bool showBlocked, bool showPathFail, bool showNoVehicles, bool showEligibile,  bool showSelected, bool showIn, bool showOut)
        {
            List<MatchData> returnList = new List<MatchData>(log.Length);

            // Iterate through log starting at current position and wrapping around.
            for (uint i = logIndex + 1; i != logIndex; ++i)
            {
                // Check for index wrap.
                if (i >= log.Length)
                {
                    i = 0;
                }

                // Apply filters.
                LogEntry thisEntry = log[i];
                bool thisBuildingIn = buildingID == 0 ? thisEntry.incoming : thisEntry.inBuilding == buildingID;
                if ((thisEntry.inBuilding != 0 | thisEntry.outBuilding != 0)
                    && (buildingID == 0 | thisEntry.inBuilding == buildingID | thisEntry.outBuilding == buildingID)
                    && ((showBlocked & thisEntry.status == MatchStatus.Blocked) |
                        (showPathFail & thisEntry.status == MatchStatus.PathFailure) |
                        (showNoVehicles & thisEntry.status == MatchStatus.NoVehicle) |
                        (showEligibile & thisEntry.status == MatchStatus.Eligible) |
                        (showSelected & thisEntry.status == MatchStatus.Selected))
                    && ((showIn & thisBuildingIn) | (showOut & !thisBuildingIn)))
                {
                    // Add entry to list.
                    returnList.Add(new MatchData(buildingID, thisEntry.reason, thisEntry.priorityIn, thisEntry.priorityOut, thisEntry.inExcluded, thisEntry.outExcluded, thisEntry.inBuilding, thisEntry.outBuilding, thisEntry.incomingPos, thisEntry.outgoingPos, thisEntry.status));
                }
            }

            return returnList;
        }
    }
}