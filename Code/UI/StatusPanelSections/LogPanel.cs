using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Panel to show current log entries.
    /// </summary>
    internal class LogPanel : StatusPanelSection
    {
        // Layout constants.
        private const float ListWidth = MatchRow.RowWidth + ScrollBarWidth;
        private const float PanelWidth = ListWidth + Margin + Margin;
        private const float TitleHeight = 50f;
        private const float FilterY = TitleHeight + Margin;
        private const float FilterRowHeight = 20f;
        private const float FilterRow1 = FilterY;
        private const float FilterRow2 = FilterRow1 + FilterRowHeight;
        private const float FilterRow3 = FilterRow2 + FilterRowHeight;
        private const float ListHeight = StatusRow.RowHeight * 20f;
        private const float LogListHeaderY = FilterRow3 + FilterRowHeight;
        private const float LogListY = LogListHeaderY + 15f;
        private const float PanelHeight = LogListY + ListHeight + Margin;
        private const float FilterColumn1 = Margin;
        private const float FilterColumn2 = 180f;
        private const float FilterColumn3 = 360f;


        // Panel components.
        private readonly UICheckBox blockedCheck, pathFailCheck, noVehicleCheck, eligibleCheck, selectedCheck, inCheck, outCheck;
        private readonly UIFastList logList;



        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal LogPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);
                backgroundSprite = "MenuPanel2";

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_OFF_LOG"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Filter checkboxes.
                blockedCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow1, Translations.Translate("TFC_LOG_BLK"));
                blockedCheck.tooltip = Translations.Translate("TFC_LOG_BLK_TIP");
                blockedCheck.isChecked = true;
                blockedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                blockedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                pathFailCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow2, Translations.Translate("TFC_LOG_PFL"));
                pathFailCheck.tooltip = Translations.Translate("TFC_LOG_PFL_TIP");
                pathFailCheck.isChecked = true;
                pathFailCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                noVehicleCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow3, Translations.Translate("TFC_LOG_NOV"));
                noVehicleCheck.tooltip = Translations.Translate("TFC_LOG_NOV_TIP");
                noVehicleCheck.isChecked = true;
                noVehicleCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                eligibleCheck = UIControls.LabelledCheckBox(this, FilterColumn2, FilterRow2, Translations.Translate("TFC_LOG_ELI"));
                eligibleCheck.tooltip = Translations.Translate("TFC_LOG_ELI_TIP");
                eligibleCheck.isChecked = false;
                eligibleCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                selectedCheck = UIControls.LabelledCheckBox(this, FilterColumn2, FilterRow3, Translations.Translate("TFC_LOG_SEL"));
                selectedCheck.tooltip = Translations.Translate("TFC_LOG_SEL_TIP");
                selectedCheck.isChecked = true;
                selectedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                inCheck = UIControls.LabelledCheckBox(this, FilterColumn3, FilterRow2, Translations.Translate("TFC_LOG_INC"));
                inCheck.tooltip = Translations.Translate("TFC_LOG_INC_TIP");
                inCheck.isChecked = true;
                inCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                outCheck = UIControls.LabelledCheckBox(this, FilterColumn3, FilterRow3, Translations.Translate("TFC_LOG_OUT"));
                outCheck.tooltip = Translations.Translate("TFC_LOG_OUT_TIP");
                outCheck.isChecked = true;
                outCheck.eventCheckChanged += (control, isChecked) => UpdateContent();

                // Header labels.
                UIControls.AddLabel(this, MatchRow.ReasonX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_MAT"), textScale: 0.7f);
                UILabel priorityLabel = UIControls.AddLabel(this, MatchRow.PriorityX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_PRI"), textScale: 0.7f);
                priorityLabel.relativePosition = new Vector2(MatchRow.OtherPriorityX + MatchRow.PriorityWidth - priorityLabel.width, LogListHeaderY);
                UIControls.AddLabel(this, MatchRow.TargetX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_TAR"), textScale: 0.7f);
                UILabel statusLabel = UIControls.AddLabel(this, MatchRow.AllowedX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_STA"), MatchRow.StatusWidth, textScale: 0.7f);
                statusLabel.textAlignment = UIHorizontalAlignment.Center;
                UILabel timeLabel = UIControls.AddLabel(this, MatchRow.TimeX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_TIM"), MatchRow.TimeWidth, textScale: 0.7f);
                timeLabel.textAlignment = UIHorizontalAlignment.Center;

                // Log list.
                logList = AddList<MatchRow>(LogListY, ListWidth, ListHeight);

                // Populate initial data.
                UpdateContent();
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up offers panel");
            }
        }


        /// <summary>
        /// Updates panel content.
        /// </summary>
        protected override void UpdateContent()
        {
            // Get filtered log list.
            List<MatchData> displayList = TransferLogging.EntryList(BuildingPanelManager.Panel.CurrentBuilding,
                blockedCheck.isChecked,
                pathFailCheck.isChecked,
                noVehicleCheck.isChecked,
                eligibleCheck.isChecked,
                selectedCheck.isChecked,
                inCheck.isChecked,
                outCheck.isChecked);

            // Set fastlist items, without changing the display.
            logList.rowsData.m_buffer = displayList.ToArray();
            logList.rowsData.m_size = displayList.Count;
            logList.Refresh();
        }
    }
}