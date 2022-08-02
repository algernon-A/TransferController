// <copyright file="Loading.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons;
    using AlgernonCommons.Notifications;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ICities;

    /// <summary>
    /// Main loading class: the mod runs from here.
    /// </summary>
    public class Loading : LoadingExtensionBase
    {
        // Internal flags.
        private static bool _isModEnabled = false;
        private bool _harmonyLoaded = false;
        private bool _conflictingMod = false;

        /// <summary>
        /// Called by the game when the mod is initialised at the start of the loading process.
        /// </summary>
        /// <param name="loading">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            // Don't do anything if not in game (e.g. if we're going into an editor).
            if (loading.currentMode != AppMode.Game)
            {
                _isModEnabled = false;
                Logging.KeyMessage("not loading into game, skipping activation");

                // Set harmonyLoaded flag to suppress Harmony warning when e.g. loading into editor.
                _harmonyLoaded = true;

                // Unload Harmony patches and exit before doing anything further.
                Patcher.Instance.UnpatchAll();
                return;
            }

            // Ensure that Harmony patches have been applied.
            _harmonyLoaded = Patcher.Instance.Patched;
            if (!_harmonyLoaded)
            {
                _isModEnabled = false;
                Logging.Error("Harmony patches not applied; aborting");
                return;
            }

            // Check for mod conflicts.
            if (ConflictDetection.IsModConflict())
            {
                // Conflict detected.
                _conflictingMod = true;
                _isModEnabled = false;

                // Unload Harmony patches and exit before doing anything further.
                Patcher.Instance.UnpatchAll();
                return;
            }

            // Passed all checks - okay to load (if we haven't already for some reason).
            if (!_isModEnabled)
            {
                _isModEnabled = true;
                Logging.KeyMessage("v " + AssemblyUtils.CurrentVersion + " loading");
            }
        }

        /// <summary>
        /// Called by the game when level loading is complete.
        /// </summary>
        /// <param name="mode">Loading mode (e.g. game, editor, scenario, etc.)</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            // Check to see that Harmony 2 was properly loaded.
            if (!_harmonyLoaded)
            {
                // Harmony 2 wasn't loaded; display warning notification and exit.
                ListNotification harmonyBox = NotificationBase.ShowNotification<ListNotification>();

                // Key text items.
                harmonyBox.AddParas(Translations.Translate("ERR_HAR0"), Translations.Translate("TFC_ERR_HAR"), Translations.Translate("TFC_ERR_FAT"), Translations.Translate("ERR_HAR1"));

                // List of dot points.
                harmonyBox.AddList(Translations.Translate("ERR_HAR2"), Translations.Translate("ERR_HAR3"));

                // Closing para.
                harmonyBox.AddParas(Translations.Translate("MES_PAGE"));

                // Exit.
                return;
            }

            // Check to see if a conflicting mod has been detected.
            if (_conflictingMod)
            {
                // Mod conflict detected - display warning notification and exit.
                ListNotification modConflictBox = NotificationBase.ShowNotification<ListNotification>();

                // Key text items.
                modConflictBox.AddParas(Translations.Translate("ERR_CON0"), Translations.Translate("TFC_ERR_FAT"), Translations.Translate("TFC_ERR_CON0"), Translations.Translate("ERR_CON1"));

                // Add conflicting mod name(s).
                modConflictBox.AddList(ConflictDetection.ConflictingModNames.ToArray());

                // Closing para.
                modConflictBox.AddParas(Translations.Translate("TFC_ERR_CON1"));

                // Exit.
                return;
            }

            // Load mod if it's enabled.
            if (_isModEnabled)
            {
                // Initialise select tool.
                ToolsModifierControl.toolController.gameObject.AddComponent<TCTool>();

                // Set up options panel event handler (need to redo this now that options panel has been reset after loading into game).
                OptionsPanelManager<OptionsPanel>.OptionsEventHook();

                // Add building info panel buttons.
                BuildingPanelManager.AddInfoPanelButtons();

                // Convert any legacy records.
                BuildingControl.ConvertLegacyRecords();

                Logging.Message("loading complete");
            }
        }
    }
}