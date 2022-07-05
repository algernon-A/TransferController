using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework.UI;



namespace TransferController
{
    /// <summary>
    /// Selected vehicle panel.
    /// </summary>
    internal class SelectedVehiclePanel : VehicleSelectionPanel
    {
        // Panel to display when no item is selected.
        private UIPanel randomPanel;


        /// <summary>
        /// Constructor - create random panel.
        /// </summary>
        internal SelectedVehiclePanel()
        {
            // Panel setup.
            randomPanel = this.AddUIComponent<UIPanel>();
            randomPanel.width = this.width;
            randomPanel.height = this.height;
            randomPanel.relativePosition = new Vector2(0f, 0f);

            // Random sprite.
            UISprite randomSprite = randomPanel.AddUIComponent<UISprite>();
            randomSprite.atlas = TextureUtils.InGameAtlas;
            randomSprite.spriteName = "Random";

            // Label.
            UILabel randomLabel = randomSprite.AddUIComponent<UILabel>();
            randomLabel.textScale = 0.8f;
            randomLabel.text = Translations.Translate("TFC_VEH_ANY");

            //  Size is 56x33, so offset -8 from left and 3.5 from top to match normal row sizing.
            randomSprite.size = new Vector2(56f, 33f);
            randomSprite.relativePosition = new Vector2(-8, (40f - randomSprite.height) / 2f);
            randomLabel.relativePosition = new Vector2(48f, (randomSprite.height - randomLabel.height) / 2f);
        }


        /// <summary>
        /// Populates the list.
        /// </summary>
        protected override void PopulateList()
        {
            List<VehicleItem> items = new List<VehicleItem>();
            List<VehicleInfo> buildingVehicles = VehicleControl.GetVehicles(ParentPanel.CurrentBuilding, ParentPanel.TransferReason);
            if (buildingVehicles != null)
            {
                foreach (VehicleInfo vehicle in buildingVehicles)
                {
                    items.Add(new VehicleItem(vehicle));
                }
            }

            // If list is empty, show random item panel (and hide otherwise).
            randomPanel.isVisible = items.Count == 0;

            vehicleList.rowsData = new FastList<object>
            {
                m_buffer = items.OrderBy(x => x.name).ToArray(),
                m_size = items.Count
            };
        }
    }
}