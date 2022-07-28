using ColossalFramework.UI;
using UnityEngine;


namespace TransferController
{
    /// <summary>
    /// UI panel which regenerates content every second.
    /// </summary>
    public abstract class UpdatingBuildingPanel : UIPanel
    {
        // Timer.
        private float ticks;

        // Current selection.
        protected ushort currentBuilding;


        /// <summary>
        /// Called by Unity every update.
        /// Used to refresh the list periodically.
        /// </summary>
        public override void Update()
        {
            base.Update();

            ticks += Time.deltaTime;

            // Refresh every second.
            if (ticks > 1)
            {
                UpdateContent();
                ticks = 0f;
            }
        }


        /// <summary>
        /// Sets the target to the selected building.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            // Set target building and regenerate the list.
            currentBuilding = buildingID;
            UpdateContent();

            // Reset timer.
            ticks = 0f;
        }


        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected abstract void UpdateContent();
    }
}