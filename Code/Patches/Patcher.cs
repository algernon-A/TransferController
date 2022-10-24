// <copyright file="Patcher.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Reflection;
    using AlgernonCommons.Patching;
    using HarmonyLib;

    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public class Patcher : PatcherBase
    {
        /// <summary>
        /// Applies vehicle selection patches.
        /// </summary>
        internal void PatchVehicles()
        {
            Harmony harmony = new Harmony(HarmonyID);

            // Patch general buildings.
            MethodInfo startTransferTranspiler = AccessTools.Method(typeof(StartTransferPatches), nameof(StartTransferPatches.Transpiler));
            foreach (MethodBase targetMethod in StartTransferPatches.TargetMethods())
            {
                harmony.Patch(targetMethod, transpiler: new HarmonyMethod(startTransferTranspiler));
            }

            // Patch warehouses.
            MethodInfo warehouseStartTransferTranspiler = AccessTools.Method(typeof(WarehouseStartTransferPatch), nameof(WarehouseStartTransferPatch.Transpiler));
            foreach (MethodBase targetMethod in WarehouseStartTransferPatch.TargetMethods())
            {
                harmony.Patch(targetMethod, transpiler: new HarmonyMethod(warehouseStartTransferTranspiler));
            }
        }
    }
}