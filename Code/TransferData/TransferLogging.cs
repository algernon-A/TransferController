// <copyright file="TransferLogging.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;
    using UnityEngine;

    /// <summary>
    /// Static class to manage tranfser log.
    /// </summary>
    public static class TransferLogging
    {
        // Log is a circular buffer of logentries.
        private static LogEntry[] s_log = new LogEntry[131072];
        private static uint s_logIndex = 0;

        /// <summary>
        /// Match status code.
        /// </summary>
        public enum MatchStatus : byte
        {
            /// <summary>
            /// No status.
            /// </summary>
            None = 0,

            /// <summary>
            /// The match was blocked because the incoming building's restrictions prevented it.
            /// </summary>
            NotPermittedIn,

            /// <summary>
            /// The match was blocked because the ougoing building's restrictions prevented it.
            /// </summary>
            NotPermittedOut,

            /// <summary>
            /// The match was an export blocked by outgoing building's export restrictions.
            /// </summary>
            ExportBlocked,

            /// <summary>
            /// The match was an import blocked by incoming building's import restrictions.
            /// </summary>
            ImportBlocked,

            /// <summary>
            /// The match was blocked due to a recent pathfinding failure.
            /// </summary>
            PathFailure,

            /// <summary>
            /// The match was blocked because no unreserved vehicles were available.
            /// </summary>
            NoVehicle,

            /// <summary>
            /// The match was eligible, but may or may not have been overridden by a closer match.
            /// </summary>
            Eligible,

            /// <summary>
            /// The match was selected for transfer.
            /// </summary>
            Selected,
        }

        /// <summary>
        /// Add a new logging entry.
        /// </summary>
        /// <param name="reason">Transfer reason.</param>
        /// <param name="incoming">True if the offer is incoming, false otherwise.</param>
        /// <param name="priorityIn">Incoming offer priority.</param>
        /// <param name="priorityOut">Outgoing offer priority.</param>
        /// <param name="inBuilding">Incoming building ID.</param>
        /// <param name="outBuilding">Outgoing building ID.</param>
        /// <param name="status">Match status.</param>
        /// <param name="inExcluded">Incoming offer excluded flag.</param>
        /// <param name="outExcluded">Outgoing offer excluded flag.</param>
        /// <param name="inPos">Incoming offer position.</param>
        /// <param name="outPos">Outgoing offer position.</param>
        internal static void AddEntry(TransferManager.TransferReason reason, bool incoming, int priorityIn, int priorityOut, ushort inBuilding, ushort outBuilding, MatchStatus status, bool inExcluded, bool outExcluded, UnityEngine.Vector3 inPos, UnityEngine.Vector3 outPos)
        {
            // Add new log entry with provided data and increment log index pointer.
            s_log[s_logIndex++] = new LogEntry
            {
                Reason = reason,
                Incoming = incoming,
                PriorityIn = (byte)priorityIn,
                PriorityOut = (byte)priorityOut,
                InBuilding = inBuilding,
                OutBuilding = outBuilding,
                Status = status,
                InExcluded = inExcluded,
                OutExcluded = outExcluded,
                IncomingPos = inPos,
                OutgoingPos = outPos,
                TimeStamp = Singleton<SimulationManager>.instance.m_simulationTimer2,
            };

            // Reset log index if we've reached the end of the buffer.
            if (s_logIndex >= s_log.Length)
            {
                s_logIndex = 0;
                Logging.Message("logging buffer wrapped");
            }
        }

        /// <summary>
        /// Returns a list of MatchData instances representing the current log, applying specified filters.
        /// </summary>
        /// <param name="buildingID">Building ID to match (0 for none).</param>
        /// <param name="showBlocked">True to show blocked transfers.</param>
        /// <param name="showPathFail">True to show transfers blocked due to recent pathfinding fail.</param>
        /// <param name="showNoVehicles">True to show transfers with no available vehicles.</param>
        /// <param name="showEligibile">True to show eligible transfers.</param>
        /// <param name="showSelected">True to show selected transfers.</param>
        /// <param name="showIn">True to show incoming transfers.</param>
        /// <param name="showOut">True to show outgoing transfers.</param>
        /// <returns>List of MatchData instances representing the current log, filtered by parameters.</returns>
        internal static List<MatchItem> EntryList(ushort buildingID, bool showBlocked, bool showPathFail, bool showNoVehicles, bool showEligibile, bool showSelected, bool showIn, bool showOut)
        {
            List<MatchItem> returnList = new List<MatchItem>(s_log.Length);

            // Iterate through log starting at current position and wrapping around.
            uint i = s_logIndex + 1;
            while (i != s_logIndex)
            {
                // Apply filters.
                LogEntry thisEntry = s_log[i];
                bool thisBuildingIn = buildingID == 0 ? thisEntry.Incoming : thisEntry.InBuilding == buildingID;
                if ((thisEntry.InBuilding != 0 | thisEntry.OutBuilding != 0)
                    && (buildingID == 0 | thisEntry.InBuilding == buildingID | thisEntry.OutBuilding == buildingID)
                    && ((showBlocked & (thisEntry.Status == MatchStatus.NotPermittedOut | thisEntry.Status == MatchStatus.NotPermittedIn | thisEntry.Status == MatchStatus.ExportBlocked | thisEntry.Status == MatchStatus.ImportBlocked)) |
                        (showPathFail & thisEntry.Status == MatchStatus.PathFailure) |
                        (showNoVehicles & thisEntry.Status == MatchStatus.NoVehicle) |
                        (showEligibile & thisEntry.Status == MatchStatus.Eligible) |
                        (showSelected & thisEntry.Status == MatchStatus.Selected))
                    && ((showIn & thisBuildingIn) | (showOut & !thisBuildingIn)))
                {
                    // Add entry to list.
                    returnList.Add(new MatchItem(buildingID, thisEntry.Reason, thisEntry.PriorityIn, thisEntry.PriorityOut, thisEntry.InExcluded, thisEntry.OutExcluded, thisEntry.InBuilding, thisEntry.OutBuilding, thisEntry.IncomingPos, thisEntry.OutgoingPos, thisEntry.Status, thisEntry.TimeStamp));
                }

                // Increament i.
                ++i;

                // Check for index wrap.
                if (i >= s_log.Length)
                {
                    // Index wrap; reset i.
                    i = 0;
                }
            }

            return returnList;
        }

        /// <summary>
        /// Transfer log entry struct.
        /// </summary>
        public struct LogEntry
        {
            /// <summary>
            /// Transfer reason.
            /// </summary>
            public TransferManager.TransferReason Reason;

            /// <summary>
            /// True if the transfer was matched from the incoming offer, false if matched from the outgoing offer.
            /// </summary>
            public bool Incoming;

            /// <summary>
            /// Incoming offer priority.
            /// </summary>
            public byte PriorityIn;

            /// <summary>
            /// Outgoing offer priority.
            /// </summary>
            public byte PriorityOut;

            /// <summary>
            /// Incoming building ID.
            /// </summary>
            public ushort InBuilding;

            /// <summary>
            /// Outgoing building ID.
            /// </summary>
            public ushort OutBuilding;

            /// <summary>
            /// Match result status.
            /// </summary>
            public MatchStatus Status;

            /// <summary>
            /// True if the incoming building was a warehouse.
            /// </summary>
            public bool InExcluded;

            /// <summary>
            /// True if the outgoing building was a warehouse.
            /// </summary>
            public bool OutExcluded;

            /// <summary>
            /// Imcoming offer position.
            /// </summary>
            public Vector3 IncomingPos;

            /// <summary>
            /// Outgoing offer position.
            /// </summary>
            public Vector3 OutgoingPos;

            /// <summary>
            /// Match timestamp.
            /// </summary>
            public float TimeStamp;
        }
    }
}