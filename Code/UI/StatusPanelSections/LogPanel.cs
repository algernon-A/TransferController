// <copyright file="LogPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel to show current log entries.
    /// </summary>
    internal class LogPanel : StatusPanelSection
    {
        // Layout constants.
        private const float ListWidth = MatchRow.RowWidth + ScrollbarWidth;
        private const float PanelWidth = ListWidth + Margin + Margin;
        private const float TitleHeight = 50f;
        private const float FilterY = TitleHeight + Margin;
        private const float FilterRowHeight = 20f;
        private const float FilterRow1 = FilterY;
        private const float FilterRow2 = FilterRow1 + FilterRowHeight;
        private const float FilterRow3 = FilterRow2 + FilterRowHeight;
        private const float ListHeight = UIList.DefaultRowHeight * 20f;
        private const float LogListHeaderY = FilterRow3 + FilterRowHeight;
        private const float LogListY = LogListHeaderY + 15f;
        private const float PanelHeight = LogListY + ListHeight + Margin;
        private const float FilterColumn1 = Margin;
        private const float FilterColumn2 = 180f;
        private const float FilterColumn3 = 360f;

        // Panel components.
        private readonly UICheckBox _blockedCheck;
        private readonly UICheckBox _pathFailCheck;
        private readonly UICheckBox _noVehicleCheck;
        private readonly UICheckBox _eligibleCheck;
        private readonly UICheckBox _selectedCheck;
        private readonly UICheckBox _inCheck;
        private readonly UICheckBox _outCheck;
        private readonly UIList _logList;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogPanel"/> class.
        /// </summary>
        internal LogPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);
                backgroundSprite = "MenuPanel2";

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, 10f, Translations.Translate("TFC_OFF_LOG"), PanelWidth, 1.2f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Filter checkboxes.
                _blockedCheck = UICheckBoxes.AddLabelledCheckBox(this, FilterColumn1, FilterRow1, Translations.Translate("TFC_LOG_BLK"));
                _blockedCheck.tooltip = Translations.Translate("TFC_LOG_BLK_TIP");
                _blockedCheck.isChecked = true;
                _blockedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                _blockedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                _pathFailCheck = UICheckBoxes.AddLabelledCheckBox(this, FilterColumn1, FilterRow2, Translations.Translate("TFC_LOG_PFL"));
                _pathFailCheck.tooltip = Translations.Translate("TFC_LOG_PFL_TIP");
                _pathFailCheck.isChecked = true;
                _pathFailCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                _noVehicleCheck = UICheckBoxes.AddLabelledCheckBox(this, FilterColumn1, FilterRow3, Translations.Translate("TFC_LOG_NOV"));
                _noVehicleCheck.tooltip = Translations.Translate("TFC_LOG_NOV_TIP");
                _noVehicleCheck.isChecked = true;
                _noVehicleCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                _eligibleCheck = UICheckBoxes.AddLabelledCheckBox(this, FilterColumn2, FilterRow2, Translations.Translate("TFC_LOG_ELI"));
                _eligibleCheck.tooltip = Translations.Translate("TFC_LOG_ELI_TIP");
                _eligibleCheck.isChecked = false;
                _eligibleCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                _selectedCheck = UICheckBoxes.AddLabelledCheckBox(this, FilterColumn2, FilterRow3, Translations.Translate("TFC_LOG_SEL"));
                _selectedCheck.tooltip = Translations.Translate("TFC_LOG_SEL_TIP");
                _selectedCheck.isChecked = true;
                _selectedCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                _inCheck = UICheckBoxes.AddLabelledCheckBox(this, FilterColumn3, FilterRow2, Translations.Translate("TFC_LOG_INC"));
                _inCheck.tooltip = Translations.Translate("TFC_LOG_INC_TIP");
                _inCheck.isChecked = true;
                _inCheck.eventCheckChanged += (control, isChecked) => UpdateContent();
                _outCheck = UICheckBoxes.AddLabelledCheckBox(this, FilterColumn3, FilterRow3, Translations.Translate("TFC_LOG_OUT"));
                _outCheck.tooltip = Translations.Translate("TFC_LOG_OUT_TIP");
                _outCheck.isChecked = true;
                _outCheck.eventCheckChanged += (control, isChecked) => UpdateContent();

                // Header labels.
                UILabels.AddLabel(this, MatchRow.ReasonX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_MAT"), textScale: 0.7f);
                UILabel priorityLabel = UILabels.AddLabel(this, MatchRow.PriorityX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_PRI"), textScale: 0.7f);
                priorityLabel.relativePosition = new Vector2(MatchRow.OtherPriorityX + MatchRow.PriorityWidth - priorityLabel.width, LogListHeaderY);
                UILabels.AddLabel(this, MatchRow.TargetX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_TAR"), textScale: 0.7f);
                UILabel statusLabel = UILabels.AddLabel(this, MatchRow.StatusX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_STA"), MatchRow.StatusWidth, textScale: 0.7f);
                statusLabel.textAlignment = UIHorizontalAlignment.Center;
                UILabel timeLabel = UILabels.AddLabel(this, MatchRow.TimeX + Margin, LogListHeaderY, Translations.Translate("TFC_LOG_TIM"), MatchRow.TimeWidth, textScale: 0.7f);
                timeLabel.textAlignment = UIHorizontalAlignment.Center;

                // Log list.
                _logList = UIList.AddUIList<MatchRow>(this, Margin, LogListY, ListWidth, ListHeight);

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
            List<MatchItem> displayList = TransferLogging.EntryList(
                BuildingPanelManager.Panel.CurrentBuilding,
                _blockedCheck.isChecked,
                _pathFailCheck.isChecked,
                _noVehicleCheck.isChecked,
                _eligibleCheck.isChecked,
                _selectedCheck.isChecked,
                _inCheck.isChecked,
                _outCheck.isChecked);

            // Set fastlist items, without changing the display.
            _logList.Data = new FastList<object>
            {
                m_buffer = displayList.ToArray(),
                m_size = displayList.Count,
            };
        }
    }
}