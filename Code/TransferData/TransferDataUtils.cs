using System.Collections.Generic;
using ColossalFramework;


namespace TransferController
{
    /// <summary>
    /// Struct to hold basic transfer information.
    /// </summary>
    public struct TransferStruct
    {
        internal BuildingRestrictionsTab panel;
        public string panelTitle, outsideText, outsideTip;
        public TransferManager.TransferReason reason;
        public byte recordNumber;
        public byte nextRecord;
        public bool spawnsVehicles;
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
        /// Thanks to t1a2l for doing a bunch of these.
        /// </summary>
        /// <param name="buildingID">ID of building to check</param>
        /// <param name="buildingInfo">BuildingInfo record of building</param>
        /// <param name="transfers">Transfer structure array to populate (size 4)</param>
        /// <returns>Number of eligible transfers</returns>
        internal static int BuildingEligibility(ushort buildingID, BuildingInfo buildingInfo, TransferStruct[] transfers)
        {
            switch (buildingInfo.GetService())
            {
                // Education - nothing special.
                case ItemClass.Service.Education:
                case ItemClass.Service.PlayerEducation:
                    transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                    transfers[0].outsideText = null;
                    transfers[0].recordNumber = BuildingControl.IncomingMask;
                    transfers[0].reason = TransferManager.TransferReason.None;
                    transfers[0].nextRecord = 0;
                    transfers[0].spawnsVehicles = false;
                    return 1;

                // Healthcare.
                case ItemClass.Service.HealthCare:
                    bool isHelicopter = buildingInfo.m_buildingAI is HelicopterDepotAI;
                    if (isHelicopter || buildingInfo.m_buildingAI is HospitalAI)
                    {
                        // Hosptials, clinics, and medical helicopter depots.
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = isHelicopter ? TransferManager.TransferReason.Sick2 : TransferManager.TransferReason.Sick;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = true;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is CemeteryAI)
                    {
                        // Deathcare.
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Dead;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = true;
                        return 1;
                    }

                    // Any other healthcare buildings (e.g. SaunaAI) aren't supported.
                    return 0;

                // Fire.
                case ItemClass.Service.FireDepartment:
                    transfers[0].panelTitle = Translations.Translate("TFC_FIR_SER");
                    transfers[0].reason = buildingInfo.m_buildingAI is HelicopterDepotAI ? TransferManager.TransferReason.Fire2 : TransferManager.TransferReason.Fire;
                    transfers[0].outsideText = null;
                    transfers[0].recordNumber = BuildingControl.IncomingMask;
                    transfers[0].nextRecord = 0;
                    transfers[0].spawnsVehicles = true;
                    return 1;

                case ItemClass.Service.Water:
                    // Water pumping.
                    if (buildingInfo.GetAI() is WaterFacilityAI waterFacilityAI && buildingInfo.m_class.m_level == ItemClass.Level.Level1 && waterFacilityAI.m_pumpingVehicles > 0)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.FloodWater;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = true;
                        return 1;
                    }
                    // Boiler station - imports oil.
                    else if (buildingInfo.GetAI() is HeatingPlantAI heatingPlantAI && buildingInfo.m_class.m_level == ItemClass.Level.Level2 && heatingPlantAI.m_resourceType == TransferManager.TransferReason.Oil)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_OIL_INC");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                        transfers[0].outsideTip = Translations.Translate("TFC_BLD_IMP_TIP");
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Oil;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = false;
                        return 1;
                    }
                    return 0;

                case ItemClass.Service.Disaster:
                    // Disaster response - trucks and helicopters.
                    if(buildingInfo.GetAI() is DisasterResponseBuildingAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_DIS_TRU");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Collapsed;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = true;
                        transfers[1].panelTitle = Translations.Translate("TFC_DIS_HEL");
                        transfers[1].outsideText = null;
                        transfers[1].recordNumber = BuildingControl.IncomingMask + 1;
                        transfers[1].reason = TransferManager.TransferReason.Collapsed2;
                        transfers[1].nextRecord = 0;
                        transfers[1].spawnsVehicles = true;
                        return 2;
                    }
                    // Sheters import goods (supplies).
                    else if (buildingInfo.GetAI() is ShelterAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_SHT_INC");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                        transfers[0].outsideTip = Translations.Translate("TFC_BLD_IMP_TIP");
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.None;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = false;
                        return 1;
                    }
                    return 0;

