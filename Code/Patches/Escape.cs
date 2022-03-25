using HarmonyLib;


namespace TransferController
{
    /// <summary>
    /// Harmony patch to implement escape key handling.
    /// </summary>
    [HarmonyPatch(typeof(GameKeyShortcuts), "Escape")]
    public static class EscapePatch
    {
        /// <summary>
        /// Harmony prefix patch to cancel the zoning tool when it's active and the escape key is pressed.
        /// </summary>
        /// <returns>True (continue on to game method) if the zoning tool isn't already active, false (pre-empt game method) otherwise</returns>
        public static bool Prefix()
        {
            // Is the TC tool tool active?
            if (TCTool.IsActiveTool)
            {
                // Yes; toggle tool status and return false (pre-empt original method).
                TCTool.ToggleTool();
                return false;
            }

            // Tool not active - don't do anything, just go on to game code.
            return true;
        }
    }
}