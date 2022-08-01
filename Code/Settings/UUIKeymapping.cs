// <copyright file="UUIKeymapping.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using AlgernonCommons.Keybinding;
    using AlgernonCommons.Translation;
    using ColossalFramework;

    /// <summary>
    /// Keycode setting control for UUI key.
    /// </summary>
    public class UUIKeymapping : OptionsKeymapping
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UUIKeymapping"/> class.
        /// </summary>
        public UUIKeymapping()
        {
            // Set label and button text.
            Label = Translations.Translate("KEY_KEY");
            ButtonLabel = SavedInputKey.ToLocalizedString("KEYNAME", KeySetting);
        }

        /// <summary>
        /// Gets or sets the mod's UUI key.
        /// </summary>
        protected override InputKey KeySetting
        {
            get => ModSettings.uuiKey.value;

            set => ModSettings.uuiKey.value = value;
        }
    }
}