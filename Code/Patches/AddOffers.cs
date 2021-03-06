// <copyright file="AddOffers.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using ColossalFramework;

    /// <summary>
    /// Harmony patches to manipulate priorities of new offers.
    /// </summary>
    public static class AddOffers
    {
        /// <summary>
        /// Harmony Prefix to manipulate priorities of incoming or outgoing offers.
        /// </summary>
        /// <param name="material">Transfer material.</param>
        /// <param name="offer">Incoming offer.</param>
        public static void AddIncomingOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer) =>
            PrioritizeOffer(material, ref offer, Building.Flags.Downgrading);

        /// <summary>
        /// Harmony Prefix to manipulate priorities of incoming or outgoing offers.
        /// </summary>
        /// <param name="material">Transfer material.</param>
        /// <param name="offer">Incoming offer.</param>
        public static void AddOutgoingOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer) =>
            PrioritizeOffer(material, ref offer, Building.Flags.Filling);

        /// <summary>
        /// Adjusts new offer priorities according to mod settings.
        /// </summary>
        /// <param name="material">Transfer material.</param>
        /// <param name="offer">Offer to prioritize.</param>
        /// <param name="warehouseFlags">Building flags to skip warehouse prioritization (e.g. skip prioritization of outgoing offers if warehouse is filling).</param>
        private static void PrioritizeOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer, Building.Flags warehouseFlags)
        {
            // Check for valid building.
            if (offer.Building != 0)
            {
                // Local references.
                ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];
                BuildingInfo buildingInfo = building.Info;
                BuildingAI buildingAI = buildingInfo.m_buildingAI;

                // Check for warehouse.
                if (buildingAI is WarehouseAI warehouseAI)
                {
                    // This is a warehouse - ignore if specified flag is set.
                    if ((building.m_flags & warehouseFlags) == 0)
                    {
                        // Check material is a warehouse good (e.g. don't want to boost crime, garbage, mail, etc.)
                        if (material == warehouseAI.m_storageType || material == (TransferManager.TransferReason)building.m_adults || material == (TransferManager.TransferReason)building.m_seniors)
                        {
                            // Eligible material - boost offer priority by global setting.
                            offer.Priority += Matching.WarehousePriority * 2;

                            // Limit priority to max of 7.
                            if (offer.Priority > 7)
                            {
                                offer.Priority = 7;
                            }
                        }
                    }
                }
            }
        }
    }
}