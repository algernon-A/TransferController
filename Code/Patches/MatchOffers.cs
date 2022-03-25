using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using ColossalFramework;
using HarmonyLib;


namespace TransferController
{
	/// <summary>
	/// Harmony patch to implement custom TransferManager functions.
	/// </summary>
	[HarmonyPatch]
	public static class TransferManagerPatches
	{
		// Matching distance multiplier.
		internal static int distancePercentage = 100;

		// Reflection info for private TransferManager fields.
		private static FieldInfo m_incomingCountField;
		private static FieldInfo m_outgoingCountField;
		private static FieldInfo m_incomingOffersField;
		private static FieldInfo m_outgoingOffersField;
		private static FieldInfo m_incomingAmountField;
		private static FieldInfo m_outgoingAmountField;


		/// <summary>
		/// Patch TransferManager.MatchOffers.
		/// </summary>
		/// <param name="harmonyInstance"></param>
		public static void Patch(Harmony harmonyInstance)
		{
			// Reflect private fields.
			m_incomingCountField = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.Instance | BindingFlags.NonPublic);
			m_outgoingCountField = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.Instance | BindingFlags.NonPublic);
			m_incomingOffersField = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
			m_outgoingOffersField = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
			m_incomingAmountField = typeof(TransferManager).GetField("m_incomingAmount", BindingFlags.Instance | BindingFlags.NonPublic);
			m_outgoingAmountField = typeof(TransferManager).GetField("m_outgoingAmount", BindingFlags.Instance | BindingFlags.NonPublic);

			// Check for errors.
			if (m_incomingCountField == null) { Logging.Error("incoming count field null"); return; }
			if (m_outgoingCountField == null) { Logging.Error("outgoing count field null"); return; }
			if (m_incomingOffersField == null) { Logging.Error("incoming offer field null"); return; }
			if (m_outgoingOffersField == null) { Logging.Error("outgoing offer field null"); return; }
			if (m_incomingAmountField == null) { Logging.Error("incoming amount field null"); return; }
			if (m_outgoingAmountField == null) { Logging.Error("outgoing amount field null"); return; }

