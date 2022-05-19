using System;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Static class to manage the mod's building info panel.
    /// </summary>
    internal static class BuildingPanelManager
    {
        // Instance references.
        private static GameObject uiGameObject;
        private static BuildingInfoPanel panel;
        internal static BuildingInfoPanel Panel => panel;


        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        /// <param name="parent">Parent component</param>
        internal static void Create<T>() where T : BuildingInfoPanel
        {
            try
            {
                // If no instance already set, create one.
                if (uiGameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    uiGameObject = new GameObject("TCBuildingInfoPanel");
                    uiGameObject.transform.parent = UIView.GetAView().transform;

                    // Add panel and set parent transform.
                    panel = uiGameObject.AddComponent<T>();
                    Panel.transform.parent = uiGameObject.transform.parent;

                    // Show panel.
                    Panel.Show();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception creating TCBuildingInfoPanel");
            }
        }


        /// <summary>
        /// Closes the panel by destroying the object (removing any ongoing UI overhead).
        /// </summary>
        internal static void Close()
        {
            GameObject.Destroy(panel);
            GameObject.Destroy(uiGameObject);

            panel = null;
            uiGameObject = null;
        }


        /// <summary>
        /// Sets the target to the selected building, creating the panel if necessary.
        /// </summary>
        /// <param name="buildingID">New building ID</param>
        internal static void SetTarget(ushort buildingID)
        {
            if (Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].Info.m_buildingAI is WarehouseAI)
            {
                if (Panel != null &&!(Panel is WarehouseInfoPanel))
                    {
                        Close();
                    }

                if (Panel == null)
                {
                    Create<WarehouseInfoPanel>();
                }
            }
            else
            {
                if (Panel != null && Panel is WarehouseInfoPanel)
                {
                    Close();
                }

                // If no existing panel, create it.
                if (Panel == null)
                {
                    Create<BuildingInfoPanel>();
                }
            }

            // Set the target.
            Panel.SetTarget(buildingID);
        }
    }
}