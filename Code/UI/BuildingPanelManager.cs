// <copyright file="BuildingPanelManager.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework.UI;
    using UnityEngine;

    /// <summary>
    /// Static class to manage the mod's building info panel.
    /// </summary>
    internal static class BuildingPanelManager
    {
        // Instance references.
        private static GameObject s_gameObject;
        private static BuildingPanel s_panel;

        // InfoPanel buttons.
        private static UIButton s_privateBuildingButton;
        private static UIButton s_playerBuildingButton;

        /// <summary>
        /// Gets the active panel instance.
        /// </summary>
        internal static BuildingPanel Panel => s_panel;

        /// <summary>
        /// Creates the panel object in-game and displays it.
        /// </summary>
        /// <typeparam name="TPanel">Panel type.</typeparam>
        internal static void Create<TPanel>()
            where TPanel : BuildingPanel
        {
            try
            {
                // If no instance already set, create one.
                if (s_gameObject == null)
                {
                    // Give it a unique name for easy finding with ModTools.
                    s_gameObject = new GameObject("TCBuildingInfoPanel");
                    s_gameObject.transform.parent = UIView.GetAView().transform;

                    // Add panel and set parent transform.
                    s_panel = s_gameObject.AddComponent<TPanel>();

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
            // Clear TCTool mode and highlighting references/
            TCTool.Instance.ClearPickMode();
            TCTool.Instance.CurrentBuilding = 0;

            GameObject.Destroy(s_panel);
            GameObject.Destroy(s_gameObject);

            s_panel = null;
            s_gameObject = null;
        }

        /// <summary>
        /// Sets the target to the selected building, creating the panel if necessary.
        /// </summary>
        /// <param name="buildingID">New building ID.</param>
        internal static void SetTarget(ushort buildingID)
        {
            // If no existing panel, create it.
            if (Panel == null)
            {
                Create<BuildingPanel>();
            }

            // Set the target.
            Panel.SetTarget(buildingID);
        }

        /// <summary>
        /// Adds the building buttons to game building info panels.
        /// </summary>
        internal static void AddInfoPanelButtons()
        {
            try
            {
                s_privateBuildingButton = AddInfoPanelButton(UIView.library.Get<ZonedBuildingWorldInfoPanel>(typeof(ZonedBuildingWorldInfoPanel).Name), -97f);
                s_playerBuildingButton = AddInfoPanelButton(UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name), -72f);
                AddInfoPanelButton(UIView.library.Get<WarehouseWorldInfoPanel>(typeof(WarehouseWorldInfoPanel).Name), -5f);
                AddInfoPanelButton(UIView.library.Get<UniqueFactoryWorldInfoPanel>(typeof(UniqueFactoryWorldInfoPanel).Name), -5f);
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception adding building info panel buttons");
            }
        }

        /// <summary>
        /// Handles button visibility when building info world target building changes.
        /// </summary>
        internal static void TargetChanged()
        {
            bool isVisible = TransferDataUtils.BuildingEligibility(WorldInfoPanel.GetCurrentInstanceID().Building, new TransferDataUtils.TransferStruct[4]);
            s_privateBuildingButton.isVisible = isVisible;
            s_playerBuildingButton.isVisible = isVisible;
        }

        /// <summary>
        /// Adds a Transfer Controller button to a building info panel to directly access that building's.
        /// </summary>
        /// <param name="infoPanel">Infopanel to apply the button to.</param>
        /// <param name="offset">Panel y-offset from default position.</param>
        /// <returns>New UIButton.</returns>
        private static UIButton AddInfoPanelButton(BuildingWorldInfoPanel infoPanel, float offset)
        {
            const float PanelButtonSize = 24f;
            UIButton panelButton = infoPanel.component.AddUIComponent<UIButton>();

            // Basic button setup.
            panelButton.atlas = UITextures.LoadSprite("TC-UUI");
            panelButton.size = new Vector2(PanelButtonSize, PanelButtonSize);
            panelButton.normalFgSprite = "normal";
            panelButton.focusedFgSprite = "normal";
            panelButton.hoveredFgSprite = "normal";
            panelButton.pressedFgSprite = "normal";
            panelButton.disabledFgSprite = "normal";
            panelButton.name = "TransferControllerButton";
            panelButton.tooltip = Translations.Translate("TFC_NAM");

            // Find ProblemsPanel relative position to position button.
            // We'll use 40f as a default relative Y in case something doesn't work.
            UIComponent problemsPanel;
            float relativeY = 40f;

            // Player info panels have wrappers, zoned ones don't.
            UIComponent wrapper = infoPanel.Find("Wrapper");
            if (wrapper == null)
            {
                problemsPanel = infoPanel.Find("ProblemsPanel");
            }
            else
            {
                problemsPanel = wrapper.Find("ProblemsPanel");
            }

            try
            {
                // Position button vertically in the middle of the problems panel.  If wrapper panel exists, we need to add its offset as well.
                relativeY = (wrapper == null ? 0 : wrapper.relativePosition.y) + problemsPanel.relativePosition.y + ((problemsPanel.height - PanelButtonSize) / 2);
            }
            catch
            {
                // Don't really care; just use default relative Y.
                Logging.Message("couldn't find ProblemsPanel relative position");
            }

            // Set position.
            panelButton.AlignTo(infoPanel.component, UIAlignAnchor.TopLeft);
            panelButton.relativePosition += new Vector3(infoPanel.component.width + offset - PanelButtonSize, relativeY, 0f);

            // Event handler.
            panelButton.eventClick += (control, clickEvent) =>
            {
                // Select current building in the building details panel and show.
                SetTarget(WorldInfoPanel.GetCurrentInstanceID().Building);

                // Manually unfocus control, otherwise it can stay focused until next UI event (looks untidy).
                control.Unfocus();
            };

            return panelButton;
        }
    }
}