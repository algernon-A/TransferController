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
        private static bool s_useNewAlgorithm = true;
        private static bool s_addOffersPatched = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Patcher"/> class.
        /// </summary>
        /// <param name="harmonyID">This mod's unique Harmony identifier.</param>
        public Patcher(string harmonyID)
            : base(harmonyID)
        {
        }

        /// <summary>
        /// Gets the active instance reference.
        /// </summary>
        public static new Patcher Instance
        {
            get
            {
                // Auto-initializing getter.
                if (s_instance == null)
                {
                    s_instance = new Patcher(PatcherMod.Instance.HarmonyID);
                }

                return s_instance as Patcher;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether new (true) or legacy (false) algorithm is in effect.
        /// </summary>
        internal static bool UseNewAlgorithm
        {
            get => s_useNewAlgorithm;

            set
            {
                // Don't do anything if already in effect.
                if (value != s_useNewAlgorithm)
                {
                    // Update value.
                    s_useNewAlgorithm = value;

                    // Apply patches.
                    Instance.PatchNewAlgorithm(value);
                }
            }
        }

        /// <summary>
        /// Apply all Harmony patches.
        /// </summary>
        public override void PatchAll()
        {
            // Don't do anything if already patched.
            if (!Patched)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage("deploying Harmony patches");

                    // Apply all annotated patches and update flag.
                    Harmony harmonyInstance = new Harmony(HarmonyID);
                    harmonyInstance.PatchAll();
                    Patched = true;

                    // Attempt to pach TransferManager.
                    TransferManagerPatches.Patch(harmonyInstance, UseNewAlgorithm);
                }
                else
                {
                    Logging.Error("Harmony not ready");
                }
            }
        }

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
                    if (s_addOffersPatched)
                    {
                        Logging.Message("unapplying MatchOffers prefixes");

                        harmonyInstance.Unpatch(matchIncomingTarget, matchIncomingPatch);
                        harmonyInstance.Unpatch(matchOutgoingTarget, matchOutgoingPatch);
                        s_addOffersPatched = false;
                    }
                }
                else
                {
                    // Applying legacy algorithm: patch legacy patches if we haven't aready.
                    if (!s_addOffersPatched)
                    {
                        Logging.Message("applying MatchOffers prefixes");

                        harmonyInstance.Patch(matchIncomingTarget, prefix: new HarmonyMethod(matchIncomingPatch));
                        harmonyInstance.Patch(matchOutgoingTarget, prefix: new HarmonyMethod(matchOutgoingPatch));
                        s_addOffersPatched = true;
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