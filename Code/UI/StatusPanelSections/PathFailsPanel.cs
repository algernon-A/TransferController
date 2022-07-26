using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace TransferController
{
    /// <summary>
    /// Panel to show pathfinding failures.
    /// </summary>
    internal class PathFailsPanel : StatusPanelSection
    {
        // Layout constants.
        internal const float PanelWidth = PathFailRow.RowWidth + ScrollBarWidth + Margin + Margin;
        internal const float PanelHeight = PathFailListY + ListHeight + Margin;
        private const float PathFailListY = ListHeaderY;
        private const float ListHeight = StatusRow.RowHeight * 5f;
        private const float ListWidth = PanelWidth - Margin - Margin;


        // Panel components.
        private readonly UIFastList pathfindList;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal PathFailsPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, Margin, Translations.Translate("TFC_PFF_TIT"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Pathfind failure list.
                pathfindList = AddList<PathFailRow>(PathFailListY, ListWidth, ListHeight);

                // Populate initial data.
                UpdateContent();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up pathfinding failures panel");
            }
        }


        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected override void UpdateContent()
        {
            // Get filtered log list.
            List<PathFailData> displayList = PathFindFailure.GetFails(currentBuilding);

            // Set fastlist items, without changing the display.
            pathfindList.rowsData.m_buffer = displayList.ToArray();
            pathfindList.rowsData.m_size = displayList.Count;
            pathfindList.Refresh();
        }
    }
}