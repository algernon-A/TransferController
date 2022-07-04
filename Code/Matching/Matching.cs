using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using ColossalFramework;
using HarmonyLib;


namespace TransferController
{
	/// <summary>
	/// Custom offer matching - new version.
	/// </summary>
	[HarmonyPatch]
	public static class Matching
	{
		// Warehouse priority value.
		private static int warehousePriority = 0;
		private static float warehouseWeighting = 0f;

		// External connection priorities.
		private static int outsideRailPriority = 0;
		private static float outsideRailWeighting = 0f;
		private static int outsideShipPriority = 0;
		private static float outsideShipWeighting = 0f;


		/// <summary>
		/// Warehouse priority.
		/// </summary>
		public static int WarehousePriority
		{
			get => warehousePriority;

			set
			{
				warehousePriority = value;
				warehouseWeighting = 1f / (1f + Mathf.Pow(value, 2));
			}
		}

		/// <summary>
		/// Outside rail connection priority.
		/// </summary>
		public static int OutsideRailPriority
		{
			get => outsideRailPriority;

			set
			{
				outsideRailPriority = value;
				outsideRailWeighting = 1f / (1f + Mathf.Pow(value, 2));
			}
		}


		/// <summary>
		/// Outside ship connection priority.
		/// </summary>
		public static int OutsideShipPriority
		{
			get => outsideShipPriority;

			set
			{
				outsideShipPriority = value;
				outsideShipWeighting = 1f / (1f + Mathf.Pow(value, 2));
			}
		}


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
				case TransferManager.TransferReason.Garbage:
				case TransferManager.TransferReason.Mail:
				case TransferManager.TransferReason.RoadMaintenance:
				case TransferManager.TransferReason.ParkMaintenance:
				case TransferManager.TransferReason.Snow:
					// Match offers incoming-first (from depots outwards).
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

					// Match any remaining outgoing offers.
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


				// Deathcare - most efficent matching is from depot outwards.
				// However, need to prioritise high prioirty requests first.
				// Start by matching highest priority outgoing requests (7-3).
				// Then service lower priorities from depot outwards for greatest efficiency:
				case TransferManager.TransferReason.Dead:
					// Then match outgoing offers first from highest to lowest priority, down to priority 3, to service any urgent issues.
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
		/// <param name="minPriority">Minimum priority to match to</param>
		/// <param name="reason">Transfer reason</param>
		/// <param name="priority">Specified reason's priority</param>
		/// <param name="index">Specified reason's index (within priority block)</param>
		/// <param name="offerBuffer">TransferReason buffer for specified transfer</param>
		/// <param name="offerCounts">Transfer counts array for specified transfer</param>
		/// <param name="candidateBuffer">TransferReason buffer for candidate offers for matching</param>
		/// <param name="candidateCounts">Transfer counts array for candidate offers for matching</param>
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
						if (PathFindFailure.HasFailure(offerBuilding, candidateBuilding))
						{
							continue;
						}

						// Apply warehouse boost if the candidate builing is a warehouse and the offer building isn't a warehouse or outside connection.
						if (candidate.Exclude & !(offer.Exclude | offerIsOutside))
						{
							// Don't apply priority to incoming transfers to emptying warehouses, or outgoing transfers from filling warehouses.
							Building.Flags warehouseFlags = buildingBuffer[candidateBuilding].m_flags;
							if (!((incoming & (warehouseFlags & Building.Flags.Downgrading) != 0) | (!incoming & (warehouseFlags & Building.Flags.Filling) != 0)))
							{
								distanceModifier *= warehouseWeighting;
							}
						}

