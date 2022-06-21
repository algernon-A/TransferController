using ColossalFramework;
using HarmonyLib;


namespace TransferController
{
	/// <summary>
	/// Harmony patches to manipulate priorities of new offers.
	/// </summary>
	[HarmonyPatch(typeof(TransferManager), nameof(TransferManager.AddIncomingOffer))]
	public static class AddOffers
	{
		// Warehouse priority value.
		internal static int warehousePriority = 0;


		/// <summary>
		/// Harmony Prefix to manipulate priorities of incoming or outgoing offers.
		/// </summary>
		/// <param name="material">Transfer material</param>
		/// <param name="offer">Incoming offer</param>
		[HarmonyPatch(nameof(TransferManager.AddIncomingOffer))]
		[HarmonyPrefix]
		public static void AddIncomingOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer) => PrioritizeOffer(material, ref offer, Building.Flags.Downgrading);




		/// <summary>
		/// Harmony Prefix to manipulate priorities of incoming or outgoing offers.
		/// </summary>
		/// <param name="material">Transfer material</param>
		/// <param name="offer">Incoming offer</param>
		[HarmonyPatch(nameof(TransferManager.AddOutgoingOffer))]
		[HarmonyPrefix]
		public static void AddOutgoingOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer) => PrioritizeOffer(material, ref offer, Building.Flags.Filling);


		/// <summary>
		/// Adjusts new offer priorities according to mod settings.
		/// </summary>
		/// <param name="material">Transfer material</param>
		/// <param name="offer">Offer to prioritize</param>
		/// <param name="warehouseFlags">Building flags to skip warehouse prioritization (e.g. skip prioritization of outgoing offers if warehouse is filling)</param>
		private static void PrioritizeOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer, Building.Flags warehouseFlags)
		{
			// Don't do this if using new matching algorithm.
			if (Matching.distanceOnly)
			{
				return;
			}

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
							offer.Priority += warehousePriority * 2;

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