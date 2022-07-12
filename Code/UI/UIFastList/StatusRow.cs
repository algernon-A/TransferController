using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
	/// <summary>
	/// Base row for status row.
	/// </summary>
	public abstract class StatusRow : UIBasicRow
	{
		// Layout constants.
		public const float RowHeight = 20f;
		internal const float DirectionX = Margin;
		internal const float ReasonX = DirectionX + DirectionWidth + Margin;
		internal const float PriorityX = ReasonX + ReasonWidth + Margin;
		internal const float ReasonWidth = 120f;
		internal const float DirectionWidth = 30f;
		internal const float PriorityWidth = 20f;
		protected const float ButtonSize = 16f;


		// Target IDs.
		protected ushort buildingID;


		/// <summary>
		/// Constructor.
		/// </summary>
		public StatusRow()
		{
			rowHeight = RowHeight;
		}


		/// <summary>
		/// Zoom to building button event handler.
		/// </summary>
		protected void ZoomToBuilding()
		{
			// Go to target building if available.
			if (buildingID != 0)
			{
				InstanceID instance = default;
				instance.Building = buildingID;
				ToolsModifierControl.cameraController.SetTarget(instance, Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID].m_position, zoomIn: true);
			}
		}


		/// <summary>
		/// Adds an zoom icon button.
		/// </summary>
		/// <param name="parent">Parent UIComponent</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="tooltipKey">Tooltip translation key</param>
		/// <returns>New UIButton</returns>
		protected UIButton AddZoomButton(UIComponent parent, float xPos, string tooltipKey)
		{
			UIButton newButton = parent.AddUIComponent<UIButton>();

			// Size and position.
			newButton.relativePosition = new Vector2(xPos, (RowHeight - ButtonSize) / 2f);
			newButton.height = ButtonSize;
			newButton.width = ButtonSize;

			// Appearance.
			newButton.atlas = TextureUtils.InGameAtlas;
			newButton.normalFgSprite = "LineDetailButton";
			newButton.focusedFgSprite = "LineDetailButtonFocused";
			newButton.hoveredFgSprite = "LineDetailButtonHovered";
			newButton.disabledFgSprite = "LineDetailButtonDisabled";
			newButton.pressedFgSprite = "LineDetailButtonPressed";

			// Tooltip.
			newButton.tooltip = Translations.Translate(tooltipKey);

			return newButton;
		}
	}
}