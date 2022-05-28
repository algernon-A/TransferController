using ICities;


namespace TransferController
{
    /// <summary>
    /// Building extension method class.  Used to handle deletion of buildings with active settings.
    /// </summary>
    public class BuildingExtension : BuildingExtensionBase
    {
        /// <summary>
        /// Checks to see if a released building has a custom settting, and if so, removes that setting.
        /// Called by the game when a building instance is released.
        /// </summary>
        /// <param name="id">Building instance ID</param>
        public override void OnBuildingReleased(ushort id) => BuildingControl.ReleaseBuilding(id);
    }
}
