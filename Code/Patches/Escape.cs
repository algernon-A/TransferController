// <copyright file="Escape.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to implement escape key handling.
    /// </summary>
    [HarmonyPatch(typeof(GameKeyShortcuts), "Escape")]
    public static class Escape
    {
        /// <summary>
        /// Harmony prefix patch to cancel the tool when it's active and the escape key is pressed.
        /// </summary>
        /// <returns>True (continue on to game method) if the zoning tool isn't already active, false (pre-empt game method) otherwise.</returns>
        public static bool Prefix()
        {
            // Is the TC tool tool active?
            if (TCTool.IsActiveTool)
            {
                // Yes; toggle tool status and return false (pre-empt original method).
                TCTool.ToggleTool();
                return false;
            }
            else if (BuildingPanelManager.Panel != null)
            {
                // If tool isn't already active, close the panel and return false (pre-empt original method).
                BuildingPanelManager.Close();
                return false;
            }

            // Tool not active - don't do anything, just go on to game code.
            return true;
        }
    }
}