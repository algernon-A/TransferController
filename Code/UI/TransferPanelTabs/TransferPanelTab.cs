using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    internal abstract class TransferPanelTab
    {
        // Layout constants.
        protected const float Margin = 5f;
        protected const float CheckMargin = 20f;
        protected const float CheckHeight = 20f;
        protected const float ButtonHeight = 28f;
        protected const float EnabledCheckY = Margin;
        protected const float SameDistrictCheckY = EnabledCheckY + CheckHeight;
        protected const float OutsideCheckY = SameDistrictCheckY + CheckHeight;
        protected const float DistrictTitleY = OutsideCheckY + CheckHeight;
        protected const float DistrictListY = DistrictTitleY + CheckHeight;
        internal const float ListHeight = 8f * DistrictRow.RowHeight;
        internal const float ColumnWidth = 210f;
        protected const float ArrowSize = 32f;
        protected const float MidControlX = Margin + ColumnWidth + Margin;
        protected const float RightColumnX = MidControlX + ArrowSize + Margin;
        internal const float PanelWidth = RightColumnX + ColumnWidth + Margin;
        internal const float PanelHeight = DistrictListY + ListHeight + Margin;




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
        protected UIButton AddIconButton(UIComponent parent, float xPos, float yPos, float size, string tooltipKey, UITextureAtlas atlas)
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
    }
}