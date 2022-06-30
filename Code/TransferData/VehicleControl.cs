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
        /// Returns the list of selected vehicles for the given building, transfer direction, and material.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="material">Transfer material</param>
        /// <returns>List of selected vehicles (empty list if none)</returns>
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
            return new List<VehicleInfo>();
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
        /// Builds the vehicle dictionary key from the provided parametes.
        /// </summary>
        /// <param name="buildingID">Building ID</param>
        /// <param name="material">Transfer material</param>
        /// <returns>Vehicle dictionary key</returns>
        private static uint BuildKey(ushort buildingID, TransferManager.TransferReason material) => ((uint)material <<24) | buildingID;
    }
}