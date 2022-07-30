using AlgernonCommons;
using ColossalFramework.UI;
using System;
using UnityEngine;


namespace TransferController
{
    /// <summary>
    /// Detail section of building status panel.
    /// </summary>
    internal abstract class StatusPanelSection : UpdatingBuildingPanel
    {
        // Layout constants.
        protected const float Margin = 5f;
        protected const float ScrollBarWidth = 20f;
        protected const float ListHeaderY = 25f;
        protected const float ListY = ListHeaderY + 15f;

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