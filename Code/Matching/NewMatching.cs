using UnityEngine;
using ColossalFramework;


namespace TransferController
{
	/// <summary>
	/// Custom offer matching - new version.
	/// </summary>
	public static class NewMatching
	{
		/// <summary>
		/// Perform transfer offer matching for a specified TransferReason.
		/// </summary>
		/// <param name="__instance">TransferManager instance</param>
		/// <param name="reason">Transfer reason</param>
		/// <param name="incomingCounts">Incoming transfer count array</param>
		/// <param name="outgoingCounts">Outgoing transfer count array</param>
		/// <param name="incomingOffers">Incoming offer buffer</param>
		/// <param name="outgoingOffers">Outgoint offer buffer</param>
		/// <param name="m_incomingAmount">Outstanding incoming amount totals array</param>
		/// <param name="m_outgoingAmount">Outstanding outgoing amount totals array</param>
		public static void MatchOffers(TransferManager __instance,
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

			Logging.Message("new");

			// Block relevant to this reason.
			int reasonBlock = (int)reason * 8;

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
						MatchOffer(true, reason, priority, incomingIndex++, incomingOffers, incomingCounts, outgoingOffers, outgoingCounts);
					}

					// Match next outgoing offer.
					if (outgoingIndex < outgoingCounts[priorityIndex])
					{
						matching = true;
						MatchOffer(false, reason, priority, outgoingIndex++, outgoingOffers, outgoingCounts, incomingOffers, incomingCounts);
					}
				}
				while (matching);

				// Matching finished - clear this priority level buffer (any unmatched offers are effectively erased).
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
		/// <param name="reason">Transfer reason</param>
		/// <param name="priority">Specified reason's priority</param>
		/// <param name="index">Specified reason's index (within priority block)</param>
		/// <param name="offerBuffer">TransferReason buffer for specified transfer</param>
		/// <param name="offerCounts">Transfer counts array for specified transfer</param>
		/// <param name="candidateBuffer">TransferReason buffer for candidate offers for matching</param>
		/// <param name="candidateCounts">Transfer counts array for candidate offers for matching</param>
		public static void MatchOffer(bool incoming, TransferManager.TransferReason reason, int priority, int index, TransferManager.TransferOffer[] offerBuffer, ushort[] offerCounts, TransferManager.TransferOffer[] candidateBuffer, ushort[] candidateCounts)
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
			int lowerPriorityBound = Mathf.Max(0, 2 - priority);

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

			// Keep going until we've used up all the offer amount with matched transfers.
			int outstandingAmount = offer.Amount;
			do
			{
				// Iterate through candidate buffer, from highest to lowest priority.
				float bestDistance = float.MaxValue;
				int matchedPriority = -1, matchedIndex = -1;
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
						bool interWarehouse = offer.Exclude & candidate.Exclude;
						if (interWarehouse & candidatePriority <= lowerPriorityBound)
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

						// Check for pathfinding fails.
						if (PathFindFailure.HasFailure(offerBuilding, candidateBuilding))
						{
							continue;
						}

						// Apply warehouse boost if the candidate builing is a warehouse and the offer building isn't a warehouse or outside connection.
						if (candidate.Exclude & !(offer.Exclude | offerIsOutside))
						{
							distanceModifier /= (1 + AddOffers.warehousePriority);
						}

						// Apply outside connection boost if candidate is an outside connection, unless offer is also an outside connection.
						if (!offerIsOutside && candidateBuildingAI is OutsideConnectionAI)
						{
							switch (candidateBuildingAI.m_info.m_class.m_subService)
							{
								case ItemClass.SubService.PublicTransportTrain:
									distanceModifier /= (1 + Mathf.Pow(Matching.outsideRailPriority, 2));
									break;

								case ItemClass.SubService.PublicTransportShip:
									distanceModifier /= (1 + Mathf.Pow(Matching.outsideShipPriority, 2));
									break;
							}
						}

						// Calculate distance between positions - use original offer positions, not owning building positions.
						float squaredDistance = Vector3.SqrMagnitude(offer.Position - candidate.Position) * distanceModifier;
						if (squaredDistance < bestDistance)
						{
							// New nearest disance - apply checks.
							byte candidateDistrict = districtManager.GetDistrict(candidatePosition);
							byte candidatePark = districtManager.GetPark(candidatePosition);
							if (incoming)
							{
								if (!Matching.ChecksPassed(incoming, (byte)priority, (byte)candidatePriority, offerBuilding, candidateBuilding, offerDistrict, candidateDistrict, offerPark, candidatePark, reason))
								{
									continue;
								}

								// If candidate (outgoing) building is a warehouse, check quota.
								if (candidate.Exclude && !WarehouseControl.CheckVehicleQuota(candidateBuildingAI, candidateBuilding, ref buildingBuffer[candidateBuilding], reason, offerBuildingAI))
								{
									continue;
								}
							}
							else
							{
								if (!Matching.ChecksPassed(incoming, (byte)candidatePriority, (byte)priority, candidateBuilding, offerBuilding, candidateDistrict, offerDistrict, candidatePark, offerPark, reason))
								{
									continue;
								}

								// Otherwise, if the offer building is a warehouse and is the outgoing party, check quota there.
								if (offer.Exclude && !WarehouseControl.CheckVehicleQuota(offerBuildingAI, offerBuilding, ref buildingBuffer[offerBuilding], reason, candidateBuildingAI))
								{
									continue;
								}
							}

							// If we got here, all checks have been passed - this is now the nominated candidate.
							bestDistance = squaredDistance;
							matchedPriority = candidatePriority;
							matchedIndex = candidateIndex;

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
					Matching.StartTransfer(Singleton<TransferManager>.instance, reason, matchedOffer, offer, transferAmount);
				}
				else
				{
					Matching.StartTransfer(Singleton<TransferManager>.instance, reason, offer, matchedOffer, transferAmount);
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
    }
}