                case ItemClass.Service.Electricity:
                    // import oil and coal for power plants
                    if(buildingInfo.GetAI() is PowerPlantAI powerPlantAI && powerPlantAI.m_resourceType != TransferManager.TransferReason.None)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_PWR_INC") + powerPlantAI.m_resourceType.ToString();
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                        transfers[0].outsideTip = Translations.Translate("TFC_BLD_IMP_TIP");
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = powerPlantAI.m_resourceType;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = false;
                        return 1;
                    }
                    return 0;

                case ItemClass.Service.PoliceDepartment:
                    Building.Flags buildingFlags = Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_flags;

                    // Police helicopter depot.
                    if (buildingInfo.GetAI() is HelicopterDepotAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Crime;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = true;

                        // Prison Helicopter Mod.
                        if ((buildingFlags & Building.Flags.Downgrading) == Building.Flags.None)
                        {
                            transfers[1].panelTitle = Translations.Translate("TFC_POL_PHI");
                            transfers[1].outsideText = null;
                            transfers[1].recordNumber = BuildingControl.IncomingMask + 1;
                            transfers[1].reason = (TransferManager.TransferReason)126;
                            transfers[1].nextRecord = 0;
                            transfers[1].spawnsVehicles = true;
                            return 2;
                        }

                        return 1;
                    }
                    else
                    {
                        // Prisons.
                        if (buildingInfo.m_class.m_level >= ItemClass.Level.Level4)
                        {
                            transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = BuildingControl.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.CriminalMove;
                            transfers[0].nextRecord = 0;
                            transfers[0].spawnsVehicles = true;
                            return 1;
                        }
                        else
                        {
                            // Normal police station.
                            transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER"); // police service restrictions
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = BuildingControl.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.Crime;
                            transfers[0].nextRecord = 0;
                            transfers[0].spawnsVehicles = true;
                            transfers[1].panelTitle = Translations.Translate("TFC_POL_PMO"); // Prisoner transfer by van to prison
                            transfers[1].outsideText = null;
                            transfers[1].recordNumber = BuildingControl.OutgoingMask;
                            transfers[1].reason = TransferManager.TransferReason.CriminalMove;
                            transfers[1].nextRecord = 0;
                            transfers[1].spawnsVehicles = true;

                            // Prison Helicopter Mod.
                            if (buildingInfo.m_buildingAI.GetType().Name.Equals("PrisonCopterPoliceStationAI"))
                            {
                                // Small (local) police station
                                if ((buildingFlags & Building.Flags.Downgrading) != Building.Flags.None)
                                {
                                    transfers[2].panelTitle = Translations.Translate("TFC_POL_PTO"); // send prisoners to a central station
                                    transfers[2].outsideText = null;
                                    transfers[2].recordNumber = BuildingControl.OutgoingMask + 1;
                                    transfers[2].reason = (TransferManager.TransferReason)125;
                                    transfers[2].nextRecord = 0;
                                    transfers[2].spawnsVehicles = false;
                                    return 3;
                                }
                                // Big (central) police station
                                else
                                {
                                    transfers[2].panelTitle = Translations.Translate("TFC_POL_PHO"); // transfer prisoners by helicopter to prison
                                    transfers[2].outsideText = null;
                                    transfers[2].recordNumber = BuildingControl.OutgoingMask + 1;
                                    transfers[2].reason = (TransferManager.TransferReason)126;
                                    transfers[2].nextRecord = 0;
                                    transfers[2].spawnsVehicles = false;
                                    transfers[3].panelTitle = Translations.Translate("TFC_POL_PTI"); // transfer prisoners from local stations
                                    transfers[3].outsideText = null;
                                    transfers[3].recordNumber = BuildingControl.IncomingMask + 1;
                                    transfers[3].reason = (TransferManager.TransferReason)125;
                                    transfers[3].nextRecord = 0;
                                    transfers[3].spawnsVehicles = false;
                                    return 4;
                                }
                            }

                            return 2;
                        }
                    }

