using ICities;


namespace TransferController
{
    /// <summary>
    /// Building extension method class.  Used to handle deletion of buildings with active settings.
    /// </summary>
    public class BuildingExtension : BuildingExtensionBase
    {
        /// <summary>
        /// Called by the game when a building instance is released.
        /// Used to clear data records relating to the released building.
        /// </summary>
        /// <param name="id">Building instance ID</param>
        public override void OnBuildingReleased(ushort id)
        {
            BuildingControl.ReleaseBuilding(id);
            PathFindFailure.ReleaseBuilding(id);
        }
    }
}
