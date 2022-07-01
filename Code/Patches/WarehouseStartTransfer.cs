using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework.Math;
using HarmonyLib;


namespace TransferController
{
    /// <summary>
    /// Harmony transpiler to various StartTransfer methods to implement vehicle selection.
    /// </summary>
    [HarmonyPatch]
    public static class WarehouseStartTransferPatches
    {
        /// <summary>
        /// Target methods.
        /// </summary>
        /// <returns>List of methods to transpile</returns>
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(WarehouseAI).GetMethod(nameof(WarehouseAI.StartTransfer));
            yield return typeof(ExtractingFacilityAI).GetMethod(nameof(ExtractingFacilityAI.StartTransfer));
            yield return typeof(ProcessingFacilityAI).GetMethod(nameof(ProcessingFacilityAI.StartTransfer));
        }

        /// <summary>
        /// Harmony transpiler for building StartTransfer methods, replacing existign calls to WarehousAI.GetTransferVehicleService with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="original">Method being transpiled</param>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Logging.Message("transpiling ", original.DeclaringType, ":", original.Name);

            // Reflection to get original and inserted methods for calls.
            MethodInfo getTransferVehicle = typeof(WarehouseAI).GetMethod(nameof(WarehouseAI.GetTransferVehicleService), BindingFlags.Public | BindingFlags.Static);
            MethodInfo chooseVehicle = typeof(WarehouseStartTransferPatches).GetMethod(nameof(WarehouseStartTransferPatches.ChooseVehicle));

            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetRandomVehicle method, then replace it with a call to our custom method.
                if (instruction.opcode == OpCodes.Call && instruction.Calls(getTransferVehicle))
                {
                    // Add buildingID and material params to call. 
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    instruction = new CodeInstruction(OpCodes.Call, chooseVehicle);
                    Logging.Message("transpiled");
                }

                // Output this instruction.
                yield return instruction;
            }
        }


        /// <summary>
        /// Chooses a vehicle for a transfer from our custom lists (reverting to game code if no custom list exists for this building and transfer).
        /// </summary>
        /// <param name="material">Transfer material</param>
        /// <param name="level">Vehicle level</param>
        /// <param name="randomizer">Randomizer reference</param>
        /// <param name="buildingID">Building ID of owning building</param>
        /// <returns>Vehicle prefab to spawn</returns>
        public static VehicleInfo ChooseVehicle(TransferManager.TransferReason material, ItemClass.Level level, ref Randomizer randomizer, ushort buildingID)
        {
            // Get any custom vehicle list for this building.
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, TransferManager.TransferReason.None);
            if (vehicleList == null)
            {
                // No custom vehicle selection - use game method.
                return WarehouseAI.GetTransferVehicleService(material, level, ref randomizer);
            }

            // Custom vehicle selection found - randomly choose one.
            int i = randomizer.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }
    }
}