// <copyright file="BuildingExtension.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using ICities;

    /// <summary>
    /// Building extension method class.  Used to handle deletion of buildings with active settings.
    /// </summary>
    public class BuildingExtension : BuildingExtensionBase
    {
        /// <summary>
        /// Called by the game when a building instance is released.
        /// Used to clear data records relating to the released building.
        /// </summary>
        /// <param name="id">Building instance ID.</param>
        public override void OnBuildingReleased(ushort id)
        {
            BuildingControl.ReleaseBuilding(id);
            PathFindFailure.ReleaseBuilding(id);
            VehicleControl.ReleaseBuilding(id);
        }
    }
}
