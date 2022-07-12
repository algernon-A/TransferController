using UnityEngine;
using ColossalFramework.UI;


namespace TransferController
{

    /// <summary>
    /// An individual fastlist row.
    /// </summary>
    public abstract class UIBasicRow : UIPanel, IUIFastListRow
    {
        // Layout constants.
        public const float DefaultRowHeight = 20f;
        protected const float Margin = 5f;

        // Row height.
        public float rowHeight = DefaultRowHeight;

        // Panel components.
        protected UIPanel panelBackground;


        // Background for each list item.
        public virtual UIPanel Background
        {
            get
            {
                if (panelBackground == null)
                {
                    panelBackground = AddUIComponent<UIPanel>();
                    panelBackground.width = width;
                    panelBackground.height = rowHeight;
                    panelBackground.relativePosition = Vector2.zero;

                    panelBackground.zOrder = 0;
                }

                return panelBackground;
            }
        }


        /// <summary>
        /// Called when dimensions are changed, including as part of initial setup (required to set correct relative position of label).
        /// </summary>
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Background.width = width;
        }


        /// <summary>
        /// Mouse click event handler - updates the selection to what was clicked.
        /// </summary>
        /// <param name="p">Mouse event parameter</param>
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            Selected();
        }


        /// <summary>
        /// Adds a text label to the current UIComponent.
        /// </summary>
        /// <param name="xPos">Label relative x-position</param>
        /// <param name="width">Label width</param>
        /// <param name="textScale">Text scale</param>
        /// <param name="wordWrap">Wordwrap status (true to enable, false to disable)</param>
        /// <returns>New UILabel</returns>
        protected UILabel AddLabel(float xPos, float width, float textScale = 0.8f, bool wordWrap = false)
        {
            UILabel newLabel = AddUIComponent<UILabel>();
            newLabel.autoSize = false;
            newLabel.height = rowHeight;
            newLabel.width = width;
            newLabel.verticalAlignment = UIVerticalAlignment.Middle;
            newLabel.clipChildren = true;
            newLabel.wordWrap = wordWrap;
            newLabel.padding.top = 1;
            newLabel.textScale = textScale;
            newLabel.font = FontUtils.Regular;
            newLabel.relativePosition = new Vector2(xPos, 0f);
            return newLabel;
        }


        /// <summary>
        /// Performs actions when this item is selected.
        /// </summary>
        protected virtual void Selected() {}


        /// <summary>
        /// Generates and displays a row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public abstract void Display(object data, bool isRowOdd);


        /// <summary>
        /// Highlights the selected row.
        /// </summary>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Select(bool isRowOdd)
        {
            Background.backgroundSprite = "ListItemHighlight";
            Background.color = new Color32(255, 255, 255, 255);
        }


        /// <summary>
        /// Unhighlights the (un)selected row.
        /// </summary>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Deselect(bool isRowOdd)
        {
            if (isRowOdd)
            {
                // Lighter background for odd rows.
                Background.backgroundSprite = "UnlockingItemBackground";
                Background.color = new Color32(0, 0, 0, 128);
            }
            else
            {
                // Darker background for even rows.
                Background.backgroundSprite = null;
            }
        }
    }
}