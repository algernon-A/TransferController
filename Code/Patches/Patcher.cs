using AlgernonCommons;
using CitiesHarmony.API;
using HarmonyLib;
using System.Reflection;


namespace TransferController
{
    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public static class Patcher
    {
        // Flag.
        internal static bool Patched => _patched;
        private static bool _patched = false, _addOffersPatched = false;
        private static bool _useNewAlgorithm = true;


        /// <summary>
        /// Whether or not new or legacy algorithm is in effect.
        /// </summary>
        internal static bool UseNewAlgorithm
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
                    PatchNewAlgorithm(new Harmony(Mod.Instance.HarmonyID), value);
                }
            }
        }


        /// <summary>
        /// Apply all Harmony patches.
        /// </summary>
        public static void PatchAll()
        {
            // Don't do anything if already patched.
            if (!_patched)
            {
                // Ensure Harmony is ready before patching.
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Logging.KeyMessage("deploying Harmony patches");

                    // Apply all annotated patches and update flag.
                    Harmony harmonyInstance = new Harmony(Mod.Instance.HarmonyID);
                    harmonyInstance.PatchAll();
                    _patched = true;

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
        /// Remove all Harmony patches.
        /// </summary>
        public static void UnpatchAll()
        {
            // Only unapply if patches appplied.
            if (_patched)
            {
                Logging.KeyMessage("reverting Harmony patches");

                // Unapply patches, but only with our HarmonyID.
                Harmony harmonyInstance = new Harmony(Mod.Instance.HarmonyID);
                harmonyInstance.UnpatchAll(Mod.Instance.HarmonyID);
                _patched = false;
            }
        }


        /// <summary>
        /// Applies Harmomny patches for the matching algorithm selection.
        /// </summary>
        /// <param name="harmonyInstance">Harmony instance</param>
        /// <param name="usingNew">True to apply new algorithm, false to use the legacy algorithm</param>
        private static void PatchNewAlgorithm(Harmony harmonyInstance, bool usingNew)
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