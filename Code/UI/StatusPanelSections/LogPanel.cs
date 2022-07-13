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
        private const float FilterColumn2 = 200f;


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
                size = new Vector2(PanelWidth, PanelHeight);
                backgroundSprite = "MenuPanel2";

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_OFF_LOG"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Filter checkboxes.
                thisBuildingCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow1, Translations.Translate("TFC_LOG_BLD"));
                thisBuildingCheck.isChecked = true;
                thisBuildingCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                blockedCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow2, Translations.Translate("TFC_LOG_BLK"));
                blockedCheck.isChecked = true;
                blockedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                allowedCheck = UIControls.LabelledCheckBox(this, FilterColumn1, FilterRow3, Translations.Translate("TFC_LOG_ALW"));
                allowedCheck.isChecked = true;
                allowedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                inCheck = UIControls.LabelledCheckBox(this, FilterColumn2, FilterRow2, Translations.Translate("TFC_LOG_INC"));
                inCheck.isChecked = true;
                inCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                outCheck = UIControls.LabelledCheckBox(this, FilterColumn2, FilterRow3, Translations.Translate("TFC_LOG_OUT"));
                outCheck.isChecked = true;
                outCheck.eventCheckChanged += (control, isChecked) => UpdateContent();

                // Header labels.
                UIControls.AddLabel(this, MatchRow.ReasonX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_MAT"), textScale: 0.7f);
                UILabel priorityLabel = UIControls.AddLabel(this, MatchRow.PriorityX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_PRI"), textScale: 0.7f);
                priorityLabel.relativePosition = new Vector2(MatchRow.OtherPriorityX + MatchRow.PriorityWidth - priorityLabel.width, LogListHeaderY);
                UIControls.AddLabel(this, MatchRow.TargetX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_TAR"), textScale: 0.7f);
                UILabel allowedLabel = UIControls.AddLabel(this, MatchRow.AllowedX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_ALW"), MatchRow.AllowedWidth, textScale: 0.7f);
                allowedLabel.textAlignment = UIHorizontalAlignment.Center;

                // Log list.
                logList = AddList<MatchRow>(LogListY, ListWidth, ListHeight);

                // Populate initial data.
                //UpdateContent();
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
            List<MatchData> displayList = TransferLogging.EntryList(thisBuildingCheck.isChecked ? BuildingPanelManager.Panel.CurrentBuilding : (ushort)0, blockedCheck.isChecked, allowedCheck.isChecked, inCheck.isChecked, outCheck.isChecked);

            // Set fastlist items, without changing the display.
            logList.rowsData.m_buffer = displayList.ToArray();
            logList.rowsData.m_size = displayList.Count;
            logList.Refresh();
        }
    }
}