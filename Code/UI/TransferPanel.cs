using System;
using UnityEngine;
using ColossalFramework.UI;


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
        protected const float DistrictListY = OutsideCheckY + CheckHeight;
        internal const float ListHeight = 8f * DistrictRow.RowHeight;
        internal const float ColumnWidth = 210f;
        private const float ArrowSize = 32f;
        private const float MidControlX = Margin + ColumnWidth + Margin;
        protected const float RightColumnX = MidControlX + ArrowSize + Margin;
        internal const float PanelWidth = RightColumnX + ColumnWidth + Margin;
        internal const float PanelHeight = DistrictListY + ListHeight + Margin;

        // Panel components.
        private readonly UICheckBox enabledCheck, sameDistrictCheck, outsideCheck;
        private readonly UILabel directionLabel;
        private readonly UIButton addDistrictButton, removeDistrictButton;
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
            get => BuildingControl.GetEnabled(currentBuilding, recordNumber);
            set => BuildingControl.SetEnabled(currentBuilding, recordNumber, value, TransferReason, NextRecord);
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
                addDistrictButton = AddIconButton(this, MidControlX, DistrictListY, ArrowSize, "TFC_DIS_ADD", TextureUtils.LoadSpriteAtlas("TC-ArrowPlus"));
                addDistrictButton.isEnabled = false;
                addDistrictButton.eventClicked += (control, clickEvent) => AddDistrict(districtSelectionPanel.SelectedDistrict);

                // Remove district button.
                removeDistrictButton = AddIconButton(this, MidControlX, DistrictListY + ArrowSize, ArrowSize, "TFC_DIS_SUB", TextureUtils.LoadSpriteAtlas("TC-ArrowMinus"));
                removeDistrictButton.isEnabled = false;
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
            BuildingControl.AddBuildingDistrict(currentBuilding, recordNumber, districtID, TransferReason, NextRecord);

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
            BuildingControl.RemoveBuildingDistrict(currentBuilding, recordNumber, buildingDistrictSelectionPanel.SelectedDistrict);

            // Remove selected district from building and clear current selection.
            buildingDistrictSelectionPanel.SelectedDistrict = 0;

            // Update district list.
            buildingDistrictSelectionPanel.RefreshList();
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
        private UIButton AddIconButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey, UITextureAtlas atlas)
        {
            UIButton newButton = parent.AddUIComponent<UIButton>();

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