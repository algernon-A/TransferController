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
    internal class OffersPanel : UIPanel
    {
        // Layout constants.
        private const float Margin = 5f;
        private const float PanelWidth = TransferPanel.PanelWidth;
        private const float TitleHeight = 50f;
        private const float ListY = TitleHeight + Margin;
        private const float ListHeight = DistrictRow.RowHeight * 20f;
        private const float PanelHeight = ListY + ListHeight + Margin;


        // Offer list.
        private readonly UIFastList offersList;

        // Current selection.
        private ushort currentBuilding;

        // Timer.
        private float ticks;


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        internal OffersPanel()
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
                UILabel titleLabel = UIControls.AddLabel(this, 0f, 10f, Translations.Translate("TFC_OFF_TIT"), PanelWidth, 1.2f);
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

                // Offers list.
                offersList = UIFastList.Create<OfferRow>(this);
                offersList.backgroundSprite = "UnlockingPanel";
                offersList.width = width - 10f;
                offersList.height = ListHeight;
                offersList.canSelect = true;
                offersList.rowHeight = DistrictRow.RowHeight;
                offersList.autoHideScrollbar = true;
                offersList.relativePosition = new Vector2(Margin, ListY);
                offersList.rowsData = new FastList<object>();
                offersList.selectedIndex = -1;

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
        /// Sets the target to the selected building.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        internal void SetTarget(ushort buildingID)
        {
            // Set target building and regenerate the list.
            currentBuilding = buildingID;
            PopulateList();
        }


        /// <summary>
        /// Populates the panel with a list of current transfers.
        /// </summary>
        private void PopulateList()
        {
            List<string> offerList = new List<string>();

            TransferManager tManager = Singleton<TransferManager>.instance;

            // Reflect transfer offer fields.
            FieldInfo incomingOfferField = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo outgoingOfferField = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            TransferManager.TransferOffer[] incomingOffers = incomingOfferField.GetValue(tManager) as TransferManager.TransferOffer[];
            TransferManager.TransferOffer[] outgoingOffers = outgoingOfferField.GetValue(tManager) as TransferManager.TransferOffer[];

            Logging.Message("reflection complete");

            // Find offers to this building.
            for (int i = 0; i < incomingOffers.Length; ++i)
            {
                // Calculate reason and priority blocks.
                TransferManager.TransferReason thisReason = (TransferManager.TransferReason)((i & 0xFFFFF800) >> 11);
                int priority = (i & 0x0700) >> 8;

                // Incoming offers.
                if (incomingOffers[i].Building == currentBuilding)
                {
                    Logging.Message(incomingOffers[i].ToString());
                    offerList.Add(String.Format("Incoming {0} priority {1}", thisReason, priority));
                }

                // Outgoing offers.
                if (outgoingOffers[i].Building == currentBuilding)
                {
                    Logging.Message(incomingOffers[i].ToString());
                    offerList.Add(String.Format("Outgoing {0} priority {1}", thisReason, priority));
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