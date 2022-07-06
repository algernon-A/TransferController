using System.IO;
using System.Collections.Generic;


namespace TransferController
{
    /// <summary>
    /// Static class to control building vehicles.
    /// </summary>
    internal static class VehicleControl
    {
        // Dictionary to hold selected vehicles.
        // Key is packed ((byte)transfertype << 24) | (ushort)buildingID.
        private static Dictionary<uint, List<VehicleInfo>> vehicles = new Dictionary<uint, List<VehicleInfo>>();


        /// <summary>
        /// Checks to see if the specified building has a custom vehicle selection.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="transferReason">Transfer reason</param>
        /// <returns>True if a custom record exists, false otherwise</returns>
        internal static bool HasRecord(ushort buildingID, TransferManager.TransferReason transferReason) => vehicles.ContainsKey(BuildKey(buildingID, transferReason));


        /// <summary>
        /// Returns the list of selected vehicles for the given building, transfer direction, and material.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="material">Transfer material</param>
        /// <returns>List of selected vehicles (null if none)</returns>
        internal static List<VehicleInfo> GetVehicles(ushort buildingID, TransferManager.TransferReason material)
        {
            // Validity check.
            if (buildingID != 0)
            {
                // Retrieve and return any existing dictionary entry.
                if (vehicles.TryGetValue(BuildKey(buildingID, material), out List<VehicleInfo> vehicleList))
                {
                    return vehicleList;
                }
                else if (material != TransferManager.TransferReason.None)
                {
                    // No entry found; try again using the default transfer material.
                    if (vehicles.TryGetValue(BuildKey(buildingID, TransferManager.TransferReason.None), out vehicleList))
                    {
                        return vehicleList;
                    }
                }
            }

            // If we got here, no entry was found; return an empty new list.
            return null;
        }


        /// <summary>
        /// Adds a vehicle the list of selected vehicles for the given building, transfer direction, and material
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="material">Transfer material</param>
        /// <param name="vehicle">Vehicle prefab to add</param>
        internal static void AddVehicle(ushort buildingID, TransferManager.TransferReason material, VehicleInfo vehicle)
        {
            // Safety checks.
            if (buildingID == 0 || vehicle == null)
            {
                Logging.Error("invalid parameter passed to VehicleControl.AddVehicle");
                return;
            }

            // Do we have an existing entry?
            uint key = BuildKey(buildingID, material);
            if (!vehicles.ContainsKey(key))
            {
                // No existing entry - create one.
                vehicles.Add(key, new List<VehicleInfo> { vehicle });
            }
            else
            {
                // Existing entry - add this vehicle to the list, if it isn't already there.
                if (!vehicles[key].Contains(vehicle))
                {
                    vehicles[key].Add(vehicle);
                }
            }
        }


        /// <summary>
        /// Removes a vehicle from list of selected vehicles for the given building, transfer direction, and material
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="material">Transfer material</param>
        /// <param name="vehicle">Vehicle prefab to remove</param>
        internal static void RemoveVehicle(ushort buildingID, TransferManager.TransferReason material, VehicleInfo vehicle)
        {
            // Safety checks.
            if (buildingID == 0 || vehicle == null)
            {
                Logging.Error("invalid parameter passed to VehicleControl.RemoveVehicle");
                return;
            }

            // Do we have an existing entry?
            uint key = BuildKey(buildingID, material);
            if (vehicles.ContainsKey(key))
            {
                // Yes - remove vehicle from list.
                vehicles[key].Remove(vehicle);

                // If no vehicles remaining in this list, remove the entire entry.
                if (vehicles[key].Count == 0)
                {
                    vehicles.Remove(key);
                }
            }
        }


        /// <summary>
        /// Removes all references to a given building from the vehicle dictionary.
        /// </summary>
        /// <param name="buildingID">BuildingID to remove</param>
        internal static void ReleaseBuilding(ushort buildingID)
        {
            // Iterate through each key in dictionary, finding any entries corresponding to the given building ID.
            List<uint> removeList = new List<uint>();
            foreach (uint key in vehicles.Keys)
            {
                if ((key & 0x0000FFFF) == buildingID)
                {
                    removeList.Add(key);
                }
            }

            // Iterate through each entry found and remove it from the dictionary.
            foreach (uint key in removeList)
            {
                vehicles.Remove(key);
            }
        }


        /// <summary>
        /// Serializes vehicle selection data.
        /// </summary>
        /// <param name="stream">Binary writer instance to serialize to</param>
        internal static void Serialize(BinaryWriter writer)
        {
            Logging.Message("serializing vehicle data");

            // Write length of dictionary.
            writer.Write(vehicles.Count);

            // Serialise each building entry.
            foreach (KeyValuePair<uint, List<VehicleInfo>> entry in vehicles)
            {
                // Local reference.
                List<VehicleInfo> vehicleList = entry.Value;

                // Serialize key.
                writer.Write(entry.Key);

                // Serialize list (vehicle names).
                writer.Write((ushort)vehicleList.Count);
                foreach (VehicleInfo vehicle in vehicleList)
                {
                    writer.Write(vehicle.name);
                }

                Logging.Message("wrote entry ", entry.Key);
            }
        }


        /// <summary>
        /// Deserializes vehicle selection data.
        /// </summary>
        /// <param name="stream">Data memory stream to deserialize from</param>
        internal static void Deserialize(BinaryReader reader)
        {
            Logging.Message("deserializing vehicle data");

            // Clear dictionary.
            vehicles.Clear();

            // Iterate through each entry read.
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; ++i)
            {
                // Dictionary entry key.
                uint key = reader.ReadUInt32();

                // List length.
                ushort numVehicles = reader.ReadUInt16();
                if (numVehicles > 0)
                {
                    // Read list.
                    List<VehicleInfo> vehicleList = new List<VehicleInfo>();
                    for (int j = 0; j < numVehicles; ++j)
                    {
                        string vehicleName = reader.ReadString();
                        if (!string.IsNullOrEmpty(vehicleName))
                        {
                            // Attempt to find matching prefab from saved vehicle name.
                            VehicleInfo thisVehicle = PrefabCollection<VehicleInfo>.FindLoaded(vehicleName);

                            // Make sure that vehicle is laoded before we add to list.
                            if (thisVehicle != null)
                            {
                                vehicleList.Add(thisVehicle);
                            }
                        }
                        else
                        {
                            Logging.Error("invalid vehicle name");
                        }
                    }

                    // If at least one vehicle was recovered, add the entry to the dictionary.
                    if (vehicleList.Count > 0)
                    {
                        vehicles.Add(key, vehicleList);
                    }
                }

                Logging.Message("read entry ", key);
            }
        }


        /// <summary>
        /// Builds the vehicle dictionary key from the provided parametes.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="material">Transfer material</param>
        /// <returns>Vehicle dictionary key</returns>
        private static uint BuildKey(ushort buildingID, TransferManager.TransferReason material) => ((uint)material <<24) | buildingID;
    }
}