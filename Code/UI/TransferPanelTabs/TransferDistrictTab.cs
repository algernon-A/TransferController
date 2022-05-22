using System;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Transfer panel (setting restrictions for the given transfer).
    /// </summary>
    internal class TransferDistrictTab : TransferPanelTab
    {
        // Panel components.
        private readonly UICheckBox enabledCheck, sameDistrictCheck;
        private readonly UIButton addDistrictButton, removeDistrictButton;
        private DistrictSelectionPanel districtSelectionPanel;
        private SelectedDistrictPanel buildingDistrictSelectionPanel;

        // Status flags.
        private bool disableEvents = false;


        /// <summary>
        /// Enabled setting.
        /// </summary>
        private bool Enabled
        {
            get => BuildingControl.GetDistrictEnabled(CurrentBuilding, RecordNumber);
            set => BuildingControl.SetDistrictEnabled(CurrentBuilding, RecordNumber, value, TransferReason, NextRecord);
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
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel</param>
        internal TransferDistrictTab(UIPanel parentPanel)
        {
            try
            {
                // Restrictions enabled checkbox.
                enabledCheck = UIControls.LabelledCheckBox(parentPanel, CheckMargin, EnabledCheckY, Translations.Translate("TFC_DIS_ENA"), tooltip: Translations.Translate("TFC_DIS_ENA_TIP"));
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
                addDistrictButton = AddIconButton(parentPanel, MidControlX, ListY, ArrowSize, "TFC_DIS_ADD", TextureUtils.LoadSpriteAtlas("TC-ArrowPlus"));
                addDistrictButton.isEnabled = false;
                addDistrictButton.eventClicked += (control, clickEvent) => AddDistrict(districtSelectionPanel.SelectedDistrict);

                // Remove district button.
                removeDistrictButton = AddIconButton(parentPanel, MidControlX, ListY + ArrowSize, ArrowSize, "TFC_DIS_SUB", TextureUtils.LoadSpriteAtlas("TC-ArrowMinus"));
                removeDistrictButton.isEnabled = false;
                removeDistrictButton.eventClicked += (control, clickEvent) => RemoveDistrict();

                // District selection panels.
                buildingDistrictSelectionPanel = parentPanel.AddUIComponent<SelectedDistrictPanel>();
                buildingDistrictSelectionPanel.relativePosition = new Vector2(Margin, ListY);
                buildingDistrictSelectionPanel.ParentPanel = this;
                districtSelectionPanel = parentPanel.AddUIComponent<DistrictSelectionPanel>();
                districtSelectionPanel.ParentPanel = this;
                districtSelectionPanel.relativePosition = new Vector2(RightColumnX, ListY);

                // District selection panel labels.
                UILabel districtSelectionLabel = UIControls.AddLabel(districtSelectionPanel, 0f, -15f, Translations.Translate("TFC_DIS_AVA"), ColumnWidth, 0.8f);
                districtSelectionLabel.textAlignment = UIHorizontalAlignment.Center;
                UILabel buildingDistrictSelectionLabel = UIControls.AddLabel(buildingDistrictSelectionPanel, 0f, -15f, Translations.Translate("TFC_DIS_SEL"), ColumnWidth, 0.8f);
                buildingDistrictSelectionLabel.textAlignment = UIHorizontalAlignment.Center;

                // Populate district selection panel (don't do the same with building panel yet, as recordNumber hasn't been assigned).
                districtSelectionPanel.RefreshList();

                // Set initial control states.
                UpdateEnabledStates();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up TransferDistrictTab");
            }
        }


        /// <summary>
        /// Update button states when district selections are updated.
        /// </summary>
        internal void SelectionUpdated()
        {
            addDistrictButton.isEnabled = districtSelectionPanel.SelectedDistrict != 0;
            removeDistrictButton.isEnabled = buildingDistrictSelectionPanel.SelectedDistrict != 0;
        }


        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected override void Refresh()
        {
            buildingDistrictSelectionPanel.RefreshList();

            // Disable events while we update controls to avoid recursively triggering event handler.
            disableEvents = true;
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

            // Add district to building and update current selection.
            buildingDistrictSelectionPanel.SelectedDistrict = districtID;

            // Update district list.
            buildingDistrictSelectionPanel.RefreshList();
        }


        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveDistrict()
        {
            // Remove selected district from building.
            BuildingControl.RemoveDistrict(CurrentBuilding, RecordNumber, buildingDistrictSelectionPanel.SelectedDistrict);

            // Remove selected district from building and clear current selection.
            buildingDistrictSelectionPanel.SelectedDistrict = 0;

            // Update district list.
            buildingDistrictSelectionPanel.RefreshList();
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
        }
    }
}