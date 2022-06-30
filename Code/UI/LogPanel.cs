using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Panel to show current log entries.
    /// </summary>
    internal class LogPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float PanelWidth = TransferPanel.PanelWidth;
        private const float TitleHeight = 50f;
        private const float FilterY = TitleHeight + Margin;
        private const float FilterRowHeight = 20f;
        private const float FilterRow1 = FilterY;
        private const float FilterRow2 = FilterRow1 + FilterRowHeight;
        private const float FilterRow3 = FilterRow2 + FilterRowHeight;
        private const float ListY = FilterRow3 + FilterRowHeight + Margin;
        private const float ListHeight = DistrictRow.DefaultRowHeight * 20f;
        private const float PanelHeight = ListY + ListHeight + Margin;
        private const float FilterColumn1 = Margin;
        private const float FilterColumn2 = 200f;


        // Timer.
        private float ticks;

        // Panel components.
        private readonly UICheckBox thisBuildingCheck, blockedCheck, allowedCheck, inCheck, outCheck;
        private readonly UIFastList logList;



        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal LogPanel()
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

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_OFF_LOG"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Close button.
                UIButton closeButton = AddUIComponent<UIButton>();
                closeButton.relativePosition = new Vector2(width - 35, 2);
                closeButton.normalBgSprite = "buttonclose";
                closeButton.hoveredBgSprite = "buttonclosehover";
                closeButton.pressedBgSprite = "buttonclosepressed";

                // Close button event handler.
                closeButton.eventClick += (component, clickEvent) =>
                {
                    Hide();
                };

                // Filter checkboxes.
                thisBuildingCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow1, Translations.Translate("TFC_LOG_BLD"));
                thisBuildingCheck.isChecked = true;
                thisBuildingCheck.eventCheckChanged += (control, isChecked) => PopulateList();
                blockedCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow2, Translations.Translate("TFC_LOG_BLK"));
                blockedCheck.isChecked = true;
                blockedCheck.eventCheckChanged += (control, isChecked) => PopulateList();
                allowedCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow3, Translations.Translate("TFC_LOG_ALW"));
                allowedCheck.isChecked = true;
                allowedCheck.eventCheckChanged += (control, isChecked) => PopulateList();
                inCheck = UIControls.LabelledCheckBox(this, FilterColumn2, FilterRow2, Translations.Translate("TFC_LOG_INC"));
                inCheck.isChecked = true;
                inCheck.eventCheckChanged += (control, isChecked) => PopulateList();
                outCheck = UIControls.LabelledCheckBox(this, FilterColumn2, FilterRow3, Translations.Translate("TFC_LOG_OUT"));
                outCheck.isChecked = true;
                outCheck.eventCheckChanged += (control, isChecked) => PopulateList();

                // Log list.
                logList = UIFastList.Create<OfferRow>(this);
                logList.backgroundSprite = "UnlockingPanel";
                logList.width = width - 10f;
                logList.height = ListHeight;
                logList.canSelect = true;
                logList.rowHeight = DistrictRow.DefaultRowHeight;
                logList.autoHideScrollbar = true;
                logList.relativePosition = new Vector2(Margin, ListY);
                logList.rowsData = new FastList<object>();
                logList.selectedIndex = -1;

            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up offers panel");
            }
        }


        /// <summary>
        /// Called by Unity every update.
        /// Used to refresh the list periodically.
        /// </summary>
        public override void Update()
        {
            base.Update();

            ticks += Time.deltaTime;

            // Refresh every second - maybe too much?
            if (ticks > 1f)
            {
                PopulateList();
                ticks = 0f;
            }
        }


        /// <summary>
        /// Populates the panel with a list of current transfers according to current filter settings.
        /// </summary>
        private void PopulateList()
        {
            // Get filtered log list.
            List<OfferData> displayList = TransferLogging.EntryList(thisBuildingCheck.isChecked ? BuildingPanelManager.Panel.CurrentBuilding : (ushort)0, blockedCheck.isChecked, allowedCheck.isChecked, inCheck.isChecked, outCheck.isChecked);

            // Set fastlist items, without changing the display.
            logList.rowsData.m_buffer = displayList.ToArray();
            logList.rowsData.m_size = displayList.Count;
            logList.Refresh();
        }
    }
}