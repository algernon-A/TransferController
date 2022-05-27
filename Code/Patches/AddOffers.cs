using System.Collections.Generic;
using System.Reflection;
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
		public static void AddIncomingOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
		{
			// Check for valid building.
			if (offer.Building != 0)
			{
				// Local reference.
				ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];

				// Check for warehouse.
				if (building.Info.m_buildingAI is WarehouseAI warehouseAI)
				{
					// This is a warehouse - ignore if warehouse is emptying.
					if ((building.m_flags & Building.Flags.Downgrading) == 0)
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


		/// <summary>
		/// Harmony Prefix to manipulate priorities of incoming or outgoing offers.
		/// </summary>
		/// <param name="material">Transfer material</param>
		/// <param name="offer">Incoming offer</param>
		[HarmonyPatch(nameof(TransferManager.AddOutgoingOffer))]
		[HarmonyPrefix]
		public static void AddOutgoingOffer(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
		{
			// Check for valid building.
			if (offer.Building != 0)
			{
				// Local reference.
				ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[offer.Building];

				// Check for warehouse.
				if (building.Info.m_buildingAI is WarehouseAI warehouseAI)
				{
					// This is a warehouse - ignore if warehouse is filling.
					if ((building.m_flags & Building.Flags.Filling) == 0)
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