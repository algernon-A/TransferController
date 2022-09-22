// <copyright file="TransferManagerPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to implement custom TransferManager functions.
    /// </summary>
    [HarmonyPatch(typeof(TransferManager))]
    public static class TransferManagerPatches
    {
        /// <summary>
        /// Harmony transpiler for TransferManager.MatchOffers, inserting a call to our custom replacement method if the TransferReason is supported, otherwise falling through to base-game code.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="generator">Harmony ILGenerator injection.</param>
        /// <returns>New ILCode.</returns>
        [HarmonyPatch("MatchOffers")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> MatchOffersTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Define label for our conditional jump.
            Label jumpLabel = generator.DefineLabel();

            // Insert conditional check - if this isn't a supported reason, we jump ahead to the original code.
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TransferManagerPatches), nameof(SupportedTransfer)));
            yield return new CodeInstruction(OpCodes.Brfalse_S, jumpLabel);

            // Insert call to custom method.
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TransferManager), "m_incomingCount"));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TransferManager), "m_outgoingCount"));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TransferManager), "m_incomingOffers"));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TransferManager), "m_outgoingOffers"));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TransferManager), "m_incomingAmount"));
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(TransferManager), "m_outgoingAmount"));
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Matching), nameof(Matching.MatchOffers)));

            // Return from method here (after this is original code, which we obviously don't want to execute).
            yield return new CodeInstruction(OpCodes.Ret);

            // Add label to following instruction.
            if (instructionsEnumerator.MoveNext())
            {
                CodeInstruction instruction = instructionsEnumerator.Current;
                instruction.labels.Add(jumpLabel);
                yield return instruction;
            }
            else
            {
                Logging.Error("empty instruction enumerator when transpiling MatchOffers");
                yield break;
            }

            // Copy over remainder of original code.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction.
                yield return instructionsEnumerator.Current;
            }
        }

        /// <summary>
        /// Determines whether or not this transfer reason is supported.
        /// </summary>
        /// <param name="reason">Transfer reason to check.</param>
        /// <returns>True if this is a supported reason, false otherwise.</returns>
        public static bool SupportedTransfer(TransferManager.TransferReason reason)
        {
            switch (reason)
            {
                // Supported reasons.
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
                case TransferManager.TransferReason.Crime:
                case TransferManager.TransferReason.CriminalMove:
                case TransferManager.TransferReason.Fire:
                case TransferManager.TransferReason.Fire2:
                case TransferManager.TransferReason.ForestFire:
                case TransferManager.TransferReason.Student1:
                case TransferManager.TransferReason.Student2:
                case TransferManager.TransferReason.Student3:
                case TransferManager.TransferReason.Sick:
                case TransferManager.TransferReason.Sick2:
                case TransferManager.TransferReason.SickMove:
                case TransferManager.TransferReason.Dead:
                case TransferManager.TransferReason.DeadMove:
                case TransferManager.TransferReason.Garbage:
                case TransferManager.TransferReason.GarbageMove:
                case TransferManager.TransferReason.GarbageTransfer:
                case TransferManager.TransferReason.Mail:
                case TransferManager.TransferReason.UnsortedMail:
                case TransferManager.TransferReason.SortedMail:
                case TransferManager.TransferReason.IncomingMail:
                case TransferManager.TransferReason.OutgoingMail:
                case TransferManager.TransferReason.ParkMaintenance:
                case TransferManager.TransferReason.RoadMaintenance:
                case TransferManager.TransferReason.Snow:
                case TransferManager.TransferReason.SnowMove:
                case TransferManager.TransferReason.FloodWater:
                case TransferManager.TransferReason.Taxi:
                case (TransferManager.TransferReason)125: // real gas station mod
                case (TransferManager.TransferReason)126: // real gas station mod
                    return true;

                default:
                    // If not explicitly supported, it isn't.
                    return false;
            }
        }
    }
}