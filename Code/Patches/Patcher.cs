using System.Reflection;
using HarmonyLib;
using CitiesHarmony.API;


namespace TransferController
{
    /// <summary>
    /// Class to manage the mod's Harmony patches.
    /// </summary>
    public static class Patcher
    {
        // Unique harmony identifier.
        private const string harmonyID = "com.github.algernon-A.csl.tc";

        // Flag.
        internal static bool Patched => _patched;
        private static bool _patched = false;
        private static bool _useNewAlgorithm = true;


        /// <summary>
        /// Whether or not new or legacy algorithm is in effect.
        /// </summary>
        internal static bool UseNewAlgorithm
        {
            get => _useNewAlgorithm;

            set
            {
                PatchNewAlgorithm(value);
                _useNewAlgorithm = value;
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
                    Harmony harmonyInstance = new Harmony(harmonyID);
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
                Harmony harmonyInstance = new Harmony(harmonyID);
                harmonyInstance.UnpatchAll(harmonyID);
                _patched = false;
            }
        }


        /// <summary>
        /// Applies Harmomny patches for the matching algorithm selection.
        /// </summary>
        /// <param name="usingNew">True to apply new algorithm, false to use the legacy algorithm.</param>
        private static void PatchNewAlgorithm(bool usingNew)
        {
            // Ensure Harmony is ready before patching.
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                TransferManagerPatches.Patch(new Harmony(harmonyID), usingNew);
            }
            else
            {
                Logging.Error("Harmony not ready");
            }
        }
    }
}