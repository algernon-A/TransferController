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

        // SavedInputKey reference for communicating with UUI.
        [XmlIgnore]
        private static readonly SavedInputKey uuiSavedKey = new SavedInputKey("Transfer Controller hotkey", "Transfer Controller hotkey", key: KeyCode.T, control: true, shift: false, alt: true, false);


        [XmlIgnore]
        public static readonly SavedInputKey keyCopy = new SavedInputKey(nameof(keyCopy), SettingsFileName, SavedInputKey.Encode(KeyCode.C, true, false, false), true);

        [XmlIgnore]
        public static readonly SavedInputKey keyPaste = new SavedInputKey(nameof(keyPaste), SettingsFileName, SavedInputKey.Encode(KeyCode.V, true, false, false), true);


        /// <summary>
        /// Panel hotkey as ColossalFramework SavedInputKey.
        /// </summary>
        [XmlIgnore]
        internal static SavedInputKey ToolSavedKey => uuiSavedKey;


        // Language.
        [XmlElement("Language")]
        public string Language
        {
            get => Translations.CurrentLanguage;

            set => Translations.CurrentLanguage = value;
        }


        // Hotkey element.
        [XmlElement("PanelKey")]
        public KeyBinding ToolKey
        {
            get
            {
                return new KeyBinding
                {
                    keyCode = (int)ToolSavedKey.Key,
                    control = ToolSavedKey.Control,
                    shift = ToolSavedKey.Shift,
                    alt = ToolSavedKey.Alt
                };
            }
            set
            {
                uuiSavedKey.Key = (KeyCode)value.keyCode;
                uuiSavedKey.Control = value.control;
                uuiSavedKey.Shift = value.shift;
                uuiSavedKey.Alt = value.alt;
            }
        }


        // Matching intensity.
        [XmlElement("DistanceMultiplier")]
        public int XMLDistanceMultiplier { get => TransferManagerPatches.distancePercentage; set => TransferManagerPatches.distancePercentage = value; }


        // Warehouse priority boost.
        [XmlElement("WarehousePriority")]
        public int XMLWarehousePriority { get => TransferManagerPatches.warehousePriority; set => TransferManagerPatches.warehousePriority = value; }


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
    }
}