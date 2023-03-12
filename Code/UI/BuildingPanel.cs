// <copyright file="BuildingPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Building info panel.
    /// </summary>
    internal class BuildingPanel : UIPanel
    {
        /// <summary>
        /// Maximum number of transfer types supported per building.
        /// </summary>
        internal const int MaxTransfers = 4;

        /// <summary>
        /// Panel width.
        /// </summary>
        internal const float PanelWidth = BuildingPanelTab.PanelWidth + (Margin * 2f);

        /// <summary>
        /// Layout margin.
        /// </summary>
        protected const float Margin = 5f;

        // Layout constants - private.
        private const float TitleHeight = 40f;
        private const float NameLabelY = TitleHeight + Margin;
        private const float NameLabelHeight = 30f;
        private const float AreaLabelHeight = 20f;
        private const float AreaLabel1Y = TitleHeight + NameLabelHeight;
        private const float AreaLabel2Y = AreaLabel1Y + AreaLabelHeight;
        private const float TabHeight = 30f;
        private const float TabPanelWidth = BuildingPanelTab.PanelWidth;
        private const float TabY = Button2Y + ButtonSize + Margin;
        private const float TabContentHeight = BuildingRestrictionsTab.PanelHeight;
        private const float TabContentY = TabY + TabHeight;
        private const float TabPanelHeight = TabHeight + TabContentHeight;
        private const float PanelHeight = TabContentY + TabContentHeight + Margin;
        private const float ButtonSize = 30f;
        private const float ButtonX = PanelWidth - ButtonSize - Margin;
        private const float Button1Y = TitleHeight + Margin;
        private const float Button2Y = Button1Y + ButtonSize + Margin;
        private const float StatusSpriteSize = 13f;

        // Maximum number of supported transfers per building.
        private const int NumTabs = MaxTransfers + 1;
        private const int VehicleTab = MaxTransfers;

        // Panel components.
        private readonly UILabel _buildingLabel;
        private readonly UILabel _areaLabel1;
        private readonly UILabel _areaLabel2;
        private readonly UIPanel _tabPanel;
        private readonly UITabstrip _tabStrip;
        private readonly TCPanelButton _offersButton;
        private readonly TCPanelButton _logButton;

        // Sub-panels.
        private readonly TransferDataUtils.TransferStruct[] _transfers = new TransferDataUtils.TransferStruct[MaxTransfers];
        private readonly BuildingPanelTab[] _tabs = new BuildingPanelTab[NumTabs];
        private readonly UIButton[] _tabButtons = new UIButton[NumTabs];
        private readonly UISprite[] _tabSprites = new UISprite[NumTabs];
        private StatusPanel _statusPanel;
        private LogPanel _logPanel;

        // Current selections.
        private ushort _currentBuilding;
        private BuildingInfo _thisBuildingInfo;

        // Event handling.
        private bool _ignoreTabChange = true;
        private bool _copyProcessing = false;
        private bool _pasteProcessing = false;

        public static Vector2 PanelPosition { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingPanel"/> class.
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
                UILabel titleLabel = UILabels.AddLabel(this, 0f, 10f, Translations.Translate("TFC_NAM"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Building label.
                _buildingLabel = UILabels.AddLabel(this, 0f, NameLabelY, string.Empty, PanelWidth);
                _buildingLabel.textAlignment = UIHorizontalAlignment.Center;

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

                // Area labels.
                _areaLabel1 = UILabels.AddLabel(this, 0f, AreaLabel1Y, string.Empty, PanelWidth, 0.9f);
                _areaLabel1.textAlignment = UIHorizontalAlignment.Center;
                _areaLabel2 = UILabels.AddLabel(this, 0f, AreaLabel2Y, string.Empty, PanelWidth, 0.9f);
                _areaLabel2.textAlignment = UIHorizontalAlignment.Center;

                // Zoom to building button.
                UIButton zoomButton = AddZoomButton(this, Margin, Margin, 30f, "TFC_STA_ZTB");
                zoomButton.eventClicked += (c, p) => ZoomToBuilding(_currentBuilding);

                // Offers button.
                _offersButton = AddIconButton(this, ButtonX, Button1Y, ButtonSize, "TFC_OFF_TIT", UITextures.LoadQuadSpriteAtlas("TC-OpenOffers"));
                _offersButton.eventClicked += ShowOffers;

                // Log button.
                _logButton = AddIconButton(this, ButtonX, Button2Y, ButtonSize, "TFC_OFF_LOG", UITextures.LoadQuadSpriteAtlas("TC-Logs"));
                _logButton.eventClicked += ShowLog;

                // Tab panel.
                _tabPanel = this.AddUIComponent<UIPanel>();
                _tabPanel.autoLayout = false;
                _tabPanel.autoSize = false;
                _tabPanel.relativePosition = new Vector2(0f, TabY);
                _tabPanel.width = TabPanelWidth;
                _tabPanel.height = TabPanelHeight;

                // Tabstrip.
                _tabStrip = _tabPanel.AddUIComponent<UITabstrip>();
                _tabStrip.relativePosition = new Vector2(Margin, 0f);
                _tabStrip.width = TabPanelWidth;
                _tabStrip.height = TabPanelHeight;
                _tabStrip.startSelectedIndex = -1;
                _tabStrip.selectedIndex = -1;

                // Tab container (the panels underneath each tab).
                UITabContainer tabContainer = _tabPanel.AddUIComponent<UITabContainer>();
                tabContainer.relativePosition = new Vector2(Margin, TabHeight);
                tabContainer.width = TabPanelWidth;
                tabContainer.height = TabContentHeight;
                _tabStrip.tabPages = tabContainer;

                // Add tabs.
                for (int i = 0; i < NumTabs; ++i)
                {
                    // Last tab is vehicles.
                    if (i == VehicleTab)
                    {
                        _tabs[i] = new BuildingVehiclesTab(AddTextTabWithSprite(_tabStrip, Translations.Translate("TFC_TAB_VEH"), i, out _tabButtons[i], out _tabSprites[i]), _tabSprites[i]);
                    }
                    else
                    {
                        // Otherwie, just add a building restrictions tab.
                        _tabs[i] = new BuildingRestrictionsTab(AddTextTabWithSprite(_tabStrip, string.Empty, i, out _tabButtons[i], out _tabSprites[i]), _tabSprites[i]);
                    }

                    // Set tab reference as tabstrip tab user data.
                    _tabStrip.tabs[i].objectUserData = _tabs[i];
                }

                _tabStrip.eventSelectedIndexChanged += (UIComponent component, int index) =>
                {
                    // Don't do anything if ignoring tab index changes.
                    if (!_ignoreTabChange)
                    {
                        RecalculateHeight(index);
                    }
                };
                SetPosition();
                eventPositionChanged += (c, v) => PanelPosition = relativePosition;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up building panel");
            }
        }

        private void SetPosition() {
            if (PanelPosition == Vector2.zero) {
                Vector2 vector = GetUIView().GetScreenResolution();
                var x = (vector.x - PanelWidth) / 2;
                var y = (vector.y - PanelHeight) / 2;
                PanelPosition = relativePosition = new Vector3(x, y);
            } else {
                relativePosition = PanelPosition;
            }
        }
        
        /// <summary>
        /// Gets the dictionary of building records.
        /// </summary>
        public static Dictionary<uint, BuildingControl.BuildingRecord> BuildingRecords => BuildingControl.BuildingRecords;

        /// <summary>
        /// Gets the current building ID.
        /// </summary>
        internal ushort CurrentBuilding => _currentBuilding;

        /// <summary>
        /// Called by Unity every update.
        /// Used to check for copy/paste keypress.
        /// </summary>
        public override void Update()
        {
            // Copy key processing - use event flag to avoid repeated triggering.
            if (ModSettings.KeyCopy.IsPressed())
            {
                if (!_copyProcessing)
                {
                    Copy();
                    _copyProcessing = true;
                }
            }
            else
            {
                // Key no longer down - resume processing of events.
                _copyProcessing = false;
            }

            // Paste key processing - use event flag to avoid repeated triggering.
            if (ModSettings.KeyPaste.IsPressed())
            {
                if (!_pasteProcessing)
                {
                    Paste();
                    _pasteProcessing = true;
                }
            }
            else
            {
                // Key no longer down - resume processing of events.
                _pasteProcessing = false;
            }

            base.Update();
        }

        /// <summary>
        /// Adds an zoom icon button.
        /// </summary>
        /// <param name="parent">Parent UIComponent.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="size">Button size.</param>
        /// <param name="tooltipKey">Tooltip translation key.</param>
        /// <returns>New UIButton.</returns>
        internal static UIButton AddZoomButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey)
        {
            UIButton newButton = parent.AddUIComponent<UIButton>();

            // Size and position.
            newButton.relativePosition = new Vector2(xPos, yPos);
            newButton.height = size;
            newButton.width = size;

            // Appearance.
            newButton.atlas = UITextures.InGameAtlas;
            newButton.normalFgSprite = "LineDetailButtonHovered";
            newButton.focusedFgSprite = "LineDetailButtonFocused";
            newButton.hoveredFgSprite = "LineDetailButton";
            newButton.disabledFgSprite = "LineDetailButtonDisabled";
            newButton.pressedFgSprite = "LineDetailButtonPressed";

            // Tooltip.
            newButton.tooltip = Translations.Translate(tooltipKey);

            return newButton;
        }

        /// <summary>
        /// Zooms to the specified building.
        /// </summary>
        /// <param name="buildingID">Target building ID.</param>
        internal static void ZoomToBuilding(ushort buildingID)
        {
            // Go to target building if available.
            if (buildingID != 0)
            {
                // Clear existing target fist to force a re-zoom-in if required.
                ToolsModifierControl.cameraController.ClearTarget();

                InstanceID instance = default;
                instance.Building = buildingID;
                ToolsModifierControl.cameraController.SetTarget(instance, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_position, zoomIn: true);
            }
        }

        /// <summary>
        /// Sets/changes the currently selected building.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal virtual void SetTarget(ushort buildingID)
        {
            // Suspend tab change event handling.
            _ignoreTabChange = true;

            // Local references.
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            // Update selected building ID.
            _currentBuilding = buildingID;
            _thisBuildingInfo = buildingManager.m_buildings.m_buffer[_currentBuilding].Info;
            TCTool.Instance.CurrentBuilding = buildingID;

            // Maximum number of panels.
            int numPanels = TransferDataUtils.BuildingEligibility(buildingID, _thisBuildingInfo, _transfers);
            int activeTabs = 0;
            int vehicleReference = -1;

            // Set up used panels.
            for (int i = 0; i < numPanels; ++i)
            {
                // Set panel instance properties.
                _tabButtons[i].text = _transfers[i].PanelTitle;
                _tabButtons[i].Show();

                if (_tabs[i] is BuildingRestrictionsTab restrictionsTab)
                {
                    restrictionsTab.IsIncoming = _transfers[i].IsIncoming;
                    restrictionsTab.TransferReason = _transfers[i].Reason;
                    restrictionsTab.CurrentBuilding = _currentBuilding;
                    restrictionsTab.OutsideLabel = _transfers[i].OutsideText;
                    restrictionsTab.OutsideTip = _transfers[i].OutsideTip;
                    if (_transfers[i].SpawnsVehicles & vehicleReference < 0)
                    {
                        vehicleReference = i;
                    }
                }

                ++activeTabs;
            }

            // Hide any unused transfer panels.
            for (int i = numPanels; i < _transfers.Length; ++i)
            {
                _tabButtons[i].Hide();
            }

            // Show/hide vehicle tab,
            if (vehicleReference >= 0)
            {
                ++activeTabs;
                _tabButtons[MaxTransfers].width = TabPanelWidth / activeTabs;
                _tabButtons[MaxTransfers].Show();

                _tabs[VehicleTab].IsIncoming = _transfers[vehicleReference].IsIncoming;
                _tabs[VehicleTab].TransferReason = _transfers[vehicleReference].Reason;
                _tabs[VehicleTab].CurrentBuilding = _currentBuilding;
            }
            else
            {
                _tabButtons[VehicleTab].Hide();
            }

            // Resize tabs to fit.
            for (int i = 0; i < activeTabs; ++i)
            {
               _tabButtons[i].width = TabPanelWidth / activeTabs;
            }

            // Are any tabs visible?
            if (activeTabs == 0)
            {
                // If no tabs are visible, hide the entire tab panel by setting index to -1.
                _tabStrip.selectedIndex = -1;
            }
            else
            {
                // Tabs are visible - if the currently selected tab indx is invalid, reset it to zero.
                if (_tabStrip.selectedIndex < 0 || !_tabButtons[_tabStrip.selectedIndex].isVisible)
                {
                    _tabStrip.selectedIndex = 0;

                    // Set start index to 0 to avoid race condition on initial setup before tabStrip.Start() is called.
                    _tabStrip.startSelectedIndex = 0;
                }

                // If only one tab is visible, hide the tab button.
                if (activeTabs == 1)
                {
                    _tabButtons[0].Hide();
                }

                // Ensure tab panel visibility.
                _tabPanel.Show();
            }

            // Resize panel to match content.
            RecalculateHeight();

            // Make sure we're visible if we're not already.
            Show();

            // Set name.
            _buildingLabel.text = buildingManager.GetBuildingName(_currentBuilding, InstanceID.Empty);

            // District text.
            StringBuilder districtText = new StringBuilder();

            // District area.
            byte currentDistrict = districtManager.GetDistrict(buildingManager.m_buildings.m_buffer[_currentBuilding].m_position);
            if (currentDistrict != 0)
            {
                districtText.Append(districtManager.GetDistrictName(currentDistrict));
            }

            // Park area.
            byte currentPark = districtManager.GetPark(buildingManager.m_buildings.m_buffer[_currentBuilding].m_position);
            if (currentPark != 0)
            {
                // Add comma between district and park names if we have both.
                if (currentDistrict != 0)
                {
                    districtText.Append(", ");
                }

                districtText.Append(districtManager.GetParkName(currentPark));
            }

            // If no current district or park, then display no area message.
            if (currentDistrict == 0 && currentPark == 0)
            {
                _areaLabel1.text = Translations.Translate("TFC_BLD_NOD");
                _areaLabel2.Hide();
            }
            else
            {
                // Current district and/or park - display generated text.
                if (currentDistrict != 0)
                {
                    // District label.
                    _areaLabel1.text = districtManager.GetDistrictName(currentDistrict);

                    // Is there also a park area?
                    if (currentPark != 0)
                    {
                        // Yes - set second label text and show.
                        _areaLabel2.text = districtManager.GetParkName(currentPark);
                        _areaLabel2.Show();
                    }
                    else
                    {
                        // Just the district - hide second area label.
                        _areaLabel2.Hide();
                    }
                }
                else if (currentPark != 0)
                {
                    // No district, but a park - set first area label text and hide the second label.
                    _areaLabel1.text = districtManager.GetParkName(currentPark);
                    _areaLabel2.Hide();
                }
            }

            // Update target for status and log panels, if open.
            _statusPanel?.SetTarget(buildingID);
            _logPanel?.SetTarget(buildingID);

            // Resume tab change event handling.
            _ignoreTabChange = false;
        }

        /// <summary>
        /// Copies the TC settings of the currently selected building.
        /// </summary>
        internal void Copy() => CopyPaste.Copy(CurrentBuilding, _thisBuildingInfo);

        /// <summary>
        /// Pastes copied TC settings to the currently selected building.
        /// </summary>
        internal void Paste()
        {
            if (CopyPaste.Paste(CurrentBuilding, _thisBuildingInfo))
            {
                // Update data via reset of target building.
                SetTarget(CurrentBuilding);
            }
        }

        /// <summary>
        /// Recalculates the panel height based on the currently selected tab.
        /// </summary>
        internal void RecalculateHeight() => RecalculateHeight(_tabStrip.selectedIndex);

        /// <summary>
        /// Recalculates the panel height based on the specified tab.
        /// </summary>
        /// <param name="tabIndex">Tab index.</param>
        internal void RecalculateHeight(int tabIndex)
        {
            // If the provided tab selection is invalid, hide the tab display entirely.
            if (tabIndex < 0 || tabIndex >= _tabStrip.tabCount)
            {
                _tabPanel.Hide();
                height = TabY;
            }
            else if (_tabStrip.tabs[tabIndex].objectUserData is BuildingPanelTab tab)
            {
                float contentHeight = tab.ContentHeight;
                height = contentHeight + TabContentY + Margin;
                _tabStrip.tabPages.height = contentHeight;
                _tabPanel.height = contentHeight;
                _tabStrip.height = contentHeight;
            }
        }

        /// <summary>
        /// Clears button states (after sub-panel is closed).
        /// </summary>
        internal void ResetButtons()
        {
            _offersButton.IgnoreStateChanges = false;
            _logButton.IgnoreStateChanges = false;
            _offersButton.state = UIButton.ButtonState.Normal;
            _logButton.state = UIButton.ButtonState.Normal;
        }

        /// <summary>
        /// Event handler for show offers button.
        /// </summary>
        /// <param name="component">Calling component (unused).</param>
        /// <param name="clickEvent">Click event (unused).</param>
        private void ShowOffers(UIComponent component, UIMouseEventParameter clickEvent)
        {
            // Close log panel if it's open.
            if (_logPanel != null)
            {
                RemoveUIComponent(_logPanel);
                GameObject.Destroy(_logPanel);
                _logPanel = null;
                ResetButtons();
            }

            // Create status panel if it isn't already created.
            if (_statusPanel == null)
            {
                _statusPanel = this.AddUIComponent<StatusPanel>();
                _statusPanel.relativePosition = new Vector2(PanelWidth + Margin, 0f);
            }

            // Set the status panel target building to match the current one.
            _statusPanel.SetTarget(_currentBuilding);

            // Ensure status panel is visible.
            _statusPanel.Show();

            // Enforce button state while panel is open.
            _offersButton.state = UIButton.ButtonState.Pressed;
            _offersButton.IgnoreStateChanges = true;
        }

        /// <summary>
        /// Event handler for show log button.
        /// </summary>
        /// <param name="component">Calling component (unused).</param>
        /// <param name="clickEvent">Click event (unused).</param>
        private void ShowLog(UIComponent component, UIMouseEventParameter clickEvent)
        {
            // Close status panel if it's open.
            if (_statusPanel != null)
            {
                RemoveUIComponent(_statusPanel);
                GameObject.Destroy(_statusPanel);
                _statusPanel = null;
                ResetButtons();
            }

            // Create log panel if it isn't already created.
            if (_logPanel == null)
            {
                _logPanel = this.AddUIComponent<LogPanel>();
                _logPanel.relativePosition = new Vector2(PanelWidth + Margin, 0f);
            }

            // Ensure offers panel is visible.
            _logPanel.Show();

            // Enforce button state while panel is open.
            _logButton.state = UIButton.ButtonState.Pressed;
            _logButton.IgnoreStateChanges = true;
        }

        /// <summary>
        /// Adds a text-based tab with a status sprite to a UI tabstrip.
        /// </summary>
        /// <param name="tabStrip">UIT tabstrip to add to.</param>
        /// <param name="tabName">Name of this tab.</param>
        /// <param name="tabIndex">Index number of this tab.</param>
        /// <param name="button">Tab button instance reference.</param>
        /// <param name="sprite">Tab status sprite instance reference.</param>
        /// <param name="width">Tab width.</param>
        /// <returns>UIHelper instance for the new tab panel.</returns>
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
            tabSprite.atlas = UITextures.InGameAtlas;
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
        /// <param name="parent">Parent UIComponent.</param>
        /// <param name="xPos">Relative X position.</param>
        /// <param name="yPos">Relative Y position.</param>
        /// <param name="size">Button size (square).</param>
        /// <param name="tooltipKey">Tooltip translation key.</param>
        /// <param name="atlas">Icon atlas.</param>
        /// <returns>New UIButton.</returns>
        private TCPanelButton AddIconButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey, UITextureAtlas atlas)
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

        /// <summary>
        /// Custom button class for persistent state.
        /// </summary>
        public class TCPanelButton : UIButton
        {
            /// <summary>
            /// Gets or sets a value indicating whether state changes should be ignored (true) or responded to (false).
            /// </summary>
            public bool IgnoreStateChanges { get; set; }

            /// <summary>
            /// Called when the button state is attempted to be changed.
            /// </summary>
            /// <param name="value">New button state.</param>
            protected override void OnButtonStateChanged(ButtonState value)
            {
                // Don't do anything if we're ignoring state changes.
                if (!IgnoreStateChanges)
                {
                    base.OnButtonStateChanged(value);
                }
            }
        }
    }
}
