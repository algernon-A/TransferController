using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Building panel tab panel.
    /// </summary>
    internal abstract class BuildingPanelTab
    {
        // Layout constants.
        protected const float Margin = 5f;
        protected const float CheckMargin = 20f;
        protected const float CheckHeight = 20f;
        protected const float ButtonHeight = 28f;
        internal const float ColumnWidth = 210f;
        internal const float ArrowSize = 32f;
        internal const float MidControlX = Margin + ColumnWidth + Margin;
        internal const float RightColumnX = MidControlX + ArrowSize + Margin;
        internal const float BuildingColumnWidth = ColumnWidth * 2f;
        internal const float PanelWidth = RightColumnX + ColumnWidth + Margin;

        // Current selection.
        private ushort currentBuilding;


        /// <summary>
        /// Current record number.
        /// </summary>
        internal byte RecordNumber { get; set; }


        /// <summary>
        /// Other record flag.
        /// </summary>
        internal byte NextRecord { get; set; }


        /// <summary>
        /// Transfer reason.
        /// </summary>
        internal TransferManager.TransferReason TransferReason { get; set; }


        /// <summary>
        /// Currently selected building.
        /// </summary>
        internal ushort CurrentBuilding
        {
            get => currentBuilding;

            set
            {
                currentBuilding = value;
                Refresh();
            }
        }


        /// <summary>
        /// Adds an icon-style button to the specified component at the specified coordinates.
        /// </summary>
        /// <param name="parent">Parent UIComponent</param>
        /// <param name="xPos">Relative X position</param>
        /// <param name="yPos">Relative Y position</param>
        /// <param name="size">Button size (square)</param>
        /// <param name="tooltipKey">Tooltip translation key</param>
        /// <param name="atlas">Icon atlas</param>
        /// <returns>New UIButton</returns>
        internal static UIButton AddIconButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey, UITextureAtlas atlas)
        {
            UIButton newButton = parent.AddUIComponent<UIButton>();

            // Size and position.
            newButton.relativePosition = new Vector2(xPos, yPos);
            newButton.height = size;
            newButton.width = size;

            // Appearance.
            newButton.atlas = atlas;
            newButton.normalFgSprite = "normal";
            newButton.focusedFgSprite = "normal";
            newButton.hoveredFgSprite = "hovered";
            newButton.disabledFgSprite = "disabled";
            newButton.pressedFgSprite = "pressed";

            // Tooltip.
            newButton.tooltip = Translations.Translate(tooltipKey);

            return newButton;
        }


        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected abstract void Refresh();
    }
}