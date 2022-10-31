// <copyright file="DepotStartTransfer.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using ColossalFramework;
    using HarmonyLib;

    /// <summary>
    /// Harmony transpiler to DepotAI.StartTransfer to implement vehicle selection.
    /// </summary>
    public static class DepotStartTransfer
    {
        /// <summary>
        /// Harmony transpiler for o DepotAI.GetPrimaryRandomVehicleInfo, replacing existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="original">Method being transpiled.</param>
        /// <returns>New ILCode.</returns>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Logging.Message("transpiling ", original.DeclaringType, ":", original.Name);

            // Reflection to get original and inserted methods for calls.
            MethodInfo getPrimaryRandomVehicleInfo = typeof(DepotAI).GetMethod("GetPrimaryRandomVehicleInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo chooseVehicle = typeof(DepotStartTransfer).GetMethod(nameof(DepotStartTransfer.ChooseVehicle));

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetPrimaryRandomVehicleInfo method, then replace it with a call to our custom method.
                if (instruction.opcode == OpCodes.Call)
                {
                    // Standard version.
                    if (instruction.Calls(getPrimaryRandomVehicleInfo))
                    {
                        // Add buildingID and material params to call.
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_3);
                        instruction = new CodeInstruction(OpCodes.Call, chooseVehicle);
                        Logging.Message("transpiled");
                    }
                }

                // Output this instruction.
                yield return instruction;
            }
        }

        /// <summary>
        /// Chooses a vehicle for a transfer from our custom lists (reverting to game code if no custom list exists for this building and transfer).
        /// </summary>
        /// <param name="depotAI">DepotAI instance.</param>
        /// <param name="buildingID">Building ID of owning building.</param>
        /// <param name="material">Transfer material.</param>
        /// <returns>Vehicle prefab to spawn.</returns>
        public static VehicleInfo ChooseVehicle(DepotAI depotAI, ushort buildingID, TransferManager.TransferReason material)
        {
            ItemClass depotItemClass = depotAI.m_overrideVehicleClass ?? depotAI.m_transportInfo.m_class;

            // Get any custom vehicle list for this build
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, material);
            if (vehicleList == null)
            {
                // No custom vehicle selection - mimic game behaviour.
                return Singleton<VehicleManager>.instance.GetRandomVehicleInfo(ref Singleton<SimulationManager>.instance.m_randomizer, depotItemClass.m_service, depotItemClass.m_subService, depotItemClass.m_level);
            }

            // Custom vehicle selection found - randomly choose one.
            int i = Singleton<SimulationManager>.instance.m_randomizer.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }
    }
}