// <copyright file="Serializer.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.IO;
    using AlgernonCommons;
    using ICities;

    /// <summary>
    /// Handles savegame data saving and loading.
    /// </summary>
    public class Serializer : SerializableDataExtensionBase
    {
        // Current data version.
        private const int DataVersion = 4;

        // Unique data ID.
        private readonly string dataID = "TransferController";

        /// <summary>
        /// Serializes data to the savegame.
        /// Called by the game on save.
        /// </summary>
        public override void OnSaveData()
        {
            base.OnSaveData();

            using (MemoryStream stream = new MemoryStream())
            {
                // Serialise savegame settings.
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // Write version.
                    writer.Write(DataVersion);

                    // Serialize building data.
                    BuildingControl.Serialize(writer);

                    // Serialize warehouse data.
                    WarehouseControl.Serialize(writer);

                    // Serialize vehicle data.
                    VehicleControl.Serialize(writer);

                    // Write to savegame.
                    serializableDataManager.SaveData(dataID, stream.ToArray());

                    Logging.Message("wrote ", stream.Length);
                }
            }
        }

        /// <summary>
        /// Deserializes data from a savegame (or initialises new data structures when none available).
        /// Called by the game on load (including a new game).
        /// </summary>
        public override void OnLoadData()
        {
            base.OnLoadData();

            // Read data from savegame.
            byte[] data = serializableDataManager.LoadData(dataID);

            // Check to see if anything was read.
            if (data != null && data.Length != 0)
            {
                // Data was read - go ahead and deserialise.
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        // Read version.
                        int version = reader.ReadInt32();
                        Logging.Message("found data version ", version);

                        // Deserialise building settings.
                        BuildingControl.Deserialize(reader, version);

                        // Deserialize warehouse settings.
                        if (version > 0)
                        {
                            WarehouseControl.Deserialize(reader);
                        }

                        // Deserialize vehicle settings.
                        if (version > 1)
                        {
                            VehicleControl.Deserialize(reader);
                        }

                        Logging.Message("read ", stream.Length);
                    }
                }
            }
            else
            {
                // No data read.
                Logging.Message("no data read");
            }
        }
    }
}