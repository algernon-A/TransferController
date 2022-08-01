// <copyright file="MatchRow.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Translation;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// UI fastlist item for match records.
    /// </summary>
    public class MatchRow : StatusRow
    {
        /// <summary>
        /// Target column width.
        /// </summary>
        internal const float TargetWidth = 150f;

        /// <summary>
        /// Status colum width.
        /// </summary>
        internal const float StatusWidth = 120f;

        /// <summary>
        /// Time column width.
        /// </summary>
        internal const float TimeWidth = 50f;

        /// <summary>
        /// This building's offer priority column relative position.
        /// </summary>
        internal const float ThisPriorityX = ReasonX + ReasonWidth + Margin;

        /// <summary>
        /// The other building's offer priority column relative position.
        /// </summary>
        internal const float OtherPriorityX = ThisPriorityX + PriorityWidth + Margin;

        /// <summary>
        /// The target name column relative position.
        /// </summary>
        internal const float TargetX = OtherPriorityX + PriorityWidth + Margin;

        /// <summary>
        /// The status column relative position.
        /// </summary>
        internal const float StatusX = TargetX + TargetWidth + Margin;

        /// <summary>
        /// The time  column relative position.
        /// </summary>
        internal const float TimeX = StatusX + StatusWidth + Margin;

        /// <summary>
        /// Row width.
        /// </summary>
        internal const float RowWidth = TimeX + TimeWidth + Margin;

        // Components.
        private UILabel _directionLabel;
        private UILabel _reasonLabel;
        private UILabel _thisPriorityLabel;
        private UILabel _otherPriorityLabel;
        private UILabel _targetLabel;
        private UILabel _statusLabel;
        private UILabel _timeLabel;

        // Transfer position.
        private Vector3 transferPos;

        /// <summary>
        /// Generates and displays a row.
        /// </summary>
        /// <param name="data">Object data to display.</param>
        /// <param name="rowIndex">Row index number (for background banding).</param>
        public override void Display(object data, int rowIndex)
        {
            // Perform initial setup for new rows.
            if (_directionLabel == null)
            {
                // Add text labels
                _directionLabel = AddLabel(DirectionX, DirectionWidth);
                _reasonLabel = AddLabel(ReasonX, ReasonWidth);
                _thisPriorityLabel = AddLabel(ThisPriorityX, PriorityWidth);
                _otherPriorityLabel = AddLabel(OtherPriorityX, PriorityWidth);
                _targetLabel = AddLabel(TargetX, TargetWidth);
                _statusLabel = AddLabel(StatusX, StatusWidth);
                _statusLabel.textAlignment = UIHorizontalAlignment.Center;
                _timeLabel = AddLabel(TimeX, TimeWidth);
                _timeLabel.textAlignment = UIHorizontalAlignment.Center;

                // Add mouse down event to zoom to building.
                this.eventClicked += (c, p) => OnClicked();
            }

            // Check for valid data.
            if (data is MatchItem thisMatch)
            {
                // Set ID records.
                if (thisMatch.BuildingID == thisMatch.IncomingBuildingID)
                {
                    // This was the incoming building; target position is the outgoing building.
                    BuildingID = thisMatch.OutgoingBuildingID;
                    transferPos = thisMatch.OutgoingPos;

                    // Set labels.
                    _directionLabel.text = Translations.Translate("TFC_LOG_IN");
                    _thisPriorityLabel.text = thisMatch.IncomingPriority.ToString();
                    _otherPriorityLabel.text = thisMatch.OutgoingPriority.ToString();
                    _targetLabel.text = thisMatch.OutgoingBuildingID == 0 ? string.Empty : Singleton<BuildingManager>.instance.GetBuildingName(thisMatch.OutgoingBuildingID, InstanceID.Empty);

                    // Add warehouse tages.
                    if (thisMatch.IncomingExcluded)
                    {
                        _thisPriorityLabel.text += "W";
                    }

                    if (thisMatch.OutgoingExcluded)
                    {
                        _otherPriorityLabel.text += "W";
                    }
                }
                else
                {
                    // This was the outgoing building; target position is the incoming building.
                    BuildingID = thisMatch.IncomingBuildingID;
                    transferPos = thisMatch.IncomingPos;

                    // Set labels.
                    _directionLabel.text = Translations.Translate("TFC_LOG_OU");
                    _thisPriorityLabel.text = thisMatch.OutgoingPriority.ToString();
                    _otherPriorityLabel.text = thisMatch.IncomingPriority.ToString();
                    _targetLabel.text = thisMatch.IncomingBuildingID == 0 ? string.Empty : Singleton<BuildingManager>.instance.GetBuildingName(thisMatch.IncomingBuildingID, InstanceID.Empty);

                    // Add warehouse tages.
                    if (thisMatch.OutgoingExcluded)
                    {
                        _thisPriorityLabel.text += "W";
                    }

                    if (thisMatch.IncomingExcluded)
                    {
                        _otherPriorityLabel.text += "W";
                    }
                }

                // Set other text.
                _reasonLabel.text = thisMatch.Reason.ToString();
                _timeLabel.text = thisMatch.TimeStamp.ToString("N0");

                switch (thisMatch.Status)
                {
                    case TransferLogging.MatchStatus.NotPermittedIn:
                        _statusLabel.text = Translations.Translate("TFC_LOG_BLI");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_BLI_TIP");
                        break;
                    case TransferLogging.MatchStatus.NotPermittedOut:
                        _statusLabel.text = Translations.Translate("TFC_LOG_BLO");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_BLO_TIP");
                        break;
                    case TransferLogging.MatchStatus.ImportBlocked:
                        _statusLabel.text = Translations.Translate("TFC_LOG_BXI");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_BXI_TIP");
                        break;
                    case TransferLogging.MatchStatus.ExportBlocked:
                        _statusLabel.text = Translations.Translate("TFC_LOG_BXO");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_BXO_TIP");
                        break;
                    case TransferLogging.MatchStatus.PathFailure:
                        _statusLabel.text = Translations.Translate("TFC_LOG_PFL");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_PFM_TIP");
                        break;
                    case TransferLogging.MatchStatus.NoVehicle:
                        _statusLabel.text = Translations.Translate("TFC_LOG_NOV");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_NOM_TIP");
                        break;
                    case TransferLogging.MatchStatus.Eligible:
                        _statusLabel.text = Translations.Translate("TFC_LOG_ELI");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_ELM_TIP");
                        break;
                    case TransferLogging.MatchStatus.Selected:
                        _statusLabel.text = Translations.Translate("TFC_LOG_SEL");
                        _statusLabel.tooltip = Translations.Translate("TFC_LOG_SEM_TIP");
                        break;
                    default:
                        _statusLabel.text = string.Empty;
                        _statusLabel.tooltip = null;
                        break;
                }
            }
            else
            {
                // Just in case (no valid offer record).
                _reasonLabel.text = string.Empty;
                _directionLabel.text = string.Empty;
                _thisPriorityLabel.text = string.Empty;
                _otherPriorityLabel.text = string.Empty;
                _targetLabel.text = string.Empty;
                _statusLabel.text = string.Empty;
            }

            // Set initial background as deselected state.
            Deselect(rowIndex);
        }

        /// <summary>
        /// Zooms to offer target when this row is selected.
        /// </summary>
        private void OnClicked()
        {
            if (BuildingID != 0)
            {
                BuildingPanel.ZoomToBuilding(BuildingID);
            }
            else if (transferPos != Vector3.zero)
            {
                // No valid building - move camera target position (clearing any existing target).
                ToolsModifierControl.cameraController.ClearTarget();
                ToolsModifierControl.cameraController.m_targetPosition = transferPos;
            }
        }
    }
}