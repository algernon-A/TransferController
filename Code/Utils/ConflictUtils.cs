using AlgernonCommons;
using ColossalFramework.Plugins;
using System.Collections.Generic;
using System.Reflection;


namespace TransferController
{
    /// <summary>
    /// Mod conflict detection.
    /// </summary>
    internal static class ConflictDetection
    {
        // List of conflcting mod names.
        internal static List<string> conflictingModNames;


        /// <summary>
        /// Checks for any known fatal mod conflicts.
        /// </summary>
        /// <returns>True if a mod conflict was detected, false otherwise</returns>
        internal static bool IsModConflict()
        {
            // Initialise flag and list of conflicting mods.
            bool conflictDetected = false;
            conflictingModNames = new List<string>();

            // Iterate through the full list of plugins.
            foreach (PluginManager.PluginInfo plugin in PluginManager.instance.GetPluginsInfo())
            {
                foreach (Assembly assembly in plugin.GetAssemblies())
                {
                    switch (assembly.GetName().Name)
                    {
                        case "VanillaGarbageBinBlocker":
                            // Garbage Bin Controller
                            conflictDetected = true;
                            conflictingModNames.Add("Garbage Bin Controller");
                            break;
                        case "Painter":
                            // Painter - this one is trickier because both Painter and Repaint use Painter.dll (thanks to CO savegame serialization...)
                            if (plugin.userModInstance.GetType().ToString().Equals("Painter.UserMod"))
                            {
                                conflictDetected = true;
                                conflictingModNames.Add("Painter");
                            }
                            break;
                        case "EnhancedDistrictServices":
                            // EDS only conflicts if enabled.
                            if (plugin.isEnabled)
                            {
                                conflictDetected = true;
                                conflictingModNames.Add("Enhanced District Services");
                            }
                            break;
                        case "MoreEffectiveTransfer":
                            // METM only conflicts if enabled.
                            if (plugin.isEnabled)
                            {
                                conflictDetected = true;
                                conflictingModNames.Add("More Effective Transfer Manager");
                            }
                            break;
                        case "TransferManagerCE":
                            // TMCE only conflicts if enabled.
                            if (plugin.isEnabled)
                            {
                                conflictDetected = true;
                                conflictingModNames.Add("Transfer Manager Community Edition");
                            }
                            break;
                    }
                }
            }

            // Was a conflict detected?
            if (conflictDetected)
            {
                // Yes - log each conflict.
                foreach (string conflictingMod in conflictingModNames)
                {
                    Logging.Error("Conflicting mod found: ", conflictingMod);
                }
                Logging.Error("exiting due to mod conflict");
            }

            return conflictDetected;
        }
    }
}
