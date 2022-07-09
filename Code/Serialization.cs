using System.IO;
using ICities;


namespace TransferController
{
    /// <summary>
    /// Handles savegame data saving and loading.
    /// </summary>
    public class Serializer : SerializableDataExtensionBase
    {
        // Unique data ID.
        private readonly string dataID = "TransferController";
        public const int DataVersion = 4;


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