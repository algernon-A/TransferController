using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    // Custom button class for persistent state.
    public class TCPanelButton : UIButton
    {
        /// <summary>
        /// Set to true to ignore any button state changes.
        /// </summary>
        public bool ignoreStateChanges = false;


        /// <summary>
        /// Called when the button state is attempted to be changed.
        /// </summary>
        /// <param name="value">New button state</param>
        protected override void OnButtonStateChanged(ButtonState value)
        {
            // Don't do anything if we're ignoring state changes.
            if (!ignoreStateChanges)
            {
                base.OnButtonStateChanged(value);
            }
        }
    }


    /// <summary>
    /// Building info panel.
    /// </summary>
    internal class BuildingPanel : UIPanel
    {
        // Layout constants.
        protected const float Margin = 5f;
        internal const float PanelWidth = BuildingPanelTab.PanelWidth + (Margin * 2f);
        private const float TitleHeight = 50f;
        private const float LabelHeight = 30f;
        private const float DistrictLabelY = TitleHeight + LabelHeight;
        private const float TabHeight = 30f;
        private const float TabPanelWidth = BuildingPanelTab.PanelWidth;
        private const float TabY = DistrictLabelY + LabelHeight;
        private const float TabContentHeight = BuildingRestrictionsTab.PanelHeight;
        private const float TabContentY = TabY + TabHeight;
        private const float TabPanelHeight = TabHeight + TabContentHeight;
        private const float PanelHeight = TabContentY + TabContentHeight + Margin;
        private const float ButtonSize = 30f;
        private const float Button1X = Margin;
        private const float Button2X = Button1X + ButtonSize + Margin;
        private const float ButtonY = Margin;
        private const float StatusSpriteSize = 13f;

        // Maximum number of supported transfers per building.
        internal const int MaxTransfers = 4;
        private const int NumTabs = MaxTransfers + 1;
        private const int VehicleTab = MaxTransfers;

        // Panel components.
        private readonly UILabel buildingLabel, districtLabel;
        private readonly UIPanel tabPanel;
        private readonly UITabstrip tabStrip;
        private readonly TCPanelButton offersButton, logButton;

        // Current selection.
        private ushort currentBuilding;
        private BuildingInfo thisBuildingInfo;

        // Sub-panels.
        private readonly TransferStruct[] transfers = new TransferStruct[MaxTransfers];
        private readonly BuildingPanelTab[] tabs = new BuildingPanelTab[NumTabs];
        private readonly UIButton[] tabButtons = new UIButton[NumTabs];
        private readonly UISprite[] tabSprites = new UISprite[NumTabs];
        private OffersPanel offersPanel;
        private LogPanel logPanel;

        // Event handling.
        private bool ignoreTabChange = true, copyProcessing = false, pasteProcessing = false;


        // Dictionary getter.
        public static Dictionary<uint, BuildingControl.BuildingRecord> BuildingRecords => BuildingControl.buildingRecords;


        /// <summary>
        /// Current building accessor.
        /// </summary>
        internal ushort CurrentBuilding => currentBuilding;


        /// <summary>
        /// Called by Unity every update.
        /// Used to check for copy/paste keypress.
        /// </summary>
        public override void Update()
        {
            // Copy key processing - use event flag to avoid repeated triggering.
            if (ModSettings.keyCopy.IsPressed())
            {
                if (!copyProcessing)
                {
                    Copy();
                    copyProcessing = true;
                }
            }
            else
            {
                // Key no longer down - resume processing of events.
                copyProcessing = false;
            }

            // Paste key processing - use event flag to avoid repeated triggering.
            if (ModSettings.keyPaste.IsPressed())
            {
                if (!pasteProcessing)
                {
                    Paste();
                    pasteProcessing = true;
                }
            }
            else
            {
                // Key no longer down - resume processing of events.
                pasteProcessing = false;
            }

            base.Update();
        }


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal BuildingPanel()
        {
            try
            {
                // Basic setup.
                autoLayout = false;
                backgroundSprite = "MenuPanel2";
                opacity = 0.95f;
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = PanelWidth;
                height = PanelHeight;

                // Default position - centre in screen.
                relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - PanelWidth) / 2), (GetUIView().fixedHeight - PanelHeight) / 2);

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_NAM"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Building label.
                buildingLabel = UIControls.AddLabel(this, 0f, TitleHeight, String.Empty, PanelWidth);
                buildingLabel.textAlignment = UIHorizontalAlignment.Center;

                // Drag handle.
                UIDragHandle dragHandle = this.AddUIComponent<UIDragHandle>();
                dragHandle.relativePosition = Vector3.zero;
                dragHandle.width = PanelWidth - 35f;
                dragHandle.height = TitleHeight;

                // Close button.
                UIButton closeButton = AddUIComponent<UIButton>();
                closeButton.relativePosition = new Vector2(width - 35f, 2f);
                closeButton.normalBgSprite = "buttonclose";
                closeButton.hoveredBgSprite = "buttonclosehover";
                closeButton.pressedBgSprite = "buttonclosepressed";

                // Close button event handler.
                closeButton.eventClick += (component, clickEvent) =>
                {
                    BuildingPanelManager.Close();
                };

                // District label.
                districtLabel = UIControls.AddLabel(this, 0f, DistrictLabelY, String.Empty, PanelWidth, 0.9f);
                districtLabel.textAlignment = UIHorizontalAlignment.Center;

                // Offers button.
                offersButton = AddIconButton(this, Button1X, ButtonY, ButtonSize, "TFC_OFF_TIT", TextureUtils.LoadSpriteAtlas("TC-OpenOffers"));
                offersButton.eventClicked += ShowOffers;

                // Log button.
                logButton = AddIconButton(this, Button2X, ButtonY, ButtonSize, "TFC_OFF_LOG", TextureUtils.LoadSpriteAtlas("TC-Logs"));
                logButton.eventClicked += ShowLog;

                // Tab panel.
                tabPanel = this.AddUIComponent<UIPanel>();
                tabPanel.autoLayout = false;
                tabPanel.autoSize = false;
                tabPanel.relativePosition = new Vector2(0f, TabY);
                tabPanel.width = TabPanelWidth;
                tabPanel.height = TabPanelHeight;

                // Tabstrip.
                tabStrip = tabPanel.AddUIComponent<UITabstrip>();
                tabStrip.relativePosition = new Vector2(Margin, 0f);
                tabStrip.width = TabPanelWidth;
                tabStrip.height = TabPanelHeight;
                tabStrip.startSelectedIndex = -1;
                tabStrip.selectedIndex = -1;

                // Tab container (the panels underneath each tab).
                UITabContainer tabContainer = tabPanel.AddUIComponent<UITabContainer>();
                tabContainer.relativePosition = new Vector2(Margin, TabHeight);
                tabContainer.width = TabPanelWidth;
                tabContainer.height = TabContentHeight;
                tabStrip.tabPages = tabContainer;

                // Add tabs.
                for (int i = 0; i < NumTabs; ++i)
                {
                    // Last tab is vehicles.
                    if (i == VehicleTab)
                    {
                        tabs[i] = new BuildingVehiclesTab(AddTextTabWithSprite(tabStrip, Translations.Translate("TFC_TAB_VEH"), i, out tabButtons[i], out tabSprites[i]), tabSprites[i]);
                    }
                    else
                    {
                        // Otherwie, just add a building restrictions tab.
                        tabs[i] = new BuildingRestrictionsTab(AddTextTabWithSprite(tabStrip, String.Empty, i, out tabButtons[i], out tabSprites[i]), tabSprites[i]);
                    }

                    // Set tab reference as tabstrip tab user data.
                    tabStrip.tabs[i].objectUserData = tabs[i];
                }

                tabStrip.eventSelectedIndexChanged += (UIComponent component, int index) =>
                {
                    // Don't do anything if ignoring tab index changes.
                    if (!ignoreTabChange)
                    {
                        RecalculateHeight(index);
                    }
                };
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up building panel");
            }
        }


        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            // Suspend tab change event handling.
            ignoreTabChange = true;

            // Local references.
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            // Update selected building ID.
            currentBuilding = buildingID;
            thisBuildingInfo = buildingManager.m_buildings.m_buffer[currentBuilding].Info;
            TCTool.Instance.CurrentBuilding = buildingID;

            // Maximum number of panels.
            int numPanels = TransferDataUtils.BuildingEligibility(buildingID, thisBuildingInfo, transfers);
            int activeTabs = 0;
            int vehicleReference = -1;

            // Set up used panels.
            for (int i = 0; i < numPanels; ++i)
            {
                // Set panel instance properties.
                tabButtons[i].text = transfers[i].panelTitle;
                tabButtons[i].Show();

                if (tabs[i] is BuildingRestrictionsTab restrictionsTab)
                {
                    restrictionsTab.IsIncoming = transfers[i].isIncoming;
                    restrictionsTab.TransferReason = transfers[i].reason;
                    restrictionsTab.CurrentBuilding = currentBuilding;
                    restrictionsTab.OutsideLabel = transfers[i].outsideText;
                    restrictionsTab.OutsideTip = transfers[i].outsideTip;
                    if (transfers[i].spawnsVehicles & vehicleReference < 0)
                    {
                        vehicleReference = i;
                    }
                }

                ++activeTabs;
            }

            // Hide any unused transfer panels.
            for (int i = numPanels; i < transfers.Length; ++i)
            {
                tabButtons[i].Hide();
            }

            // Show/hide vehicle tab,
            if (vehicleReference >= 0)
            {
                ++activeTabs;
                tabButtons[MaxTransfers].width = TabPanelWidth / activeTabs;
                tabButtons[MaxTransfers].Show();

                tabs[VehicleTab].IsIncoming = transfers[vehicleReference].isIncoming;
                tabs[VehicleTab].TransferReason = transfers[vehicleReference].reason;
                tabs[VehicleTab].CurrentBuilding = currentBuilding;
            }
            else
            {
                tabButtons[VehicleTab].Hide();
            }

            // Resize tabs to fit.
            for (int i = 0; i < activeTabs; ++i)
            {
               tabButtons[i].width = TabPanelWidth / activeTabs;
            }

            // Are any tabs visible?
            if (activeTabs == 0)
            {
                // If no tabs are visible, hide the entire tab panel by setting index to -1.
                tabStrip.selectedIndex = -1;
            }
            else
            {
                // Tabs are visible - if the currently selected tab indx is invalid, reset it to zero.
                if (tabStrip.selectedIndex < 0 || !tabButtons[tabStrip.selectedIndex].isVisible)
                {
                    tabStrip.selectedIndex = 0;

                    // Set start index to 0 to avoid race condition on initial setup before tabStrip.Start() is called.
                    tabStrip.startSelectedIndex = 0;
                }

                // If only one tab is visible, hide the tab button.
                if (activeTabs == 1)
                {
                    tabButtons[0].Hide();
                }

                // Ensure tab panel visibility.
                tabPanel.Show();
            }

            // Resize panel to match content.
            RecalculateHeight();

            // Make sure we're visible if we're not already.
            Show();

            // Set name.
            buildingLabel.text = buildingManager.GetBuildingName(currentBuilding, InstanceID.Empty);

            // District text.
            StringBuilder districtText = new StringBuilder();

            // District area.
            byte currentDistrict = districtManager.GetDistrict(buildingManager.m_buildings.m_buffer[currentBuilding].m_position);
            if (currentDistrict != 0)
            {
                districtText.Append(districtManager.GetDistrictName(currentDistrict));
            }

            // Park area.
            byte currentPark = districtManager.GetPark(buildingManager.m_buildings.m_buffer[currentBuilding].m_position);
            if (currentPark != 0)
            {
                // Add comma between district and park names if we have both.
                if (currentDistrict != 0)
                {
                    districtText.Append(", ");
                }
                districtText.Append(districtManager.GetParkName(currentPark));
            }

            // If no current district or park, then display no district message.
            if (currentDistrict == 0 && currentPark == 0)
            {
                districtLabel.text = Translations.Translate("TFC_BLD_NOD");
            }
            else
            {
                // Current district or park - display generated text.
                districtLabel.text = districtText.ToString();
            }

            // Update target for offer panel, if open.
            offersPanel?.SetTarget(buildingID);

            // Resume tab change event handling.
            ignoreTabChange = false;
        }


        /// <summary>
        /// Copies the TC settings of the currently selected building.
        /// </summary>
        internal void Copy() => CopyPaste.Copy(CurrentBuilding, thisBuildingInfo);


        /// <summary>
        /// Pastes copied TC settings to the currently selected building.
        /// </summary>
        internal void Paste()
        {
            if (CopyPaste.Paste(CurrentBuilding, thisBuildingInfo))
            {
                // Update data via reset of target building.
                SetTarget(CurrentBuilding);
            }
        }


        /// <summary>
        /// Recalculates the panel height based on the currently selected tab.
        /// </summary>
        internal void RecalculateHeight() => RecalculateHeight(tabStrip.selectedIndex);


        /// <summary>
        /// Recalculates the panel height based on the specified tab.
        /// </summary>
        /// <param name="tabIndex">Tab index</param>
        internal void RecalculateHeight(int tabIndex)
        {
            // If the provided tab selection is invalid, hide the tab display entirely.
            if (tabIndex < 0 || tabIndex >= tabStrip.tabCount)
            {
                tabPanel.Hide();
                height = TabY;
            }
            else if (tabStrip.tabs[tabIndex].objectUserData is BuildingPanelTab tab)
            {
                float contentHeight = tab.ContentHeight;
                height = contentHeight + TabContentY + Margin;
                tabStrip.tabPages.height = contentHeight;
                tabPanel.height = contentHeight;
                tabStrip.height = contentHeight;
            }
        }


        /// <summary>
        /// Clears button states (after sub-panel is closed).
        /// </summary>
        internal void ResetButtons()
        {
            offersButton.ignoreStateChanges = false;
            logButton.ignoreStateChanges = false;
            offersButton.state = UIButton.ButtonState.Normal;
            logButton.state = UIButton.ButtonState.Normal;
        }


        /// <summary>
        /// Event handler for show offers button.
        /// </summary>
        /// <param name="component">Calling component (unused)</param>
        /// <param name="clickEvent">Click event (unused)</param>
        private void ShowOffers(UIComponent component, UIMouseEventParameter clickEvent)
        {
            // Close log panel if it's open.
            if (logPanel != null)
            {
                RemoveUIComponent(logPanel);
                GameObject.Destroy(logPanel);
                logPanel = null;
                ResetButtons();
            }

            // Create offers panel if it isn't already created.
            if (offersPanel == null)
            {
                offersPanel = this.AddUIComponent<OffersPanel>();
                offersPanel.relativePosition = new Vector2(PanelWidth + Margin, 0f);
            }

            // Set the offer panel target building to match the current one.
            offersPanel.SetTarget(currentBuilding);

            // Ensure offers panel is visible.
            offersPanel.Show();

            // Enforce button state while panel is open.
            offersButton.state = UIButton.ButtonState.Pressed;
            offersButton.ignoreStateChanges = true;
        }


        /// <summary>
        /// Event handler for show log button.
        /// </summary>
        /// <param name="component">Calling component (unused)</param>
        /// <param name="clickEvent">Click event (unused)</param>
        private void ShowLog(UIComponent component, UIMouseEventParameter clickEvent)
        {
            // Close offers panel if it's open.
            if (offersPanel != null)
            {
                RemoveUIComponent(offersPanel);
                GameObject.Destroy(offersPanel);
                offersPanel = null;
                ResetButtons();
            }

            // Create log panel if it isn't already created.
            if (logPanel == null)
            {
                logPanel = this.AddUIComponent<LogPanel>();
                logPanel.relativePosition = new Vector2(PanelWidth + Margin, 0f);
            }

            // Ensure offers panel is visible.
            logPanel.Show();

            // Enforce button state while panel is open.
            logButton.state = UIButton.ButtonState.Pressed;
            logButton.ignoreStateChanges = true;
        }


        /// <summary>
        /// Adds a text-based tab with a status sprite to a UI tabstrip.
        /// </summary>
        /// <param name="tabStrip">UIT tabstrip to add to</param>
        /// <param name="tabName">Name of this tab</param>
        /// <param name="tabIndex">Index number of this tab</param>
        /// <param name="button">Tab button instance reference</param>
        /// <param name="sprite">Tab status sprite instance reference</param>
        /// <param name="width">Tab width</param>
        /// <returns>UIHelper instance for the new tab panel</returns>
        private UIPanel AddTextTabWithSprite(UITabstrip tabStrip, string tabName, int tabIndex, out UIButton button, out UISprite sprite, float width = PanelWidth / 6f)
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

            // Size and text formatting.
            tabButton.height = TabHeight;
            tabButton.width = width;
            tabButton.textScale = 0.7f;
            tabButton.wordWrap = true;
            tabButton.verticalAlignment = UIVerticalAlignment.Middle;

            // Add status sprite.
            UISprite tabSprite = tabButton.AddUIComponent<UISprite>();
            tabSprite.atlas = TextureUtils.InGameAtlas;
            tabSprite.autoSize = false;
            tabSprite.height = StatusSpriteSize;
            tabSprite.width = StatusSpriteSize;
            tabSprite.relativePosition = new Vector2(Margin, (TabHeight - StatusSpriteSize) / 2f);
            tabSprite.spriteName = "AchievementCheckedFalse";

            // Adjust button text layout to accomodate sprite.
            tabButton.textPadding.left = (int)(StatusSpriteSize + Margin + Margin);
            tabButton.textPadding.top = 3;
            tabButton.textHorizontalAlignment = UIHorizontalAlignment.Left;

            // Get tab root panel.
            UIPanel rootPanel = tabStrip.tabContainer.components[tabIndex] as UIPanel;

            // Panel setup.
            rootPanel.autoLayout = false;

            // Return new instances.
            button = tabButton;
            sprite = tabSprite;
            return rootPanel;
        }


        /// <summary>
        /// Adds an icon-style button to the specified component at the specified coordinates.
        /// </summary>
        /// <param name="parent">Parent UIComponent</param>
        /// <param name="xPos">Relative X position</param>
        /// <param name="yPos">Relative Y position</param>
        /// <param name="size">Button size (square)</param>
        /// <param name="tooltipKey">Tooltip translation key</param>
        /// <param name="atlas">Icon atlas</param>
        /// <returns>New UIButton</returns>
        private static TCPanelButton AddIconButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey, UITextureAtlas atlas)
        {
            TCPanelButton newButton = parent.AddUIComponent<TCPanelButton>();

            // Size and position.
            newButton.relativePosition = new Vector2(xPos, yPos);
            newButton.height = size;
            newButton.width = size;

            // Appearance.
            newButton.atlas = atlas;
            newButton.normalFgSprite = "normal";
            newButton.focusedFgSprite = "normal";
            newButton.hoveredFgSprite = "hovered";
            newButton.disabledFgSprite = "disabled";
            newButton.pressedFgSprite = "pressed";

            // Tooltip.
            newButton.tooltip = Translations.Translate(tooltipKey);

            return newButton;
        }
    }
}