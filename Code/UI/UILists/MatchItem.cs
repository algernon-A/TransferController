// <copyright file="MatchItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using UnityEngine;

    /// <summary>
    /// Class to hold match data from logged transfer matches.
    /// </summary>
    public class MatchItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatchItem"/> class.
        /// </summary>
        /// <param name="reason">Transfer reason.</param
        /// <param name="buildingID">Building ID of this building.</param>
        /// <param name="incomingPriority">Incoming offer priority.</param>
        /// <param name="outgoingPriority">Outgoing offer priority.</param>
        /// <param name="incomingExcluded">Incoming offer exclusion status.</param>
        /// <param name="outgoingExcluded">Outgoing offer exclusion status.</param>
        /// <param name="incomingBuildingID">Incoming building ID.</param>
        /// <param name="outgoingBuildingID">Outgoing building ID.</param>
        /// <param name="incomingPos">Incoming offer position.</param>
        /// <param name="outgoingPos">Outgoing offer position.</param>
        /// <param name="timeStamp">Match framecount timetamp.</param>
        /// <param name="status">Match status.</param>
        public MatchItem(
            ushort buildingID,
            TransferManager.TransferReason reason,
            byte incomingPriority,
            byte outgoingPriority,
            bool incomingExcluded,
            bool outgoingExcluded,
            ushort incomingBuildingID,
            ushort outgoingBuildingID,
            Vector3 incomingPos,
            Vector3 outgoingPos,
            TransferLogging.MatchStatus status,
            float timeStamp)
        {
            BuildingID = buildingID;
            Reason = reason;
            IncomingPriority = incomingPriority;
            OutgoingPriority = outgoingPriority;
            IncomingExcluded = incomingExcluded;
            OutgoingExcluded = outgoingExcluded;
            IncomingBuildingID = incomingBuildingID;
            OutgoingBuildingID = outgoingBuildingID;
            IncomingPos = incomingPos;
            OutgoingPos = outgoingPos;
            Status = status;
            TimeStamp = timeStamp;
        }

        /// <summary>
        /// Gets the match's tranfer reason.
        /// </summary>
        public TransferManager.TransferReason Reason { get; private set; }

        /// <summary>
        /// Gets the match's incoming priority.
        /// </summary>
        public byte IncomingPriority { get; private set; }

        /// <summary>
        /// Gets the match's outgoing priority.
        /// </summary>
        public byte OutgoingPriority { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the transfer was initiated by the incoming (true) or outgoing (false) partners.
        /// </summary>
        public bool Incoming { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the incoming building was a warehouse.
        /// </summary>
        public bool IncomingExcluded { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the outgoing building was a warehouse.
        /// </summary>
        public bool OutgoingExcluded { get; private set; }

        /// <summary>
        /// Gets this building's ID.
        /// </summary>
        public ushort BuildingID { get; private set; }

        /// <summary>
        /// Gets the ID of the incoming building.
        /// </summary>
        public ushort IncomingBuildingID { get; private set; }

        /// <summary>
        /// Gets the ID of the outgoing building.
        /// </summary>
        public ushort OutgoingBuildingID { get; private set; }

        /// <summary>
        /// Gets the incoming offer position.
        /// </summary>
        public Vector3 IncomingPos { get; private set; }

        /// <summary>
        /// Gets the outgoing offer position.
        /// </summary>
        public Vector3 OutgoingPos { get; private set; }

        /// <summary>
        /// Gets the match status.
        /// </summary>
        public TransferLogging.MatchStatus Status { get; private set; }

        /// <summary>
        /// Gets the match timestamp.
        /// </summary>
        public float TimeStamp { get; private set; }
    }
}