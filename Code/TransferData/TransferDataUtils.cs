using ColossalFramework;


namespace TransferController
{
    /// <summary>
    /// Struct to hold basic transfer information.
    /// </summary>
    public struct TransferStruct
    {
        internal TransferPanel panel;
        public string panelTitle, outsideText, outsideTip;
        public TransferManager.TransferReason reason;
        public byte recordNumber;
        public byte nextRecord;
    }


    /// <summary>
    /// Transfer data utilities.
    /// </summary>
    internal static class TransferDataUtils
    {
        /// <summary>
        /// Checks if the given building has supported transfer types.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="transfers">Transfer structure array to populate (size 4)</param>
        /// <returns>True if any transfers are supported for this building, false if none</returns>
        internal static bool BuildingEligibility(ushort buildingID, TransferStruct[] transfers) => BuildingEligibility(buildingID, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info, transfers) > 0;


        /// <summary>
        /// Determines the eligible transfers (if any) for the given building.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="buildingInfo">BuildingInfo record of building</param>
        /// <param name="transfers">Transfer structure array to populate (size 4)</param>
        /// <returns>Number of eligible transfers</returns>
        internal static int BuildingEligibility(ushort buildingID, BuildingInfo buildingInfo, TransferStruct[] transfers)
        {
            switch (buildingInfo.GetService())
            {
                case ItemClass.Service.Education:
                case ItemClass.Service.HealthCare:
                case ItemClass.Service.PlayerEducation:
                    // Basic service offering; incoming restrictions only, generic title, no specific reason.
                    transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                    transfers[0].outsideText = null;
                    transfers[0].recordNumber = ServiceLimits.IncomingMask;
                    transfers[0].reason = TransferManager.TransferReason.None;
                    transfers[0].nextRecord = 0;
                    return 1;
                case ItemClass.Service.FireDepartment:
                    // Basic service offering; incoming restrictions only, generic title, no specific reason.
                    transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                    transfers[0].outsideText = null;
                    transfers[0].recordNumber = ServiceLimits.IncomingMask;
                    transfers[0].reason = TransferManager.TransferReason.Fire;
                    transfers[0].nextRecord = 0;

                    if (buildingInfo.GetAI() is HelicopterDepotAI)
                    {
                        transfers[1].panelTitle = "Forest fire restrictions";
                        transfers[1].outsideText = null;
                        transfers[1].recordNumber = ServiceLimits.IncomingMask + 1;
                        transfers[1].reason = TransferManager.TransferReason.Fire2;
                        transfers[1].nextRecord = 0;
                        return 2;
                    }

                    return 1;
                case ItemClass.Service.Water:
                    // Basic service offering; incoming restrictions only, generic title, no specific reason.
                    if(buildingInfo.GetAI() is WaterFacilityAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = ServiceLimits.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.FloodWater;
                        transfers[0].nextRecord = 0;
                        return 1;
                    }
                    return 0;
                case ItemClass.Service.Disaster:
                    transfers[0].panelTitle = "Disastor response vehicle restrictions";
                    transfers[0].outsideText = null;
                    transfers[0].recordNumber = ServiceLimits.IncomingMask;
                    transfers[0].reason = TransferManager.TransferReason.Collapsed;
                    transfers[0].nextRecord = 0;
                    transfers[1].panelTitle = "Disastor response helicopter restrictions";
                    transfers[1].outsideText = null;
                    transfers[1].recordNumber = ServiceLimits.IncomingMask + 1;
                    transfers[1].reason = TransferManager.TransferReason.Collapsed2;
                    transfers[1].nextRecord = 0;
                    return 2;
                case ItemClass.Service.PoliceDepartment:
                    var building = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

                    // Police helicopter depot
                    if(buildingInfo.GetAI() is HelicopterDepotAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = ServiceLimits.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Crime;
                        transfers[0].nextRecord = 0;

                        // Prison Helicopter Mod
                        if ((building.m_flags & Building.Flags.Downgrading) == 0)
                        {
                            transfers[1].panelTitle = "Prison Helicopters restrictions";
                            transfers[1].outsideText = null;
                            transfers[1].recordNumber = ServiceLimits.IncomingMask + 1;
                            transfers[1].reason = (TransferManager.TransferReason)126;
                            transfers[1].nextRecord = 0;

                             return 2;
                        }

                        return 1;
                    }
                    else 
                    {
                        // Prison
                        if(buildingInfo.m_class.m_level >= ItemClass.Level.Level4)
                        {
                            transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = ServiceLimits.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.CriminalMove;
                            transfers[0].nextRecord = 0;

                            // Prison Helicopter Mod
                            if (buildingInfo.m_buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI"))
                            {
                                transfers[1].panelTitle = "Prisoners transfer via helicopters";
                                transfers[1].outsideText = null;
                                transfers[1].recordNumber = ServiceLimits.OutgoingMask;
                                transfers[1].reason = (TransferManager.TransferReason)126;
                                transfers[1].nextRecord = 0;
                                return 2;
                            }

                            return 1;
                        }
                        else
                        {
                            // Normal police station
                            transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = ServiceLimits.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.Crime;
                            transfers[0].nextRecord = 0;
                            transfers[1].panelTitle = Translations.Translate("TFC_POL_CMO");
                            transfers[1].outsideText = null;
                            transfers[1].recordNumber = ServiceLimits.OutgoingMask;
                            transfers[1].reason = TransferManager.TransferReason.CriminalMove;
                            transfers[1].nextRecord = 0;

                            // Prison Helicopter Mod
                            if (buildingInfo.m_buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI"))
                            {
                                // Small police station
                                if ((building.m_flags & Building.Flags.Downgrading) != 0)
                                {
                                    transfers[2].panelTitle = "Criminal transfer to a central police station";
                                    transfers[2].outsideText = null;
                                    transfers[2].recordNumber = ServiceLimits.OutgoingMask + 1;
                                    transfers[2].reason = (TransferManager.TransferReason)125;
                                    transfers[2].nextRecord = 0;
                                    return 3;
                                }
                                // Big police station
                                else
                                {
                                    transfers[2].panelTitle = "Criminal air transfer to prison";
                                    transfers[2].outsideText = null;
                                    transfers[2].recordNumber = ServiceLimits.OutgoingMask + 1;
                                    transfers[2].reason = (TransferManager.TransferReason)126;
                                    transfers[2].nextRecord = 0;
                                    transfers[3].panelTitle = "Criminal transfer from a local police stations";
                                    transfers[3].outsideText = null;
                                    transfers[3].recordNumber = ServiceLimits.IncomingMask + 1;
                                    transfers[3].reason = (TransferManager.TransferReason)125;
                                    transfers[3].nextRecord = 0;
                                    return 4;
                                }
                            }

                            return 2;
                        }
                    }
                case ItemClass.Service.Industrial:
                case ItemClass.Service.PlayerIndustry:
                    // Industrial buildings get both incoming and outgoing restrictions (buy/sell).
                    transfers[0].panelTitle = Translations.Translate("TFC_GEN_BUY");
                    transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                    transfers[0].outsideTip = Translations.Translate("TFC_BLD_IMP_TIP");
                    transfers[0].recordNumber = ServiceLimits.IncomingMask;
                    transfers[0].reason = TransferManager.TransferReason.None;
                    transfers[0].nextRecord = 0;
                    transfers[1].panelTitle = Translations.Translate("TFC_GEN_SEL");
                    transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                    transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                    transfers[1].recordNumber = ServiceLimits.OutgoingMask;
                    transfers[1].reason = TransferManager.TransferReason.None;
                    transfers[1].nextRecord = 0;
                    return 2;

                case ItemClass.Service.Road:
                case ItemClass.Service.Beautification:
                    // Maintenance depots only, and only incoming.
                    if (buildingInfo.GetAI() is MaintenanceDepotAI || buildingInfo.GetAI() is SnowDumpAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = ServiceLimits.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.None;
                        transfers[0].nextRecord = 0;
                        return 1;
                    }
                    Logging.Message("undefined road or beautification service");
                    return 0;

                case ItemClass.Service.PublicTransport:
                    if (buildingInfo.GetAI() is PostOfficeAI postOfficeAI)
                    {
                        // Post office vs. mail sorting facility - post offices have vans.
                        if (postOfficeAI.m_postVanCount > 0)
                        {
                            // Post office.
                            transfers[0].panelTitle = Translations.Translate("TFC_MAI_IML");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = ServiceLimits.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.Mail;
                            transfers[0].nextRecord = ServiceLimits.IncomingMask + 1;

                            transfers[1].panelTitle = Translations.Translate("TFC_MAI_OSD");
                            transfers[1].outsideText = null;
                            transfers[1].recordNumber = ServiceLimits.OutgoingMask;
                            transfers[1].reason = TransferManager.TransferReason.SortedMail;
                            transfers[1].nextRecord = ServiceLimits.OutgoingMask + 1;

                            transfers[2].panelTitle = Translations.Translate("TFC_MAI_OUN");
                            transfers[2].outsideText = null;
                            transfers[2].recordNumber = ServiceLimits.OutgoingMask + 1;
                            transfers[2].reason = TransferManager.TransferReason.UnsortedMail;
                            transfers[2].nextRecord = 0;

                            transfers[3].panelTitle = Translations.Translate("TFC_MAI_IST");
                            transfers[3].outsideText = null;
                            transfers[3].recordNumber = ServiceLimits.IncomingMask + 1;
                            transfers[3].reason = TransferManager.TransferReason.SortedMail;
                            transfers[3].nextRecord = 0;

                            return 4;
                        }

                        // Mail sorting facility.
                        transfers[0].panelTitle = Translations.Translate("TFC_MAI_IUN");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = ServiceLimits.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.UnsortedMail;
                        transfers[0].nextRecord = 0;

                        transfers[1].panelTitle = Translations.Translate("TFC_MAI_OST");
                        transfers[1].outsideText = null;
                        transfers[1].recordNumber = ServiceLimits.OutgoingMask;
                        transfers[1].reason = TransferManager.TransferReason.SortedMail;
                        transfers[1].nextRecord = 0;

                        return 2;
                    }
                    Logging.Message("undefined public transport service");
                    return 0;

                case ItemClass.Service.Garbage:
                    int numTransfers = 0;
                    byte incomingIndex = 0, outgoingIndex = 0;
                    if (buildingInfo.GetAI() is LandfillSiteAI landfillAI)
                    {
                        if (buildingInfo.GetClassLevel() == ItemClass.Level.Level4)
                        {
                            // Waste transfer facility (level 4).
                            transfers[numTransfers].panelTitle = Translations.Translate("TFC_GAR_ITF");
                            transfers[numTransfers].outsideText = null;
                            transfers[numTransfers].recordNumber = (byte)(ServiceLimits.IncomingMask + incomingIndex++);
                            transfers[numTransfers].reason = TransferManager.TransferReason.GarbageTransfer;
                            transfers[numTransfers++].nextRecord = 0;
                        }
                        else
                        {
                            // Basic garbage collection (waste transfer facility is level 4).
                            transfers[numTransfers].panelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[numTransfers].outsideText = null;
                            transfers[numTransfers].recordNumber = (byte)(ServiceLimits.IncomingMask + incomingIndex++);
                            transfers[numTransfers].reason = TransferManager.TransferReason.Garbage;
                            transfers[numTransfers++].nextRecord = 0;

                            // Waste transfer to facility.
                            transfers[numTransfers].panelTitle = Translations.Translate("TFC_GAR_OTF");
                            transfers[numTransfers].outsideText = null;
                            transfers[numTransfers].recordNumber = (byte)(ServiceLimits.OutgoingMask + outgoingIndex++);
                            transfers[numTransfers].reason = TransferManager.TransferReason.GarbageTransfer;
                            transfers[numTransfers++].nextRecord = 0;
                        }

                        if (landfillAI.m_materialProduction != 0)
                        {
                            // Recycling centre - add material delivery.
                            transfers[numTransfers].panelTitle = Translations.Translate("TFC_GAR_ORR");
                            transfers[numTransfers].outsideText = null;
                            transfers[numTransfers].recordNumber = (byte)(ServiceLimits.OutgoingMask + outgoingIndex++);
                            transfers[numTransfers].reason = TransferManager.TransferReason.None;
                            transfers[numTransfers++].nextRecord = 0;
                        }

                        return numTransfers;
                    }

                    // Undefined service.
                    Logging.Message("undefined garbage service");
                    return 0;

                case ItemClass.Service.Fishing:
                    if(buildingInfo.GetAI() is FishFarmAI || buildingInfo.GetAI() is FishingHarborAI)
                    {
                        transfers[0].panelTitle = "Deliver fish";
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = ServiceLimits.OutgoingMask;
                        transfers[0].reason = TransferManager.TransferReason.Fish;
                        transfers[0].nextRecord = 0;
                        return 1;
                    }
                    else if(buildingInfo.GetAI() is MarketAI)
                    {
                        transfers[0].panelTitle = "Fish delivery from harbor";
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = ServiceLimits.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Fish;
                        transfers[0].nextRecord = 0;
                        return 1;
                    }
                    else if(buildingInfo.GetAI() is ProcessingFacilityAI)
                    {
                        transfers[0].panelTitle = "Fish Delivery from harbor";
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = ServiceLimits.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Fish;
                        transfers[0].nextRecord = 0;
                        transfers[1].panelTitle = "Canned fish for sale";
                        transfers[1].outsideText = null;
                        transfers[1].recordNumber = ServiceLimits.OutgoingMask;
                        transfers[1].reason = TransferManager.TransferReason.Goods;
                        transfers[1].nextRecord = 0;
                        return 2;
                    }
                    // Undefined service.
                    Logging.Message("undefined fish service");
                    return 0;

                default:
                    // If not explicitly supported, then it's not supported.
                    return 0;
            }
        }
    }
}