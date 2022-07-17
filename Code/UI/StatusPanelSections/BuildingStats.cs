using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;


namespace TransferController
{
    /// <summary>
    /// Building stats panel.
    /// </summary>
    internal class BuildingStatsPanel : StatusPanelSection
    {
        // Layout constants.
        internal const float PanelWidth = Margin + StatsPanel.PanelWidth + Margin;
        internal const float PanelHeight = OffersPanel.PanelHeight;


        // Panel components.
        StatsPanel statsPanel;

        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal BuildingStatsPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 0f, Translations.Translate("TFC_BST_TIT"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Stats panel.
                statsPanel = this.AddUIComponent<StatsPanel>();
                statsPanel.relativePosition = new Vector2(Margin, PanelHeight - StatsPanel.PanelHeight - Margin);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up offers panel");
            }
        }


        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected override void UpdateContent()
        {
            statsPanel.UpdateContent(currentBuilding);
        }
    }
}
