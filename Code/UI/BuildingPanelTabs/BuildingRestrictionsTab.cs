using System;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Location restrictions tab for buildings.
    /// </summary>
    internal class BuildingRestrictionsTab : BuildingPanelTab
    {
        // Layout constants.
        internal const float PanelHeight = BuildingListY + ListHeight + Margin;
        internal const float ListHeight = 10f * DistrictRow.DefaultRowHeight;
        private const float OutsideCheckY = Margin;
        private const float EnabledCheckY = OutsideCheckY + CheckHeight;
        private const float SameDistrictCheckY = EnabledCheckY + CheckHeight;
        private const float DistrictListTitleY = SameDistrictCheckY + CheckHeight;
        private const float DistrictListY = DistrictListTitleY + CheckHeight;
        private const float BuildingListTitleY = DistrictListY + ListHeight + Margin;
        private const float BuildingListY = BuildingListTitleY + CheckHeight;
        private const float BuildingButtonX = MidControlX + ColumnWidth;


        // Panel components.
        private readonly UICheckBox enabledCheck, sameDistrictCheck, outsideCheck;
        private readonly UIButton addDistrictButton, removeDistrictButton;
        private DistrictSelectionPanel districtSelectionPanel;
        private SelectedDistrictPanel buildingDistrictSelectionPanel;
        private readonly UIButton addBuildingButton, removeBuildingButton;
        private SelectedBuildingPanel selectedBuildingPanel;

        // Status flags.
        private bool disableEvents = false;


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
                    outsideCheck.Hide();
                }
                else
                {
                    outsideCheck.text = value;

                    // Ensure visibility.
                    outsideCheck.Show();
                }
            }
        }


        /// <summary>
        /// Sets the outside connection checkbox tooltip text.
        /// </summary>
        internal string OutsideTip { set => outsideCheck.tooltip = value; }


        /// <summary>
        /// Enabled setting.
        /// </summary>
        private bool Enabled
        {
            get => BuildingControl.GetDistrictEnabled(CurrentBuilding, RecordNumber) || BuildingControl.GetBuildingEnabled(CurrentBuilding, RecordNumber);
            set
            {
                BuildingControl.SetDistrictEnabled(CurrentBuilding, RecordNumber, value, TransferReason, NextRecord);
                BuildingControl.SetBuildingEnabled(CurrentBuilding, RecordNumber, value, TransferReason, NextRecord);
            }
        }


        /// <summary>
        /// Same district setting.
        /// </summary>
        private bool SameDistrict
        {
            get => BuildingControl.GetSameDistrict(CurrentBuilding, RecordNumber);
            set => BuildingControl.SetSameDistrict(CurrentBuilding, RecordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Outside connection setting.
        /// </summary>
        private bool OutsideConnection
        {
            get => BuildingControl.GetOutsideConnection(CurrentBuilding, RecordNumber);
            set => BuildingControl.SetOutsideConnection(CurrentBuilding, RecordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel</param>
        internal BuildingRestrictionsTab(UIPanel parentPanel)
        {
            try
            {
                // Outside connection checkbox.
                // Note state is inverted - underlying flag is restrictive, but checkbox is permissive.
                outsideCheck = UIControls.LabelledCheckBox(parentPanel, CheckMargin, OutsideCheckY, Translations.Translate("TFC_BLD_IMP"), tooltip: Translations.Translate("TFC_BLD_IMP_TIP"));
                outsideCheck.isChecked = !OutsideConnection;
                outsideCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!disableEvents)
                    {
                        OutsideConnection = !isChecked;
                    }
                };

                // Restrictions enabled checkbox.
                enabledCheck = UIControls.LabelledCheckBox(parentPanel, CheckMargin, EnabledCheckY, Translations.Translate("TFC_RES_ENA"), tooltip: Translations.Translate("TFC_RES_ENA_TIP"));
                enabledCheck.isChecked = Enabled;
                enabledCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!disableEvents)
                    {
                        Enabled = isChecked;
                    }

                    // Enable/disable other controls based on new state - event status is irrelevant.
                    UpdateEnabledStates();
                };

                // Same district only checkbox.
                // Note state is inverted - underlying flag is restrictive, but checkbox is permissive.
                sameDistrictCheck = UIControls.LabelledCheckBox(parentPanel, CheckMargin, SameDistrictCheckY, Translations.Translate("TFC_BLD_SDO"), tooltip: Translations.Translate("TFC_BLD_SDO_TIP"));
                sameDistrictCheck.isChecked = !SameDistrict;
                sameDistrictCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!disableEvents)
                    {
                        SameDistrict = !isChecked;
                    }
                };

                // 'Add district' button.
                addDistrictButton = AddIconButton(parentPanel, MidControlX, DistrictListY, ArrowSize, "TFC_DIS_ADD", TextureUtils.LoadSpriteAtlas("TC-ArrowPlus"));
                addDistrictButton.isEnabled = false;
                addDistrictButton.eventClicked += (control, clickEvent) => AddDistrict(districtSelectionPanel.SelectedDistrict);

                // Remove district button.
                removeDistrictButton = AddIconButton(parentPanel, MidControlX, DistrictListY + ArrowSize, ArrowSize, "TFC_DIS_SUB", TextureUtils.LoadSpriteAtlas("TC-ArrowMinus"));
                removeDistrictButton.isEnabled = false;
                removeDistrictButton.eventClicked += (control, clickEvent) => RemoveDistrict();

                // District selection panels.
                buildingDistrictSelectionPanel = parentPanel.AddUIComponent<SelectedDistrictPanel>();
                buildingDistrictSelectionPanel.relativePosition = new Vector2(Margin, DistrictListY);
                buildingDistrictSelectionPanel.ParentPanel = this;
                districtSelectionPanel = parentPanel.AddUIComponent<DistrictSelectionPanel>();
                districtSelectionPanel.ParentPanel = this;
                districtSelectionPanel.relativePosition = new Vector2(RightColumnX, DistrictListY);

                // District selection panel labels.
                UILabel districtSelectionLabel = UIControls.AddLabel(districtSelectionPanel, 0f, -15f, Translations.Translate("TFC_DIS_AVA"), ColumnWidth, 0.8f);
                districtSelectionLabel.textAlignment = UIHorizontalAlignment.Center;
                UILabel buildingDistrictSelectionLabel = UIControls.AddLabel(buildingDistrictSelectionPanel, 0f, -15f, Translations.Translate("TFC_DIS_SEL"), ColumnWidth, 0.8f);
                buildingDistrictSelectionLabel.textAlignment = UIHorizontalAlignment.Center;

                // 'Add building' button.
                addBuildingButton = AddIconButton(parentPanel, BuildingButtonX, BuildingListY, ArrowSize, "TFC_BUI_ADD", TextureUtils.LoadSpriteAtlas("TC-RoundPlus"));
                addBuildingButton.eventClicked += (control, clickEvent) =>
                {
                    // Add building via tool selection.
                    TCTool.Instance.SetPickMode(this);
                    TCTool.Activate();
                };
                
                // Remove building button.
                removeBuildingButton = AddIconButton(parentPanel, BuildingButtonX, BuildingListY + ArrowSize, ArrowSize, "TFC_BUI_SUB", TextureUtils.LoadSpriteAtlas("TC-RoundMinus"));
                removeBuildingButton.isEnabled = false;
                removeBuildingButton.eventClicked += (control, clickEvent) => RemoveBuilding();

                // Building selection panel.
                selectedBuildingPanel = parentPanel.AddUIComponent<SelectedBuildingPanel>();
                selectedBuildingPanel.ParentPanel = this;
                selectedBuildingPanel.relativePosition = new Vector2(Margin, BuildingListY);

                // Building selection panel label.
                UILabel buildingSelectionLabel = UIControls.AddLabel(selectedBuildingPanel, 0f, -15f, Translations.Translate("TFC_BUI_SEL"), ColumnWidth, 0.8f);
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
        /// Update button states when district selections are updated.
        /// </summary>
        internal void SelectionUpdated()
        {
            addDistrictButton.isEnabled = districtSelectionPanel.SelectedDistrict != 0;
            removeDistrictButton.isEnabled = buildingDistrictSelectionPanel.SelectedDistrict != 0;
            removeBuildingButton.isEnabled = selectedBuildingPanel.SelectedBuilding != 0;
        }


        /// <summary>
        /// Adds a building to the list for this building.
        /// </summary>
        /// <param name="buildingID">Building ID to add</param>
        internal void AddBuilding(ushort buildingID)
        {
            // Add district to building.
            BuildingControl.AddBuilding(CurrentBuilding, RecordNumber, buildingID, TransferReason, NextRecord);

            // Update current selection.
            selectedBuildingPanel.SelectedBuilding = buildingID;

            // Update district list.
            selectedBuildingPanel.RefreshList();
        }


        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected override void Refresh()
        {
            buildingDistrictSelectionPanel.RefreshList();
            districtSelectionPanel.RefreshList();
            selectedBuildingPanel.RefreshList();

            // Disable events while we update controls to avoid recursively triggering event handler.
            disableEvents = true;
            outsideCheck.isChecked = !OutsideConnection;
            enabledCheck.isChecked = Enabled;
            sameDistrictCheck.isChecked = !SameDistrict;
            disableEvents = false;
        }


        /// <summary>
        /// Adds a district to the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        /// <param name="districtID">District ID to add (negated if park area)</param>
        private void AddDistrict(int districtID)
        {
            // Add district to building.
            BuildingControl.AddDistrict(CurrentBuilding, RecordNumber, districtID, TransferReason, NextRecord);

            // Update current selection.
            buildingDistrictSelectionPanel.SelectedDistrict = districtID;

            // Update district lists.
            buildingDistrictSelectionPanel.RefreshList();
            districtSelectionPanel.RefreshList();
        }


        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveDistrict()
        {
            // Remove selected district from building.
            BuildingControl.RemoveDistrict(CurrentBuilding, RecordNumber, buildingDistrictSelectionPanel.SelectedDistrict);

            // Clear current selection.
            buildingDistrictSelectionPanel.SelectedDistrict = 0;

            // Update district lists.
            buildingDistrictSelectionPanel.RefreshList();
            districtSelectionPanel.RefreshList();
        }


        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveBuilding()
        {
            // Remove selected district from building.
            BuildingControl.RemoveBuilding(CurrentBuilding, RecordNumber, selectedBuildingPanel.SelectedBuilding);

            // Clear current selection.
            selectedBuildingPanel.SelectedBuilding = 0;

            // Update district list.
            selectedBuildingPanel.RefreshList();
        }


        /// <summary>
        /// Toggles checkbox states based on 'Enable restrictions' setting.
        /// </summary>
        private void UpdateEnabledStates()
        {
            sameDistrictCheck.isVisible = enabledCheck.isChecked;
            addDistrictButton.isVisible = enabledCheck.isChecked;
            removeDistrictButton.isVisible = enabledCheck.isChecked;
            districtSelectionPanel.isVisible = enabledCheck.isChecked;
            buildingDistrictSelectionPanel.isVisible = enabledCheck.isChecked;
            addBuildingButton.isVisible = enabledCheck.isChecked;
            removeBuildingButton.isVisible = enabledCheck.isChecked;
            selectedBuildingPanel.isVisible = enabledCheck.isChecked;
        }
    }
}