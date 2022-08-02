// <copyright file="BuildingWorldInfoPanelPatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using System.Reflection;
    using HarmonyLib;

    /// <summary>
    /// Harmony Postfix patch to show/hide building panel button when building selection changes.
    /// </summary>
    [HarmonyPatch]
    public static class BuildingWorldInfoPanelPatch
    {
        /// <summary>
        /// Determines list of target methods to patch - in this case, BuildingWorldInfoPanel method OnSetTarget.
        /// </summary>
        /// <returns>List of target methods to patch.</returns>
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