			// Patch method with new pre-emptive prefix.
			MethodBase targetMethod = typeof(TransferManager).GetMethod("MatchOffers", BindingFlags.Instance | BindingFlags.NonPublic);
			harmonyInstance.Patch(targetMethod, prefix: new HarmonyMethod(typeof(TransferManagerPatches), nameof(TransferManagerPatches.MatchOffers)));
			Logging.Message("MatchOffers patched");
		}


		/*
		 * Transfer offer arrays are in blocks of 256, organised by reason, then by priority within each reason (8 prorities): block ID is (reason * 8) + priority.
		 * [block 0] 0 - 255: TransferReason.Garbage, Priority 0
		 * [block 1] 256 - 511: TransferReason.Garbage, Priority 1
		 * [block 2] 512 - 767: TransferReason.Garbage, Priority 2
		 * etc.
		 */

		/// <summary>
		/// Harmony pre-emptive Prefix patch for TransferManager.MatchOffers.
		/// Implements the district limitation system.
		/// </summary>
		/// <param name="__instance">TransferManager instance</param>
		/// <param name="material">Material to match</param>
		/// <returns>Always false (never execute original method)</returns>
		public static bool MatchOffers(TransferManager __instance, TransferManager.TransferReason material)
		{
			/*
			 * Offers are matched in blocks, from highest priority to lowest.
			 */

			// Don't do anything if no material to match.
			if (material == TransferManager.TransferReason.None)
			{
				return false;
			}

			// --- Reflection to access private members.
			ushort[] m_incomingCount = m_incomingCountField.GetValue(__instance) as ushort[];
			ushort[] m_outgoingCount = m_outgoingCountField.GetValue(__instance) as ushort[];
			TransferManager.TransferOffer[] m_incomingOffers = m_incomingOffersField.GetValue(__instance) as TransferManager.TransferOffer[];
			TransferManager.TransferOffer[] m_outgoingOffers = m_outgoingOffersField.GetValue(__instance) as TransferManager.TransferOffer[];
			int[] m_incomingAmount = m_incomingAmountField.GetValue(__instance) as int[];
			int[] m_outgoingAmount = m_outgoingAmountField.GetValue(__instance) as int[];

			// --- End reflection.

			// --- Setup for code inserts.
			DistrictManager districtManager = Singleton<DistrictManager>.instance;
			Vehicle[] vehicleBuffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
			Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

			bool supportedReason = SupportedTransfer(material);

			// --- End setup for code inserts.

			// Distance multiplier for this transfer.
			float distanceMultiplier = GetDistanceMultiplier(material);

			// num = optimalDistanceSquared (offers within this distance are automatically accepted first go, with no further candidates examined).
			float optimalDistanceSquared = ((distanceMultiplier == 0f) ? 0f : (0.01f / distanceMultiplier));
			// ---- Start code insert
			if (supportedReason)
			{
				optimalDistanceSquared *= distancePercentage / 100f;
			}
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
						ushort incomingBuilding = 0;
						byte incomingDistrict = 0;
						byte incomingPark = 0;

						// Set up for exclusion checking if this is a supported reason.
						if (supportedReason)
						{
							// Get incoming building and vehicle IDs.
							incomingBuilding = incomingOfferToMatch.Building;

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

							int num10 = ((!incomingOfferToMatch.Exclude) ? lowerPriorityBound : Mathf.Max(0, offerBlock - thisPriority));

							// num11 = matchedPriority
							int matchedPriority = -1;

							// num12 = matchedIndex
							int matchedIndex = -1;

							// num13 = bestDistanceValue
							float bestDistanceValue = -1f;

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
									if (!(incomingOfferToMatch.m_object != outgoingOfferCandidate.m_object) || (outgoingOfferCandidate.Exclude && otherPriority < num10))
									{
										continue;
									}

									// ---- Start code insert

									// Apply custom districts filter - if failed, skip this candidate and cotinue to next candidate.
									if (supportedReason)
									{
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
											// Position of incoming building (source building or vehicle source building)
											Vector3 outCandidatePosition = outCandidateBuilding == 0 ? outgoingOfferCandidate.Position : buildingBuffer[outCandidateBuilding].m_position;

											if (!DistrictChecksPassed(true, incomingBuilding, outgoingOfferCandidate.Building, incomingDistrict, districtManager.GetDistrict(outCandidatePosition), incomingPark, districtManager.GetPark(outCandidatePosition), material))
											{
												continue;
											}
										}
									}
									// ---- End code insert

									// num19 = squaredDistance
									float squaredDistance = Vector3.SqrMagnitude(outgoingOfferCandidate.Position - incomingPosition);

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
									float distanceValue = ((!(distanceMultiplier < 0f)) ? (otherPriorityPlus / (1f + squaredDistance * distanceMultiplier)) : (otherPriorityPlus - otherPriorityPlus / (1f - squaredDistance * distanceMultiplier)));
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
								StartTransfer(__instance, material, matchedOutgoingOffer, incomingOfferToMatch, transferAmount);
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
					ushort outgoingBuilding = 0;
					byte outgoingDistrict = 0;
					byte outgoingPark = 0;

					// Set up for exclusion checking if this is a supported reason.
					if (supportedReason)
					{
						// Get incoming building and vehicle IDs.
						outgoingBuilding = outgoingOfferToMatch.Building;
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
						}
					}

					// ---- End code insert

					// num24 = outgoingAmount
					int outgoingAmount = outgoingOfferToMatch.Amount;
					do
					{
						// num9 = lowerPriorityBound
						int lowerPriorityBound = Mathf.Max(0, 2 - thisPriority);
						int num26 = ((!outgoingOfferToMatch.Exclude) ? lowerPriorityBound : Mathf.Max(0, 3 - thisPriority));

						// num27 = matchedPriority
						int matchedPriority = -1;

						// num28 = matchedIndex
						int matchedIndex = -1;

						// num29 = bestDistanceValue
						float bestDistanceValue = -1f;

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
								if (!(outgoingOfferToMatch.m_object != incomingOfferCandidate.m_object) || (incomingOfferCandidate.Exclude && otherPriority < num26))
								{
									continue;
								}

								// ---- Start code insert
								// Apply custom districts filter - if failed, skip this candidate and cotinue to next candidate.
								if (supportedReason)
								{
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
										// Position of incoming building (source building or vehicle source building)
										Vector3 inCandidatePosition = inCandidateBuilding == 0 ? incomingOfferCandidate.Position : buildingBuffer[inCandidateBuilding].m_position;

										if (!DistrictChecksPassed(false, incomingOfferCandidate.Building, outgoingOfferToMatch.Building, districtManager.GetDistrict(inCandidatePosition), outgoingDistrict, districtManager.GetPark(inCandidatePosition), outgoingPark, material))
										{
											continue;
										}
									}
								}
								// ---- End code insert


								// num35 = squaredDistance
								float squaredDistance = Vector3.SqrMagnitude(incomingOfferCandidate.Position - outgoingPosition);

								// num36 = distanceValue
								// See above re num20 for details.
								float distanceValue = ((!(distanceMultiplier < 0f)) ? (otherPriorityPlus / (1f + squaredDistance * distanceMultiplier)) : (otherPriorityPlus - otherPriorityPlus / (1f - squaredDistance * distanceMultiplier)));
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
							StartTransfer(__instance, material, outgoingOfferToMatch, matchedIncomingOffer, transferAmount);
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

			// Don't execute game method.
			return false;
		}


		/// <summary>
		/// Harmony reverse patch to access private method TransferManager.GetDistanceMultiplier.
		/// </summary>
		/// <param name="instance">TransferManager instance</param>
		/// <param name="material">Transfer material</param>
		/// <returns></returns>
		[HarmonyReversePatch]
		[HarmonyPatch((typeof(TransferManager)), "GetDistanceMultiplier")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static float GetDistanceMultiplier(TransferManager.TransferReason material)
		{
			Logging.Error("GetDistanceMultiplier reverse Harmony patch wasn't applied, params: ", material);
			throw new NotImplementedException("Harmony reverse patch not applied");
		}


		/// <summary>
		/// Harmony reverse patch to access private method TransferManager.StartTransfer.
		/// </summary>
		/// <param name="instance">TransferManager instance</param>
		/// <param name="material">Transfer material</param>
		/// <returns></returns>
		[HarmonyReversePatch]
		[HarmonyPatch((typeof(TransferManager)), "StartTransfer")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void StartTransfer(object instance, TransferManager.TransferReason material, TransferManager.TransferOffer offerOut, TransferManager.TransferOffer offerIn, int delta)
		{
			Logging.Error("StartTransfer reverse Harmony patch wasn't applied, params: ", instance, material, offerOut, offerIn, delta);
			throw new NotImplementedException("Harmony reverse patch not applied");
		}


		/// <summary>
		/// Applies district fileters, both incoming and outgoing.
		/// </summary>
		/// <param name="incoming">True if this is an incoming offer, false otherwise</param
		/// <param name="incomingBuildingID">Building ID to check</param
		/// <param name="outgoingBuildingID">Building ID to check</param>
		/// <param name="incomingDistrict">District of incoming offer</param>
		/// <param name="outgoingDistrict">District of outgoing offer</param>
		/// <param name="incomingPark">Park area of incoming offer</param>
		/// <param name="outgoingPark">Park area of outgoing offer</param>
		/// <param name="reason">Transfer reason</param>
		/// <returns>True if the transfer is permitted, false if prohibited</returns>
		private static bool DistrictChecksPassed(bool incoming, ushort incomingBuildingID, ushort outgoingBuildingID, byte incomingDistrict, byte outgoingDistrict, byte incomingPark, byte outgoingPark, TransferManager.TransferReason reason)
		{
			// First, check for incoming district restrictions.
			if (IncomingDistrictChecksPassed(incomingBuildingID, outgoingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingPark, reason))
			{
				// Then, outgoing.
				bool result = OutgoingDistrictChecksPassed(outgoingBuildingID, incomingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingDistrict, reason);
				TransferLogging.AddEntry(reason, incoming, incomingBuildingID, outgoingBuildingID, result, result ? LogEntry.BlockReason.None : LogEntry.BlockReason.OutgoingDistrict);
				return result;
			}

			// Failed incoming district restrictions - return false.
			TransferLogging.AddEntry(reason, incoming, incomingBuildingID, outgoingBuildingID, false, LogEntry.BlockReason.IncomingDistrict);
			return false;
        }


		/// <summary>
		///  Applies incoming district filters.
		/// </summary>
		/// <param name="buildingID">Building ID to check</param>
		/// <param name="outgoingBuildingID">Building ID of outgoing building</param>
		/// <param name="incomingDistrict">District of incoming offer</param>
		/// <param name="outgoingDistrict">District of outgoing offer</param>
		/// <param name="incomingPark">Park area of incoming offer</param>
		/// <param name="outgoingPark">Park area of outgoing offer</param>
		/// <param name="transferReason">Transfer reason</param>
		/// <returns>True if the transfer is permitted, false if prohibited</returns>
		private static bool IncomingDistrictChecksPassed(ushort buildingID, ushort outgoingBuildingID, byte incomingDistrict, byte outgoingDistrict, byte incomingPark, byte outgoingPark, TransferManager.TransferReason transferReason)
		{
			// Calculate building record ID.
			uint mask = ServiceLimits.IncomingMask << 24;
			uint buildingRecordID = (uint)(buildingID + mask);


			// Get building record.
			if (ServiceLimits.buildingRecords.TryGetValue(buildingRecordID, out ServiceLimits.BuildingRecord buildingRecord))
            {
				// Check for transfer reason match.
				if (buildingRecord.reason != TransferManager.TransferReason.None && buildingRecord.reason != transferReason)
				{
					// Transfer reason didn't match; try secondary record
					if (buildingRecord.nextRecord == 0)
                    {
						// No secondary record; no relevant restrictions.
						return true;
                    }

					// Get secondary record. 
					if (!ServiceLimits.buildingRecords.TryGetValue(buildingID | (uint)(buildingRecord.nextRecord << 24), out buildingRecord))
                    {
						// No secondary record in dictionary; no relevant restrictions.
						return true;
                    }

					// Check secondary transfer reason match.
					if (buildingRecord.reason != TransferManager.TransferReason.None && buildingRecord.reason != transferReason)
					{
						// No secondary transfer reason match; no relevant restrictions.
						return true;
					}
				}

				// Check outside connection.
				if (((buildingRecord.flags & ServiceLimits.RestrictionFlags.BlockOutsideConnection) != ServiceLimits.RestrictionFlags.None) && Singleton<BuildingManager>.instance.m_buildings.m_buffer[outgoingBuildingID].Info.m_buildingAI is OutsideConnectionAI)
				{
					// This is an outside connection and it's not permitted; return false.
					return false;
				}

				// Check same-district setting.
				if ((buildingRecord.flags & ServiceLimits.RestrictionFlags.BlockSameDistrict) == ServiceLimits.RestrictionFlags.None && (incomingDistrict != 0 && incomingDistrict == outgoingDistrict || (incomingPark != 0 && incomingPark == outgoingPark)))
				{
					// Same district match - permitted.
					return true;
				}

				// No same-district setting: return value is if the transfer reason is a match and if outgoing district is in the allowed districts for this building.
				if ((buildingRecord.reason == TransferManager.TransferReason.None || buildingRecord.reason == transferReason) && buildingRecord.districts != null)
				{
					return buildingRecord.districts.Contains(outgoingDistrict) || buildingRecord.districts.Contains(~outgoingPark);
				}
			}
			else
            {
				// No record means no restrictions.
				return true;
            }

			// If we got here, we didn't get a record.
			return false;
		}



		/// <summary>
		///  Applies outgoing district filters.
		/// </summary>
		/// <param name="buildingID">Building ID to check</param>
		/// <param name="incomingBuildingID">Building ID of incoming building</param>
		/// <param name="incomingDistrict">District of incoming offer</param>
		/// <param name="incomingPark">Park area of incoming offer</param>
		/// <param name="outgoingPark">Park area of outgoing offer</param>
		/// <param name="outgoingDistrict">District of outgoing offer</param>
		/// <param name="transferReason">Transfer reason</param>
		/// <returns>True if the transfer is permitted, false if prohibited</returns>
		private static bool OutgoingDistrictChecksPassed(ushort buildingID, ushort incomingBuildingID, byte incomingDistrict, byte outgoingDistrict, byte incomingPark, byte outgoingPark, TransferManager.TransferReason transferReason)
		{
			// Calculate building record ID.
			uint mask = (uint)ServiceLimits.OutgoingMask << 24;
			uint buildingRecordID = (uint)(buildingID + mask);

			// Get building record.
			if (ServiceLimits.buildingRecords.TryGetValue(buildingRecordID, out ServiceLimits.BuildingRecord buildingRecord))
			{
				// Check for transfer reason match.
				if (buildingRecord.reason != TransferManager.TransferReason.None && buildingRecord.reason != transferReason)
				{
					// Transfer reason didn't match; try secondary record
					if (buildingRecord.nextRecord == 0)
					{
						// No secondary record; no relevant restrictions.
						return true;
					}

					// Get secondary record. 
					if (!ServiceLimits.buildingRecords.TryGetValue(buildingID | (uint)(buildingRecord.nextRecord << 24), out buildingRecord))
					{
						// No secondary record in dictionary; no relevant restrictions.
						return true;
					}

					// Check secondary transfer reason match.
					if (buildingRecord.reason != TransferManager.TransferReason.None && buildingRecord.reason != transferReason)
                    {
						// No secondary transfer reason match; no relevant restrictions.
						return true;
                    }
				}

				// Check outside connection.
				if (((buildingRecord.flags & ServiceLimits.RestrictionFlags.BlockOutsideConnection) != ServiceLimits.RestrictionFlags.None) && Singleton<BuildingManager>.instance.m_buildings.m_buffer[incomingBuildingID].Info.m_buildingAI is OutsideConnectionAI)
                {
					// This is an outside connection and it's not permitted; return false.
					return false;
                }

				// Check same-district setting.
				if ((buildingRecord.flags & ServiceLimits.RestrictionFlags.BlockSameDistrict) == ServiceLimits.RestrictionFlags.None && (incomingDistrict != 0 && incomingDistrict == outgoingDistrict || (incomingPark != 0 && incomingPark == outgoingPark)))
				{
					// Same district match - permitted.
					return true;
				}

				// Only block specified transfers.
				if (buildingRecord.reason == TransferManager.TransferReason.None)
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
						case TransferManager.TransferReason.Taxi:
						case TransferManager.TransferReason.AnimalProducts:
						case TransferManager.TransferReason.Fish:
							// Legitimate transfer reason; resume normal outgoing district check.
							break;

						default:
							// Not a recognised ougoing transfer; automatically permit the transfer.
							return true;
					}
                }

				// No same-district setting: return value is if the transfer reason is a match and if outgoing district is in the allowed districts for this building.
				if ((buildingRecord.reason == TransferManager.TransferReason.None || buildingRecord.reason == transferReason) && buildingRecord.districts != null)
				{
					return buildingRecord.districts.Contains(incomingDistrict) || buildingRecord.districts.Contains(~incomingPark);
				}
			}
			else
			{
				// No record means no restrictions.
				return true;
			}

			// If we got here, we didn't get a record.
			return false;
		}


		/// <summary>
		/// Dertermines whether or not this transfer reason is supported.
		/// </summary>
		/// <param name="reason">Transfer reason to check</param>
		/// <returns>True if this is a supported reason, false otherwise</returns>
		private static bool SupportedTransfer(TransferManager.TransferReason reason)
        {
			switch (reason)
            {
				// Supported reasons.
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
				case TransferManager.TransferReason.Crime:
				case TransferManager.TransferReason.CriminalMove:
				case TransferManager.TransferReason.Fire:
				case TransferManager.TransferReason.Fire2:
				case TransferManager.TransferReason.ForestFire:
				case TransferManager.TransferReason.Sick:
				case TransferManager.TransferReason.Sick2:
				case TransferManager.TransferReason.SickMove:
				case TransferManager.TransferReason.Dead:
				case TransferManager.TransferReason.DeadMove:
				case TransferManager.TransferReason.Garbage:
				case TransferManager.TransferReason.GarbageMove:
				case TransferManager.TransferReason.GarbageTransfer:
				case TransferManager.TransferReason.Mail:
				case TransferManager.TransferReason.UnsortedMail:
				case TransferManager.TransferReason.SortedMail:
				case TransferManager.TransferReason.IncomingMail:
				case TransferManager.TransferReason.OutgoingMail:
				case TransferManager.TransferReason.ParkMaintenance:
				case TransferManager.TransferReason.RoadMaintenance:
				case TransferManager.TransferReason.Snow:
				case TransferManager.TransferReason.SnowMove:
				case TransferManager.TransferReason.FloodWater:
				case (TransferManager.TransferReason)125:
				case (TransferManager.TransferReason)126:
					return true;

				default:
					// If not explicitly supported, it isn't.
					return false;
            }
        }
	}
}