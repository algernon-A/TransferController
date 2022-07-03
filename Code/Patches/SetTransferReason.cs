using HarmonyLib;

namespace TransferController
{
    /// <summary>
    /// Harmony patch to clear warehouse building vehicle selections when the warehouse material is changed.
    /// </summary>
    [ HarmonyPatch(typeof(WarehouseAI), nameof(WarehouseAI.SetTransferReason))]
    public static class SetTransferReasonPatch
    {
        /// <summary>
        /// Harmony Prefix patch to WarehouseAI.SetTransferReason to clear building vehicle selection on material change.
        /// </summary>
        /// <param name="__instance">WarehouseAI instance</param>
        /// <param name="buildingID">Warehouse building ID</param>
        /// <param name="data">Warehouse building data reference</param>
        /// <param name="material">New warehouse material</param>
        public static void Prefix(WarehouseAI __instance, ushort buildingID, ref Building data, TransferManager.TransferReason material)
        {
            // Duplicate vanilla storagetype check (excluding fixed-purpose storages).
            if (__instance.m_storageType != TransferManager.TransferReason.None)
            {
                return;
            }

            // Clear building vehicle record if the transfer reason has changed.
            TransferManager.TransferReason seniors = (TransferManager.TransferReason)data.m_seniors;
            if (material != seniors)
            {
                VehicleControl.ReleaseBuilding(buildingID);
            }
        }
    }
}