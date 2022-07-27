using ColossalFramework.Plugins;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace TransferController
{
    /// <summary>
    /// Class that manages interactions with other mods, including compatibility and functionality checks.
    /// </summary>
    internal static class AssemblyUtils
    {
        // List of conflcting mod names.
        internal static List<string> conflictingModNames;

        // Mod assembly path cache.
        private static string assemblyPath = null;


        /// <summary>
        /// Returns the current mod version as a string, leaving off any trailing zero versions for build and revision.
        /// </summary>
        internal static string CurrentVersion
        {
            get
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                if (currentVersion.Revision != 0)
                {
                    return currentVersion.ToString(4);
                }
                else if (currentVersion.Build != 0)
                {
                    return currentVersion.ToString(3);
                }
                else
                {
                    return currentVersion.ToString(2);
                }
            }
        }


        /// <summary>
        /// Returns the filepath of the current mod assembly.
        /// </summary>
        /// <returns>Mod assembly filepath</returns>
        internal static string AssemblyPath
        {
            get
            {
                // Return cached path if it exists.
                if (assemblyPath != null)
                {
                    return assemblyPath;
                }

                // No path cached - get list of currently active plugins.
                IEnumerable<PluginManager.PluginInfo> plugins = PluginManager.instance.GetPluginsInfo();

                // Iterate through list.
                foreach (PluginManager.PluginInfo plugin in plugins)
                {
                    try
                    {
                        // Get all (if any) mod instances from this plugin.
                        IUserMod[] mods = plugin.GetInstances<IUserMod>();

                        // Check to see if the primary instance is this mod.
                        if (mods.FirstOrDefault() is TransferControllerMod)
                        {
                            // Found it! Return path.
                            return plugin.modPath;
                        }
                    }
                    catch
                    {
                        // Don't care.
                    }
                }

                // If we got here, then we didn't find the assembly.
                Logging.Error("assembly path not found");
                throw new FileNotFoundException(TransferControllerMod.ModName + ": assembly path not found!");
            }
        }


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
