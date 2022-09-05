// <copyright file="Loading.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Patching;
    using AlgernonCommons.UI;
    using ICities;

    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : PatcherLoadingBase<OptionsPanel, PatcherBase>
    {
        /// <summary>
        /// Performs any actions upon successful level loading completion.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.).</param>
        protected override void LoadedActions(LoadMode mode)
        {
            base.LoadedActions(mode);

            // Initialise select tool.
            ToolsModifierControl.toolController.gameObject.AddComponent<TCTool>();

            // Set up options panel event handler (need to redo this now that options panel has been reset after loading into game).
            OptionsPanelManager<OptionsPanel>.OptionsEventHook();

            // Add building info panel buttons.
            BuildingPanelManager.AddInfoPanelButtons();

            // Convert any legacy records.
            BuildingControl.ConvertLegacyRecords();
        }
    }
}