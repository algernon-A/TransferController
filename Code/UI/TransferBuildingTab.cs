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
        // Layout constants.
        private const float ExtraListHeight = DistrictRow.RowHeight * 2f;
        private const float BuildingListY = DistrictListY - ExtraListHeight;
        internal const float BuildingListHeight = ListHeight + ExtraListHeight;

        // Panel components.
        private readonly UICheckBox enabledCheck;
        private readonly UIButton addBuildingButton, removeBuildingButton;
        private SelectedBuildingPanel selectedBuildingPanel;

        // Current selections.
        private ushort currentBuilding;
        private byte recordNumber;

        // Status flags.
        private bool disableEvents = false;


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
                    Refresh();
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
                    Refresh();
                }
            }
        }


        /// <summary>
        /// Transfer reason.
        /// </summary>
        internal TransferManager.TransferReason TransferReason { private get; set; }


        /// <summary>
        /// Other record flag.
        /// </summary>
        internal byte NextRecord { private get; set; }


        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        private void Refresh()
        {
            selectedBuildingPanel.RefreshList();

            // Disable events while we update same district check avoid triggering event handler.
            disableEvents = true;
            enabledCheck.isChecked = Enabled;
            disableEvents = false;
        }


        /// <summary>
        /// Enabled setting.
        /// </summary>
        private bool Enabled
        {
            get => BuildingControl.GetBuildingEnabled(currentBuilding, recordNumber);
            set => BuildingControl.SetBuildingEnabled(currentBuilding, recordNumber, value, TransferReason, NextRecord);
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
                addBuildingButton = AddIconButton(parentPanel, MidControlX, BuildingListY, ArrowSize, "TFC_BUI_ADD", TextureUtils.LoadSpriteAtlas("TC-ArrowPlus"));
                addBuildingButton.eventClicked += (control, clickEvent) =>
                {
                    // Add building via tool selection.
                    TCTool.transferBuildingTab = this;
                    TCTool.pickMode = true;
                    TCTool.Activate();
                };

                // Remove building button.
                removeBuildingButton = AddIconButton(parentPanel, MidControlX, BuildingListY + ArrowSize, ArrowSize, "TFC_BUI_SUB", TextureUtils.LoadSpriteAtlas("TC-ArrowMinus"));
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
                Logging.LogException(e, "exception setting up outgoing panel");
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
            BuildingControl.AddBuilding(currentBuilding, recordNumber, buildingID, TransferReason, NextRecord);

            // Add district to building and update current selection.
            selectedBuildingPanel.SelectedBuilding = buildingID;

            // Update district list.
            selectedBuildingPanel.RefreshList();
        }


        /// <summary>
        /// Removes the currently selected district from the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        private void RemoveBuilding()
        {
            // Remove selected district from building.
            BuildingControl.RemoveBuilding(currentBuilding, recordNumber, selectedBuildingPanel.SelectedBuilding);

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