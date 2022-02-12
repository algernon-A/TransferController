using System;
using UnityEngine;
using ColossalFramework.UI;
using ColossalFramework;
using System.Collections.Generic;
using System.Linq;

namespace TransferController
{
    /// <summary>
    /// Building info panel base class.
    /// </summary>
    internal class TransferPanel : UIPanel
    {
        // Layout constants.
        protected const float Margin = 5f;
        protected const float CheckMargin = 20f;
        protected const float CheckHeight = 20f;
        protected const float ButtonHeight = 28f;
        protected const float HeadingHeight = 25f;
        protected const float HeadingY = 10f;
        protected const float EnabledCheckY = HeadingY + HeadingHeight;
        protected const float SameDistrictCheckY = EnabledCheckY + CheckHeight;
        protected const float OutsideCheckY = SameDistrictCheckY + CheckHeight;
        protected const float ButtonY = OutsideCheckY + CheckHeight;
        protected const float DistrictListY = ButtonY + ButtonHeight + Margin;
        internal const float ListHeight = 8f * DistrictRow.RowHeight;
        internal const float ColumnWidth = 210f;
        protected const float RightColumnX = ColumnWidth + (Margin * 2f);
        internal const float PanelWidth = (ColumnWidth * 2f) + (Margin * 3f);
        internal const float PanelHeight = DistrictListY + ListHeight + Margin;

        // Panel components.
        private readonly UICheckBox enabledCheck, sameDistrictCheck, outsideCheck;
        private readonly UILabel directionLabel;
        internal DistrictSelectionPanel districtSelectionPanel;
        internal BuildingDistrictSelectionPanel buildingDistrictSelectionPanel;

        // Current selections.
        private ushort currentBuilding;
        private byte recordNumber;

        // Event status.
        private bool disableEvents = false;

        
        /// <summary>
        /// Sets the direction label text.
        /// </summary>
        internal string DirectionTitle { set => directionLabel.text = value; }


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
                    outsideCheck.text = value;
                    outsideCheck.Show();
                }
                else
                {
                    // No value - hide checkbox.
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
            get => ServiceLimits.GetEnabled(currentBuilding, recordNumber);
            set => ServiceLimits.SetEnabled(currentBuilding, recordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Same district setting.
        /// </summary>
        private bool SameDistrict
        {
            get => ServiceLimits.GetSameDistrict(currentBuilding, recordNumber);
            set => ServiceLimits.SetSameDistrict(currentBuilding, recordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Outside connection setting.
        /// </summary>
        private bool OutsideConnection
        {
            get => ServiceLimits.GetOutsideConnection(currentBuilding, recordNumber);
            set => ServiceLimits.SetOutsideConnection(currentBuilding, recordNumber, value, TransferReason, NextRecord);
        }


        /// <summary>
        /// Construtor - performs initial setup.
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


                // Restrictions enabled checkbox.
                enabledCheck = UIControls.LabelledCheckBox(this, CheckMargin, EnabledCheckY, Translations.Translate("TFC_BLD_ENA"), tooltip: Translations.Translate("TFC_BLD_ENA_TIP"));
                enabledCheck.isChecked = Enabled;
                enabledCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!disableEvents)
                    {
                        Enabled = isChecked;
                    }
                };

                // Same district only checkbox.
                // Note state is inverted - underlying flag is restrictive, but checkbox is permissive.
                sameDistrictCheck = UIControls.LabelledCheckBox(this, CheckMargin, SameDistrictCheckY, Translations.Translate("TFC_BLD_SDO"), tooltip: Translations.Translate("TFC_BLD_SDO_TIP"));
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
                outsideCheck = UIControls.LabelledCheckBox(this, CheckMargin, OutsideCheckY, Translations.Translate("TFC_BLD_IMP"), tooltip: string.Empty);
                outsideCheck.isChecked = !OutsideConnection;
                outsideCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!disableEvents)
                    {
                        OutsideConnection = !isChecked;
                    }
                };

                // Direction label.
                directionLabel = UIControls.AddLabel(this, 0f, HeadingY, string.Empty, PanelWidth, 0.9f);
                directionLabel.textAlignment = UIHorizontalAlignment.Center;

                // 'Add district' button.
                UIButton addDistrictButton = UIControls.AddSmallerButton(this, Margin, ButtonY, "Add >", ColumnWidth);
                addDistrictButton.eventClicked += (control, clickEvent) => AddDistrict(districtSelectionPanel.selectedDistrict);

                // Remove district button.
                UIButton removeDistrictButton = UIControls.AddSmallerButton(this, RightColumnX, ButtonY, "< Remove", ColumnWidth);
                removeDistrictButton.eventClicked += (control, clickEvent) => RemoveDistrict();

                // District selection panels.
                districtSelectionPanel = this.AddUIComponent<DistrictSelectionPanel>();
                districtSelectionPanel.relativePosition = new Vector2(Margin, DistrictListY);
                buildingDistrictSelectionPanel = this.AddUIComponent<BuildingDistrictSelectionPanel>();
                buildingDistrictSelectionPanel.relativePosition = new Vector2(RightColumnX, DistrictListY);

                // Populate district selection panel (don't do the same with building panel yet, as recordNumber hasn't been assigned).
                districtSelectionPanel.RefreshList();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up outgoing panel");
            }
        }


        /// <summary>
        /// Adds a district to the list for this building.
        /// Should be called as base after district has been updated by child class.
        /// </summary>
        /// <param name="districtID">District ID to add (negated if park area)</param>
        private void AddDistrict(int districtID)
        {
            // Add district to building.
            ServiceLimits.AddBuildingDistrict(currentBuilding, recordNumber, districtID, TransferReason, NextRecord);

            // Add district to building and update current selection.
            buildingDistrictSelectionPanel.selectedDistrict = districtID;

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
            ServiceLimits.RemoveBuildingDistrict(currentBuilding, recordNumber, buildingDistrictSelectionPanel.selectedDistrict);

            // Remove selected district from building and clear current selection.
            buildingDistrictSelectionPanel.selectedDistrict = 0;

            // Update district list.
            buildingDistrictSelectionPanel.RefreshList();
        }
    }
}