// <copyright file="PathFailItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using ColossalFramework;

    /// <summary>
    /// Class to hold data from pathfinding fails.
    /// </summary>
    public class PathFailItem
    {
        // Pathfail data.
        private ushort _buildingID;
        private bool _incoming;
        private string _buildingName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathFailItem"/> class.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <param name="incoming">True if the failure was from incoming to outgoing, false otherwise.</param>
        public PathFailItem(ushort buildingID, bool incoming)
        {
            _buildingID = buildingID;
            _incoming = incoming;

            // Get target building name.
            ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];
            if (buildingID != 0 && (building.m_flags & Building.Flags.Created) != 0 && building.Info != null)
            {
                _buildingName = Singleton<BuildingManager>.instance.GetBuildingName(buildingID, InstanceID.Empty);
            }
            else
            {
                // Invalid building.
                _buildingName = string.Empty;
            }
        }

        /// <summary>
        /// Gets the row's building ID.
        /// </summary>
        public ushort BuildingID => _buildingID;

        /// <summary>
        /// Gets a value indicating whether the transfer was incoming (true) or outgoing (false).
        /// </summary>
        public bool IsIncoming => _incoming;

        /// <summary>
        /// Gets the row's building name.
        /// </summary>
        public string BuildingName => _buildingName;
    }
}