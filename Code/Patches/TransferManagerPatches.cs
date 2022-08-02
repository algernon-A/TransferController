// <copyright file="TransferManagerPatches.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using AlgernonCommons;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to implement custom TransferManager functions.
    /// </summary>
    [HarmonyPatch]
    public static class TransferManagerPatches
    {
        // Reflection info for private TransferManager fields.
        private static FieldInfo _incomingCountField;
        private static FieldInfo _outgoingCountField;
        private static FieldInfo _incomingOffersField;
        private static FieldInfo _outgoingOffersField;
        private static FieldInfo _incomingAmountField;
        private static FieldInfo _outgoingAmountField;

        // Patch method for transpiler.
        private static MethodInfo _patchMethod;

        /// <summary>
        /// Patch TransferManager.MatchOffers.
        /// </summary>
        /// <param name="harmonyInstance">Harmony instance.</param>
        /// <param name="newAlgorithm">True to patch using the new algoritm, false for legacy.</param>
        public static void Patch(Harmony harmonyInstance, bool newAlgorithm)
        {
            // Reflect private fields.
            _incomingCountField = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.Instance | BindingFlags.NonPublic);
            _outgoingCountField = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.Instance | BindingFlags.NonPublic);
            _incomingOffersField = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            _outgoingOffersField = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            _incomingAmountField = typeof(TransferManager).GetField("m_incomingAmount", BindingFlags.Instance | BindingFlags.NonPublic);
            _outgoingAmountField = typeof(TransferManager).GetField("m_outgoingAmount", BindingFlags.Instance | BindingFlags.NonPublic);

            // Check for errors.
            if (_incomingCountField == null)
            {
                Logging.Error("incoming count field null");
                return;
            }

            if (_outgoingCountField == null)
            {
                Logging.Error("outgoing count field null");
                return;
            }

            if (_incomingOffersField == null)
            {
                Logging.Error("incoming offer field null");
                return;
            }

            if (_outgoingOffersField == null)
            {
                Logging.Error("outgoing offer field null");
                return;
            }

            if (_incomingAmountField == null)
            {
                Logging.Error("incoming amount field null");
                return;
            }

            if (_outgoingAmountField == null)
            {
                Logging.Error("outgoing amount field null");
                return;
            }

            // Patch method with new pre-emptive prefix.
            MethodBase targetMethod = typeof(TransferManager).GetMethod("MatchOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            _patchMethod = newAlgorithm ? AccessTools.Method(typeof(Matching), nameof(Matching.MatchOffers)) : AccessTools.Method(typeof(OldMatching), nameof(OldMatching.MatchOffers));
            harmonyInstance.Patch(targetMethod, transpiler: new HarmonyMethod(typeof(TransferManagerPatches), nameof(TransferManagerPatches.MatchOffersTranspiler)));
            Logging.KeyMessage("MatchOffers patched");
        }

        /// <summary>
        /// Harmony transpiler for TransferManager.MatchOffers, inserting a call to our custom replacement method if the TransferReason is supported, otherwise falling through to base-game code.
        /// </summary>
        /// <param name="instructions">Original ILCode.</param>
        /// <param name="generator">Harmony ILGenerator injection.</param>
        /// <returns>New ILCode.</returns>
        public static IEnumerable<CodeInstruction> MatchOffersTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // Instruction enumerator.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();

            // Define label for our conditional jump.
            Label jumpLabel = generator.DefineLabel();

            // Insert conditional check - if this isn't a supported reason, we jump ahead to the original code.
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TransferManagerPatches), nameof(TransferManagerPatches.SupportedTransfer)));
            yield return new CodeInstruction(OpCodes.Brfalse_S, jumpLabel);

            // Insert call to custom method.
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, _incomingCountField);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, _outgoingCountField);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, _incomingOffersField);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, _outgoingOffersField);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, _incomingAmountField);
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldfld, _outgoingAmountField);
            yield return new CodeInstruction(OpCodes.Call, _patchMethod);

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
                case (TransferManager.TransferReason)125:
                case (TransferManager.TransferReason)126:
                    return true;

                default:
                    // If not explicitly supported, it isn't.
                    return false;
            }
        }
    }
}