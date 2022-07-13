using System;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


using System.Reflection;

namespace TransferController
{
    /// <summary>
    /// Panel to show current building offers.
    /// </summary>
    internal class OffersPanel : StatusPanelSection
    {
        // Layout constants.
        internal const float PanelWidth = OfferRow.RowWidth + ScrollBarWidth + Margin + Margin;
        internal const float PanelHeight = ListY + ListHeight + Margin;
        private const float ListHeight = StatusRow.RowHeight * 4f;


        // Offer list.
        private readonly UIFastList offersList;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal OffersPanel()
        {
            try
            {
                // Basic setup.
                size = new Vector2(PanelWidth, PanelHeight);

                // Title label.
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 0f, Translations.Translate("TFC_OFF_TIT"), PanelWidth, 1f);
                titleLabel.textAlignment = UIHorizontalAlignment.Center;

                // Header labels.
                UIControls.AddLabel(this, OfferRow.ReasonX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_MAT"), textScale: 0.7f);
                UIControls.AddLabel(this, OfferRow.PriorityX + Margin, ListHeaderY, Translations.Translate("TFC_LOG_PRI"), textScale: 0.7f);

                // Offers list.
                offersList = AddList<OfferRow>(ListY, width - 10f, ListHeight);
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
            List<OfferData> offerList = new List<OfferData>();

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
                byte priority =(byte)((i & 0x0700) >> 8);

                // Incoming offers.
                if (incomingOffers[i].Building == currentBuilding)
                {
                    // Add to list.
                    offerList.Add(new OfferData(thisReason, priority, true));
                }

                // Outgoing offers.
                if (outgoingOffers[i].Building == currentBuilding)
                {
                    // Add to list.
                    offerList.Add(new OfferData(thisReason, priority, false));
                }
            }

            // Set fastlist items.
            offersList.rowsData = new FastList<object>
            {
                m_buffer = offerList.ToArray(),
                m_size = offerList.Count
            };
        }
    }
}