using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


using System.Reflection;

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
        private const float ListY = TitleHeight + Margin;
        private const float ListHeight = DistrictRow.RowHeight * 20f;
        private const float PanelHeight = ListY + ListHeight + Margin;


        // Log list.
        private readonly UIFastList logList;

        // Timer.
        private float ticks;


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

                // Log list.
                logList = UIFastList.Create<OfferRow>(this);
                logList.backgroundSprite = "UnlockingPanel";
                logList.width = width - 10f;
                logList.height = ListHeight;
                logList.canSelect = true;
                logList.rowHeight = DistrictRow.RowHeight;
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
            if (ticks > 1)
            {
                PopulateList();
                ticks = 0f;
            }
        }


        /// <summary>
        /// Populates the panel with a list of current transfers.
        /// </summary>
        private void PopulateList()
        {
            List<string> displayList = TransferLogging.EntryList();

            // Set fastlist items.
            logList.rowsData = new FastList<object>
            {
                m_buffer = displayList.ToArray(),
                m_size = displayList.Count
            };
        }
    }
}