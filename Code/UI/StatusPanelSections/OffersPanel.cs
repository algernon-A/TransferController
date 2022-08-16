// <copyright file="OffersPanel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Panel to show current building offers.
    /// </summary>
    internal class OffersPanel : StatusPanelSection
    {
        /// <summary>
        /// Panel width.
        /// </summary>
        internal const float PanelWidth = OfferRow.RowWidth + ScrollbarWidth + Margin + Margin;

        /// <summary>
        /// Panel height.
        /// </summary>
        internal const float PanelHeight = ListY + ListHeight + Margin;

        // Layout constants - private.
        private const float ListHeight = UIList.DefaultRowHeight * 4f;

        // Offer list.
        private readonly UIList _offersList;

        /// <summary>
        /// Initializes a new instance of the <see cref="OffersPanel"/> class.
        /// </summary>
        internal OffersPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UILabels.AddLabel(this, 0f, Margin, Translations.Translate("TFC_OFF_TIT"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Header labels.
                UILabels.AddLabel(this, OfferRow.ReasonX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_MAT"), textScale: 0.7f);
                UILabel priorityLabel = UILabels.AddLabel(this, OfferRow.PriorityX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_PRI"), textScale: 0.7f);
                priorityLabel.relativePosition = new Vector2(PanelWidth - priorityLabel.width - Margin, ListHeaderY);

                // Offers list.
                _offersList = UIList.AddUIList<OfferRow>(this, Margin, ListY, width - 10f, ListHeight);
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
            List<OfferItem> selectedOffers = new List<OfferItem>();

            TransferManager tManager = Singleton<TransferManager>.instance;

            // Reflect transfer offer fields.
            FieldInfo incomingOfferField = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo outgoingOfferField = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            TransferManager.TransferOffer[] incomingOffers = incomingOfferField.GetValue(tManager) as TransferManager.TransferOffer[];
            TransferManager.TransferOffer[] outgoingOffers = outgoingOfferField.GetValue(tManager) as TransferManager.TransferOffer[];

            // Find offers to this building.
            for (int i = 0; i < incomingOffers.Length; ++i)
            {
                // Calculate reason and priority blocks.
                TransferManager.TransferReason thisReason = (TransferManager.TransferReason)((i & 0xFFFFF800) >> 11);
                byte priority = (byte)((i & 0x0700) >> 8);

                // Incoming offers.
                if (incomingOffers[i].Building == CurrentBuilding)
                {
                    // Add to list.
                    selectedOffers.Add(new OfferItem(thisReason, priority, true));
                }

                // Outgoing offers.
                if (outgoingOffers[i].Building == CurrentBuilding)
                {
                    // Add to list.
                    selectedOffers.Add(new OfferItem(thisReason, priority, false));
                }
            }

            // Set fastlist items, without changing the display.
            _offersList.Data = new FastList<object>()
            {
                m_buffer = selectedOffers.ToArray(),
                m_size = selectedOffers.Count,
            };
        }
    }
}