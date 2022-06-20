using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
		// Distance matching only (don't factor priority into distance).
		internal static bool distanceOnly = true;

		// Matching distance multiplier.
		internal static int distancePercentage = 100;

		// External connection priorities.
		internal static int outsideRailPriority = 0;
		internal static int outsideShipPriority = 0;


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
			harmonyInstance.Patch(targetMethod, transpiler: new HarmonyMethod(typeof(TransferManagerPatches), nameof(TransferManagerPatches.MatchOffersTranspiler)));
			Logging.Message("MatchOffers patched");
		}


		/// <summary>
		/// Harmony transpiler for TransferManager.MatchOffers, inserting a call to our custom replacement method if the TransferReason is supported, otherwise falling through to base-game code.
		/// </summary>
		/// <param name="instructions">Original ILCode</param>
		/// <param name="instructions">Harmony ILGenerator injection</param>
		public static IEnumerable<CodeInstruction> MatchOffersTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Instruction enumerator.
			IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

			// Define label for our conditional jump.
			Label jumpLabel = generator.DefineLabel();

			// Insert conditional check - if this isn't a supported reason, we jump ahead to the original code.
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TransferManagerPatches), nameof(TransferManagerPatches.SupportedTransfer)));
			yield return new CodeInstruction(OpCodes.Brfalse_S, jumpLabel);

			// Insert call to custom method.
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldarg_1);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, m_incomingCountField);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, m_outgoingCountField);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, m_incomingOffersField);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, m_outgoingOffersField);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, m_incomingAmountField);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, m_outgoingAmountField);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TransferManagerPatches), nameof(TransferManagerPatches.MatchOffers)));

			// Return from method here (after this is original code, which we obviously don't want to execute).
			yield return new CodeInstruction(OpCodes.Ret);

			// Add label to following instruction.
			if (instructionsEnumerator.MoveNext())
			{
				CodeInstruction instruction = instructionsEnumerator.Current;
				instruction.labels.Add(jumpLabel);
				yield return instruction;
			}
			else
			{
				Logging.Error("empty instruction enumerator when transpiling MatchOffers");
				yield break;
			}

			// Copy over remainder of original code.
			while (instructionsEnumerator.MoveNext())
			{
				// Get next instruction.
				yield return instructionsEnumerator.Current;
			}
		}


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
													otherPriorityPlus += outsideRailPriority;
													distanceModifier /= (1 + Mathf.Pow(outsideRailPriority, 2));
												}
												else if (candidateInfo.m_class.m_subService == ItemClass.SubService.PublicTransportShip)
												{
													otherPriorityPlus += outsideShipPriority;
													distanceModifier /= (1 + Mathf.Pow(outsideShipPriority, 2));
												}
											}
										}
										else if (incomingRailBoosted)
										{
											otherPriorityPlus += outsideRailPriority;
											distanceModifier /= (1 + Mathf.Pow(outsideRailPriority, 2));
										}
										else if (incomingShipBoosted)
										{
											otherPriorityPlus += outsideShipPriority;
											distanceModifier /= (1 + Mathf.Pow(outsideShipPriority, 2));
										}

										// Position of incoming building (source building or vehicle source building)
										Vector3 outCandidatePosition = outCandidateBuilding == 0 ? outgoingOfferCandidate.Position : buildingBuffer[outCandidateBuilding].m_position;

										if (!ChecksPassed(true, (byte)thisPriority, (byte)otherPriority, incomingBuilding, outCandidateBuilding, incomingDistrict, districtManager.GetDistrict(outCandidatePosition), incomingPark, districtManager.GetPark(outCandidatePosition), material))
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
												otherPriorityPlus += outsideRailPriority;
												distanceModifier /= (1 + Mathf.Pow(outsideRailPriority, 2));
											}
											else if (candidateInfo.m_class.m_subService == ItemClass.SubService.PublicTransportShip)
											{
												otherPriorityPlus += outsideShipPriority;
												distanceModifier /= (1 + Mathf.Pow(outsideShipPriority, 2));
											}
										}
									}
									else if (outgoingRailBoosted)
									{
										otherPriorityPlus += outsideRailPriority;
										distanceModifier /= (1 + Mathf.Pow(outsideRailPriority, 2));
									}
									else if (outgoingShipBoosted)
									{
										otherPriorityPlus += outsideShipPriority;
										distanceModifier /= (1 + Mathf.Pow(outsideShipPriority, 2));
									}

									// Position of incoming building (source building or vehicle source building)
									Vector3 inCandidatePosition = inCandidateBuilding == 0 ? incomingOfferCandidate.Position : buildingBuffer[inCandidateBuilding].m_position;

									if (!ChecksPassed(false, (byte)otherPriority, (byte)thisPriority, inCandidateBuilding, outgoingBuilding, districtManager.GetDistrict(inCandidatePosition), outgoingDistrict, districtManager.GetPark(inCandidatePosition), outgoingPark, material))
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


		/// <summary>
		/// Harmony reverse patch to access private method TransferManager.StartTransfer.
		/// </summary>
		/// <param name="instance">TransferManager instance</param>
		/// <param name="material">Transfer material</param>
		[HarmonyReversePatch]
		[HarmonyPatch((typeof(TransferManager)), "StartTransfer")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void StartTransfer(object instance, TransferManager.TransferReason material, TransferManager.TransferOffer offerOut, TransferManager.TransferOffer offerIn, int delta)
		{
			Logging.Error("StartTransfer reverse Harmony patch wasn't applied, params: ", instance, material, offerOut, offerIn, delta);
			throw new NotImplementedException("Harmony reverse patch not applied");
		}


		/// <summary>
		/// Checks against district and building filters, both incoming and outgoing.
		/// </summary>
		/// <param name="incoming">True if this is an incoming offer, false otherwise</param
		/// <param name="priorityIn">Incoming offer priority</param
		/// <param name="priorityOut">Outgoing offer priority</param
		/// <param name="incomingBuildingID">Building ID to check</param
		/// <param name="outgoingBuildingID">Building ID to check</param>
		/// <param name="incomingDistrict">District of incoming offer</param>
		/// <param name="outgoingDistrict">District of outgoing offer</param>
		/// <param name="incomingPark">Park area of incoming offer</param>
		/// <param name="outgoingPark">Park area of outgoing offer</param>
		/// <param name="reason">Transfer reason</param>
		/// <returns>True if the transfer is permitted, false if prohibited</returns>
		private static bool ChecksPassed(bool incoming, byte priorityIn, byte priorityOut, ushort incomingBuildingID, ushort outgoingBuildingID, byte incomingDistrict, byte outgoingDistrict, byte incomingPark, byte outgoingPark, TransferManager.TransferReason reason)
		{
			// First, check for incoming restrictions.
			if (IncomingChecksPassed(incomingBuildingID, outgoingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingPark, reason))
			{
				// Then, outgoing.
				bool result = OutgoingChecksPassed(outgoingBuildingID, incomingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingPark, reason);
				TransferLogging.AddEntry(reason, incoming, priorityIn, priorityOut, incomingBuildingID, outgoingBuildingID, result);
				return result;
			}

			// Failed incoming district restrictions - return false.
			TransferLogging.AddEntry(reason, incoming, priorityIn, priorityOut, incomingBuildingID, outgoingBuildingID, false);
			return false;
		}


		/// <summary>
		///  Applies incoming district and building filters.
		/// </summary>
		/// <param name="buildingID">Building ID to check</param>
		/// <param name="priority">Offer priority</param
		/// <param name="outgoingBuildingID">Building ID of outgoing building</param>
		/// <param name="incomingDistrict">District of incoming offer</param>
		/// <param name="outgoingDistrict">District of outgoing offer</param>
		/// <param name="incomingPark">Park area of incoming offer</param>
		/// <param name="outgoingPark">Park area of outgoing offer</param>
		/// <param name="transferReason">Transfer reason</param>
		/// <returns>True if the transfer is permitted, false if prohibited</returns>
		private static bool IncomingChecksPassed(ushort buildingID, ushort outgoingBuildingID, byte incomingDistrict, byte outgoingDistrict, byte incomingPark, byte outgoingPark, TransferManager.TransferReason transferReason)
		{
			// Calculate building record ID.
			uint mask = BuildingControl.IncomingMask << 24;
			uint buildingRecordID = (uint)(buildingID + mask);


			// Get building record.
			if (BuildingControl.buildingRecords.TryGetValue(buildingRecordID, out BuildingControl.BuildingRecord buildingRecord))
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
					if (!BuildingControl.buildingRecords.TryGetValue(buildingID | (uint)(buildingRecord.nextRecord << 24), out buildingRecord))
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
				if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[outgoingBuildingID].Info.m_buildingAI is OutsideConnectionAI)
				{
					if ((buildingRecord.flags & BuildingControl.RestrictionFlags.BlockOutsideConnection) == 0)
					{
						return true;
					}
				}
				else if ((buildingRecord.flags & (BuildingControl.RestrictionFlags.DistrictEnabled | BuildingControl.RestrictionFlags.BuildingEnabled)) == 0)
				{
					// If not an outside connection, transfer is permitted if no restrictions are enabled.
					return true;
				}


				// Check district settings.
				if ((buildingRecord.flags & BuildingControl.RestrictionFlags.DistrictEnabled) != 0)
				{
					// Check same-district setting.
					if ((buildingRecord.flags & BuildingControl.RestrictionFlags.BlockSameDistrict) == 0 && (outgoingDistrict != 0 && incomingDistrict == outgoingDistrict || (outgoingPark != 0 && incomingPark == outgoingPark)))
					{
						// Same district match - permitted.
						return true;
					}

					// Check permitted districts.
					if (buildingRecord.districts != null)
					{
						if (buildingRecord.districts.Contains(outgoingDistrict) || buildingRecord.districts.Contains(-outgoingPark))
						{
							// Permitted district.
							return true;
						}
					}
				}

				// Check building settings.
				if ((buildingRecord.flags & BuildingControl.RestrictionFlags.BuildingEnabled) != 0)
				{
					// Check permitted buildings.
					if (buildingRecord.buildings != null)
					{
						if (buildingRecord.buildings.Contains(outgoingBuildingID))
						{
							// Permitted building.
							return true;
						}
					}
				}
			}
			else
			{
				// No record means no restrictions.
				return true;
			}

			// If we got here, we found a record but no permitted match was found; return false.
			return false;
		}


		/// <summary>
		///  Applies outgoing district and building filters.
		/// </summary>
		/// <param name="buildingID">Building ID to check</param>
		/// <param name="incomingBuildingID">Building ID of incoming building</param>
		/// <param name="incomingDistrict">District of incoming offer</param>
		/// <param name="incomingPark">Park area of incoming offer</param>
		/// <param name="outgoingPark">Park area of outgoing offer</param>
		/// <param name="outgoingDistrict">District of outgoing offer</param>
		/// <param name="transferReason">Transfer reason</param>
		/// <returns>True if the transfer is permitted, false if prohibited</returns>
		private static bool OutgoingChecksPassed(ushort buildingID, ushort incomingBuildingID, byte incomingDistrict, byte outgoingDistrict, byte incomingPark, byte outgoingPark, TransferManager.TransferReason transferReason)
		{
			// Calculate building record ID.
			uint mask = (uint)BuildingControl.OutgoingMask << 24;
			uint buildingRecordID = (uint)(buildingID + mask);

			// Get building record.
			if (BuildingControl.buildingRecords.TryGetValue(buildingRecordID, out BuildingControl.BuildingRecord buildingRecord))
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
					if (!BuildingControl.buildingRecords.TryGetValue(buildingID | (uint)(buildingRecord.nextRecord << 24), out buildingRecord))
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

				// Only block specified goods transfers where the 'None' wildcard is applied.
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

				// Check outside connection.
				if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[incomingBuildingID].Info.m_buildingAI is OutsideConnectionAI)
				{
					if ((buildingRecord.flags & BuildingControl.RestrictionFlags.BlockOutsideConnection) == 0)
					{
						return true;
					}
				}
				else if ((buildingRecord.flags & (BuildingControl.RestrictionFlags.DistrictEnabled | BuildingControl.RestrictionFlags.BuildingEnabled)) == 0)
				{
					// If not an outside connection, transfer is permitted if no restrictions are enabled.
					return true;
				}

				// Check district settings.
				if ((buildingRecord.flags & BuildingControl.RestrictionFlags.DistrictEnabled) != 0)
				{
					// Check same-district setting.
					if ((buildingRecord.flags & BuildingControl.RestrictionFlags.BlockSameDistrict) == BuildingControl.RestrictionFlags.None && (incomingDistrict != 0 && incomingDistrict == outgoingDistrict || (incomingPark != 0 && incomingPark == outgoingPark)))
					{
						// Same district match - permitted.
						return true;
					}

					// Check permitted districts.
					if ((buildingRecord.reason == TransferManager.TransferReason.None || buildingRecord.reason == transferReason) && buildingRecord.districts != null)
					{
						if (buildingRecord.districts.Contains(incomingDistrict) || buildingRecord.districts.Contains(-incomingPark))
						{
							// Permitted district.
							return true;
						}
					}
				}

				// Check building settings.
				if ((buildingRecord.flags & BuildingControl.RestrictionFlags.BuildingEnabled) != 0)
				{
					// Check permitted buildings.
					if (buildingRecord.buildings != null)
					{
						if (buildingRecord.buildings.Contains(incomingBuildingID))
						{
							// Permitted building.
							return true;
						}
					}
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