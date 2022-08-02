// <copyright file="ModSettings.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.IO;
    using System.Xml.Serialization;
    using AlgernonCommons.Keybinding;
    using AlgernonCommons.XML;
    using UnityEngine;

    /// <summary>
    /// Global mod settings.
    /// </summary>
    [XmlRoot("TransferController")]
    public class ModSettings : SettingsXMLBase
    {
        /// <summary>
        /// Copy key.
        /// </summary>
        [XmlIgnore]
        public static readonly Keybinding KeyCopy = new Keybinding(KeyCode.C, true, false, false);

        /// <summary>
        /// Paste key.
        /// </summary>
        [XmlIgnore]
        public static readonly Keybinding KeyPaste = new Keybinding(KeyCode.V, true, false, false);

        /// <summary>
        /// UUI key.
        /// </summary>
        [XmlIgnore]
        private static readonly UnsavedInputKey UUIKey = new UnsavedInputKey(name: "Transfer Controller hotkey", keyCode: KeyCode.T, control: true, shift: false, alt: true);

        /// <summary>
        /// Gets the settings file name.
        /// </summary>
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "TransferController.xml");

        /// <summary>
        /// Gets or sets the tool hotkey.
        /// </summary>
        [XmlElement("ToolKey")]
        public Keybinding XMLToolKey
        {
            get => UUIKey.Keybinding;

            set => UUIKey.Keybinding = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the new matching algorithm should be used (true) or the legacy algorithm (false).
        /// </summary>
        [XmlElement("UseNewAlgorithm")]
        public bool XMLUseNewAlgorithm { get => Patcher.UseNewAlgorithm; set => Patcher.UseNewAlgorithm = value; }

        /// <summary>
        /// Gets or sets the distance matching intensity for the legacy algorithm.
        /// </summary>
        [XmlElement("DistanceMultiplier")]
        public int XMLDistanceMultiplier { get => OldMatching.DistancePercentage; set => OldMatching.DistancePercentage = value; }

        /// <summary>
        /// Gets or sets the global warehouse priority boost.
        /// </summary>
        [XmlElement("WarehousePriority")]
        public int XMLWarehousePriority { get => Matching.WarehousePriority; set => Matching.WarehousePriority = value; }

        /// <summary>
        /// Gets or sets the outside rail connection priority boost.
        /// </summary>
        [XmlElement("OutsideRailPriority")]
        public int XMLOutsideRailPriority { get => Matching.OutsideRailPriority; set => Matching.OutsideRailPriority = value; }

        /// <summary>
        /// Gets or sets the outside shipping connection priority boost.
        /// </summary>
        [XmlElement("OutsideShipPriority")]
        public int XMLOutsideShipPriority { get => Matching.OutsideShipPriority; set => Matching.OutsideShipPriority = value; }

        /// <summary>
        /// Gets or sets a value indicating whether to block transfers with recent failed pathfinds (true).
        /// </summary>
        [XmlElement("BlockFailedPathfinds")]
        public bool XMLBlockFailedPathfinds { get => PathFindFailure.EnableFailTracking; set => PathFindFailure.EnableFailTracking = value; }

        /// <summary>
        /// Gets the current hotkey as a UUI UnsavedInputKey.
        /// </summary>
        [XmlIgnore]
        internal static UnsavedInputKey ToolKey => UUIKey;

        /// <summary>
        /// Loads settings from file.
        /// </summary>
        internal static void Load() => XMLFileUtils.Load<ModSettings>(SettingsFileName);

        /// <summary>
        /// Saves settings to file.
        /// </summary>
        internal static void Save() => XMLFileUtils.Save<ModSettings>(SettingsFileName);
    }
}