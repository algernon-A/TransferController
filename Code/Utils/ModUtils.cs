using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using ICities;
using ColossalFramework.Plugins;


namespace TransferController
{
    /// <summary>
    /// Class that manages interactions with other mods, including compatibility and functionality checks.
    /// </summary>
    internal static class ModUtils
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


        /// <summary>
        /// Returns the filepath of the current mod assembly.
        /// </summary>
        /// <returns>Mod assembly filepath</returns>
        internal static string GetAssemblyPath()
        {
            // Get list of currently active plugins.
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
}
