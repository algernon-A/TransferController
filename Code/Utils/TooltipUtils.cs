using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Static utilities class for managing custom tooltips.
    /// </summary>
    internal static class TooltipUtils
    {
        // Custom tooltip box.
        private static UILabel customTooltipBox;


        /// <summary>
        /// Custom tooltip box.
        /// </summary>
        internal static UILabel TooltipBox
        {
            get
            {
                if (customTooltipBox == null)
                {
                    customTooltipBox = CustomTooltipBox();
                }

                return customTooltipBox;
            }
        }


        /// <summary>
        /// Creates a custom tooltip box.
        /// </summary>
        /// <returns>New tooltip box</returns>
        private static UILabel CustomTooltipBox()
        {
            // Create GameObject and attach new UILabel.
            GameObject tooltipGameObject = new GameObject("RealPop2Tooltip");
            tooltipGameObject.transform.parent = UIView.Find("DefaultTooltip").gameObject.transform.parent;
            UILabel tipBox = tooltipGameObject.AddComponent<UILabel>();

            // Size.
            tipBox.autoSize = true;
            tipBox.minimumSize = new Vector2(500f, 12f);
            tipBox.wordWrap = true;

            // Mimic game's default tooltop.
            tipBox.padding = new RectOffset(23, 23, 5, 5);
            tipBox.verticalAlignment = UIVerticalAlignment.Middle;
            tipBox.pivot = UIPivotPoint.BottomLeft;
            tipBox.arbitraryPivotOffset = new Vector2(-3, 6);

            // Appearance.
            tipBox.backgroundSprite = "InfoDisplay";

            // Start hidden and off to the side.
            tipBox.transformPosition = new Vector2(-2f, -2f);
            tipBox.isVisible = false;

            return tipBox;
        }
    }
}