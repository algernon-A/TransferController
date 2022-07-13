using System;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Detail section of building status panel.
    /// </summary>
    internal abstract class StatusPanelSection : UIPanel
    {
        // Layout constants.
        protected const float Margin = 5f;
        protected const float ScrollBarWidth = 20f;
        protected const float ListHeaderY = 25f;
        protected const float ListY = ListHeaderY + 15f;

        // Current selection.
        protected ushort currentBuilding;

        // Timer.
        private float ticks;

        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal StatusPanelSection()
        {
            try
            {
                // Basic setup.
                autoLayout = false;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up status panel section");
            }
        }


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
        internal void SetTarget(ushort buildingID)
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


        /// <summary>
        /// Adds a UI fastlist of the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="yPos">Relative Y position</param>
        /// <param name="listWidth">List width</param>
        /// <param name="listHeight">List height</param>
        /// <returns>New UIFastList</returns>
        protected UIFastList AddList<T>(float yPos, float listWidth, float listHeight) where T : UIPanel, IUIFastListRow
        {
            // List setup.
            UIFastList newList = UIFastList.Create<T>(this);
            newList.backgroundSprite = "UnlockingPanel";
            newList.width = listWidth;
            newList.height = listHeight;
            newList.canSelect = true;
            newList.rowHeight = StatusRow.RowHeight;
            newList.autoHideScrollbar = true;
            newList.relativePosition = new Vector2(Margin, yPos);
            newList.rowsData = new FastList<object>();
            newList.selectedIndex = -1;

            return newList;
        }
    }
}