// <copyright file="BuildingItem.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using ColossalFramework;

    /// <summary>
    /// Building list item record.
    /// </summary>
    public class BuildingItem
    {
        // Building data.
        private ushort _buildingID;
        private string _buildingName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingItem"/> class.
        /// </summary>
        /// <param name="id">Building ID for this item.</param>
        public BuildingItem(ushort id)
        {
            ID = id;
        }

        /// <summary>
        /// Gets the building's name (empty string if none).
        /// </summary>
        public string Name => _buildingName;

        /// <summary>
        /// Gets or sets the building ID for this record.
        /// </summary>
        public ushort ID
        {
            get => _buildingID;

            set
            {
                _buildingID = value;

                // Local reference.
                ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[_buildingID];

                // Check for valid entry.
                if (value != 0 && (building.m_flags & Building.Flags.Created) != 0 && building.Info != null)
                {
                    _buildingName = Singleton<BuildingManager>.instance.GetBuildingName(_buildingID, InstanceID.Empty);
                }
                else
                {
                    // Invalid building.
                    _buildingName = string.Empty;
                }
            }
        }
    }
}