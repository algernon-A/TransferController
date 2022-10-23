// <copyright file="Matching.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Runtime.CompilerServices;
    using AlgernonCommons;
    using ColossalFramework;
    using HarmonyLib;
    using UnityEngine;

    /// <summary>
    /// Custom offer matching - new version.
    /// </summary>
    [HarmonyPatch]
    public static class Matching
    {
        // Warehouse priority value.
        private static int _warehousePriority = 0;
        private static float _warehouseWeighting = 0f;

        // External connection priorities.
        private static int _outsideRailPriority = 0;
        private static float _outsideRailWeighting = 0f;
        private static int _outsideShipPriority = 0;
        private static float _outsideShipWeighting = 0f;

        /// <summary>
        /// Gets or sets the global warehouse priority boost.
        /// </summary>
        public static int WarehousePriority
        {
            get => _warehousePriority;

            set
            {
                _warehousePriority = value;
                _warehouseWeighting = 1f / (1f + Mathf.Pow(value, 2));
            }
        }

        /// <summary>
        /// Gets or sets the global outside rail connection priority boost.
        /// </summary>
        public static int OutsideRailPriority
        {
            get => _outsideRailPriority;

            set
            {
                _outsideRailPriority = value;
                _outsideRailWeighting = 1f / (1f + Mathf.Pow(value, 2));
            }
        }

        /// <summary>
        /// Gets or sets the global outside ship connection priority boost.
        /// </summary>
        public static int OutsideShipPriority
        {
            get => _outsideShipPriority;

            set
            {
                _outsideShipPriority = value;
                _outsideShipWeighting = 1f / (1f + Mathf.Pow(value, 2));
            }
        }

        /// <summary>
        /// Perform transfer offer matching for a specified TransferReason.
        /// </summary>
        /// <param name="__instance">TransferManager instance.</param>
        /// <param name="reason">Transfer reason.</param>
        /// <param name="incomingCounts">Incoming transfer count array.</param>
        /// <param name="outgoingCounts">Outgoing transfer count array.</param>
        /// <param name="incomingOffers">Incoming offer buffer.</param>
        /// <param name="outgoingOffers">Outgoint offer buffer.</param>
        /// <param name="m_incomingAmount">Outstanding incoming amount totals array.</param>
        /// <param name="m_outgoingAmount">Outstanding outgoing amount totals array.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
        public static void MatchOffers(
            TransferManager __instance,
            TransferManager.TransferReason reason,
            ushort[] incomingCounts,
            ushort[] outgoingCounts,
            TransferManager.TransferOffer[] incomingOffers,
            TransferManager.TransferOffer[] outgoingOffers,
            int[] m_incomingAmount,
            int[] m_outgoingAmount)
        {
            /*
             * Transfer offer arrays are in blocks of 256, organised by reason, then by priority within each reason (8 prorities): block ID is (reason * 8) + priority.
             * [block 0] 0 - 255: TransferReason.Garbage, Priority 0
             * [block 1] 256 - 511: TransferReason.Garbage, Priority 1
             * [block 2] 512 - 767: TransferReason.Garbage, Priority 2
             * etc.
             */

            // Block relevant to this reason.
            int reasonBlock = (int)reason * 8;

            switch (reason)
            {
                // Any outgoing emergency transfers get treated first (this prioritises closest response to incident).
                case TransferManager.TransferReason.Fire:
                case TransferManager.TransferReason.Fire2:
                case TransferManager.TransferReason.ForestFire:
                case TransferManager.TransferReason.Sick:
                case TransferManager.TransferReason.Sick2:
                case TransferManager.TransferReason.Crime:
                case TransferManager.TransferReason.Collapsed:
                case TransferManager.TransferReason.Collapsed2:
                case TransferManager.TransferReason.FloodWater:

                // Schools - go to closest (elementary and high, tertiary education is not necessarily closest-first):
                case TransferManager.TransferReason.Student1:
                case TransferManager.TransferReason.Student2:

                    // Match from highest to lowest priority.
                    for (int priority = 7; priority >= 0; --priority)
                    {
                        int priorityIndex = reasonBlock + priority;
                        int outgoingIndex = 0;

                        // Keep iterating while offers are remaining in this outgoing priority block.
                        while (outgoingIndex < outgoingCounts[priorityIndex])
                        {
                            MatchOffer(false, 0, reason, priority, outgoingIndex++, outgoingOffers, outgoingCounts, incomingOffers, incomingCounts);
                        }
                    }

                    // Match any remaining incoming offers incoming-first (from depots outwards).
                    for (int priority = 7; priority >= 0; --priority)
                    {
                        int priorityIndex = reasonBlock + priority;
                        int incomingIndex = 0;

                        // Keep iterating while offers are remaining in this incoming priority block.
                        while (incomingIndex < incomingCounts[priorityIndex])
                        {
                            MatchOffer(true, 0, reason, priority, incomingIndex++, incomingOffers, incomingCounts, outgoingOffers, outgoingCounts);
                        }
                    }

                    break;

                // Basic city services - most efficent matching is from depot outwards.
                // However, need to prioritise high prioirty requests first.
                // Start by matching highest priority outgoing requests (7-3).
                // Then service lower priorities from depot outwards for greatest efficiency:
                case TransferManager.TransferReason.RoadMaintenance:
                case TransferManager.TransferReason.ParkMaintenance:
                case TransferManager.TransferReason.Snow:
                    // Match outgoing offers first from highest to lowest priority, down to priority 3, to service any urgent issues.
                    for (int priority = 7; priority >= 3; --priority)
                    {
                        int priorityIndex = reasonBlock + priority;
                        int outgoingIndex = 0;

                        // Keep iterating while offers are remaining in this outgoing priority block.
                        while (outgoingIndex < outgoingCounts[priorityIndex])
                        {
                            MatchOffer(false, 0, reason, priority, outgoingIndex++, outgoingOffers, outgoingCounts, incomingOffers, incomingCounts);
                        }
                    }

                    // Then match all remaining incoming offers (depot outwards).
                    for (int priority = 7; priority >= 0; --priority)
                    {
                        int priorityIndex = reasonBlock + priority;
                        int incomingIndex = 0;

                        // Keep iterating while offers are remaining in this incoming priority block.
                        while (incomingIndex < incomingCounts[priorityIndex])
                        {
                            MatchOffer(true, 0, reason, priority, incomingIndex++, incomingOffers, incomingCounts, outgoingOffers, outgoingCounts);
                        }
                    }

                    // Then match any remaining outgoing offers.
                    for (int priority = 7; priority >= 0; --priority)
                    {
                        int priorityIndex = reasonBlock + priority;
                        int outgoingIndex = 0;

                        // Keep iterating while offers are remaining in this outgoing priority block.
                        while (outgoingIndex < outgoingCounts[priorityIndex])
                        {
                            MatchOffer(false, 0, reason, priority, outgoingIndex++, outgoingOffers, outgoingCounts, incomingOffers, incomingCounts);
                        }
                    }

                    break;

                // Default treatment is to match incoming then outgoing by descending priority level (i.e. incoming 7, outgoing 7, incoming 6, outgoing 6, etc.)
                default:
                    // Match by priority within this reason, descending.
                    for (int priority = 7; priority >= 0; --priority)
                    {
                        int priorityIndex = reasonBlock + priority;
                        int incomingIndex = 0, outgoingIndex = 0;

                        // Keep iterating while offers are remaining in this priority block.
                        bool matching;
                        do
                        {
                            // Set status flag to false (will be set to true if any processing takes place).
                            matching = false;

                            // Match next incoming offer.
                            if (incomingIndex < incomingCounts[priorityIndex])
                            {
                                matching = true;
                                MatchOffer(true, 0, reason, priority, incomingIndex++, incomingOffers, incomingCounts, outgoingOffers, outgoingCounts);
                            }

                            // Match next outgoing offer.
                            if (outgoingIndex < outgoingCounts[priorityIndex])
                            {
                                matching = true;
                                MatchOffer(false, 0, reason, priority, outgoingIndex++, outgoingOffers, outgoingCounts, incomingOffers, incomingCounts);
                            }
                        }
                        while (matching);
                    }

                    break;
            }

            // Wipe any remaining offers.
            for (int priority = 7; priority >= 0; --priority)
            {
                int priorityIndex = reasonBlock + priority;
                incomingCounts[priorityIndex] = 0;
                outgoingCounts[priorityIndex] = 0;
            }

            // Clear TransferManager outstanding amount totals for this TransferReason.
            m_incomingAmount[(int)reason] = 0;
            m_outgoingAmount[(int)reason] = 0;
        }

        /// <summary>
        /// Finds a match for the specified offer, using closest-distance.
        /// </summary>
        /// <param name="incoming">True if this is an incoming offer, false otherwise.</param>
        /// <param name="minPriority">Minimum priority to match to.</param>
        /// <param name="reason">Transfer reason.</param>
        /// <param name="priority">Specified reason's priority.</param>
        /// <param name="index">Specified reason's index (within priority block).</param>
        /// <param name="offerBuffer">TransferReason buffer for specified transfer.</param>
        /// <param name="offerCounts">Transfer counts array for specified transfer.</param>
        /// <param name="candidateBuffer">TransferReason buffer for candidate offers for matching.</param>
        /// <param name="candidateCounts">Transfer counts array for candidate offers for matching.</param>
        public static void MatchOffer(bool incoming, int minPriority, TransferManager.TransferReason reason, int priority, int index, TransferManager.TransferOffer[] offerBuffer, ushort[] offerCounts, TransferManager.TransferOffer[] candidateBuffer, ushort[] candidateCounts)
        {
            // Offer to match.
            // Material block index.
            int reasonBlock = (int)reason * 8;
            int offerBlock = reasonBlock + priority;
            int bufferIndex = (offerBlock * 256) + index;
            ref TransferManager.TransferOffer offer = ref offerBuffer[bufferIndex];

            // Skip any offers with no amount.
            if (offer.Amount <= 0)
            {
                return;
            }

            // Local references.
            DistrictManager districtManager = Singleton<DistrictManager>.instance;
            Vehicle[] vehicleBuffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            Citizen[] citizenBuffer = Singleton<CitizenManager>.instance.m_citizens.m_buffer;

            // Defaults.
            BuildingAI offerBuildingAI = null;
            Vector3 offerPosition = offer.Position;

            /*
             * Matching is done in descending priority order; a lower priority bound is set for lower priorities;
             * an offer with priority 0 will only consider matching with priorities 7 down to 2, an offer with priority 1 will only consider priorities 7 down to 1.
             * This way lower-priority transfers will have slightly fewer candidates for matching than higher-priority transfers.
             */
            int lowerPriorityBound = Mathf.Max(Mathf.Max(0, 2 - priority), minPriority);

            // TODO: Skip zero amounts.

            // If no building, use vehicle source building, if any.
            ushort offerBuilding = offer.Building;
            if (offerBuilding == 0)
            {
                ushort offerVehicle = offer.Vehicle;
                if (offerVehicle != 0)
                {
                    offerBuilding = vehicleBuffer[offerVehicle].m_sourceBuilding;
                }
                else
                {
                    // No vehicle or building - use citizen home building, if appropriate.
                    uint offerCitizen = offer.Citizen;
                    if (offerCitizen != 0)
                    {
                        switch (reason)
                        {
                            // These cases are picked up from their current location.
                            case TransferManager.TransferReason.Sick:
                            case TransferManager.TransferReason.Sick2:
                            case TransferManager.TransferReason.SickMove:
                            case TransferManager.TransferReason.Taxi:
                                break;

                            // Otherwise, we use the citizen's home building.
                            default:
                                offerBuilding = citizenBuffer[offerCitizen].m_homeBuilding;
                                break;
                        }
                    }
                }

                // Did we find a suitable building?
                if (offerBuilding != 0)
                {
                    // Yes - update offer position and AI reference to match.
                    offerPosition = buildingBuffer[offerBuilding].m_position;
                    offerBuildingAI = buildingBuffer[offerBuilding].Info.m_buildingAI;
                }
            }
            else
            {
                // Initial offer was a building; assume the offer position is the building position.
                // Just update AI reference.
                offerBuildingAI = buildingBuffer[offerBuilding].Info.m_buildingAI;
            }

            // Get offer district and park area.
            byte offerDistrict = districtManager.GetDistrict(offerPosition);
            byte offerPark = districtManager.GetPark(offerPosition);

            // Outside connection status.
            bool offerIsOutside = offerBuildingAI is OutsideConnectionAI;

            // Same-district modifier.
            float offerSameDistrict = CheckPreferSameDistrict(offerBuilding, incoming, reason);

            // Keep going until we've used up all the offer amount with matched transfers.
            int outstandingAmount = offer.Amount;
            do
            {
                // Iterate through candidate buffer, from highest to lowest priority.
                float bestDistance = float.MaxValue;
                int matchedPriority = -1, matchedIndex = -1;
                ushort matchedBuilding = 0;
                for (int candidatePriority = 7; candidatePriority >= lowerPriorityBound; --candidatePriority)
                {
                    int candidateBlock = reasonBlock + candidatePriority;
                    int blockCount = candidateCounts[candidateBlock];

                    // Iterate through all candidates in this candidate block.
                    for (int candidateIndex = 0; candidateIndex < blockCount; ++candidateIndex)
                    {
                        // Candidate offer.
                        TransferManager.TransferOffer candidate = candidateBuffer[(candidateBlock * 256) + candidateIndex];

                        // Skip identical offers or offers with no amounts.
                        if (offer.m_object == candidate.m_object | candidate.Amount <= 0)
                        {
                            continue;
                        }

                        // Skip inter-warehouse transfers if they're at the bottom level of our current priority matching.
                        // Warehouses uniquely set the Exclude flag on their transfers.
                        if (offer.Exclude & candidate.Exclude & candidatePriority <= lowerPriorityBound)
                        {
                            continue;
                        }

                        // If within park area with deliveries, skip offers not
                        // also within the same park area.
                        // Do not let name `isLocalPark` fool you, this is not a
                        // boolean - it is a park ID. It is set in
                        // AddIncomingOffer or AddOutgoingOffer for buildings within
                        // pedestrian zones that handle deliveries connected to
                        // a pedestrian road, or in zones with the force deliveries
                        // policy.
                        if (offer.m_isLocalPark != candidate.m_isLocalPark)
                        {
                            continue;
                        }

                        // Defaults.
                        Vector3 candidatePosition = candidate.Position;
                        ushort candidateBuilding = candidate.Building;
                        BuildingAI candidateBuildingAI = null;
                        float distanceModifier = 1f;

                        // If no building, use vehicle source building, if any.
                        if (candidateBuilding == 0)
                        {
                            ushort candidateVehicle = candidate.Vehicle;
                            if (candidateVehicle != 0)
                            {
                                candidateBuilding = vehicleBuffer[candidateVehicle].m_sourceBuilding;
                            }

                            // No vehicle or building - use citizen home building, if appropriate.
                            uint candidateCitizen = offer.Citizen;
                            if (candidateCitizen != 0)
                            {
                                switch (reason)
                                {
                                    // These cases are picked up from their current location.
                                    case TransferManager.TransferReason.Sick:
                                    case TransferManager.TransferReason.Sick2:
                                    case TransferManager.TransferReason.SickMove:
                                    case TransferManager.TransferReason.Taxi:
                                        break;

                                    // Otherwise, we use the citizen's home building.
                                    default:
                                        candidateBuilding = citizenBuffer[candidateCitizen].m_homeBuilding;
                                        break;
                                }
                            }

                            // Did we find a suitable building?
                            if (candidateBuilding != 0)
                            {
                                // Yes - update offer position and AI reference to match.
                                candidatePosition = buildingBuffer[candidateBuilding].m_position;
                                candidateBuildingAI = buildingBuffer[candidateBuilding].Info.m_buildingAI;
                            }
                        }
                        else
                        {
                            // Candidate offer was a building; assume the offer position is the building position.
                            // Just update AI reference.
                            candidateBuildingAI = buildingBuffer[candidateBuilding].Info.m_buildingAI;
                        }

                        // Don't transfer to/from the same building, even indirectly.
                        if (candidateBuilding != 0 & candidateBuilding == offerBuilding)
                        {
                            continue;
                        }

                        // Check for pathfinding fails.
                        if ((offer.Active && PathFindFailure.HasFailure(offerBuilding, candidateBuilding)) || (candidate.Active && PathFindFailure.HasFailure(candidateBuilding, offerBuilding)))
                        {
                            if (incoming)
                            {
                                TransferLogging.AddEntry(reason, incoming, priority, candidatePriority, offerBuilding, candidateBuilding, TransferLogging.MatchStatus.PathFailure, offer.Exclude, candidate.Exclude, offerPosition, candidatePosition);
                            }
                            else
                            {
                                TransferLogging.AddEntry(reason, incoming, candidatePriority, priority, candidateBuilding, offerBuilding, TransferLogging.MatchStatus.PathFailure, candidate.Exclude, offer.Exclude, candidatePosition, offerPosition);
                            }

                            continue;
                        }

                        // Apply warehouse boost if the candidate builing is a warehouse and the offer building isn't a warehouse or outside connection.
                        if (candidate.Exclude & !(offer.Exclude | offerIsOutside))
                        {
                            // Don't apply priority to incoming transfers to emptying warehouses, or outgoing transfers from filling warehouses.
                            Building.Flags warehouseFlags = buildingBuffer[candidateBuilding].m_flags;
                            if (!((incoming & (warehouseFlags & Building.Flags.Downgrading) != 0) | (!incoming & (warehouseFlags & Building.Flags.Filling) != 0)))
                            {
                                distanceModifier *= _warehouseWeighting;
                            }
                        }

                        // Apply outside connection boost if candidate is an outside connection, unless offer is also an outside connection.
                        if (!offerIsOutside && candidateBuildingAI is OutsideConnectionAI)
                        {
                            switch (candidateBuildingAI.m_info.m_class.m_subService)
                            {
                                case ItemClass.SubService.PublicTransportTrain:
                                    distanceModifier *= _outsideRailWeighting;
                                    break;

                                case ItemClass.SubService.PublicTransportShip:
                                    distanceModifier *= _outsideShipWeighting;
                                    break;
                            }
                        }

                        // Apply prefer-same-district boost if applicatble.
                        byte candidateDistrict = districtManager.GetDistrict(candidatePosition);
                        byte candidatePark = districtManager.GetPark(candidatePosition);
                        if (candidateDistrict == offerDistrict | candidatePark == offerPark)
                        {
                            // Offer building modifier.
                            distanceModifier *= offerSameDistrict;

                            // Candidate building modifier.
                            distanceModifier *= CheckPreferSameDistrict(candidateBuilding, !incoming, reason);
                        }

                        // This is a park delivery. Distance is not a consideration
                        // for intra-park deliveries since they happen
                        // instantaneously without using a vehicle.
                        if (offer.m_isLocalPark == candidate.m_isLocalPark)
                        {
                            distanceModifier = 0;
                        }

                        // Calculate distance between positions - use original offer positions, not owning building positions.
                        float squaredDistance = Vector3.SqrMagnitude(offer.Position - candidate.Position) * distanceModifier;
                        if (squaredDistance < bestDistance)
                        {
                            // New nearest disance - apply checks.
                            if (incoming)
                            {
                                if (!ChecksPassed(offerBuilding, candidateBuilding, offerDistrict, candidateDistrict, offerPark, candidatePark, reason, out TransferLogging.MatchStatus result))
                                {
                                    TransferLogging.AddEntry(reason, incoming, priority, candidatePriority, offerBuilding, candidateBuilding, result, offer.Exclude, candidate.Exclude, offerPosition, candidatePosition);
                                    continue;
                                }

                                // If candidate (outgoing) building is a warehouse, check quota.
                                if (candidate.Exclude && !WarehouseControl.CheckVehicleQuota(candidateBuildingAI, candidateBuilding, ref buildingBuffer[candidateBuilding], reason, offerBuildingAI))
                                {
                                    TransferLogging.AddEntry(reason, incoming, priority, candidatePriority, offerBuilding, candidateBuilding, TransferLogging.MatchStatus.NoVehicle, offer.Exclude, candidate.Exclude, offerPosition, candidatePosition);
                                    continue;
                                }
                            }
                            else
                            {
                                if (!ChecksPassed(candidateBuilding, offerBuilding, candidateDistrict, offerDistrict, candidatePark, offerPark, reason, out TransferLogging.MatchStatus result))
                                {
                                    TransferLogging.AddEntry(reason, incoming, candidatePriority, priority, candidateBuilding, offerBuilding, result, candidate.Exclude, offer.Exclude, candidatePosition, offerPosition);
                                    continue;
                                }

                                // Otherwise, if the offer building is a warehouse and is the outgoing party, check quota there.
                                if (offer.Exclude && !WarehouseControl.CheckVehicleQuota(offerBuildingAI, offerBuilding, ref buildingBuffer[offerBuilding], reason, candidateBuildingAI))
                                {
                                    TransferLogging.AddEntry(reason, incoming, candidatePriority, priority, candidateBuilding, offerBuilding, TransferLogging.MatchStatus.NoVehicle, candidate.Exclude, offer.Exclude, candidatePosition, offerPosition);
                                    continue;
                                }
                            }

                            // If we got here, all checks have been passed - this is now the nominated candidate.
                            bestDistance = squaredDistance;
                            matchedPriority = candidatePriority;
                            matchedIndex = candidateIndex;
                            matchedBuilding = candidateBuilding;

                            // Log eligible match.
                            if (incoming)
                            {
                                TransferLogging.AddEntry(reason, incoming, priority, candidatePriority, offerBuilding, candidateBuilding, TransferLogging.MatchStatus.Eligible, offer.Exclude, candidate.Exclude, offerPosition, candidatePosition);
                            }
                            else
                            {
                                TransferLogging.AddEntry(reason, incoming, candidatePriority, priority, candidateBuilding, offerBuilding, TransferLogging.MatchStatus.Eligible, candidate.Exclude, offer.Exclude, candidatePosition, offerPosition);
                            }

                            // TODO: break if distance is less than minimum.
                        }
                    }
                }

                // Finished iterating through candidate offers; did we get a match?
                if (matchedPriority < 0)
                {
                    // No match; quit iterating.
                    break;
                }

                // Matched - get successful offer.
                int matchedBlock = reasonBlock + matchedPriority;
                int matchedBufferIndex = (matchedBlock * 256) + matchedIndex;
                TransferManager.TransferOffer matchedOffer = candidateBuffer[matchedBufferIndex];
                int matchedAmount = matchedOffer.Amount;

                // Calculate transfer amount.
                int transferAmount = Mathf.Min(outstandingAmount, matchedAmount);

                // Start the transfer.
                if (incoming)
                {
                    TransferLogging.AddEntry(reason, incoming, priority, matchedPriority, offerBuilding, matchedBuilding, TransferLogging.MatchStatus.Selected, offer.Exclude, matchedOffer.Exclude, offerPosition, matchedOffer.Position);
                    StartTransfer(Singleton<TransferManager>.instance, reason, matchedOffer, offer, transferAmount);
                }
                else
                {
                    TransferLogging.AddEntry(reason, incoming, matchedPriority, priority, matchedBuilding, offerBuilding, TransferLogging.MatchStatus.Selected, matchedOffer.Exclude, offer.Exclude, matchedOffer.Position, offerPosition);
                    StartTransfer(Singleton<TransferManager>.instance, reason, offer, matchedOffer, transferAmount);
                }

                // Decrease offer amounts by the amount transferred.
                outstandingAmount -= transferAmount;
                matchedAmount -= transferAmount;

                // Has all the matched offer amount been used up?
                if (matchedAmount == 0)
                {
                    // Matched outgoing offer amount fully used; remove from offer buffer.
                    int newOfferCount = candidateCounts[matchedBlock] - 1;
                    candidateCounts[matchedBlock] = (ushort)newOfferCount;

                    // Copy over this reference with the existing one from the end of the chain.
                    ref TransferManager.TransferOffer reference = ref candidateBuffer[matchedBufferIndex];
                    reference = candidateBuffer[(matchedBlock * 256) + newOfferCount];
                }
                else
                {
                    // Matched outgoing offer amount partially used; reduce outstanding amount in offer.
                    candidateBuffer[matchedBufferIndex].Amount = matchedAmount;
                }
            }

            // Keep iterating through offers until we've matched everything (or can't go any further).
            while (outstandingAmount > 0);

            // Did we sucesfully match the entire transfer amount?
            if (outstandingAmount == 0)
            {
                // Matched outgoing offer amount fully used; remove from offer buffer.
                int newOfferCount = offerCounts[offerBlock] - 1;
                offerCounts[offerBlock] = (ushort)newOfferCount;

                // Copy over this reference with the existing one from the end of the chain.
                ref TransferManager.TransferOffer reference = ref offerBuffer[bufferIndex];
                reference = offerBuffer[(offerBlock * 256) + newOfferCount];
            }
            else
            {
                // Matched outgoing offer amount partially used; update transfer entry with outstanding amount.
                offer.Amount = outstandingAmount;
            }
        }

        /// <summary>
        /// Harmony reverse patch to access private method TransferManager.StartTransfer.
        /// </summary>
        /// <param name="instance">TransferManager instance.</param>
        /// <param name="material">Transfer material.</param>
        /// <param name="offerOut">Outgoing offer.</param>
        /// <param name="offerIn">Incoming offer.</param>
        /// <param name="delta">Offer amount.</param>
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(TransferManager), "StartTransfer")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StartTransfer(
            object instance,
            TransferManager.TransferReason material,
            TransferManager.TransferOffer offerOut,
            TransferManager.TransferOffer offerIn,
            int delta)
        {
            Logging.Error("StartTransfer reverse Harmony patch wasn't applied, params: ", instance, material, offerOut, offerIn, delta);
            throw new NotImplementedException("Harmony reverse patch not applied");
        }

        /// <summary>
        /// Checks against district and building filters, both incoming and outgoing.
        /// </summary>
        /// <param name="incomingBuildingID">Incoming building ID.</param>
        /// <param name="outgoingBuildingID">Outoging building ID.</param>
        /// <param name="incomingDistrict">District of incoming offer.</param>
        /// <param name="outgoingDistrict">District of outgoing offer.</param>
        /// <param name="incomingPark">Park area of incoming offer.</param>
        /// <param name="outgoingPark">Park area of outgoing offer.</param>
        /// <param name="reason">Transfer reason.</param>
        /// <param name="result">Matching result check.</param>
        /// <returns>True if the transfer is permitted, false if prohibited.</returns>
        internal static bool ChecksPassed(
            ushort incomingBuildingID,
            ushort outgoingBuildingID,
            byte incomingDistrict,
            byte outgoingDistrict,
            byte incomingPark,
            byte outgoingPark,
            TransferManager.TransferReason reason,
            out TransferLogging.MatchStatus result)
        {
            // First, check for incoming restrictions.
            if (IncomingChecksPassed(incomingBuildingID, outgoingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingPark, reason, out result))
            {
                // Then, outgoing.
                return OutgoingChecksPassed(outgoingBuildingID, incomingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingPark, reason, out result);
            }

            // Failed incoming district restrictions - return false.
            return false;
        }

        /// <summary>
        ///  Applies incoming district and building filters.
        /// </summary>
        /// <param name="buildingID">Building ID to check.</param>
        /// <param name="outgoingBuildingID">Building ID of outgoing building.</param>
        /// <param name="incomingDistrict">District of incoming offer.</param>
        /// <param name="outgoingDistrict">District of outgoing offer.</param>
        /// <param name="incomingPark">Park area of incoming offer.</param>
        /// <param name="outgoingPark">Park area of outgoing offer.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="result">Matching result check.</param>
        /// <returns>True if the transfer is permitted, false if prohibited.</returns>
        private static bool IncomingChecksPassed(
            ushort buildingID,
            ushort outgoingBuildingID,
            byte incomingDistrict,
            byte outgoingDistrict,
            byte incomingPark,
            byte outgoingPark,
            TransferManager.TransferReason transferReason,
            out TransferLogging.MatchStatus result)
        {
            // Calculate building record ID.
            uint buildingRecordID = BuildingControl.CalculateEntryKey(buildingID, true, transferReason);

            // Get building record.
            if (!BuildingControl.BuildingRecords.TryGetValue(buildingRecordID, out BuildingControl.BuildingRecord buildingRecord))
            {
                // No record found; try TransferReason.None wildcard.
                buildingRecordID = BuildingControl.CalculateEntryKey(buildingID, true, TransferManager.TransferReason.None);
                if (!BuildingControl.BuildingRecords.TryGetValue(buildingRecordID, out buildingRecord))
                {
                    // No record found, therefore no restrictions.
                    result = TransferLogging.MatchStatus.Eligible;
                    return true;
                }
            }

            // Check outside connection.
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[outgoingBuildingID].Info.m_buildingAI is OutsideConnectionAI)
            {
                if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.BlockOutsideConnection) == 0)
                {
                    // Outside connection permitted.
                    result = TransferLogging.MatchStatus.Eligible;
                    return true;
                }
                else
                {
                    // Outside connection blocked.
                    result = TransferLogging.MatchStatus.ImportBlocked;
                    return false;
                }
            }
            else if ((buildingRecord.Flags & (BuildingControl.RestrictionFlags.DistrictEnabled | BuildingControl.RestrictionFlags.BuildingEnabled)) == 0)
            {
                // If not an outside connection, transfer is permitted if no restrictions are enabled.
                result = TransferLogging.MatchStatus.Eligible;
                return true;
            }

            // Check district settings.
            if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.DistrictEnabled) != 0)
            {
                // Check same-district setting.
                if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.BlockSameDistrict) == 0 && ((outgoingDistrict != 0 && incomingDistrict == outgoingDistrict) || (outgoingPark != 0 && incomingPark == outgoingPark)))
                {
                    // Same district match - permitted.
                    result = TransferLogging.MatchStatus.Eligible;
                    return true;
                }

                // Check permitted districts.
                if (buildingRecord.Districts != null)
                {
                    if (buildingRecord.Districts.Contains(outgoingDistrict) || buildingRecord.Districts.Contains(-outgoingPark))
                    {
                        // Permitted district.
                        result = TransferLogging.MatchStatus.Eligible;
                        return true;
                    }
                }
            }

            // Check building settings.
            if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.BuildingEnabled) != 0)
            {
                // Check permitted buildings.
                if (buildingRecord.Buildings != null)
                {
                    if (buildingRecord.Buildings.Contains(outgoingBuildingID))
                    {
                        // Permitted building.
                        result = TransferLogging.MatchStatus.Eligible;
                        return true;
                    }
                }
            }

            // If we got here, we found a record but no permitted match was found; return false.
            result = TransferLogging.MatchStatus.NotPermittedIn;
            return false;
        }

        /// <summary>
        ///  Applies outgoing district and building filters.
        /// </summary>
        /// <param name="buildingID">Building ID to check.</param>
        /// <param name="incomingBuildingID">Building ID of incoming building.</param>
        /// <param name="incomingDistrict">District of incoming offer.</param>
        /// <param name="outgoingDistrict">District of outgoing offer.</param>
        /// <param name="incomingPark">Park area of incoming offer.</param>
        /// <param name="outgoingPark">Park area of outgoing offer.</param>
        /// <param name="transferReason">Transfer reason.</param>
        /// <param name="result">Matching result check.</param>
        /// <returns>True if the transfer is permitted, false if prohibited.</returns>
        private static bool OutgoingChecksPassed(
            ushort buildingID,
            ushort incomingBuildingID,
            byte incomingDistrict,
            byte outgoingDistrict,
            byte incomingPark,
            byte outgoingPark,
            TransferManager.TransferReason transferReason,
            out TransferLogging.MatchStatus result)
        {
            // Calculate building record ID.
            uint buildingRecordID = BuildingControl.CalculateEntryKey(buildingID, false, transferReason);

            // Try to get building record.
            if (!BuildingControl.BuildingRecords.TryGetValue(buildingRecordID, out BuildingControl.BuildingRecord buildingRecord))
            {
                // No record found; if this is a cargo transfer, try none.
                if (IsCargoReason(transferReason))
                {
                    buildingRecordID = BuildingControl.CalculateEntryKey(buildingID, false, TransferManager.TransferReason.None);
                    if (!BuildingControl.BuildingRecords.TryGetValue(buildingRecordID, out buildingRecord))
                    {
                        // No record found, therefore no restrictions.
                        result = TransferLogging.MatchStatus.Eligible;
                        return true;
                    }
                }
                else
                {
                    // No relevant reason found, therefore no restrictions.
                    result = TransferLogging.MatchStatus.Eligible;
                    return true;
                }
            }

            // Check outside connection.
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[incomingBuildingID].Info.m_buildingAI is OutsideConnectionAI)
            {
                if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.BlockOutsideConnection) == 0)
                {
                    // Outside connection permitted.
                    result = TransferLogging.MatchStatus.Eligible;
                    return true;
                }
                else
                {
                    // Outside connection blocked.
                    result = TransferLogging.MatchStatus.ExportBlocked;
                    return false;
                }
            }
            else if ((buildingRecord.Flags & (BuildingControl.RestrictionFlags.DistrictEnabled | BuildingControl.RestrictionFlags.BuildingEnabled)) == 0)
            {
                // If not an outside connection, transfer is permitted if no restrictions are enabled.
                result = TransferLogging.MatchStatus.Eligible;
                return true;
            }

            // Check district settings.
            if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.DistrictEnabled) != 0)
            {
                // Check same-district setting.
                if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.BlockSameDistrict) == BuildingControl.RestrictionFlags.None && ((incomingDistrict != 0 && incomingDistrict == outgoingDistrict) || (incomingPark != 0 && incomingPark == outgoingPark)))
                {
                    // Same district match - permitted.
                    result = TransferLogging.MatchStatus.Eligible;
                    return true;
                }

                // Check permitted districts.
                if (buildingRecord.Districts != null)
                {
                    if (buildingRecord.Districts.Contains(incomingDistrict) || buildingRecord.Districts.Contains(-incomingPark))
                    {
                        // Permitted district.
                        result = TransferLogging.MatchStatus.Eligible;
                        return true;
                    }
                }
            }

            // Check building settings.
            if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.BuildingEnabled) != 0)
            {
                // Check permitted buildings.
                if (buildingRecord.Buildings != null)
                {
                    if (buildingRecord.Buildings.Contains(incomingBuildingID))
                    {
                        // Permitted building.
                        result = TransferLogging.MatchStatus.Eligible;
                        return true;
                    }
                }
            }

            // If we got here, we didn't get a record.
            result = TransferLogging.MatchStatus.NotPermittedOut;
            return false;
        }

        /// <summary>
        /// /// Checks if the given building has an active "prefer same district" setting for the specified TransferReason and returns the appropriate multiplier.
        /// </summary>
        /// <param name="buildingID">Building ID to check.</param>
        /// <param name="incoming">True if this is an incoming offer, false otherwise.</param>
        /// <param name="reason">Transfer reason.</param>
        /// <returns>0.1f if the given building has an active prefer same district setting, 1f otherwise.</returns>
        private static float CheckPreferSameDistrict(ushort buildingID, bool incoming, TransferManager.TransferReason reason)
        {
            // If outgoing transfer, the None wildcard only applies if it's a cargo reason.
            if (!incoming & reason == TransferManager.TransferReason.None)
            {
                if (!IsCargoReason(reason))
                {
                    // Not a valid cargo reason; return 1.
                    return 1f;
                }
            }

            // Calculate building record ID.
            uint buildingRecordID = BuildingControl.CalculateEntryKey(buildingID, incoming, reason);

            // Get building record.
            if (BuildingControl.BuildingRecords.TryGetValue(buildingRecordID, out BuildingControl.BuildingRecord buildingRecord))
            {
                // Record found - check flag.
                if ((buildingRecord.Flags & BuildingControl.RestrictionFlags.PreferSameDistrict) != 0)
                {
                    // Found matching flag; return 0.1.
                    return 0.1f;
                }
            }

            // If we got here, no record was found; return 1.
            return 1f;
        }

        /// <summary>
        /// Checks if the given TransferReason is a valid cargo transfer type.
        /// </summary>
        /// <param name="transferReason">TransferReason to check.</param>
        /// <returns>True if the given reason is a cargo type, false otherwise.</returns>
        private static bool IsCargoReason(TransferManager.TransferReason transferReason)
        {
            switch (transferReason)
            {
                case TransferManager.TransferReason.Oil:
                case TransferManager.TransferReason.Ore:
                case TransferManager.TransferReason.Logs:
                case TransferManager.TransferReason.Grain:
                case TransferManager.TransferReason.Goods:
                case TransferManager.TransferReason.Coal:
                case TransferManager.TransferReason.Food:
                case TransferManager.TransferReason.Lumber:
                case TransferManager.TransferReason.Flours:
                case TransferManager.TransferReason.Paper:
                case TransferManager.TransferReason.PlanedTimber:
                case TransferManager.TransferReason.Petrol:
                case TransferManager.TransferReason.Petroleum:
                case TransferManager.TransferReason.Plastics:
                case TransferManager.TransferReason.Glass:
                case TransferManager.TransferReason.Metals:
                case TransferManager.TransferReason.LuxuryProducts:
                case TransferManager.TransferReason.AnimalProducts:
                case TransferManager.TransferReason.Fish:
                    // Recognised cargo transfer type.
                    return true;

                default:
                    // Not a recognised cargo transfer.
                    return false;
            }
        }
    }
}