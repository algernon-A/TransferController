using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using ColossalFramework;
using HarmonyLib;


namespace TransferController
{
	/// <summary>
	/// Custom offer matching - old version.
	/// </summary>
	[HarmonyPatch]
	public static class OldMatching
	{
		// Matching distance multiplier.
		internal static int distancePercentage = 100;


		/*
		 * Transfer offer arrays are in blocks of 256, organised by reason, then by priority within each reason (8 prorities): block ID is (reason * 8) + priority.
		 * [block 0] 0 - 255: TransferReason.Garbage, Priority 0
		 * [block 1] 256 - 511: TransferReason.Garbage, Priority 1
		 * [block 2] 512 - 767: TransferReason.Garbage, Priority 2
		 * etc.
		 */

		/// <summary>
		/// Replacemnet method for TransferManager.MatchOffers.
		/// </summary>
		/// <param name="__instance">TransferManager instance</param>
		/// <param name="material">Material to match</param>
		public static void MatchOffers(TransferManager __instance,
			TransferManager.TransferReason material,
			ushort[] m_incomingCount,
			ushort[] m_outgoingCount,
			TransferManager.TransferOffer[] m_incomingOffers,
			TransferManager.TransferOffer[] m_outgoingOffers,
			int[] m_incomingAmount,
			int[] m_outgoingAmount)
		{
			/*
			 * Offers are matched in blocks, from highest priority to lowest.
			 */

			// Don't do anything if no material to match.
			if (material == TransferManager.TransferReason.None)
			{
				return;
			}

			// --- Setup for code inserts.

			DistrictManager districtManager = Singleton<DistrictManager>.instance;
			Vehicle[] vehicleBuffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
			Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

			// --- End setup for code inserts.

			// Distance multiplier for this transfer.
			float distanceMultiplier = GetDistanceMultiplier(material);

			// num = optimalDistanceSquared (offers within this distance are automatically accepted first go, with no further candidates examined).
			float optimalDistanceSquared = ((distanceMultiplier == 0f) ? 0f : (0.01f / distanceMultiplier));
			// ---- Start code insert
			optimalDistanceSquared *= distancePercentage / 100f;
			// ---- End code insert

			// num2 = thisPriority
			for (int thisPriority = 7; thisPriority >= 0; thisPriority--)
			{
				// num3 = offerBlock.
				int offerBlock = (int)material * 8 + thisPriority;

				// num4 = incomingCount
				int incomingCount = m_incomingCount[offerBlock];

				// num5 = outgoingCount
				int outgoingCount = m_outgoingCount[offerBlock];

				// num6 = incomingIndex
				int incomingIndex = 0;

				// num7 = outgoingIndex
				int outgoingIndex = 0;

				// Increment through all incoming and outgoing offers of this priority.
				while (incomingIndex < incomingCount || outgoingIndex < outgoingCount)
				{
					// Match incoming offers first, if we haven't exhausted all incoming offers with this priority.
					if (incomingIndex < incomingCount)
					{
						// transferOffer = incomingOfferToMatch
						TransferManager.TransferOffer incomingOfferToMatch = m_incomingOffers[offerBlock * 256 + incomingIndex];

						// postion = incomingPosition
						Vector3 incomingPosition = incomingOfferToMatch.Position;

						// ---- Start code insert

						// If this is a supported transfer, try to get ulitmate building source (leaving as zero by default).
						byte incomingDistrict = 0;
						byte incomingPark = 0;

						// AI reference.
						BuildingAI incomingAI = null;
						bool incomingIsOutside = false;

						// Boosted status.
						bool incomingRailBoosted = false, incomingShipBoosted = false;

						// Set up for exclusion checking.
						// Get incoming building and vehicle IDs.
						ushort incomingBuilding = incomingOfferToMatch.Building;

						// If no building, use vehicle source building, if any.
						if (incomingBuilding == 0)
						{
							ushort incomingVehicle = incomingOfferToMatch.Vehicle;
							if (incomingVehicle != 0)
							{
								incomingBuilding = vehicleBuffer[incomingVehicle].m_sourceBuilding;
							}
						}

						// Position of incoming building (source building or vehicle source building), if any.
						if (incomingBuilding != 0)
						{
							incomingPosition = buildingBuffer[incomingBuilding].m_position;

							// Incoming district.
							incomingDistrict = districtManager.GetDistrict(incomingPosition);
							incomingPark = districtManager.GetPark(incomingPosition);

							// Get AI reference.
							BuildingInfo incomingInfo = buildingBuffer[incomingBuilding].Info;
							incomingAI = incomingInfo.m_buildingAI;

							// Get boosted status.
							if (incomingAI is OutsideConnectionAI)
							{
								incomingIsOutside = true;
								incomingRailBoosted = incomingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTrain;
								incomingShipBoosted = incomingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportShip;
							}
						}

						// ---- End code insert

						// num8 = incoming offer amount
						int incomingOfferAmount = incomingOfferToMatch.Amount;
						do
						{
							/*
							 * Matching is done in descending priority order; a lower priority bound is set for lower priorities;
							 * an offer with priority 0 will only consider matching with priorities 7 down to 2, an offer with priority 1 will only consider priorities 7 down to 1.
							 * This way lower-priority transfers will have slightly fewer candidates for matching than higher-priority transfers.
							 */

							// num9 = lowerPriorityBound
							int lowerPriorityBound = Mathf.Max(0, 2 - thisPriority);

							// num10 = minPriority (minimum priority to accept)
							int minPriority = ((!incomingOfferToMatch.Exclude) ? lowerPriorityBound : Mathf.Max(0, offerBlock - thisPriority));

							// num11 = matchedPriority
							int matchedPriority = -1;

							// num12 = matchedIndex
							int matchedIndex = -1;

							// num13 = bestDistanceValue
							float bestDistanceValue = -1f;

							/// ---- Start code insert
							float closestDistance = float.MaxValue;
							/// ---- End code insert

							// num14 = currentIncomingIndex
							int currentIncomingIndex = outgoingIndex;

							// num15 = otherPriority
							for (int otherPriority = thisPriority; otherPriority >= lowerPriorityBound; otherPriority--)
							{
								// num16 = otherBlock
								int otherBlock = (int)material * 8 + otherPriority;

								// num17 = blockCount
								int blockCount = m_outgoingCount[otherBlock];

								// num18 = otherPriorityPlus
								float otherPriorityPlus = (float)otherPriority + 0.1f;

								// Breaks when distanceValue (see below) is greater than the other priority.
								// This means that a lower-level priority will be matched as 'good enough', but higher-level priorities will be more aggressively matched.
								if (bestDistanceValue >= otherPriorityPlus)
								{
									break;
								}

								// i = candidateIndex
								for (int candidateIndex = currentIncomingIndex; candidateIndex < blockCount; candidateIndex++)
								{
									// transferOffer 2 = outgoingOfferCandidate
									TransferManager.TransferOffer outgoingOfferCandidate = m_outgoingOffers[otherBlock * 256 + candidateIndex];
									if (!(incomingOfferToMatch.m_object != outgoingOfferCandidate.m_object) || (outgoingOfferCandidate.Exclude && otherPriority < minPriority))
									{
										continue;
									}

									// ---- Start code insert

									// Additional distance modifier for specific transactions.
									float distanceModifier = 1f;

									// Record default otherPriorityPlus.
									otherPriorityPlus = (float)otherPriority + 0.1f;

									// Apply custom districts filter - if failed, skip this candidate and cotinue to next candidate.
									// Get outgoing building and vehicle IDs.
									ushort outCandidateBuilding = outgoingOfferCandidate.Building;

									// If no building, use vehicle source building, if any.
									if (outCandidateBuilding == 0)
									{
										ushort outCandidateVehicle = outgoingOfferCandidate.Vehicle;
										if (outCandidateVehicle != 0)
										{
											outCandidateBuilding = vehicleBuffer[outCandidateVehicle].m_sourceBuilding;
										}
									}

									// Ensure we've got at least one valid building in the match before going further.
									if (incomingBuilding + outCandidateBuilding != 0)
									{
										// Check for pathfinding fails.
										if (PathFindFailure.HasFailure(incomingBuilding, outCandidateBuilding))
										{
											continue;
										}

										// Check for warehouses and other boosts.
										BuildingInfo candidateInfo = buildingBuffer[outCandidateBuilding].Info;
										BuildingAI candidateAI = candidateInfo.m_buildingAI;
										if (incomingAI is WarehouseAI)
										{
											// Is the candidate building also a warehouse, or an outside connection?
											if (candidateAI is WarehouseAI || candidateAI is OutsideConnectionAI)
											{
												// Yes - reverse warehouse priority modifier (this doesn't apply to warehouse-warehouse or warehouse-outside connection transfers).
												// Note - warehouses set to fill/empty aren't assigned the bonus to begin with, so this decreases below the original.  This is intentional to prioritise other transfers.
												otherPriorityPlus -= AddOffers.warehousePriority * 2f;
												if (otherPriorityPlus < 0)
												{
													otherPriorityPlus = 0;
												}
											}
											else
											{
												// No - add additional warehouse distance divisor.
												distanceModifier /= (1 + AddOffers.warehousePriority);
											}
										}
										else if (candidateAI is WarehouseAI outgoingWarehouseAI)
										{
											// Outgoing candidate is warehouse (but this incoming one isn't) - check vehicle quotas.
											if (!WarehouseControl.CheckVehicleQuota(outgoingWarehouseAI, outCandidateBuilding, ref buildingBuffer[outCandidateBuilding], material, incomingAI))
											{
												continue;
											}

											// Is this an outside connection?
											if (!(incomingAI is OutsideConnectionAI))
											{
												// No - adjust distance modifier for warehouse priority (this doesn't apply to warehouse-warehouse or warehouse-outside connection transfers).
												distanceModifier /= (1 + AddOffers.warehousePriority);
											}
										}
										else if (candidateAI is OutsideConnectionAI)
										{
											// Apply outside connection boosts as applicable.
											if (!incomingIsOutside)
											{
												if (candidateInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTrain)
												{
													otherPriorityPlus += Matching.outsideRailPriority;
													distanceModifier /= (1 + Mathf.Pow(Matching.outsideRailPriority, 2));
												}
												else if (candidateInfo.m_class.m_subService == ItemClass.SubService.PublicTransportShip)
												{
													otherPriorityPlus += Matching.outsideShipPriority;
													distanceModifier /= (1 + Mathf.Pow(Matching.outsideShipPriority, 2));
												}
											}
										}
										else if (incomingRailBoosted)
										{
											otherPriorityPlus += Matching.outsideRailPriority;
											distanceModifier /= (1 + Mathf.Pow(Matching.outsideRailPriority, 2));
										}
										else if (incomingShipBoosted)
										{
											otherPriorityPlus += Matching.outsideShipPriority;
											distanceModifier /= (1 + Mathf.Pow(Matching.outsideShipPriority, 2));
										}

										// Position of incoming building (source building or vehicle source building)
										Vector3 outCandidatePosition = outCandidateBuilding == 0 ? outgoingOfferCandidate.Position : buildingBuffer[outCandidateBuilding].m_position;

										if (!Matching.ChecksPassed(true, (byte)thisPriority, (byte)otherPriority, incomingBuilding, outCandidateBuilding, incomingDistrict, districtManager.GetDistrict(outCandidatePosition), incomingPark, districtManager.GetPark(outCandidatePosition), material, incomingOfferToMatch.Exclude, outgoingOfferCandidate.Exclude, incomingOfferToMatch.Position, outgoingOfferCandidate.Position))
										{
											continue;
										}
									}
									// ---- End code insert

									// num19 = squaredDistance
									float squaredDistance = Vector3.SqrMagnitude(outgoingOfferCandidate.Position - incomingPosition);


									/// ---- Start code replacement (additional if-else).
									if (squaredDistance < closestDistance)
									{
										matchedPriority = otherPriority;
										matchedIndex = candidateIndex;
										closestDistance = squaredDistance;
									}
									else
									{
										// num20 = distanceValue
										// E.g. distanceMultiplier for Fire is 1E-0.5f.
										// For other priority 5 and distance 1,000: 5.1 - 5.1 / (1f - 1,000^2 * 0.00001) = 0.4636364
										// For other priority 5 and distance 400: 5.1 - 5.1 / (1f - 400^2 * 0.00001) = 1.961539
										// For other priority 5 and distance 100: 5.1 - 5.1 / (1f - 100^2 * 0.00001) = 4.636364
										// For other priority 2 and distance 1,000: 2.1 - 2.1 / (1f - 1,000^2 * 0.00001) = 0.1909091
										// For other priority 2 and distance 400: 2.1 - 2.1 / (1f - 400^2 * 0.00001) = 0.8076923
										// For other priority 2 and distance 100: 2.1 - 2.1 / (1f - 100^2 * 0.00001) = 1.909091
										// This means that distance is more important for higher-level transfers.
										// A lower-priority transfer will take priority only if it's much closer, or conversely, a higher-priority offer will take precedence over a greater radius.
										float distanceValue = ((!(distanceMultiplier < 0f)) ? (otherPriorityPlus / (1f + squaredDistance * distanceMultiplier)) : (otherPriorityPlus - otherPriorityPlus / (1f - squaredDistance * distanceMultiplier))) * distanceModifier;
										if (distanceValue > bestDistanceValue)
										{
											matchedPriority = otherPriority;
											matchedIndex = candidateIndex;
											bestDistanceValue = distanceValue;

											// Automatically accept offers within the optimal distance.
											if (squaredDistance < optimalDistanceSquared)
											{
												break;
											}
										}
									}
									/// --- End code replacement (additional if-else)
								}
								currentIncomingIndex = 0;
							}
							if (matchedPriority == -1)
							{
								break;
							}

							// num21 = outgoingBlock
							int outgoingBlock = (int)material * 8 + matchedPriority;

							// transferOffer3 = matchedOutgoingOffer
							TransferManager.TransferOffer matchedOutgoingOffer = m_outgoingOffers[outgoingBlock * 256 + matchedIndex];

							// amount = matchedOutgoingAmount
							int matchedOutgoingAmount = matchedOutgoingOffer.Amount;

							// num22 = transferAmount
							int transferAmount = Mathf.Min(incomingOfferAmount, matchedOutgoingAmount);
							if (transferAmount != 0)
							{
								Matching.StartTransfer(__instance, material, matchedOutgoingOffer, incomingOfferToMatch, transferAmount);
							}
							incomingOfferAmount -= transferAmount;
							matchedOutgoingAmount -= transferAmount;
							if (matchedOutgoingAmount == 0)
							{
								// Matched outgoing offer amount fully used; remove from offer buffer.

								// num23 = newOfferCount
								int newOfferCount = m_outgoingCount[outgoingBlock] - 1;

								m_outgoingCount[outgoingBlock] = (ushort)newOfferCount;
								ref TransferManager.TransferOffer reference = ref m_outgoingOffers[outgoingBlock * 256 + matchedIndex];
								reference = m_outgoingOffers[outgoingBlock * 256 + newOfferCount];
								if (outgoingBlock == offerBlock)
								{
									outgoingCount = newOfferCount;
								}
							}
							else
							{
								// Matched outgoing offer amount partially used; reduce outstanding amount in offer.
								matchedOutgoingOffer.Amount = matchedOutgoingAmount;
								m_outgoingOffers[outgoingBlock * 256 + matchedIndex] = matchedOutgoingOffer;
							}
							incomingOfferToMatch.Amount = incomingOfferAmount;
						}
						while (incomingOfferAmount != 0);
						if (incomingOfferAmount == 0)
						{
							// Incoming offer amount fully used; remove incoming offer from queue.
							incomingCount--;
							m_incomingCount[offerBlock] = (ushort)incomingCount;
							ref TransferManager.TransferOffer reference2 = ref m_incomingOffers[offerBlock * 256 + incomingIndex];
							reference2 = m_incomingOffers[offerBlock * 256 + incomingCount];
						}
						else
						{
							// Incoming offer amount not fully used; reduce outstanding amount in offer.
							incomingOfferToMatch.Amount = incomingOfferAmount;
							m_incomingOffers[offerBlock * 256 + incomingIndex] = incomingOfferToMatch;
							incomingIndex++;
						}
					}
					if (outgoingIndex >= outgoingCount)
					{
						continue;
					}

					// transferOffer4 = outgoingOfferToMatch
					TransferManager.TransferOffer outgoingOfferToMatch = m_outgoingOffers[offerBlock * 256 + outgoingIndex];

					// position2 = outgoingPosition
					Vector3 outgoingPosition = outgoingOfferToMatch.Position;

					// ---- Start code insert

					// If this is a supported transfer, try to get ulitmate building source (leaving as zero by default).
					byte outgoingDistrict = 0;
					byte outgoingPark = 0;

					// AI reference.
					BuildingAI outgoingAI = null;
					bool outgoingIsOutside = false;

					// Boosted status.
					bool outgoingRailBoosted = false, outgoingShipBoosted = false;

					// Set up for exclusion checking.
					// Get incoming building and vehicle IDs.
					ushort outgoingBuilding = outgoingOfferToMatch.Building;
					ushort outgoingVehicle = outgoingOfferToMatch.Vehicle;

					// If no building, use vehicle source building, if any.
					if (outgoingBuilding == 0 & outgoingVehicle != 0)
					{
						outgoingBuilding = vehicleBuffer[outgoingVehicle].m_sourceBuilding;
					}

					// Position of incoming building (source building or vehicle source building), if any.
					if (outgoingBuilding != 0)
					{
						outgoingPosition = buildingBuffer[outgoingBuilding].m_position;

						// Outgoing district.
						outgoingDistrict = districtManager.GetDistrict(outgoingPosition);
						outgoingPark = districtManager.GetPark(outgoingPosition);

						// Get AI reference.
						BuildingInfo outgoingInfo = buildingBuffer[outgoingBuilding].Info;
						outgoingAI = outgoingInfo.m_buildingAI;

						// Get boosted status.
						if (outgoingAI is OutsideConnectionAI)
						{
							outgoingIsOutside = true;
							outgoingRailBoosted = outgoingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTrain;
							outgoingShipBoosted = outgoingInfo.m_class.m_subService == ItemClass.SubService.PublicTransportShip;
						}
					}

					// ---- End code insert

					// num24 = outgoingAmount
					int outgoingAmount = outgoingOfferToMatch.Amount;
					do
					{
						// num9 = lowerPriorityBound
						int lowerPriorityBound = Mathf.Max(0, 2 - thisPriority);

						// num26 = lowerPriorityBound (resuing from above)
						lowerPriorityBound = ((!outgoingOfferToMatch.Exclude) ? lowerPriorityBound : Mathf.Max(0, 3 - thisPriority));

						// num27 = matchedPriority
						int matchedPriority = -1;

						// num28 = matchedIndex
						int matchedIndex = -1;

						// num29 = bestDistanceValue
						float bestDistanceValue = -1f;

						/// ---- Start code insert
						float closestDistance = float.MaxValue;
						/// ---- End code insert

						// num30 = currentOutgoingIndex
						int currentOutgoingIndex = incomingIndex;

						// num31 = otherPriority
						for (int otherPriority = thisPriority; otherPriority >= lowerPriorityBound; otherPriority--)
						{
							// num32 = otherBlock
							int otherBlock = (int)material * 8 + otherPriority;

							// num33 = blockCount
							int blockCount = m_incomingCount[otherBlock];

							// num34 - otherPriorityPlus
							float otherPriorityPlus = (float)otherPriority + 0.1f;

							// Breaks when distanceValue (see below) is greater than the other priority.
							// This means that a lower-level priority will be matched as 'good enough', but higher-level priorities will be more aggressively matched.
							if (bestDistanceValue >= otherPriorityPlus)
							{
								break;
							}

							// j = candidateIndex
							for (int candidateIndex = currentOutgoingIndex; candidateIndex < blockCount; candidateIndex++)
							{
								// transferOffer5 = incomingOfferCandidate
								TransferManager.TransferOffer incomingOfferCandidate = m_incomingOffers[otherBlock * 256 + candidateIndex];
								if (!(outgoingOfferToMatch.m_object != incomingOfferCandidate.m_object) || (incomingOfferCandidate.Exclude && otherPriority < lowerPriorityBound))
								{
									continue;
								}

								// ---- Start code insert

								// Additional distance modifier for specific transactions.
								float distanceModifier = 1f;

								// Record default otherPriorityPlus.
								otherPriorityPlus = (float)otherPriority + 0.1f;

								// Apply custom districts filter - if failed, skip this candidate and cotinue to next candidate.
								// Get incoming building and vehicle IDs.
								ushort inCandidateBuilding = incomingOfferCandidate.Building;

								// If no building, use vehicle source building, if any.
								if (inCandidateBuilding == 0)
								{
									ushort inCandidateVehicle = incomingOfferCandidate.Vehicle;
									if (inCandidateVehicle != 0)
									{
										inCandidateBuilding = vehicleBuffer[inCandidateVehicle].m_sourceBuilding;
									}
								}

								// Ensure we've got at least one valid building in the match before going further.
								if (outgoingBuilding + inCandidateBuilding != 0)
								{
									// Check for pathfinding fails.
									if (PathFindFailure.HasFailure(inCandidateBuilding, outgoingBuilding))
									{
										continue;
									}

									// Check for warehouses and other boosts.
									BuildingInfo candidateInfo = buildingBuffer[inCandidateBuilding].Info;
									BuildingAI candidateAI = candidateInfo.m_buildingAI;
									if (outgoingAI is WarehouseAI outgoingWarehouseAI)
									{
										// Outgoing building is warehouse - check vehicle quotas.
										if (!WarehouseControl.CheckVehicleQuota(outgoingWarehouseAI, outgoingBuilding, ref buildingBuffer[outgoingBuilding], material, candidateAI))
										{
											continue;
										}

										// Is the candidate building also a warehouse, or an outside connection?
										if (candidateAI is WarehouseAI || candidateAI is OutsideConnectionAI)
										{
											// Yes - reverse warehouse priority modifier (this doesn't apply to warehouse-warehouse or warehouse-outside connection transfers).

											// Note - warehouses set to fill/empty aren't assigned the bonus to begin with, so this decreases below the original.  This is intentional to prioritise other transfers.
											otherPriorityPlus -= AddOffers.warehousePriority * 2f;
											if (otherPriorityPlus < 0)
											{
												otherPriorityPlus = 0;
											}
										}
										else
										{
											// No - add additional warehouse distance divisor.
											distanceModifier /= (1 + AddOffers.warehousePriority);
										}
									}
									else if (candidateAI is WarehouseAI)
									{
										// Is this an outside connection?
										if (!(outgoingAI is OutsideConnectionAI))
										{
											// No - adjust distance modifier for warehouse priority (this doesn't apply to warehouse-warehouse or warehouse-outside connection transfers).
											distanceModifier /= (1 + AddOffers.warehousePriority);
										}
									}
									else if (candidateAI is OutsideConnectionAI)
									{
										// Apply outside connection boosts as applicable.
										if (!outgoingIsOutside)
										{
											if (candidateInfo.m_class.m_subService == ItemClass.SubService.PublicTransportTrain)
											{
												otherPriorityPlus += Matching.outsideRailPriority;
												distanceModifier /= (1 + Mathf.Pow(Matching.outsideRailPriority, 2));
											}
											else if (candidateInfo.m_class.m_subService == ItemClass.SubService.PublicTransportShip)
											{
												otherPriorityPlus += Matching.outsideShipPriority;
												distanceModifier /= (1 + Mathf.Pow(Matching.outsideShipPriority, 2));
											}
										}
									}
									else if (outgoingRailBoosted)
									{
										otherPriorityPlus += Matching.outsideRailPriority;
										distanceModifier /= (1 + Mathf.Pow(Matching.outsideRailPriority, 2));
									}
									else if (outgoingShipBoosted)
									{
										otherPriorityPlus += Matching.outsideShipPriority;
										distanceModifier /= (1 + Mathf.Pow(Matching.outsideShipPriority, 2));
									}

									// Position of incoming building (source building or vehicle source building)
									Vector3 inCandidatePosition = inCandidateBuilding == 0 ? incomingOfferCandidate.Position : buildingBuffer[inCandidateBuilding].m_position;

									if (!Matching.ChecksPassed(false, (byte)otherPriority, (byte)thisPriority, inCandidateBuilding, outgoingBuilding, districtManager.GetDistrict(inCandidatePosition), outgoingDistrict, districtManager.GetPark(inCandidatePosition), outgoingPark, material, incomingOfferCandidate.Exclude, outgoingOfferToMatch.Exclude, incomingOfferCandidate.Position, outgoingOfferToMatch.Position))
									{
										continue;
									}
								}
								// ---- End code insert


								// num35 = squaredDistance
								float squaredDistance = Vector3.SqrMagnitude(incomingOfferCandidate.Position - outgoingPosition);

								/// ---- Start code replacement (additional if-else).
								if (squaredDistance < closestDistance)
								{
									matchedPriority = otherPriority;
									matchedIndex = candidateIndex;
									closestDistance = squaredDistance;
								}
								else
								{
									// num36 = distanceValue
									// See above re num20 for details.
									float distanceValue = (!(distanceMultiplier < 0f)) ? (otherPriorityPlus / (1f + squaredDistance * distanceMultiplier)) : (otherPriorityPlus - otherPriorityPlus / (1f - squaredDistance * distanceMultiplier)) * distanceModifier;
									if (distanceValue > bestDistanceValue)
									{
										matchedPriority = otherPriority;
										matchedIndex = candidateIndex;
										bestDistanceValue = distanceValue;

										// Automatically accept offers within the optimal distance.
										if (squaredDistance < optimalDistanceSquared)
										{
											break;
										}
									}
								}
								/// --- End code replacement (additional if-else)
							}
							currentOutgoingIndex = 0;
						}
						if (matchedPriority == -1)
						{
							break;
						}

						// num37 = incomingBlock
						int incomingBlock = (int)material * 8 + matchedPriority;

						// transferOffer6 = matchedIncomingOffer
						TransferManager.TransferOffer matchedIncomingOffer = m_incomingOffers[incomingBlock * 256 + matchedIndex];

						// amount2 = incomingAmount
						int incomingAmount = matchedIncomingOffer.Amount;

						// num38 = transferAmount
						int transferAmount = Mathf.Min(outgoingAmount, incomingAmount);
						if (transferAmount != 0)
						{
							Matching.StartTransfer(__instance, material, outgoingOfferToMatch, matchedIncomingOffer, transferAmount);
						}
						outgoingAmount -= transferAmount;
						incomingAmount -= transferAmount;
						if (incomingAmount == 0)
						{
							// Matched incoming offer amount fully used; remove from offer buffer.

							// num39 = newOfferCount
							int newOfferCount = m_incomingCount[incomingBlock] - 1;
							m_incomingCount[incomingBlock] = (ushort)newOfferCount;
							ref TransferManager.TransferOffer reference3 = ref m_incomingOffers[incomingBlock * 256 + matchedIndex];
							reference3 = m_incomingOffers[incomingBlock * 256 + newOfferCount];
							if (incomingBlock == offerBlock)
							{
								incomingCount = newOfferCount;
							}
						}
						else
						{
							// Matched incoming offer amount partially used; reduce outstanding amount in offer.
							matchedIncomingOffer.Amount = incomingAmount;
							m_incomingOffers[incomingBlock * 256 + matchedIndex] = matchedIncomingOffer;
						}
						outgoingOfferToMatch.Amount = outgoingAmount;
					}
					while (outgoingAmount != 0);
					if (outgoingAmount == 0)
					{
						// Outgoing offer amount fully used; remove outgoing offer from queue.
						outgoingCount--;
						m_outgoingCount[offerBlock] = (ushort)outgoingCount;
						ref TransferManager.TransferOffer reference4 = ref m_outgoingOffers[offerBlock * 256 + outgoingIndex];
						reference4 = m_outgoingOffers[offerBlock * 256 + outgoingCount];
					}
					else
					{
						// Outgoing offer amount not fully used; reduce outstanding amount in offer.
						outgoingOfferToMatch.Amount = outgoingAmount;
						m_outgoingOffers[offerBlock * 256 + outgoingIndex] = outgoingOfferToMatch;
						outgoingIndex++;
					}
				}
			}
			for (int k = 0; k < 8; k++)
			{
				int num40 = (int)material * 8 + k;
				m_incomingCount[num40] = 0;
				m_outgoingCount[num40] = 0;
			}
			m_incomingAmount[(int)material] = 0;
			m_outgoingAmount[(int)material] = 0;
		}


		/// <summary>
		/// Harmony reverse patch to access private method TransferManager.GetDistanceMultiplier.
		/// </summary>
		/// <param name="instance">TransferManager instance</param>
		/// <param name="material">Transfer material</param>
		/// <returns>Distance multiplier</returns>
		[HarmonyReversePatch]
		[HarmonyPatch((typeof(TransferManager)), "GetDistanceMultiplier")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static float GetDistanceMultiplier(TransferManager.TransferReason material)
		{
			Logging.Error("GetDistanceMultiplier reverse Harmony patch wasn't applied, params: ", material);
			throw new NotImplementedException("Harmony reverse patch not applied");
		}
	}
}