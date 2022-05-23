using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using UnifiedUI.Helpers;


namespace TransferController
{
	/// <summary>
	/// The TransferController building selection tool.
	/// </summary>
	public class TCTool : DefaultTool
	{
		// Cursor textures.
		private CursorInfo selectCursorOn, selectCursorOff, pickCursorOn, pickCursorOff;
		private CursorInfo currentCursorOn, currentCursorOff;

		// Building target picking mode flag and reference.
		private bool pickMode = false;
		private TransferBuildingTab transferBuildingTab;

		// Transfer struct for eligibility checking.
		private readonly TransferStruct[] transfers = new TransferStruct[4];

		/// <summary>
		/// Instance reference.
		/// </summary>
		public static TCTool Instance => ToolsModifierControl.toolController?.gameObject?.GetComponent<TCTool>();

		/// <summary>
		/// Returns true if the zoning tool is currently active, false otherwise.
		/// </summary>
		public static bool IsActiveTool => Instance != null && ToolsModifierControl.toolController.CurrentTool == Instance;



		/// <summary>
		/// Initialise the tool.
		/// Called by unity when the tool is created.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();

			// Load cursors.
			selectCursorOn = TextureUtils.LoadCursor("TC-CursorOn.png");
			selectCursorOff = TextureUtils.LoadCursor("TC-CursorOff.png");
			pickCursorOn = TextureUtils.LoadCursor("TC-CursorPickOn.png");
			pickCursorOff = TextureUtils.LoadCursor("TC-CursorPickOff.png");
			currentCursorOn = selectCursorOn;
			currentCursorOff = selectCursorOff;
			m_cursor = currentCursorOff;

			// Create new UUI button.
			UIComponent uuiButton = UUIHelpers.RegisterToolButton(
				name: nameof(TCTool),
				groupName: null, // default group
				tooltip: Translations.Translate("TFC_NAM"),
				tool: this,
				icon: UUIHelpers.LoadTexture(UUIHelpers.GetFullPath<TransferControllerMod>("Resources", "TC-UUI.png")),
				hotkeys: new UUIHotKeys { ActivationKey = ModSettings.UUIKey });
		}


		// Ignore nodes, citizens, disasters, districts, transport lines, and vehicles.
		public override NetNode.Flags GetNodeIgnoreFlags() => NetNode.Flags.All;
		public override CitizenInstance.Flags GetCitizenIgnoreFlags() => CitizenInstance.Flags.All;
		public override DisasterData.Flags GetDisasterIgnoreFlags() => DisasterData.Flags.All;
		public override District.Flags GetDistrictIgnoreFlags() => District.Flags.All;
		public override TransportLine.Flags GetTransportIgnoreFlags() => TransportLine.Flags.None;
		public override VehicleParked.Flags GetParkedVehicleIgnoreFlags() => VehicleParked.Flags.All;
		public override TreeInstance.Flags GetTreeIgnoreFlags() => TreeInstance.Flags.All;
		public override PropInstance.Flags GetPropIgnoreFlags() => PropInstance.Flags.All;
		public override Vehicle.Flags GetVehicleIgnoreFlags() => Vehicle.Flags.LeftHandDrive | Vehicle.Flags.Created | Vehicle.Flags.Deleted | Vehicle.Flags.Spawned | Vehicle.Flags.Inverted | Vehicle.Flags.TransferToTarget | Vehicle.Flags.TransferToSource | Vehicle.Flags.Emergency1 | Vehicle.Flags.Emergency2 | Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped | Vehicle.Flags.Leaving | Vehicle.Flags.Arriving | Vehicle.Flags.Reversed | Vehicle.Flags.TakingOff | Vehicle.Flags.Flying | Vehicle.Flags.Landing | Vehicle.Flags.WaitingSpace | Vehicle.Flags.WaitingCargo | Vehicle.Flags.GoingBack | Vehicle.Flags.WaitingTarget | Vehicle.Flags.Importing | Vehicle.Flags.Exporting | Vehicle.Flags.Parking | Vehicle.Flags.CustomName | Vehicle.Flags.OnGravel | Vehicle.Flags.WaitingLoading | Vehicle.Flags.Congestion | Vehicle.Flags.DummyTraffic | Vehicle.Flags.Underground | Vehicle.Flags.Transition | Vehicle.Flags.InsideBuilding;


		// Select all buildings.
		public override Building.Flags GetBuildingIgnoreFlags() => Building.Flags.None;


