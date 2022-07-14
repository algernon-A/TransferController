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
		/// Adds an zoom icon button.
		/// </summary>
		/// <param name="parent">Parent UIComponent</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="tooltipKey">Tooltip translation key</param>
		/// <returns>New UIButton</returns>
		protected UIButton AddZoomButton(UIComponent parent, float xPos, string tooltipKey) => BuildingPanel.AddZoomButton(parent, xPos, (RowHeight - ButtonSize) / 2f, ButtonSize, tooltipKey);
	}
}