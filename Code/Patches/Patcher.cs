// <copyright file="Patcher.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Reflection;
    using AlgernonCommons;
    using AlgernonCommons.Patching;
    using CitiesHarmony.API;
    using HarmonyLib;

    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public class Patcher : PatcherBase
    {
        // Flags.
        private bool _useNewAlgorithm = true;
        private bool _addOffersPatched = false;

        /// <summary>
        /// Gets or sets a value indicating whether new (true) or legacy (false) algorithm is in effect.
        /// </summary>
        internal bool UseNewAlgorithm
        {
            get => _useNewAlgorithm;

            set
            {
                // Don't do anything if already in effect.
                if (value != _useNewAlgorithm)
                {
                    // Update value.
                    _useNewAlgorithm = value;

                    // Apply patches.
                    PatchNewAlgorithm(value);
                }
            }
        }

        /// <summary>
        /// Peforms any additional actions (such as custom patching) after PatchAll is called.
        /// </summary>
        /// <param name="harmonyInstance">Haromny instance for patching.</param>
        protected override void OnPatchAll(Harmony harmonyInstance) => TransferManagerPatches.Patch(harmonyInstance, UseNewAlgorithm);

        /// <summary>
        /// Applies Harmomny patches for the matching algorithm selection.
        /// </summary>
        /// <param name="usingNew">True to apply new algorithm, false to use the legacy algorithm.</param>
        private void PatchNewAlgorithm(bool usingNew) => PatchNewAlgorithm(new Harmony(HarmonyID), usingNew);

        /// <summary>
        /// Applies Harmomny patches for the matching algorithm selection.
        /// </summary>
        /// <param name="harmonyInstance">Harmony instance.</param>
        /// <param name="usingNew">True to apply new algorithm, false to use the legacy algorithm.</param>
        private void PatchNewAlgorithm(Harmony harmonyInstance, bool usingNew)
        {
            // Ensure Harmony is ready before patching.
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                // Patch MatchOffers.
                TransferManagerPatches.Patch(harmonyInstance, usingNew);

                // Patch/unpatch AddOffers patches.
                MethodInfo matchIncomingTarget = typeof(TransferManager).GetMethod(nameof(TransferManager.AddIncomingOffer));
                MethodInfo matchOutgoingTarget = typeof(TransferManager).GetMethod(nameof(TransferManager.AddOutgoingOffer));
                MethodInfo matchIncomingPatch = typeof(AddOffers).GetMethod(nameof(AddOffers.AddIncomingOffer));
                MethodInfo matchOutgoingPatch = typeof(AddOffers).GetMethod(nameof(AddOffers.AddOutgoingOffer));

                // Patch/unpatch MatchOffers.
                if (usingNew)
                {
                    // Applying new algorithm: unpatch legacy patches if they're appied.
                    if (_addOffersPatched)
                    {
                        Logging.Message("unapplying MatchOffers prefixes");

                        harmonyInstance.Unpatch(matchIncomingTarget, matchIncomingPatch);
                        harmonyInstance.Unpatch(matchOutgoingTarget, matchOutgoingPatch);
                        _addOffersPatched = false;
                    }
                }
                else
                {
                    // Applying legacy algorithm: patch legacy patches if we haven't aready.
                    if (!_addOffersPatched)
                    {
                        Logging.Message("applying MatchOffers prefixes");

                        harmonyInstance.Patch(matchIncomingTarget, prefix: new HarmonyMethod(matchIncomingPatch));
                        harmonyInstance.Patch(matchOutgoingTarget, prefix: new HarmonyMethod(matchOutgoingPatch));
                        _addOffersPatched = true;
                    }
                }
            }
            else
            {
                Logging.Error("Harmony not ready");
            }
        }
    }
}