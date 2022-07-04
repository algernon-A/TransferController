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
        /// True if this is an incoming transfer, false if outgoing.
        /// </summary>
        internal bool IsIncoming { get; set; }


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
        /// Refreshes the controls with current data.
        /// </summary>
        protected abstract void Refresh();
    }
}