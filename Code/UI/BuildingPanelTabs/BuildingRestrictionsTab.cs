// <copyright file="BuildingRestrictionsTab.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Location restrictions tab for buildings.
    /// </summary>
    internal class BuildingRestrictionsTab : BuildingPanelTab
    {
        /// <summary>
        /// Panel height.
        /// </summary>
        internal const float PanelHeight = BuildingListY + ListHeight + Margin;

        /// <summary>
        /// List height.
        /// </summary>
        internal const float ListHeight = 10f * DistrictRow.DefaultRowHeight;

        // Layout constants - private.
        private const float OutsideCheckY = Margin;
        private const float PreferCheckY = OutsideCheckY + CheckHeight;
        private const float EnabledCheckY = PreferCheckY + CheckHeight;
        private const float SameDistrictCheckY = EnabledCheckY + CheckHeight;
        private const float DistrictListTitleY = SameDistrictCheckY + CheckHeight;
        private const float DistrictListY = DistrictListTitleY + CheckHeight;
        private const float BuildingListTitleY = DistrictListY + ListHeight + Margin;
        private const float BuildingListY = BuildingListTitleY + CheckHeight;
        private const float BuildingButtonX = MidControlX + ColumnWidth;

        // Panel components.
        private readonly UICheckBox _enabledCheck;
        private readonly UICheckBox _preferSameCheck;
        private readonly UICheckBox _sameDistrictCheck;
        private readonly UICheckBox _outsideCheck;
        private readonly UIButton _addDistrictButton;
        private readonly UIButton _addBuildingButton;
        private readonly UIButton _removeDistrictButton;
        private readonly UIButton _removeBuildingButton;
        private DistrictSelectionPanel _districtSelectionPanel;
        private SelectedDistrictPanel _buildingDistrictSelectionPanel;
        private SelectedBuildingPanel _selectedBuildingPanel;

        // Status flags.
        private bool _disableEvents = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingRestrictionsTab"/> class.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel.</param>
        /// <param name="tabSprite">Tab status sprite.</param>
        internal BuildingRestrictionsTab(UIPanel parentPanel, UISprite tabSprite)
            : base(parentPanel, tabSprite)
        {
            try
            {
                // Outside connection checkbox.
                // Note state is inverted - underlying flag is restrictive, but checkbox is permissive.
                _outsideCheck = UICheckBoxes.AddLabelledCheckBox(parentPanel, CheckMargin, OutsideCheckY, Translations.Translate("TFC_BLD_IMP"), tooltip: Translations.Translate("TFC_BLD_IMP_TIP"));
                _outsideCheck.isChecked = !OutsideConnection;
                _outsideCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!_disableEvents)
                    {
                        OutsideConnection = !isChecked;
                    }
                };

                // Prefer same district checkbox.
                _preferSameCheck = UICheckBoxes.AddLabelledCheckBox(parentPanel, CheckMargin, PreferCheckY, Translations.Translate("TFC_RES_PLD"), tooltip: Translations.Translate("TFC_RES_PLD_TIP"));
                _preferSameCheck.isChecked = PreferSameDistrict;
                _preferSameCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!_disableEvents)
                    {
                        PreferSameDistrict = isChecked;
                    }
                };

                // Restrictions enabled checkbox.
                _enabledCheck = UICheckBoxes.AddLabelledCheckBox(parentPanel, CheckMargin, EnabledCheckY, Translations.Translate("TFC_RES_ENA"), tooltip: Translations.Translate("TFC_RES_ENA_TIP"));
                _enabledCheck.isChecked = Enabled;
                _enabledCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!_disableEvents)
                    {
                        Enabled = isChecked;
                    }

                    // Enable/disable other controls based on new state - event status is irrelevant.
                    UpdateEnabledStates();
                };

                // Same district only checkbox.
                // Note state is inverted - underlying flag is restrictive, but checkbox is permissive.
                _sameDistrictCheck = UICheckBoxes.AddLabelledCheckBox(parentPanel, CheckMargin, SameDistrictCheckY, Translations.Translate("TFC_BLD_SDO"), tooltip: Translations.Translate("TFC_BLD_SDO_TIP"));
                _sameDistrictCheck.isChecked = !SameDistrict;
                _sameDistrictCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!_disableEvents)
                    {
                        SameDistrict = !isChecked;
                    }
                };

                // 'Add district' button.
                _addDistrictButton = UIButtons.AddIconButton(
                    parentPanel,
                    MidControlX,
                    DistrictListY,
                    ArrowSize,
                    UITextures.LoadQuadSpriteAtlas("TC-ArrowPlus"),
                    Translations.Translate("TFC_DIS_ADD"));
                _addDistrictButton.isEnabled = false;
                _addDistrictButton.eventClicked += (control, clickEvent) => AddDistrict(_districtSelectionPanel.SelectedDistrict);

                // Remove district button.
                _removeDistrictButton = UIButtons.AddIconButton(
                    parentPanel,
                    MidControlX,
                    DistrictListY + ArrowSize,
                    ArrowSize,
                    UITextures.LoadQuadSpriteAtlas("TC-ArrowMinus"),
                    Translations.Translate("TFC_DIS_SUB"));
                _removeDistrictButton.isEnabled = false;
                _removeDistrictButton.eventClicked += (control, clickEvent) => RemoveDistrict();

                // District selection panels.
                _buildingDistrictSelectionPanel = parentPanel.AddUIComponent<SelectedDistrictPanel>();
                _buildingDistrictSelectionPanel.relativePosition = new Vector2(Margin, DistrictListY);
                _buildingDistrictSelectionPanel.ParentPanel = this;
                _districtSelectionPanel = parentPanel.AddUIComponent<DistrictSelectionPanel>();
                _districtSelectionPanel.ParentPanel = this;
                _districtSelectionPanel.relativePosition = new Vector2(RightColumnX, DistrictListY);

                // District selection panel labels.
                UILabel districtSelectionLabel = UILabels.AddLabel(_districtSelectionPanel, 0f, -15f, Translations.Translate("TFC_DIS_AVA"), ColumnWidth, 0.8f);
                districtSelectionLabel.textAlignment = UIHorizontalAlignment.Center;
                UILabel buildingDistrictSelectionLabel = UILabels.AddLabel(_buildingDistrictSelectionPanel, 0f, -15f, Translations.Translate("TFC_DIS_SEL"), ColumnWidth, 0.8f);
                buildingDistrictSelectionLabel.textAlignment = UIHorizontalAlignment.Center;

                // 'Add building' button.
                _addBuildingButton = UIButtons.AddIconButton<BuildingPanel.TCPanelButton>(parentPanel, BuildingButtonX, BuildingListY, ArrowSize, UITextures.LoadQuadSpriteAtlas("TC-RoundPlus"), Translations.Translate("TFC_BUI_ADD"));
                _addBuildingButton.eventClicked += (control, clickEvent) =>
                {
                    // Add building via tool selection.
                    TCTool.Instance.SetPickMode(this);
                    TCTool.Activate();
                };

                // Remove building button.
                _removeBuildingButton = UIButtons.AddIconButton<BuildingPanel.TCPanelButton>(parentPanel, BuildingButtonX, BuildingListY + ArrowSize, ArrowSize, UITextures.LoadQuadSpriteAtlas("TC-RoundMinus"), Translations.Translate("TFC_BUI_SUB"));
                _removeBuildingButton.isEnabled = false;
                _removeBuildingButton.eventClicked += (control, clickEvent) => RemoveBuilding();

                // Building selection panel.
                _selectedBuildingPanel = parentPanel.AddUIComponent<SelectedBuildingPanel>();
                _selectedBuildingPanel.ParentPanel = this;
                _selectedBuildingPanel.relativePosition = new Vector2(Margin, BuildingListY);

                // Building selection panel label.
                UILabel buildingSelectionLabel = UILabels.AddLabel(_selectedBuildingPanel, 0f, -15f, Translations.Translate("TFC_BUI_SEL"), ColumnWidth, 0.8f);
                buildingSelectionLabel.textAlignment = UIHorizontalAlignment.Center;

                // Set initial control states.
                UpdateEnabledStates();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up BuildingRestrictionsTab");
            }
        }

        /// <summary>
        /// Gets the current content height.
        /// </summary>
        internal override float ContentHeight => _enabledCheck.isChecked ? PanelHeight : DistrictListTitleY;

        /// <summary>
        /// Sets the outside connection checkbox label text.
        /// </summary>
        internal string OutsideLabel
        {
            set
            {
                // Hide checkbox if value is null.
                if (value == null)
                {
                    _outsideCheck.Hide();
                }
                else
                {
                    _outsideCheck.text = value;

                    // Ensure visibility.
                    _outsideCheck.Show();
                }
            }
        }

        /// <summary>
        /// Sets the outside connection checkbox tooltip text.
        /// </summary>
        internal string OutsideTip { set => _outsideCheck.tooltip = value; }

        /// <summary>
        /// Gets or sets a value indicating whether outside connections are restricted (true) or permitted (false).
        /// </summary>
        private bool OutsideConnection
        {
            get => BuildingControl.GetOutsideConnection(CurrentBuilding, IsIncoming, TransferReason);

            set
            {
                BuildingControl.SetOutsideConnection(CurrentBuilding, IsIncoming, TransferReason, value);
                SetSprite();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the prefer same district setting is enabled (true) or disabled (false).
        /// </summary>
        private bool PreferSameDistrict
        {
            get => BuildingControl.GetPreferSameDistrict(CurrentBuilding, IsIncoming, TransferReason);

            set
            {
                BuildingControl.SetPreferSameDistrict(CurrentBuilding, IsIncoming, TransferReason, value);
                SetSprite();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether location restrictions are enabled (true) or disabled (false).
        /// </summary>
        private bool Enabled
        {
            get => BuildingControl.GetDistrictEnabled(CurrentBuilding, IsIncoming, TransferReason) || BuildingControl.GetBuildingEnabled(CurrentBuilding, IsIncoming, TransferReason);

            set
            {
                BuildingControl.SetDistrictEnabled(CurrentBuilding, IsIncoming, TransferReason, value);
                BuildingControl.SetBuildingEnabled(CurrentBuilding, IsIncoming, TransferReason, value);
                SetSprite();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether same district transfers are automatically permitted (false) or not (true).
        /// </summary>
        private bool SameDistrict
        {
            get => BuildingControl.GetSameDistrict(CurrentBuilding, IsIncoming, TransferReason);

            set
            {
                BuildingControl.SetSameDistrict(CurrentBuilding, IsIncoming, TransferReason, value);
                SetSprite();
            }
        }

        /// <summary>
        /// Update button states when district selections are updated.
        /// </summary>
        internal void SelectionUpdated()
        {
            _addDistrictButton.isEnabled = _districtSelectionPanel.SelectedDistrict != 0;
            _removeDistrictButton.isEnabled = _buildingDistrictSelectionPanel.SelectedDistrict != 0;
            _removeBuildingButton.isEnabled = _selectedBuildingPanel.SelectedBuilding != 0;
        }

        /// <summary>
        /// Adds a building to the list for this building.
        /// </summary>
        /// <param name="buildingID">Building ID to add.</param>
        internal void AddBuilding(ushort buildingID)
        {
            // Add district to building.
            BuildingControl.AddBuilding(CurrentBuilding, IsIncoming, TransferReason, buildingID);

            // Update current selection.
            _selectedBuildingPanel.SelectedBuilding = buildingID;

            // Update district list.
            _selectedBuildingPanel.RefreshList();
        }

        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected override void Refresh()
        {
            _buildingDistrictSelectionPanel.RefreshList();
            _districtSelectionPanel.RefreshList();
            _selectedBuildingPanel.RefreshList();

            // Disable events while we update controls to avoid recursively triggering event handler.
            _disableEvents = true;
            _outsideCheck.isChecked = !OutsideConnection;
            _preferSameCheck.isChecked = PreferSameDistrict;
            _enabledCheck.isChecked = Enabled;
            _sameDistrictCheck.isChecked = !SameDistrict;
            _disableEvents = false;

            // Resize panel.
            Panel.height = ContentHeight;

            // Set tab sprite status.
            SetSprite();
        }

        /// <summary>
        /// Adds a district to the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        /// <param name="districtID">District ID to add (negated if park area).</param>
        private void AddDistrict(int districtID)
        {
            // Add district to building.
            BuildingControl.AddDistrict(CurrentBuilding, IsIncoming, TransferReason, districtID);

            // Update current selection.
            _buildingDistrictSelectionPanel.SelectedDistrict = districtID;

            // Update district lists.
            _buildingDistrictSelectionPanel.RefreshList();
            _districtSelectionPanel.RefreshList();
        }

        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveDistrict()
        {
            // Remove selected district from building.
            BuildingControl.RemoveDistrict(CurrentBuilding, IsIncoming, TransferReason, _buildingDistrictSelectionPanel.SelectedDistrict);

            // Clear current selection.
            _buildingDistrictSelectionPanel.SelectedDistrict = 0;

            // Update district lists.
            _buildingDistrictSelectionPanel.RefreshList();
            _districtSelectionPanel.RefreshList();
        }

        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveBuilding()
        {
            // Remove selected district from building.
            BuildingControl.RemoveBuilding(CurrentBuilding, IsIncoming, TransferReason, _selectedBuildingPanel.SelectedBuilding);

            // Clear current selection.
            _selectedBuildingPanel.SelectedBuilding = 0;

            // Update district list.
            _selectedBuildingPanel.RefreshList();
        }

        /// <summary>
        /// Toggles checkbox states based on 'Enable restrictions' setting.
        /// </summary>
        private void UpdateEnabledStates()
        {
            _sameDistrictCheck.isVisible = _enabledCheck.isChecked;
            _addDistrictButton.isVisible = _enabledCheck.isChecked;
            _removeDistrictButton.isVisible = _enabledCheck.isChecked;
            _districtSelectionPanel.isVisible = _enabledCheck.isChecked;
            _buildingDistrictSelectionPanel.isVisible = _enabledCheck.isChecked;
            _addBuildingButton.isVisible = _enabledCheck.isChecked;
            _removeBuildingButton.isVisible = _enabledCheck.isChecked;
            _selectedBuildingPanel.isVisible = _enabledCheck.isChecked;

            // Resize parent panel.
            BuildingPanelManager.Panel?.RecalculateHeight();
        }

        /// <summary>
        /// Sets the tab status sprite to the correct state according to current settings.
        /// </summary>
        private void SetSprite() => StatusSprite.spriteName = BuildingControl.HasRecord(CurrentBuilding, IsIncoming, TransferReason) ? "AchievementCheckedTrue" : "AchievementCheckedFalse";
    }
}