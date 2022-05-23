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
        // Layout constants .
        private const float Margin = 5f;
        private const float TitleY = 10f;
        private const float HeadingHeight = 20f;
        private const float TabY = TitleY + HeadingHeight;
        private const float TabHeight = 20f;
        internal const float PanelWidth = TransferPanelTab.PanelWidth + Margin + Margin;
        internal const float PanelHeight = TabY + TabHeight + TransferPanelTab.PanelHeight + Margin;

        // Tab indexes.
        private enum TabIndexes : int
        {
            DistrictTab = 0,
            BuildingTab,
            OutsideTab
        }

        // Panel components.
        private readonly UILabel directionLabel;
        private readonly TransferDistrictTab districtPanel;
        private readonly TransferBuildingTab buildingPanel;
        private readonly TransferOutsideTab outsidePanel;
        private readonly UIButton outsideButton;
        private readonly UITabstrip tabStrip;

        // Current selections.
        private ushort currentBuilding;
        private byte recordNumber, nextRecord;
        private TransferManager.TransferReason material;


        /// <summary>
        /// Outside panel reference.
        /// </summary>
        internal TransferOutsideTab OutsidePanel => outsidePanel;


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
                    outsidePanel.CurrentBuilding = value;
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
                recordNumber = value;
                districtPanel.RecordNumber = value;
                buildingPanel.RecordNumber = value;
                outsidePanel.RecordNumber = value;
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
                outsidePanel.TransferReason = value;
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
                outsidePanel.NextRecord = value;
            }
        }


        /// <summary>
        /// Sets the outside connection checkbox label text.
        /// </summary>
        internal string OutsideLabel
        {
            set
            {
                // Set outside panel label.
                outsidePanel.OutsideLabel = value;

                // Valid value?
                if (value == null)
                {
                    // No valid value - deselect outside tab if selected.
                    if (tabStrip.selectedIndex == (int)TabIndexes.OutsideTab)
                    {
                        tabStrip.selectedIndex = (int)TabIndexes.DistrictTab;
                    }

                    // Hide outside connection tab.
                    outsideButton.Hide();
                }
                else
                {
                    // Valid import/export building; show outside connection tab and set text.
                    if ((recordNumber & BuildingControl.OutgoingMask) != 0)
                    {
                        // Exports - outgoing.
                        outsideButton.text = Translations.Translate("TFC_TAB_EXP");
                    }
                    else
                    {
                        // Imports - incoming.
                        outsideButton.text = Translations.Translate("TFC_TAB_IMP");
                    }
                    outsideButton.Show();
                }
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
                tabStrip = this.AddUIComponent<UITabstrip>();
                tabStrip.relativePosition = new Vector2(Margin, TabY);
                tabStrip.size = new Vector2(TransferPanelTab.PanelWidth, TransferPanelTab.PanelHeight + TabHeight);

                // Tab container (the panels underneath each tab).
                UITabContainer tabContainer = this.AddUIComponent<UITabContainer>();
                tabContainer.relativePosition = new Vector2(Margin, TabY + TabHeight);
                tabContainer.size = new Vector2(TransferPanelTab.PanelWidth, TransferPanelTab.PanelHeight);
                tabStrip.tabPages = tabContainer;

                // Add tabs.
                districtPanel = new TransferDistrictTab(AddTextTab(tabStrip, Translations.Translate("TFC_TAB_DIS"), (int)TabIndexes.DistrictTab, out UIButton _));
                buildingPanel = new TransferBuildingTab(AddTextTab(tabStrip, Translations.Translate("TFC_TAB_BLD"), (int)TabIndexes.BuildingTab, out UIButton _));
                outsidePanel = new TransferOutsideTab(AddTextTab(tabStrip, Translations.Translate("TFC_TAB_IMP"), (int)TabIndexes.OutsideTab, out outsideButton));
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
        /// <param name="button">Tab button instance reference</param>
        /// <param name="width">Tab width</param>
        /// <returns>UIHelper instance for the new tab panel</returns>
        private UIPanel AddTextTab(UITabstrip tabStrip, string tabName, int tabIndex, out UIButton button, float width = TransferPanelTab.PanelWidth / 3f)
        {
            // Create tab.
            UIButton tabButton = tabStrip.AddTab(tabName);
            button = tabButton;

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
            rootPanel.autoLayout = false;
            rootPanel.autoLayoutDirection = LayoutDirection.Vertical;
            rootPanel.autoLayoutPadding.top = 5;
            rootPanel.autoLayoutPadding.left = 10;

            return rootPanel;
        }
    }
}