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
    public static class StartTransferPatches
    {
        /// <summary>
        /// Target methods.
        /// </summary>
        /// <returns>List of methods to transpile</returns>
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(HospitalAI).GetMethod(nameof(HospitalAI.StartTransfer));
            yield return typeof(PoliceStationAI).GetMethod(nameof(PoliceStationAI.StartTransfer));
            yield return typeof(CemeteryAI).GetMethod(nameof(CemeteryAI.StartTransfer));
            yield return typeof(MaintenanceDepotAI).GetMethod(nameof(MaintenanceDepotAI.StartTransfer));
            yield return typeof(PostOfficeAI).GetMethod(nameof(PostOfficeAI.StartTransfer));
            yield return typeof(LandfillSiteAI).GetMethod(nameof(LandfillSiteAI.StartTransfer));
            yield return typeof(FireStationAI).GetMethod(nameof(FireStationAI.StartTransfer));
        }

        /// <summary>
        /// Harmony transpiler for building StartTransfer methods, replacing existing calls to VehicleManager.GetRandomVehicleInfo with a call to our custom replacement instead.
        /// </summary>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="original">Method being transpiled</param>
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            Logging.Message("transpiling ", original.DeclaringType, ":", original.Name);

            // Reflection to get original and inserted methods for calls.
            MethodInfo getRandomVehicle = typeof(VehicleManager).GetMethod(nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level) });
            MethodInfo getRandomVehicleType = typeof(VehicleManager).GetMethod(nameof(VehicleManager.GetRandomVehicleInfo), new Type[] { typeof(Randomizer).MakeByRefType(), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level), typeof(VehicleInfo.VehicleType) });
            MethodInfo chooseVehicle = typeof(StartTransferPatches).GetMethod(nameof(StartTransferPatches.ChooseVehicle));
            MethodInfo chooseVehicleType = typeof(StartTransferPatches).GetMethod(nameof(StartTransferPatches.ChooseVehicleType));

            // Instruction enumerator.
            IEnumerator <CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Iterate through each instruction in original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                CodeInstruction instruction = instructionsEnumerator.Current;

                // If this instruction calls the GetRandomVehicle method, then replace it with a call to our custom method.
                if (instruction.opcode == OpCodes.Callvirt)
                {
                    // Standard version.
                    if (instruction.Calls(getRandomVehicle))
                    {
                        // Add buildingID and material params to call. 
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_3);
                        instruction = new CodeInstruction(OpCodes.Call, chooseVehicle);
                        Logging.Message("transpiled");
                    }
                    // Overload with additional VehicleType argument.
                    else if (instruction.Calls(getRandomVehicleType))
                    {
                        // Add buildingID and material params to call. 
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_3);
                        instruction = new CodeInstruction(OpCodes.Call, chooseVehicleType);
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
        /// <param name="vehicleManager">VehicleManager instance</param>
        /// <param name="r">Randomizer reference</param>
        /// <param name="service">Vehicle service</param>
        /// <param name="subService">Vehicle subservice</param>
        /// <param name="level">Vehicle level</param>
        /// <param name="buildingID">Building ID of owning building</param>
        /// <param name="material">Transfer material</param>
        /// <returns>Vehicle prefab to spawn</returns>
        public static VehicleInfo ChooseVehicle(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, ushort buildingID, TransferManager.TransferReason material)
        {
            // Get any custom vehicle list for this build
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, material);
            if (vehicleList == null)
            {
                // No custom vehicle selection - use game method.
                return vehicleManager.GetRandomVehicleInfo(ref r, service, subService, level);
            }

            // Custom vehicle selection found - randomly choose one.
            int i = r.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }


        /// <summary>
        /// Chooses a vehicle for a transfer from our custom lists (reverting to game code if no custom list exists for this building and transfer).
        /// Special version with additional VehicleType argument.
        /// </summary>
        /// <param name="vehicleManager">VehicleManager instance</param>
        /// <param name="r">Randomizer reference</param>
        /// <param name="service">Vehicle service</param>
        /// <param name="subService">Vehicle subservice</param>
        /// <param name="level">Vehicle level</param>
        /// <param name="buildingID">Building ID of owning building</param>
        /// <param name="material">Transfer material</param>
        /// <returns>Vehicle prefab to spawn</returns>
        public static VehicleInfo ChooseVehicleType(VehicleManager vehicleManager, ref Randomizer r, ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level, VehicleInfo.VehicleType type, ushort buildingID, TransferManager.TransferReason material)
        {
            // Get any custom vehicle list for this build
            List<VehicleInfo> vehicleList = VehicleControl.GetVehicles(buildingID, material);
            if (vehicleList == null)
            {
                // No custom vehicle selection - use game method.
                return vehicleManager.GetRandomVehicleInfo(ref r, service, subService, level, type);
            }

            // Custom vehicle selection found - randomly choose one.
            int i = r.Int32((uint)vehicleList.Count);
            {
                return vehicleList[i];
            }
        }
    }
}