using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using ColossalFramework;


namespace TransferController
{
    /// <summary>
    /// Global mod settings.
    /// </summary>
	[XmlRoot("TransferController")]
    public class ModSettings
    {
        // Settings file name.
        [XmlIgnore]
        private static readonly string SettingsFileName = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, "TransferController.xml");

        // UUI hotkey.
        [XmlIgnore]
        private static readonly UnsavedInputKey uuiKey = new UnsavedInputKey(name: "Transfer Controller hotkey", keyCode: KeyCode.T, control: true, shift: false, alt: true);

        [XmlIgnore]
        public static readonly SavedInputKey keyCopy = new SavedInputKey(nameof(keyCopy), SettingsFileName, SavedInputKey.Encode(KeyCode.C, true, false, false), true);

        [XmlIgnore]
        public static readonly SavedInputKey keyPaste = new SavedInputKey(nameof(keyPaste), SettingsFileName, SavedInputKey.Encode(KeyCode.V, true, false, false), true);


        // Language.
        [XmlElement("Language")]
        public string Language
        {
            get => Translations.CurrentLanguage;

            set => Translations.CurrentLanguage = value;
        }


        // Tool hotkey.
        [XmlElement("ToolKey")]
        public KeyBinding XMLToolKey
        {
            get => uuiKey.KeyBinding;

            set => uuiKey.KeyBinding = value;
        }


        // Distance-only matching.
        [XmlElement("UseNewAlgorithm")]
        public bool XMLUseNewAlgorithm { get => Patcher.UseNewAlgorithm; set => Patcher.UseNewAlgorithm = value; }


        // Matching intensity.
        [XmlElement("DistanceMultiplier")]
        public int XMLDistanceMultiplier { get => OldMatching.distancePercentage; set => OldMatching.distancePercentage = value; }


        // Warehouse priority boost.
        [XmlElement("WarehousePriority")]
        public int XMLWarehousePriority { get => AddOffers.warehousePriority; set => AddOffers.warehousePriority = value; }

        // Outside rail connection boost.
        [XmlElement("OutsideRailPriority")]
        public int XMLOutsideRailPriority { get => Matching.outsideRailPriority; set => Matching.outsideRailPriority = value; }


        // Outside shipping connection boost.
        [XmlElement("OutsideShipPriority")]
        public int XMLOutsideShipPriority { get => Matching.outsideShipPriority; set => Matching.outsideShipPriority = value; }


        // Monitor pathfind failures.
        [XmlElement("BlockFailedPathfinds")]
        public bool XMLBlockFailedPathfinds { get => PathFindFailure.EnableFailTracking; set => PathFindFailure.EnableFailTracking = value; }


        /// <summary>
        /// Current hotkey as UUI UnsavedInputKey.
        /// </summary>
        [XmlIgnore]
        internal static UnsavedInputKey UUIKey => uuiKey;


        /// <summary>
        /// The current hotkey settings as ColossalFramework InputKey.
        /// </summary>
        [XmlIgnore]
        internal static InputKey ToolKey
        {
            get => uuiKey.value;

            set => uuiKey.value = value;
        }


        /// <summary>
        /// Load settings from XML file.
        /// </summary>
        internal static void Load()
        {
            try
            {
                // Check to see if configuration file exists.
                if (File.Exists(SettingsFileName))
                {
                    // Read it.
                    using (StreamReader reader = new StreamReader(SettingsFileName))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                        if (!(xmlSerializer.Deserialize(reader) is ModSettings settingsFile))
                        {
                            Logging.Error("couldn't deserialize settings file");
                        }
                    }
                }
                else
                {
                    Logging.Message("no settings file found");
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reading XML settings file");
            }
        }


        /// <summary>
        /// Save settings to XML file.
        /// </summary>
        internal static void Save()
        {
            try
            {
                // Pretty straightforward.  Serialisation is within GBRSettingsFile class.
                using (StreamWriter writer = new StreamWriter(SettingsFileName))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
                    xmlSerializer.Serialize(writer, new ModSettings());
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception saving XML settings file");
            }
        }
    }


    /// <summary>
    /// Basic keybinding class - code and modifiers.
    /// </summary>
    public class KeyBinding
    {
        [XmlAttribute("KeyCode")]
        public int keyCode;

        [XmlAttribute("Control")]
        public bool control;

        [XmlAttribute("Shift")]
        public bool shift;

        [XmlAttribute("Alt")]
        public bool alt;


        /// <summary>
        /// Encode keybinding as saved input key for UUI.
        /// </summary>
        /// <returns></returns>
        internal InputKey Encode() => SavedInputKey.Encode((KeyCode)keyCode, control, shift, alt);
    }


    /// <summary>
    /// UUI unsaved input key.
    /// </summary>
    public class UnsavedInputKey : UnifiedUI.Helpers.UnsavedInputKey
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Reference name</param>
        /// <param name="keyCode">Keycode</param>
        /// <param name="control">Control modifier key status</param>
        /// <param name="shift">Shift modifier key status</param>
        /// <param name="alt">Alt modifier key status</param>
        public UnsavedInputKey(string name, KeyCode keyCode, bool control, bool shift, bool alt) :
            base(keyName: name, modName: "Repaint", Encode(keyCode, control: control, shift: shift, alt: alt))
        {
        }


        /// <summary>
        /// Called by UUI when a key conflict is resolved.
        /// Used here to save the new key setting.
        /// </summary>
        public override void OnConflictResolved() => ModSettings.Save();


        /// <summary>
        /// 
        /// </summary>
        public KeyBinding KeyBinding
        {
            get => new KeyBinding { keyCode = (int)Key, control = Control, shift = Shift, alt = Alt };
            set => this.value = value.Encode();
        }
    }
}