                case ItemClass.Service.Industrial:
                    // Industrial buildings get both incoming and outgoing restrictions (buy/sell).
                    transfers[0].panelTitle = Translations.Translate("TFC_GEN_BUY");
                    transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                    transfers[0].outsideTip = Translations.Translate("TFC_BLD_IMP_TIP");
                    transfers[0].recordNumber = BuildingControl.IncomingMask;
                    transfers[0].reason = TransferManager.TransferReason.None;
                    transfers[0].nextRecord = 0;
                    transfers[0].spawnsVehicles = false;
                    transfers[1].panelTitle = Translations.Translate("TFC_GEN_SEL");
                    transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                    transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                    transfers[1].recordNumber = BuildingControl.OutgoingMask;
                    transfers[1].reason = TransferManager.TransferReason.None;
                    transfers[1].nextRecord = 0;
                    transfers[1].spawnsVehicles = true;
                    return 2;

                case ItemClass.Service.PlayerIndustry: // industries dlc
                    if (buildingInfo.m_buildingAI is ExtractingFacilityAI extractingAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[0].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                        transfers[0].recordNumber = BuildingControl.OutgoingMask;
                        transfers[0].reason = extractingAI.m_outputResource;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = true;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is ProcessingFacilityAI processingAI && buildingInfo.m_class.m_level < ItemClass.Level.Level5)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_BUY");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                        transfers[0].outsideTip = Translations.Translate("TFC_BLD_IMP_TIP");
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.None;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = false;
                        transfers[1].panelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                        transfers[1].recordNumber = BuildingControl.OutgoingMask;
                        transfers[1].reason = processingAI.m_outputResource;
                        transfers[1].nextRecord = 0;
                        transfers[1].spawnsVehicles = true;
                        return 2;
                    }
                    else if (buildingInfo.m_buildingAI is UniqueFactoryAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_BUY");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.None;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = false;
                        transfers[1].panelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                        transfers[1].recordNumber = BuildingControl.OutgoingMask;
                        transfers[1].reason = TransferManager.TransferReason.LuxuryProducts;
                        transfers[1].nextRecord = 0;
                        transfers[1].spawnsVehicles = true;
                        return 2;
                    }
                    else if (buildingInfo.m_buildingAI is WarehouseAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_BUY");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_IMP");
                        transfers[0].outsideTip = Translations.Translate("TFC_BLD_IMP_TIP");
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.None;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = false;
                        transfers[1].panelTitle = Translations.Translate("TFC_GEN_SEL");
                        transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                        transfers[1].recordNumber = BuildingControl.OutgoingMask;
                        transfers[1].reason = TransferManager.TransferReason.None;
                        transfers[1].nextRecord = 0;
                        transfers[1].spawnsVehicles = true;
                        return 2;
                    }
                    return 0;

                case ItemClass.Service.Road:
                case ItemClass.Service.Beautification:
                    // Maintenance depots and snow dumps only, and only incoming.
                    if (buildingInfo.m_buildingAI is MaintenanceDepotAI || buildingInfo.m_buildingAI is SnowDumpAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_GEN_SER");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.None;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = true;
                        return 1;
                    }
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
                            transfers[0].recordNumber = BuildingControl.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.Mail;
                            transfers[0].nextRecord = BuildingControl.IncomingMask + 1;
                            transfers[0].spawnsVehicles = true;

                            // Post offices send sorted mail out to other post offices.
                            transfers[1].panelTitle = Translations.Translate("TFC_MAI_OSD");
                            transfers[1].outsideText =  null;
                            transfers[1].recordNumber = BuildingControl.OutgoingMask;
                            transfers[1].reason = TransferManager.TransferReason.SortedMail;
                            transfers[1].nextRecord = BuildingControl.OutgoingMask + 1;
                            transfers[1].spawnsVehicles = false;

                            // Post offices send unsorted mail via their trucks.
                            transfers[2].panelTitle = Translations.Translate("TFC_MAI_OUN");
                            transfers[2].outsideText = Translations.Translate("TFC_BLD_EXP");
                            transfers[2].recordNumber = BuildingControl.OutgoingMask + 1;
                            transfers[2].reason = TransferManager.TransferReason.UnsortedMail;
                            transfers[2].nextRecord = 0;
                            transfers[2].spawnsVehicles = true;

                            // Post offices pick up sorted mail via their trucks.
                            transfers[3].panelTitle = Translations.Translate("TFC_MAI_IST");
                            transfers[3].outsideText = Translations.Translate("TFC_BLD_IMP");
                            transfers[3].recordNumber = BuildingControl.IncomingMask + 1;
                            transfers[3].reason = TransferManager.TransferReason.SortedMail;
                            transfers[3].nextRecord = 0;
                            transfers[3].spawnsVehicles = true;

                            return 4;
                        }

                        // Mail sorting facility.
                        transfers[0].panelTitle = Translations.Translate("TFC_MAI_IUN");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.UnsortedMail;
                        transfers[0].nextRecord = 0;
                        transfers[0].spawnsVehicles = false;

                        transfers[1].panelTitle = Translations.Translate("TFC_MAI_OST");
                        transfers[1].outsideText = null;
                        transfers[1].recordNumber = BuildingControl.OutgoingMask;
                        transfers[1].reason = TransferManager.TransferReason.SortedMail;
                        transfers[1].nextRecord = 0;
                        transfers[1].spawnsVehicles = true;

                        transfers[2].panelTitle = Translations.Translate("TFC_MAI_OGM");
                        transfers[2].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[2].recordNumber = BuildingControl.OutgoingMask + 1;
                        transfers[2].reason = TransferManager.TransferReason.OutgoingMail;
                        transfers[2].nextRecord = 0;
                        transfers[2].spawnsVehicles = true;

                        transfers[3].panelTitle = Translations.Translate("TFC_MAI_ICM");
                        transfers[3].outsideText = Translations.Translate("TFC_BLD_IMP");
                        transfers[3].recordNumber = BuildingControl.IncomingMask + 1;
                        transfers[3].reason = TransferManager.TransferReason.IncomingMail;
                        transfers[3].nextRecord = 0;
                        transfers[3].spawnsVehicles = false;

                        return 4;
                    }
                    return 0;

                case ItemClass.Service.Garbage:
                    if (buildingInfo.GetAI() is LandfillSiteAI landfillAI)
                    {
                        // Incineration Plant.
                        if (buildingInfo.GetClassLevel() == ItemClass.Level.Level1 && landfillAI.m_electricityProduction != 0)
                        {
                            // Garbage Collection.
                            transfers[0].panelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = BuildingControl.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.Garbage;
                            transfers[0].nextRecord = 0;
                            transfers[0].spawnsVehicles = true;

                            return 1;
                        }
                        // Recycling Center.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level2 && landfillAI.m_materialProduction != 0)
                        {
                            // Garbage Collection.
                            transfers[0].panelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = BuildingControl.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.Garbage;
                            transfers[0].nextRecord = 0;
                            transfers[0].spawnsVehicles = true;

                            // Recovered resources for sale.
                            transfers[1].panelTitle = Translations.Translate("TFC_GAR_ORR");
                            transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                            transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                            transfers[1].recordNumber = BuildingControl.OutgoingMask;
                            transfers[1].reason = TransferManager.TransferReason.None;
                            transfers[1].nextRecord = 0;
                            transfers[1].spawnsVehicles = false;

                            return 2;
                        }
                        // Waste Transfer Facility and Landfill Site.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level3 || buildingInfo.GetClassLevel() == ItemClass.Level.Level1 && landfillAI.m_electricityProduction == 0)
                        {
                            // Garbage collection.
                            transfers[0].panelTitle = Translations.Translate("TFC_GAR_ICO");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = BuildingControl.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.Garbage;
                            transfers[0].nextRecord = 0;
                            transfers[0].spawnsVehicles = true;

                            // Garbage Transfer for processing in a Waste Processing Complex.
                            transfers[1].panelTitle = Translations.Translate("TFC_GAR_OTF");
                            transfers[1].outsideText = null;
                            transfers[1].recordNumber = BuildingControl.OutgoingMask;
                            transfers[1].reason = TransferManager.TransferReason.GarbageTransfer;
                            transfers[1].nextRecord = 0;
                            transfers[1].spawnsVehicles = false;

                            return 2;
                        }
                        // Waste Processing Complex.
                        else if (buildingInfo.GetClassLevel() == ItemClass.Level.Level4)
                        {
                            // Garbage Transfer for proccessing from Waste Transfer Facility and Landfill Site.
                            transfers[0].panelTitle = Translations.Translate("TFC_GAR_ITF");
                            transfers[0].outsideText = null;
                            transfers[0].recordNumber = BuildingControl.IncomingMask;
                            transfers[0].reason = TransferManager.TransferReason.GarbageTransfer;
                            transfers[0].nextRecord = 0;
                            transfers[0].spawnsVehicles = true;

                            // Recovered resources for sale.
                            transfers[1].panelTitle = Translations.Translate("TFC_GAR_ORR");
                            transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                            transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                            transfers[1].recordNumber = BuildingControl.OutgoingMask;
                            transfers[1].reason = TransferManager.TransferReason.None;
                            transfers[1].nextRecord = 0;
                            transfers[1].spawnsVehicles = false;

                            return 2;
                        }

                    }

                    // Undefined service.
                    return 0;

                case ItemClass.Service.Fishing:
                    if (buildingInfo.m_buildingAI is FishFarmAI || buildingInfo.m_buildingAI is FishingHarborAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_FIS_MKO");
                        transfers[0].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[0].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                        transfers[0].recordNumber = BuildingControl.OutgoingMask;
                        transfers[0].reason = TransferManager.TransferReason.Fish;
                        transfers[0].nextRecord = 0;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is MarketAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_FIS_MKI");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Fish;
                        transfers[0].nextRecord = 0;
                        return 1;
                    }
                    else if (buildingInfo.m_buildingAI is ProcessingFacilityAI)
                    {
                        transfers[0].panelTitle = Translations.Translate("TFC_FIS_MKI");
                        transfers[0].outsideText = null;
                        transfers[0].recordNumber = BuildingControl.IncomingMask;
                        transfers[0].reason = TransferManager.TransferReason.Fish;
                        transfers[0].nextRecord = 0;
                        transfers[1].panelTitle = Translations.Translate("TFC_FIS_CFO");
                        transfers[1].outsideText = Translations.Translate("TFC_BLD_EXP");
                        transfers[1].outsideTip = Translations.Translate("TFC_BLD_EXP_TIP");
                        transfers[1].recordNumber = BuildingControl.OutgoingMask;
                        transfers[1].reason = TransferManager.TransferReason.Goods;
                        transfers[1].nextRecord = 0;
                        return 2;
                    }
                    // Undefined service.
                    return 0;

                default:
                    // If not explicitly supported, then it's not supported.
                    return 0;
            }
        }


        /// <summary>
        /// Validates the districts in the provided district list.
        /// </summary>
        /// <param name="districtList">District list to validate</param>
        internal static void ValidateDistricts(HashSet<int> districtList)
        {
            // Local references.
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            District[] districtBuffer = districtManager.m_districts.m_buffer;
            DistrictPark[] parkBuffer = districtManager.m_parks.m_buffer;

            // Check any district records for validity.
            if (districtList != null && districtList.Count > 0)
            {
                List<int> invalidDistricts = new List<int>();

                // Iterate through each district record and check validity.
                foreach (int districtID in districtList)
                {
                    if (districtID > 0)
                    {
                        // Districts.
                        if ((districtBuffer[districtID].m_flags & District.Flags.Created) != 0)
                        {
                            continue;
                        }
                    }
                    else if (districtID < 0)
                    {
                        // Parks.
                        if ((parkBuffer[-districtID].m_flags & DistrictPark.Flags.Created) != 0)
                        {
                            continue;
                        }
                    }

                    // If we got here (district ID is zero, or district created flag isn't set) this is an invalid district; add to list to remove.
                    invalidDistricts.Add(districtID);
                }

                // Iterate through removal list and remove all invalid districts.
                foreach (int districtID in invalidDistricts)
                {
                    districtList.Remove(districtID);
                }
            }
        }
    }
}
