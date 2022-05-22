using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Building info panel.
    /// </summary>
    internal class BuildingInfoPanel : UIPanel
    {
        // Layout constants.
        protected const float Margin = 5f;
        protected const float PanelWidth = TransferPanel.PanelWidth;
        private const float TitleHeight = 50f;
        private const float LabelHeight = 30f;
        private const float ButtonHeight = 30f;
        private const float DistrictLabelY = TitleHeight + LabelHeight;
        protected const float ControlY = DistrictLabelY + LabelHeight + Margin;
        private const float PanelXOffset = -(TransferPanel.PanelWidth + Margin);
        private const float PanelYOffset = TransferPanel.PanelHeight + Margin;
        private const float ButtonWidth = 150f;


        // Panel components.
        private readonly UILabel buildingLabel, districtLabel;

        // Current selection.
        private ushort currentBuilding;
        private BuildingInfo thisBuildingInfo;

        // Sub-panels.
        private readonly TransferStruct[] transfers = new TransferStruct[4];
        private OffersPanel offersPanel;
        private LogPanel logPanel;


        // Layout constants.
        protected virtual float PanelHeight => ControlY + ButtonHeight + Margin;
        private float Panel1Y => PanelHeight + Margin;


        // Dictionary getter.
        public static Dictionary<uint, BuildingControl.BuildingRecord> BuildingRecords => BuildingControl.buildingRecords;


        /// <summary>
        /// Current building accessor.
        /// </summary>
        internal ushort CurrentBuilding => currentBuilding;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal BuildingInfoPanel()
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
                size = new Vector2(PanelWidth, PanelHeight);

                // Default position - centre in screen.
                relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2) - TransferPanel.PanelHeight);

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

                // Offers button.
                UIButton offersButton = UIControls.AddSmallerButton(this, Margin, PanelHeight - ButtonHeight - Margin, Translations.Translate("TFC_OFF_TIT"), ButtonWidth);
                offersButton.eventClicked += ShowOffers;

                // Log button.
                UIButton logButton = UIControls.AddSmallerButton(this, Margin + ButtonWidth + Margin, PanelHeight - ButtonHeight - Margin, Translations.Translate("TFC_OFF_LOG"), ButtonWidth);
                logButton.eventClicked += ShowLog;

                // District label.
                districtLabel = UIControls.AddLabel(this, 0f, DistrictLabelY, String.Empty, PanelWidth, 0.9f);
                districtLabel.textAlignment = UIHorizontalAlignment.Center;
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
            // Local references.
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            DistrictManager districtManager = Singleton<DistrictManager>.instance;

            // Update selected building ID.
            currentBuilding = buildingID;
            thisBuildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[currentBuilding].Info;

            // Maximum number of panels.
            int numPanels = TransferDataUtils.BuildingEligibility(buildingID, thisBuildingInfo, transfers);

            // Set up used panels.
            for (int i = 0; i < numPanels; ++i)
            {
                // Create panel instance if there isn't one already there.
                if (transfers[i].panel == null)
                {
                    transfers[i].panel = this.AddUIComponent<TransferPanel>();
                    transfers[i].panel.relativePosition = new Vector2(PanelXOffset * (i >> 1), Panel1Y + ((i % 2) * PanelYOffset));
                }

                // Set panel instance properties.
                transfers[i].panel.RecordNumber = transfers[i].recordNumber;
                transfers[i].panel.NextRecord = transfers[i].nextRecord;
                transfers[i].panel.TransferReason = transfers[i].reason;
                transfers[i].panel.CurrentBuilding = currentBuilding;
                transfers[i].panel.DirectionTitle = transfers[i].panelTitle;
                transfers[i].panel.OutsideLabel = transfers[i].outsideText;
                transfers[i].panel.OutsidePanel.OutsideTip = transfers[i].outsideTip;
            }

            // Destroy any unused panels.
            for (int i = numPanels; i < transfers.Length; ++i)
            {
                if (transfers[i].panel != null)
                {
                    this.RemoveUIComponent(transfers[i].panel);
                    GameObject.Destroy(transfers[i].panel);
                    transfers[i].panel = null;
                }
            }

            // If no panels are visible, then hide and exit.
            if (numPanels == 0)
            {
                Hide();
                return;
            }

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
        }


        /// <summary>
        /// Event handler for show offers button.
        /// </summary>
        /// <param name="component">Calling component (unused)</param>
        /// <param name="clickEvent">Click event (unused)</param>
        private void ShowOffers(UIComponent component, UIMouseEventParameter clickEvent)
        {
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
        }


        /// <summary>
        /// Event handler for show log button.
        /// </summary>
        /// <param name="component">Calling component (unused)</param>
        /// <param name="clickEvent">Click event (unused)</param>
        private void ShowLog(UIComponent component, UIMouseEventParameter clickEvent)
        {
            // Create log panel if it isn't already created.
            if (logPanel == null)
            {
                logPanel = this.AddUIComponent<LogPanel>();
                logPanel.relativePosition = new Vector2(PanelWidth + Margin, 0f);
            }

            // Ensure offers panel is visible.
            logPanel.Show();
        }
    }
}