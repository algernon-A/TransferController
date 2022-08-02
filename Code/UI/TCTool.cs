// <copyright file="TCTool.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace TransferController
{
    using System.Collections.Generic;
    using AlgernonCommons.Translation;
    using AlgernonCommons.UI;
    using ColossalFramework;
    using ColossalFramework.UI;
    using UnifiedUI.Helpers;
    using UnityEngine;

    /// <summary>
    /// The TransferController building selection tool.
    /// </summary>
    public class TCTool : DefaultTool
    {
        // Transfer struct for eligibility checking.
        private readonly TransferDataUtils.TransferStruct[] _transfers = new TransferDataUtils.TransferStruct[4];

        // Cursor textures.
        private CursorInfo _selectCursorOn;
        private CursorInfo _selectCursorOff;
        private CursorInfo _pickCursorOn;
        private CursorInfo _pickCursorOff;
        private CursorInfo _currentCursorOn;
        private CursorInfo _currentCursorOff;

        // Building targets.
        private bool _pickMode = false;
        private BuildingRestrictionsTab _buildingRestrictionsTab;
        private ushort _currentBuilding;

        /// <summary>
        /// Gets the active tool instance.
        /// </summary>
        public static TCTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<TCTool>();

        /// <summary>
        /// Gets a value indicating whether the tool is currently active (true) or inactive (false).
        /// </summary>
        public static bool IsActiveTool => Instance != null && ToolsModifierControl.toolController.CurrentTool == Instance;

        /// <summary>
        /// Sets the building currently selected by the info panel.
        /// </summary>
        internal ushort CurrentBuilding { set => _currentBuilding = value; }

        /// <summary>
        /// Called by the game.  Sets which network segments are ignored by the tool (always returns all, i.e. no segments are selectable by the tool).
        /// </summary>
        /// <param name="nameOnly">Always set to false.</param>
        /// <returns>NetSegment.Flags.All.</returns>
        public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
        {
            nameOnly = false;
            return NetSegment.Flags.All;
        }

        /// <summary>
        /// Sets vehicle ingore flags to ignore all vehicles.
        /// </summary>
        /// <returns>Vehicle flags ignoring all vehicles.</returns>
        public override Vehicle.Flags GetVehicleIgnoreFlags() => Vehicle.Flags.LeftHandDrive | Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding;

        /// <summary>
        /// Called by the game every simulation step.
        /// Performs raycasting to select hovered instance.
        /// </summary>
        public override void SimulationStep()
        {
            // Get base mouse ray.
            Ray mouseRay = m_mouseRay;

            // Get raycast input.
            RaycastInput input = new RaycastInput(mouseRay, m_mouseRayLength)
            {
                m_rayRight = m_rayRight,
                m_netService = GetService(),
                m_buildingService = GetService(),
                m_propService = GetService(),
                m_treeService = GetService(),
                m_districtNameOnly = Singleton<InfoManager>.instance.CurrentMode != InfoManager.InfoMode.Districts,
                m_ignoreTerrain = GetTerrainIgnore(),
                m_ignoreNodeFlags = NetNode.Flags.All,
                m_ignoreSegmentFlags = GetSegmentIgnoreFlags(out input.m_segmentNameOnly),
                m_ignoreBuildingFlags = Building.Flags.None,
                m_ignoreTreeFlags = global::TreeInstance.Flags.All,
                m_ignorePropFlags = PropInstance.Flags.All,
                m_ignoreVehicleFlags = GetVehicleIgnoreFlags(),
                m_ignoreParkedVehicleFlags = VehicleParked.Flags.All,
                m_ignoreCitizenFlags = CitizenInstance.Flags.All,
                m_ignoreTransportFlags = TransportLine.Flags.All,
                m_ignoreDistrictFlags = District.Flags.All,
                m_ignoreParkFlags = GetParkIgnoreFlags(),
                m_ignoreDisasterFlags = DisasterData.Flags.All,
                m_transportTypes = GetTransportTypes(),
            };

            ToolErrors errors = ToolErrors.None;
            RaycastOutput output;

            // Is the base mouse ray valid?
            if (m_mouseRayValid)
            {
                // Yes - raycast.
                if (RayCast(input, out output))
                {
                    // Create new hover instance.
                    InstanceID hoverInstance = InstanceID.Empty;

                    // Set base tool accurate position.
                    m_accuratePosition = output.m_hitPos;

                    // Select parent building of any 'untouchable' (sub-)building.
                    if (output.m_building != 0 && (Singleton<BuildingManager>.instance.m_buildings.m_buffer[output.m_building].m_flags & Building.Flags.Untouchable) != 0)
                    {
                        output.m_building = Building.FindParentBuilding((ushort)output.m_building);
                    }

                    // Check for building hits.
                    if (output.m_building != 0)
                    {
                        // Building - record hit position and check building eligibility.
                        output.m_hitPos = Singleton<BuildingManager>.instance.m_buildings.m_buffer[output.m_building].m_position;

                        // Building has eligible transfers - set hover, and set cursor to light.
                        hoverInstance.Building = (ushort)output.m_building;
                        m_cursor = _currentCursorOn;
                    }
                    else
                    {
                        // No hovered building - set dark cursor.
                        m_cursor = _currentCursorOff;
                    }

                    // Has the hovered instance changed since last time?
                    if (hoverInstance != m_hoverInstance)
                    {
                        // Hover instance has changed.
                        // Unhide any previously-hidden buildings.
                        if (m_hoverInstance.Building != 0)
                        {
                            // Local references.
                            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
                            Building[] buildingBuffer = buildingManager.m_buildings.m_buffer;

                            // Unhide previously hovered building.
                            if ((buildingBuffer[m_hoverInstance.Building].m_flags & Building.Flags.Hidden) != 0)
                            {
                                buildingBuffer[m_hoverInstance.Building].m_flags &= ~Building.Flags.Hidden;
                                buildingManager.UpdateBuildingRenderer(m_hoverInstance.Building, updateGroup: true);
                            }
                        }
                    }

                    // Update tool hover instance.
                    m_hoverInstance = hoverInstance;
                }
                else
                {
                    // Raycast failed.
                    errors = ToolErrors.RaycastFailed;
                    m_cursor = _currentCursorOff;
                }
            }
            else
            {
                // No valid mouse ray.
                output = default;
                errors = ToolErrors.RaycastFailed;
                m_cursor = _currentCursorOff;
            }

            // Set mouse position and record errors.
            m_mousePosition = output.m_hitPos;
            m_selectErrors = errors;
        }

        /// <summary>
        /// Called by game when overlay is to be rendered.
        /// </summary>
        /// <param name="cameraInfo">Current camera instance.</param>
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            // Local references.
            ToolManager toolManager = Singleton<ToolManager>.instance;
            Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

            // Highlight linked buildings if in picking mode.
            if (_pickMode)
            {
                // Linked building list.
                HashSet<uint> hashSet = BuildingControl.GetBuildings(_buildingRestrictionsTab.CurrentBuilding, _buildingRestrictionsTab.IsIncoming, _buildingRestrictionsTab.TransferReason);
                if (hashSet != null && hashSet.Count > 0)
                {
                    // Apply yellow overlay to each linked building.
                    Color yellow = new Color(1f, 1f, 0f, 0.75f);
                    foreach (uint building in hashSet)
                    {
                        BuildingTool.RenderOverlay(cameraInfo, ref buildingBuffer[building], yellow, yellow);
                        ++toolManager.m_drawCallData.m_overlayCalls;
                    }
                }
            }
            else
            {
                // If not in building picker mode, highlight all buildings with Transfer Controller settings in magenta.
                Color magenta = new Color(1f, 0f, 1f, 0.75f);
                foreach (uint key in BuildingControl.BuildingRecords.Keys)
                {
                    // Apply overlay.
                    ushort buildingID = (ushort)(key & 0x0000FFFF);

                    // Skip current building.
                    if (buildingID != _currentBuilding)
                    {
                        BuildingTool.RenderOverlay(cameraInfo, ref buildingBuffer[buildingID], magenta, magenta);
                        ++toolManager.m_drawCallData.m_overlayCalls;
                    }
                }
            }

            // Highlight selected building in red.
            if (_currentBuilding != 0)
            {
                Color red = new Color(1f, 0f, 0f, 0.75f);
                BuildingTool.RenderOverlay(cameraInfo, ref buildingBuffer[_currentBuilding], Color.red, Color.red);
                ++toolManager.m_drawCallData.m_overlayCalls;
            }
        }

        /// <summary>
        /// Activates the TCTool.
        /// </summary>
        internal static void Activate() => ToolsModifierControl.toolController.CurrentTool = Instance;

        /// <summary>
        /// Toggles the current tool to/from the TCTool.
        /// </summary>
        internal static void ToggleTool()
        {
            // Activate TCTool tool if it isn't already; if already active, deactivate it by selecting the default tool instead.
            if (!IsActiveTool)
            {
                // Activate tool.
                ToolsModifierControl.toolController.CurrentTool = Instance;
            }
            else
            {
                // Are we in pick mode?
                if (Instance._pickMode)
                {
                    // Yes - clear pick mode.
                    Instance._pickMode = false;
                    Instance._buildingRestrictionsTab = null;
                }

                // Activate default tool.
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        /// <summary>
        /// Sets the tool to pick mode (selecting buildings).
        /// </summary>
        /// <param name="callingTab">Building restrictions tab instance that's triggered pick mode.</param>
        internal void SetPickMode(BuildingRestrictionsTab callingTab)
        {
            _buildingRestrictionsTab = callingTab;
            _pickMode = true;
            _currentCursorOn = _pickCursorOn;
            _currentCursorOff = _pickCursorOff;
            m_cursor = _currentCursorOff;
        }

        /// <summary>
        /// Clears pick mode.
        /// </summary>
        internal void ClearPickMode()
        {
            _pickMode = false;
            _buildingRestrictionsTab = null;
            _currentCursorOn = _selectCursorOn;
            _currentCursorOff = _selectCursorOff;
        }

        /// <summary>
        /// Initialise the tool.
        /// Called by unity when the tool is created.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Load cursors.
            _selectCursorOn = UITextures.LoadCursor("TC-CursorOn.png");
            _selectCursorOff = UITextures.LoadCursor("TC-CursorOff.png");
            _pickCursorOn = UITextures.LoadCursor("TC-CursorPickOn.png");
            _pickCursorOff = UITextures.LoadCursor("TC-CursorPickOff.png");
            _currentCursorOn = _selectCursorOn;
            _currentCursorOff = _selectCursorOff;
            m_cursor = _currentCursorOff;

            // Create new UUI button.
            UIComponent uuiButton = UUIHelpers.RegisterToolButton(
                name: nameof(TCTool),
                groupName: null, // default group
                tooltip: Translations.Translate("TFC_NAM"),
                tool: this,
                icon: UUIHelpers.LoadTexture(UUIHelpers.GetFullPath<Mod>("Resources", "TC-UUI.png")),
                hotkeys: new UUIHotKeys { ActivationKey = ModSettings.ToolKey });
        }

        /// <summary>
        /// Called by the game when the tool is disabled.
        /// </summary>
        protected override void OnDisable()
        {
            ClearPickMode();

            base.OnDisable();
        }

        /// <summary>
        /// Unity late update handling.
        /// Called by game every late update.
        /// </summary>
        protected override void OnToolLateUpdate()
        {
            base.OnToolLateUpdate();

            // Force the info mode to none.
            ToolBase.ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
        }

        /// <summary>
        /// Tool GUI event processing.
        /// Called by game every GUI update.
        /// </summary>
        /// <param name="e">Event parameter.</param>
        protected override void OnToolGUI(Event e)
        {
            // Don't do anything if mouse is inside UI or if there are any errors other than failed raycast.
            if (m_toolController.IsInsideUI || (m_selectErrors != ToolErrors.None && m_selectErrors != ToolErrors.RaycastFailed))
            {
                return;
            }

            // Try to get a hovered building instance.
            ushort building = m_hoverInstance.Building;
            if (building != 0)
            {
                // Check for mousedown events with button zero.
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    // Got one; use the event.
                    e.Use();

                    // Are we in pick mode?
                    if (_pickMode)
                    {
                        // Yes - communicate selection back to requesting panel and clear pick mode.
                        _buildingRestrictionsTab?.AddBuilding(building);
                        ClearPickMode();
                    }
                    else
                    {
                        // Not in pick mode - create the info panel with the hovered building prefab.
                        BuildingPanelManager.SetTarget(building);
                    }
                }
            }

            // Handle paste through tool-only selection if panel isn't open.
            if (ModSettings.KeyPaste.IsPressed(e) && BuildingPanelManager.Panel == null)
            {
                e.Use();
                if (building != 0)
                {
                    CopyPaste.Paste(building);
                }
            }

            // Right-click disables tool.
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                // Cancel tool on right-click.
                e.Use();
                ToggleTool();
            }
        }
    }
}