		/// <summary>
		/// Called by the game.  Sets which network segments are ignored by the tool (always returns all, i.e. no segments are selectable by the tool).
		/// </summary>
		/// <param name="nameOnly">Always set to false</param>
		/// <returns>NetSegment.Flags.All</returns>
		public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
		{
			nameOnly = false;
			return NetSegment.Flags.All;
		}


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
				m_ignoreNodeFlags = GetNodeIgnoreFlags(),
				m_ignoreSegmentFlags = GetSegmentIgnoreFlags(out input.m_segmentNameOnly),
				m_ignoreBuildingFlags = GetBuildingIgnoreFlags(),
				m_ignoreTreeFlags = GetTreeIgnoreFlags(),
				m_ignorePropFlags = GetPropIgnoreFlags(),
				m_ignoreVehicleFlags = GetVehicleIgnoreFlags(),
				m_ignoreParkedVehicleFlags = GetParkedVehicleIgnoreFlags(),
				m_ignoreCitizenFlags = GetCitizenIgnoreFlags(),
				m_ignoreTransportFlags = GetTransportIgnoreFlags(),
				m_ignoreDistrictFlags = GetDistrictIgnoreFlags(),
				m_ignoreParkFlags = GetParkIgnoreFlags(),
				m_ignoreDisasterFlags = GetDisasterIgnoreFlags(),
				m_transportTypes = GetTransportTypes()
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
						if (TransferDataUtils.BuildingEligibility(output.m_building, transfers))
						{
							// Building has eligible transfers - set hover, and set cursor to light.
							hoverInstance.Building = (ushort)output.m_building;
							m_cursor = currentCursorOn;
						}
						else
                        {
							// Ineligible building - set dark cursor.
							m_cursor = currentCursorOff;
						}
					}
					else
                    {
						// No hovered building - set dark cursor.
						m_cursor = currentCursorOff;
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
					m_cursor = currentCursorOff;
				}
			}
			else
			{
				// No valid mouse ray.
				output = default;
				errors = ToolErrors.RaycastFailed;
				m_cursor = currentCursorOff;
			}

			// Set mouse position and record errors.
			m_mousePosition = output.m_hitPos;
			m_selectErrors = errors;
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
		/// Called by game when overlay is to be rendered.
		/// </summary>
		/// <param name="cameraInfo">Current camera instance</param>
		public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
		{
			base.RenderOverlay(cameraInfo);

			// Local references.
			ToolManager toolManager = Singleton<ToolManager>.instance;
			Building[] buildingBuffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

			// Highlight linked buildings if in picking mode.
			if (pickMode)
			{
				// Linked building list.
				HashSet<uint> hashSet = BuildingControl.GetBuildings(transferBuildingTab.CurrentBuilding, transferBuildingTab.RecordNumber);
				if (hashSet != null && hashSet.Count > 0)
				{
					// Apply yellow overlay to each linked building.
					Color yellow = new Color(1f, 1f, 0f, 0.75f);
					foreach (uint building in hashSet)
					{
						BuildingTool.RenderOverlay(cameraInfo, ref buildingBuffer[building], yellow, yellow);
						toolManager.m_drawCallData.m_overlayCalls++;
					}
				}
			}
			else
			{
				// If not in buildng picker mode, highlight all buildings with Transfer Controller settings in magenta.
				Color magenta = new Color(1f, 0f, 1f, 0.75f);
				foreach (uint key in BuildingControl.buildingRecords.Keys)
				{
					// Skip any secondary records.
					if ((key & (BuildingControl.NextRecordMask << 24)) != 0)
					{
						continue;
					}

					// Apply overlay.
					ushort buildingID = (ushort)(key & 0x0000FFFF);
					BuildingTool.RenderOverlay(cameraInfo, ref buildingBuffer[buildingID], magenta, magenta);
					toolManager.m_drawCallData.m_overlayCalls++;
				}
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
				if (Instance.pickMode)
				{
					// Yes - clear pick mode.
					Instance.pickMode = false;
					Instance.transferBuildingTab = null;
				}

				// Activate default tool.
				ToolsModifierControl.SetTool<DefaultTool>();
			}
		}


		/// <summary>
		/// Sets the tool to pick mode (selecting buildings).
		/// </summary>
		internal void SetPickMode(TransferBuildingTab callingTab)
        {
			transferBuildingTab = callingTab;
			pickMode = true;
			currentCursorOn = pickCursorOn;
			currentCursorOff = pickCursorOff;
			m_cursor = currentCursorOff;
		}


		/// <summary>
		/// Clears pick mode.
		/// </summary>
		internal void ClearPickMode()
		{
			pickMode = false;
			transferBuildingTab = null;
			currentCursorOn = selectCursorOn;
			currentCursorOff = selectCursorOff;
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
		/// <param name="e">Event</param>
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
					UIInput.MouseUsed();

					// Are we in pick mode?
					if (pickMode)
					{
						// Yes - communicate selection back to requesting panel and clear pick mode.
						transferBuildingTab?.AddBuilding(building);
						ClearPickMode();
					}
					else
					{
						// Not in pick mode - create the info panel with the hovered building prefab.
						BuildingPanelManager.SetTarget(building);
					}
				}
			}

			// Check for copy key.
			if (ModSettings.keyCopy.IsPressed(e))
			{
				TransferStruct[] transfers = new TransferStruct[4];
				if (building != 0 && TransferDataUtils.BuildingEligibility(building, transfers))
				{
					CopyPaste.BuildingTemplate = building;
					CopyPaste.Transfers = transfers;
				}
			}
			// Check for paste key, if we've got a copied record.
			else if (CopyPaste.BuildingTemplate != 0 && ModSettings.keyPaste.IsPressed(e))
            {
				TransferStruct[] transfers = new TransferStruct[4];
				if (building != 0 && TransferDataUtils.BuildingEligibility(building, transfers) && CopyPaste.CanCopy(CopyPaste.BuildingTemplate, building))
                {
					if (!CopyPaste.CopyPolicyTo(building, transfers))
                    {
						Logging.Error("Error copying transfer settings to building ", building);
                    }
                }
            }

			// Right-click disables tool.
			if (e.type == EventType.MouseDown && e.button == 1)
			{
				// Cancel tool on right-click.
				ToggleTool();
			}
		}
	}
}
