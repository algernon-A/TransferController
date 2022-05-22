using System;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Transfer panel (setting restrictions for the given transfer).
    /// </summary>
    internal class TransferPanel : UIPanel
    {
        // Layout constants - main panel.
        private const float Margin = 5f;
        private const float TitleY = 10f;
        private const float HeadingHeight = 20f;
        private const float TabY = TitleY + HeadingHeight;
        private const float TabHeight = 20f;
        internal const float PanelWidth = TransferPanelTab.PanelWidth + Margin + Margin;
        internal const float PanelHeight = TabY + TabHeight + TransferPanelTab.PanelHeight + Margin;

        // Panel components.
        private readonly UILabel directionLabel;
        private readonly TransferDistrictTab districtPanel;
        private readonly TransferBuildingTab buildingPanel;

        // Current selections.
        private ushort currentBuilding;
        private byte recordNumber, nextRecord;
        private TransferManager.TransferReason material;

        /// <summary>
        /// District panel reference.
        /// </summary>
        internal TransferDistrictTab DistrictPanel => districtPanel;


        /// <summary>
        /// Sets the direction label text.
        /// </summary>
        internal string DirectionTitle { set => directionLabel.text = value; }


        /// <summary>
        /// Currently selected building.
        /// </summary>
        internal ushort CurrentBuilding
        {
            get => currentBuilding;

            set
            {
                if (currentBuilding != value)
                {
                    currentBuilding = value;
                    districtPanel.CurrentBuilding = value;
                    buildingPanel.CurrentBuilding = value;
                }
            }
        }


        /// <summary>
        /// Current record number.
        /// </summary>
        internal byte RecordNumber
        {
            get => recordNumber;

            set
            {
                if (recordNumber != value)
                {
                    recordNumber = value;
                    districtPanel.RecordNumber = value;
                    buildingPanel.RecordNumber = value;
                }
            }
        }


        /// <summary>
        /// Transfer reason.
        /// </summary>
        internal TransferManager.TransferReason TransferReason
        {
            private get => material;

            set
            {
                material = value;
                districtPanel.TransferReason = value;
                buildingPanel.TransferReason = value;
            }
        }


        /// <summary>
        /// Other record flag.
        /// </summary>
        internal byte NextRecord
        {
            private get => nextRecord;
            
            set
            {
                nextRecord = value;
                districtPanel.NextRecord = value;
                buildingPanel.NextRecord = value;
            }
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        internal TransferPanel()
        {
            try
            {
                // Basic setup.
                autoLayout = false;
                backgroundSprite = "UnlockingPanel2";
                opacity = 0.95f;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                size = new Vector2(PanelWidth, PanelHeight);

                // Direction label.
                directionLabel = UIControls.AddLabel(this, 0f, TitleY, string.Empty, PanelWidth, 0.9f);
                directionLabel.textAlignment = UIHorizontalAlignment.Center;

                // Add tabstrip.
                UITabstrip tabStrip = this.AddUIComponent<UITabstrip>();
                tabStrip.relativePosition = new Vector2(Margin, TabY);
                tabStrip.size = new Vector2(TransferPanelTab.PanelWidth, TransferPanelTab.PanelHeight + TabHeight);

                // Tab container (the panels underneath each tab).
                UITabContainer tabContainer = this.AddUIComponent<UITabContainer>();
                tabContainer.relativePosition = new Vector2(Margin, TabY + TabHeight);
                tabContainer.size = new Vector2(TransferPanelTab.PanelWidth, TransferPanelTab.PanelHeight);
                tabStrip.tabPages = tabContainer;

                // Add tabs.
                districtPanel = new TransferDistrictTab(AddTextTab(tabStrip, "District", 0));
                buildingPanel = new TransferBuildingTab(AddTextTab(tabStrip, "Building", 1));
                tabStrip.selectedIndex = 1;
                tabStrip.selectedIndex = 0;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up outgoing panel");
            }
        }


        /// <summary>
        /// Adds a text-based tab to a UI tabstrip.
        /// </summary>
        /// <param name="tabStrip">UIT tabstrip to add to</param>
        /// <param name="tabName">Name of this tab</param>
        /// <param name="tabIndex">Index number of this tab</param>
        /// <param name="width">Tab width</param>
        /// <param name="autoLayout">Default autoLayout setting</param>
        /// <returns>UIHelper instance for the new tab panel</returns>
        private UIPanel AddTextTab(UITabstrip tabStrip, string tabName, int tabIndex, float width = 200f, bool autoLayout = false)
        {
            // Create tab.
            UIButton tabButton = tabStrip.AddTab(tabName);

            // Sprites.
            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";

            // Tooltip.
            tabButton.tooltip = tabName;

            // Force width.
            tabButton.width = width;

            // Force size.
            tabButton.height = TabHeight;
            tabButton.textScale = 0.8f;
            tabButton.textPadding.top = 2;

            // Get tab root panel.
            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;

            // Panel setup.
            rootPanel.autoLayout = autoLayout;
            rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
            rootPanel.autoLayoutPadding.top = 5;
            rootPanel.autoLayoutPadding.left = 10;

            return rootPanel;
        }
    }
}