						// Apply outside connection boost if candidate is an outside connection, unless offer is also an outside connection.
						if (!offerIsOutside && candidateBuildingAI is OutsideConnectionAI)
						{
							switch (candidateBuildingAI.m_info.m_class.m_subService)
							{
								case ItemClass.SubService.PublicTransportTrain:
									distanceModifier *= outsideRailWeighting;
									break;

								case ItemClass.SubService.PublicTransportShip:
									distanceModifier *= outsideShipWeighting;
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

						// Calculate distance between positions - use original offer positions, not owning building positions.
						float squaredDistance = Vector3.SqrMagnitude(offer.Position - candidate.Position) * distanceModifier;
						if (squaredDistance < bestDistance)
						{
							// New nearest disance - apply checks.
							if (incoming)
							{
								if (!ChecksPassed(incoming, (byte)priority, (byte)candidatePriority, offerBuilding, candidateBuilding, offerDistrict, candidateDistrict, offerPark, candidatePark, reason, offer.Exclude, candidate.Exclude, offer.Position, candidate.Position))
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
								if (!ChecksPassed(incoming, (byte)candidatePriority, (byte)priority, candidateBuilding, offerBuilding, candidateDistrict, offerDistrict, candidatePark, offerPark, reason, candidate.Exclude, offer.Exclude, candidate.Position, offer.Position))
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
					StartTransfer(Singleton<TransferManager>.instance, reason, matchedOffer, offer, transferAmount);
				}
				else
				{
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
		internal static bool ChecksPassed(bool incoming, byte priorityIn, byte priorityOut, ushort incomingBuildingID, ushort outgoingBuildingID, byte incomingDistrict, byte outgoingDistrict, byte incomingPark, byte outgoingPark, TransferManager.TransferReason reason, bool incomingExcluded, bool outgoingExcluded, Vector3 incomingPos, Vector3 outgoingPos)
		{
			// First, check for incoming restrictions.
			if (IncomingChecksPassed(incomingBuildingID, outgoingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingPark, reason))
			{
				// Then, outgoing.
				bool result = OutgoingChecksPassed(outgoingBuildingID, incomingBuildingID, incomingDistrict, outgoingDistrict, incomingPark, outgoingPark, reason);
				TransferLogging.AddEntry(reason, incoming, priorityIn, priorityOut, incomingBuildingID, outgoingBuildingID, result, incomingExcluded, outgoingExcluded, incomingPos, outgoingPos);
				return result;
			}

			// Failed incoming district restrictions - return false.
			TransferLogging.AddEntry(reason, incoming, priorityIn, priorityOut, incomingBuildingID, outgoingBuildingID, false, incomingExcluded, outgoingExcluded, incomingPos, outgoingPos);
			return false;
		}


		/// <summary>
		///  Applies incoming district and building filters.
		/// </summary>
		/// <param name="buildingID">Building ID to check</param>
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

				// Where the 'None' wildcard is applied, only block outgoing cargo transfers.
				if (buildingRecord.reason == TransferManager.TransferReason.None && !IsCargoReason(transferReason))
				{
					// Not a recognised cargo transfer; automatically permit the transfer.
					return true;
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
		/// /// Checks if the given building has an active "prefer same district" setting for the specified TransferReason and returns the appropriate multiplier.
		/// </summary>
		/// <param name="buildingID">Building ID to check</param>
		/// <param name="incoming">True if this is an incoming offer, false otherwise</param
		/// <param name="reason">Transfer reason</param>
		/// <returns>0.1f if the given building has an active prefer same district setting, 1f otherwise</returns>
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
			uint mask = (uint)(incoming ? BuildingControl.IncomingMask : BuildingControl.OutgoingMask) << 24;
			uint buildingRecordID = (uint)(buildingID + mask);

			// Get building record.
			if (BuildingControl.buildingRecords.TryGetValue(buildingRecordID, out BuildingControl.BuildingRecord buildingRecord))
			{
				// Record found - check for reason match.
				if (reason == TransferManager.TransferReason.None | reason == buildingRecord.reason)
				{
					// Matching reason - check flag.
					if ((buildingRecord.flags & BuildingControl.RestrictionFlags.PreferSameDistrict) != 0)
					{
						// Found matching flag; return 0.1.
						return 0.1f;
					}
				}
				// No matching reason - check next record if available.
				else if (buildingRecord.nextRecord != 0)
				{
					if (BuildingControl.buildingRecords.TryGetValue(buildingID | (uint)(buildingRecord.nextRecord << 24), out buildingRecord))
					{
						if (reason == TransferManager.TransferReason.None | reason == buildingRecord.reason & ((buildingRecord.flags & BuildingControl.RestrictionFlags.PreferSameDistrict) != 0))
						{
							// Found matching flag in the secondary record; return 0.1.
							return 0.1f;
						}
					}
				}
			}

			// If we got here, no record was found; return 1.
			return 1f;
		}



		/// <summary>
		/// Checks if the given TransferReason is a valid cargo transfer type.
		/// </summary>
		/// <param name="transferReason">TransferReason to check</param>
		/// <returns>True if the given reason is a cargo type, false otherwise</returns>
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