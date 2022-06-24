using System;
using System.Text;
using System.Collections.Generic;


namespace TransferController
{
    /// <summary>
    /// Transfer log entry struct.
    /// </summary>
    public struct LogEntry
    {
        public TransferManager.TransferReason reason;
        public bool incoming;
        public byte priorityIn, priorityOut;
        public ushort inBuilding, outBuilding;
        public bool allowed, inExcluded, outExcluded;
        public UnityEngine.Vector3 incomingPos, outgoingPos;
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
        /// <param name="inExcluded">Incoming offer excluded flag</param>
        /// <param name="outExcluded">Outgoing offer excluded flag</param>
        /// <param name="inPos">Incoming offer position</param>
        /// <param name="outPos">Outgoing offer position</param>
        internal static void AddEntry(TransferManager.TransferReason reason, bool incoming, byte priorityIn, byte priorityOut, ushort inBuilding, ushort outBuilding, bool allowed, bool inExcluded, bool outExcluded, UnityEngine.Vector3 inPos, UnityEngine.Vector3 outPos)
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
                inExcluded = inExcluded,
                outExcluded = outExcluded,
                incomingPos = inPos,
                outgoingPos = outPos
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
                bool thisBuildingIn = buildingID == 0 ? thisEntry.incoming : thisEntry.inBuilding == buildingID;
                if ((thisEntry.inBuilding != 0 | thisEntry.outBuilding != 0)
                    && (buildingID == 0 | thisEntry.inBuilding == buildingID | thisEntry.outBuilding == buildingID)
                    && ((showBlocked & !thisEntry.allowed) | (showAllowed & thisEntry.allowed))
                    && ((showIn & thisBuildingIn) | (showOut & !thisBuildingIn)))
                {
                    ushort otherBuilding;
                    UnityEngine.Vector3 thisPos;

                    // Format display string.
                    StringBuilder displayText = new StringBuilder();
                    displayText.Append(thisEntry.reason);
                    if (thisBuildingIn)
                    {
                        displayText.Append(" in");
                        if (thisEntry.incoming)
                        {
                            displayText.Append('*');
                        }
                    }
                    else
                    {
                        displayText.Append(" out");
                        if (!thisEntry.incoming)
                        {
                            displayText.Append('*');
                        }
                    }
                    displayText.Append(' ');
                    displayText.Append(thisEntry.priorityIn);
                    displayText.Append('-');

                    // Warehouse flags (excluded flag).
                    if (thisEntry.inExcluded)
                    {
                        displayText.Append('W');
                    }
                    displayText.Append(thisEntry.priorityOut);
                    if (thisEntry.outExcluded)
                    {
                        displayText.Append('W');
                    }

                    displayText.Append(": ");
                    if (thisBuildingIn)
                    {
                        otherBuilding = thisEntry.outBuilding;
                        thisPos = thisEntry.outgoingPos;
                    }
                    else
                    {
                        otherBuilding = thisEntry.inBuilding;
                        thisPos = thisEntry.incomingPos;
                    }
                    displayText.Append(otherBuilding);
                    displayText.Append(": ");
                    displayText.Append(thisEntry.allowed ? "Allowed" : "Blocked");

                    // Add entry to list.
                    returnList.Add(new OfferData(displayText.ToString(), otherBuilding, thisPos));
                }
            }

            return returnList;
        }
    }
}