using System;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Transfer panel (setting restrictions for the given transfer).
    /// </summary>
    internal class TransferBuildingTab : TransferPanelTab
    {
        // Panel components.
        private readonly UICheckBox enabledCheck;
        private readonly UIButton addBuildingButton, removeBuildingButton;
        private SelectedBuildingPanel selectedBuildingPanel;

        // Status flags.
        private bool disableEvents = false;


        /// <summary>
        /// Enabled setting.
        /// </summary>
        private bool Enabled
        {
            get => BuildingControl.GetBuildingEnabled(CurrentBuilding, RecordNumber);
            set => BuildingControl.SetBuildingEnabled(CurrentBuilding, RecordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel</param>
        internal TransferBuildingTab(UIPanel parentPanel)
        {
            try
            {
                // Restrictions enabled checkbox.
                enabledCheck = UIControls.LabelledCheckBox(parentPanel, CheckMargin, EnabledCheckY, Translations.Translate("TFC_BLD_ENA"), tooltip: Translations.Translate("TFC_BLD_ENA_TIP"));
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

                // 'Add building' button.
                addBuildingButton = AddIconButton(parentPanel, MidControlX, ListY, ArrowSize, "TFC_BUI_ADD", TextureUtils.LoadSpriteAtlas("TC-ArrowPlus"));
                addBuildingButton.eventClicked += (control, clickEvent) =>
                {
                    // Add building via tool selection.
                    TCTool.Instance.SetPickMode(this);
                    TCTool.Activate();
                };

                // Remove building button.
                removeBuildingButton = AddIconButton(parentPanel, MidControlX, ListY + ArrowSize, ArrowSize, "TFC_BUI_SUB", TextureUtils.LoadSpriteAtlas("TC-ArrowMinus"));
                removeBuildingButton.isEnabled = false;
                removeBuildingButton.eventClicked += (control, clickEvent) => RemoveBuilding();

                // Building selection panel.
                selectedBuildingPanel = parentPanel.AddUIComponent<SelectedBuildingPanel>();
                selectedBuildingPanel.ParentPanel = this;
                selectedBuildingPanel.relativePosition = new Vector2(Margin, ListY);

                // Building selection panel label.
                UILabel buildingSelectionLabel = UIControls.AddLabel(selectedBuildingPanel, 0f, -15f, Translations.Translate("TFC_BUI_SEL"), ColumnWidth, 0.8f);
                buildingSelectionLabel.textAlignment = UIHorizontalAlignment.Center;

                // Set initial control states.
                UpdateEnabledStates();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up TransferBuildingTab");
            }
        }


        /// <summary>
        /// Update button states when district selections are updated.
        /// </summary>
        internal void SelectionUpdated()
        {
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

            // Add district to building and update current selection.
            selectedBuildingPanel.SelectedBuilding = buildingID;

            // Update district list.
            selectedBuildingPanel.RefreshList();
        }


        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected override void Refresh()
        {
            selectedBuildingPanel.RefreshList();

            // Disable events while we update controls to avoid recursively triggering event handler.
            disableEvents = true;
            enabledCheck.isChecked = Enabled;
            disableEvents = false;
        }


        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveBuilding()
        {
            // Remove selected district from building.
            BuildingControl.RemoveBuilding(CurrentBuilding, RecordNumber, selectedBuildingPanel.SelectedBuilding);

            // Remove selected district from building and clear current selection.
            selectedBuildingPanel.SelectedBuilding = 0;

            // Update district list.
            selectedBuildingPanel.RefreshList();
        }


        /// <summary>
        /// Toggles checkbox states based on 'Enable restrictions' setting.
        /// </summary>
        private void UpdateEnabledStates()
        {
            addBuildingButton.isVisible = enabledCheck.isChecked;
            removeBuildingButton.isVisible = enabledCheck.isChecked;
            selectedBuildingPanel.isVisible = enabledCheck.isChecked;
        }
    }
}