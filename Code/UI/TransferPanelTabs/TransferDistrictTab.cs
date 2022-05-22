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
        private readonly UICheckBox enabledCheck, sameDistrictCheck, outsideCheck;
        private readonly UIButton addDistrictButton, removeDistrictButton;
        private DistrictSelectionPanel districtSelectionPanel;
        private SelectedDistrictPanel buildingDistrictSelectionPanel;

        // Current selections.
        private ushort currentBuilding;
        private byte recordNumber;

        // Status flags.
        private bool disableEvents = false, outsideEligible = false;


        /// <summary>
        /// Sets the outside connection checkbox label text.
        /// </summary>
        internal string OutsideLabel
        {
            set
            {
                // Show button if text isn't null.
                if (value != null)
                {
                    outsideEligible = true;
                    outsideCheck.text = value;

                    // Only show if the 'enabled' checkbox is checked.
                    outsideCheck.isVisible = enabledCheck.isChecked;
                }
                else
                {
                    // No value - hide checkbox.
                    outsideEligible = false;
                    outsideCheck.Hide();
                }
            }
        }


        /// <summary>
        /// Sets the outside connection checkbox tooltip text.
        /// </summary>
        internal string OutsideTip { set => outsideCheck.tooltip = value; }


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
            buildingDistrictSelectionPanel.RefreshList();

            // Disable events while we update same district check avoid triggering event handler.
            disableEvents = true;
            enabledCheck.isChecked = Enabled;
            sameDistrictCheck.isChecked = !SameDistrict;
            outsideCheck.isChecked = !OutsideConnection;
            disableEvents = false;
        }


        /// <summary>
        /// Enabled setting.
        /// </summary>
        private bool Enabled
        {
            get => BuildingControl.GetDistrictEnabled(currentBuilding, recordNumber);
            set => BuildingControl.SetDistrictEnabled(currentBuilding, recordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Same district setting.
        /// </summary>
        private bool SameDistrict
        {
            get => BuildingControl.GetSameDistrict(currentBuilding, recordNumber);
            set => BuildingControl.SetSameDistrict(currentBuilding, recordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Outside connection setting.
        /// </summary>
        private bool OutsideConnection
        {
            get => BuildingControl.GetOutsideConnection(currentBuilding, recordNumber);
            set => BuildingControl.SetOutsideConnection(currentBuilding, recordNumber, value, TransferReason, NextRecord);
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

                // Outside connection checkbox.
                // Note state is inverted - underlying flag is restrictive, but checkbox is permissive.
                outsideCheck = UIControls.LabelledCheckBox(parentPanel, CheckMargin, OutsideCheckY, Translations.Translate("TFC_BLD_IMP"), tooltip: string.Empty);
                outsideCheck.isChecked = !OutsideConnection;
                outsideCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!disableEvents)
                    {
                        OutsideConnection = !isChecked;
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

                // Populate district selection panel (don't do the same with building panel yet, as recordNumber hasn't been assigned).
                districtSelectionPanel.RefreshList();

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
            addDistrictButton.isEnabled = districtSelectionPanel.SelectedDistrict != 0;
            removeDistrictButton.isEnabled = buildingDistrictSelectionPanel.SelectedDistrict != 0;
        }


        /// <summary>
        /// Adds a district to the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        /// <param name="districtID">District ID to add (negated if park area)</param>
        private void AddDistrict(int districtID)
        {
            // Add district to building.
            BuildingControl.AddDistrict(currentBuilding, recordNumber, districtID, TransferReason, NextRecord);

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
            BuildingControl.RemoveDistrict(currentBuilding, recordNumber, buildingDistrictSelectionPanel.SelectedDistrict);

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

            // Outside checkbox should only be shown if this building is eligible for outside connections.
            outsideCheck.isVisible = enabledCheck.isChecked & outsideEligible;
        }
    }
}