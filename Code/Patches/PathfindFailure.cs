// <copyright file="PathfindFailure.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using System.Collections.Generic;
    using ColossalFramework;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch to block transfers bewtween buildings with recent pathfind failures.
    /// </summary>
    [HarmonyPatch(typeof(CarAI), "PathfindFailure")]
    public static class PathFindFailure
    {
        // Timeout period.
        private const long Timeout = 5 * TimeSpan.TicksPerMinute;

        // Enable recording of failures.
        private static bool enableFailureTracking = true;

        // Dictionary of failed pathfinds (building pair plus timestamp in ticks).
        private static Dictionary<BuildingPair, long> pathFails = new Dictionary<BuildingPair, long>();

        /// <summary>
        /// Gets or sets a value indicating whether pathfind failure tracking is enabled (true) or disabled (false).
        /// </summary>
        internal static bool EnableFailTracking
        {
            get => enableFailureTracking;

            set
            {
                enableFailureTracking = value;

                // If pathfinding is disabled, clear the dictionary.
                if (!value)
                {
                    pathFails.Clear();
                }
            }
        }

        /// <summary>
        /// Harmony Postfix to CarAI.PathFindFailure to record failed pathfinds.
        /// </summary>
        /// <param name="data">Vehicle data record.</param>
        public static void Postfix(ref Vehicle data)
        {
            // Don't do anything if not recording fails.
            if (!enableFailureTracking)
            {
                return;
            }

            // See if the failed pathfind has a valid source and target building.
            if (data.m_targetBuilding != 0 && data.m_sourceBuilding != 0)
            {
                // Create BuildingPair.
                BuildingPair thisPair = new BuildingPair { SourceBuilding = data.m_sourceBuilding, TargetBuilding = data.m_targetBuilding };

                // Check if we already have an entry for this building pair.
                if (pathFails.ContainsKey(thisPair))
                {
                    // Yes - update failure time with current times.
                    pathFails[thisPair] = DateTime.Now.Ticks;
                }
                else
                {
                    // No - add new entry with this value.
                    pathFails.Add(thisPair, DateTime.Now.Ticks);
                }
            }
        }

        /// <summary>
        /// Checks to see if a pathfinding failure has been recorded involving the given building within the past 5 minutes.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <returns>True if a pathfinding failure affecting this building has been registered in the past five minutes, false otherwise.</returns>
        internal static bool HasFailure(ushort buildingID)
        {
            // Local reference.
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            // Only entries newer than this are valid.
            long expiryTick = DateTime.Now.Ticks - Timeout;

            // Iterate through dictionary looking for matching records.
            foreach (KeyValuePair<BuildingPair, long> entry in pathFails)
            {
                // Exclude outside connections.
                if ((entry.Key.SourceBuilding == buildingID && !(buildings[entry.Key.TargetBuilding].Info.m_buildingAI is OutsideConnectionAI)) |
                    (entry.Key.TargetBuilding == buildingID && !(buildings[entry.Key.TargetBuilding].Info.m_buildingAI is OutsideConnectionAI)))
                {
                    // Found a record referring to the building - check timestamp.
                    if (entry.Value > expiryTick)
                    {
                        // Timestamp is valid; return true.
                        return true;
                    }
                }
            }

            // If we got here, no recent failure was recorded.
            return false;
        }

        /// <summary>
        /// Checks to see if a pathfinding failure has been recorded between the given building pairs within the past 5 minutes, excluding outside connections.
        /// </summary>
        /// <param name="sourceBuilding">Source building ID.</param>
        /// <param name="targetBuilding">Target building ID.</param>
        /// <returns>True if a pathfinding failure between these buildings has been registered in the past five minutes, false otherwise.</returns>
        internal static bool HasFailure(ushort sourceBuilding, ushort targetBuilding)
        {
            // Only entries newer than this are valid.
            long expiryTick = DateTime.Now.Ticks - Timeout;

            // Create BuildingPair.
            BuildingPair thisPair = new BuildingPair { SourceBuilding = sourceBuilding, TargetBuilding = targetBuilding };

            // Check if we've got a pathfind failure record for this building pair.
            if (pathFails.TryGetValue(thisPair, out long time))
            {
                // Yes - check to see if five minutes have passed.
                if (time > expiryTick)
                {
                    // Five minutes haven't passed - block the transfer.
                    return true;
                }
                else
                {
                    // More than five minutes have passed since the last failure - remove the record (now stale) and fall through.
                    pathFails.Remove(thisPair);
                }
            }

            // If we got here, it's all good; no recent failure recorded.
            return false;
        }

        /// <summary>
        /// Remove all records relating to the given building, plus any that have expried.
        /// </summary>
        /// <param name="buildingID">Building ID to release.</param>
        internal static void ReleaseBuilding(ushort buildingID)
        {
            // List of records to remove.
            List<BuildingPair> recordList = new List<BuildingPair>();

            // Remove all records older than this.
            long expiryTick = DateTime.Now.Ticks - Timeout;

            // Iterate through dictionary looking for records to remove.
            foreach (KeyValuePair<BuildingPair, long> entry in pathFails)
            {
                if (entry.Key.SourceBuilding == buildingID || entry.Key.TargetBuilding == buildingID || entry.Value < expiryTick)
                {
                    // Found a record referring to the building - add to list.
                    recordList.Add(entry.Key);
                }
            }

            // Now, remove all identified records from the dictionary.
            foreach (BuildingPair record in recordList)
            {
                pathFails.Remove(record);
            }
        }

        /// <summary>
        /// Gets a list of all current pathfind failures for the specified building, excluding outside connections.
        /// </summary>
        /// <param name="buildingID">Building ID.</param>
        /// <returns>List of known pathfind fails affecting this building (empty list if none).</returns>
        internal static List<PathFailItem> GetFails(ushort buildingID)
        {
            // Local reference.
            Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            // Iterate through dictionary looking for matching records.
            List<PathFailItem> fails = new List<PathFailItem>();
            foreach (KeyValuePair<BuildingPair, long> entry in pathFails)
            {
                // Exclude outside connections.
                if (entry.Key.SourceBuilding == buildingID && !(buildings[entry.Key.TargetBuilding].Info.m_buildingAI is OutsideConnectionAI))
                {
                    // Found a record referring to the building - add to list.
                    fails.Add(new PathFailItem(entry.Key.TargetBuilding, false));
                }
                else if (entry.Key.TargetBuilding == buildingID && !(buildings[entry.Key.SourceBuilding].Info.m_buildingAI is OutsideConnectionAI))
                {
                    // Found a record referring to the building - add to list.
                    fails.Add(new PathFailItem(entry.Key.SourceBuilding, true));
                }
            }

            return fails;
        }

        /// <summary>
        /// Simple building pair struct.
        /// </summary>
        private struct BuildingPair
        {
            public ushort SourceBuilding;
            public ushort TargetBuilding;
        }
    }
}