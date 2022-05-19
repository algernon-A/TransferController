using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;


namespace TransferController
{
    /// <summary>
    /// Harmony Postfix patch to show/hide building panel button when building selection changes.
    /// </summary>
    [HarmonyPatch]
    public static class BuildingPanelPatch
    {
        /// <summary>
        /// Determines list of target methods to patch - in this case, BuildingWorldInfoPanel method OnSetTarget
        /// </summary>
        /// <returns>List of target methods to patch</returns>
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(ZonedBuildingWorldInfoPanel), "OnSetTarget");
            yield return AccessTools.Method(typeof(CityServiceWorldInfoPanel), "OnSetTarget");
            yield return AccessTools.Method(typeof(WarehouseWorldInfoPanel), "OnSetTarget");
            yield return AccessTools.Method(typeof(UniqueFactoryWorldInfoPanel), "OnSetTarget");
        }

        /// <summary>
        /// Harmony Postfix patch to update building info panel button visibility when building selection changes.
        /// </summary>
        public static void Postfix()
        {
            BuildingPanelManager.TargetChanged();
        }
    }
}