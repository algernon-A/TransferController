using System;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Transfer panel (setting restrictions for the given transfer).
    /// </summary>
    internal class TransferOutsideTab : TransferPanelTab
    {
        // Panel components.
        private readonly UICheckBox outsideCheck;

        // Status flags.
        private bool disableEvents = false;


        /// <summary>
        /// Sets the outside connection checkbox label text.
        /// </summary>
        internal string OutsideLabel
        {
            set
            {
                // Show button if text isn't null.
                if (value != null)
                {
                    outsideCheck.text = value;
                    outsideCheck.Show();
                }
                else
                {
                    // No value - hide checkbox.
                    outsideCheck.Hide();
                }
            }
        }


        /// <summary>
        /// Sets the outside connection checkbox tooltip text.
        /// </summary>
        internal string OutsideTip { set => outsideCheck.tooltip = value; }


        /// <summary>
        /// Constructor - performs initial setup.
        /// </summary>
        /// <param name="parentPanel">Containing UI panel</param>
        internal TransferOutsideTab(UIPanel parentPanel)
        {
            try
            {

                // Outside connection checkbox.
                // Note state is inverted - underlying flag is restrictive, but checkbox is permissive.
                outsideCheck = UIControls.LabelledCheckBox(parentPanel, CheckMargin, EnabledCheckY, Translations.Translate("TFC_BLD_IMP"), tooltip: string.Empty);
                outsideCheck.isChecked = !OutsideConnection;
                outsideCheck.eventCheckChanged += (control, isChecked) =>
                {
                    if (!disableEvents)
                    {
                        OutsideConnection = !isChecked;
                    }
                };
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception setting up TransferOutsideTab");
            }
        }


        /// <summary>
        /// Refreshes the controls with current data.
        /// </summary>
        protected override void Refresh()
        {
            // Disable events while we update controls to avoid recursively triggering event handler.
            disableEvents = true;
            outsideCheck.isChecked = !OutsideConnection;
            disableEvents = false;
        }


        /// <summary>
        /// Outside connection setting.
        /// </summary>
        private bool OutsideConnection
        {
            get => BuildingControl.GetOutsideConnection(CurrentBuilding, RecordNumber);
            set => BuildingControl.SetOutsideConnection(CurrentBuilding, RecordNumber, value, TransferReason, NextRecord);
        }
    }
}