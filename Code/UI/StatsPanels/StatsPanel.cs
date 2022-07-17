using ColossalFramework;
using ColossalFramework.UI;


namespace TransferController
{
    /// <summary>
    /// Building stats detail panel.
    /// </summary>
    internal class StatsPanel : UIPanel
    {
        // Layout constants.
        internal const float PanelWidth = StatLabelX + AmountLabelX + AmountLabelWidth + Margin;
        internal const float PanelHeight = (RowHeight * 6f) + Margin;
        private const float Margin = 5f;
        private const float StatLabelX = Margin;
        private const float StatLabelWidth = 150f;
        private const float AmountLabelX = StatLabelWidth + Margin;
        private const float AmountLabelWidth = 100f;
        private const float RowHeight = 15f;


        // Status index enum.
        private enum StatusIndex : int
        {
            Garbage = 0,
            Mail,
            Crime,
            Fire,
            Sick,
            Dead,
            NumLabels
        }

        // Status index titles.
        private readonly string[] statusTitleKeys = new string[(int)StatusIndex.NumLabels]
        {
            "TFC_BST_GAR",
            "TFC_BST_MAI",
            "TFC_BST_CRI",
            "TFC_BST_FIR",
            "TFC_BST_SIC",
            "TFC_BST_DEA"
        };


        // Panel components.
        private UILabel[] titleLabels = new UILabel[(int)StatusIndex.NumLabels];
        private UILabel[] amountLabels = new UILabel[(int)StatusIndex.NumLabels];


        /// <summary>
        /// Constructor.
        /// </summary>
        internal StatsPanel()
        {
            autoSize = false;
            autoLayout = false;
            height = PanelHeight;
            width = PanelWidth;
            backgroundSprite = "UnlockingPanel";

            // Stats labels.
            for (int i = 0; i < (int)StatusIndex.NumLabels; ++i)
            {
                titleLabels[i] = UIControls.AddLabel(this, Margin, Margin + (i * RowHeight), Translations.Translate(statusTitleKeys[i]), StatLabelWidth, 0.7f);
                amountLabels[i] = UIControls.AddLabel(titleLabels[i], AmountLabelX, 0f, string.Empty, AmountLabelWidth, 0.7f);
                amountLabels[i].textAlignment = UIHorizontalAlignment.Right;
            }
        }


        /// <summary>
        /// Updates panel content.
        /// </summary>
        internal void UpdateContent(ushort buildingID)
        {
            // Local reference.
            ref Building building = ref Singleton<BuildingManager>.instance.m_buildings.m_buffer[buildingID];

            // Update each label.
            amountLabels[(int)StatusIndex.Mail].text = building.m_mailBuffer.ToString("N0");
            amountLabels[(int)StatusIndex.Garbage].text = building.m_garbageBuffer.ToString("N0");
            amountLabels[(int)StatusIndex.Crime].text = building.m_crimeBuffer.ToString();
            amountLabels[(int)StatusIndex.Fire].text = building.m_fireIntensity.ToString();
            amountLabels[(int)StatusIndex.Sick].text = CheckCitizens(ref building, Citizen.Flags.Sick).ToString();
            amountLabels[(int)StatusIndex.Dead].text = CheckCitizens(ref building, Citizen.Flags.Dead).ToString();
        }


        /// <summary>
        /// Gets the number of citizens in this building with (any of) the specified citizen flags.
        /// </summary>
        /// <param name="building">Building data record</param>
        /// <param name="flags">Citizen flags to check</param>
        /// <returns>Count of citzens in this building matching any of the specified flags</returns>
        private int CheckCitizens(ref Building building, Citizen.Flags flags)
        {
            int citizenCount = 0;

            // Local references.
            Citizen[] citizens = Singleton<CitizenManager>.instance.m_citizens.m_buffer;
            CitizenUnit[] citizenUnits = Singleton<CitizenManager>.instance.m_units.m_buffer;

            // Iterate through each unit in building.
            uint citizenUnit = building.m_citizenUnits;
            while (citizenUnit != 0)
            {
                // Check each (potential) citizen in unit and increment counter if flags match.
                citizenCount += CheckCitizen(citizens, citizenUnits[citizenUnit].m_citizen0, flags);
                citizenCount += CheckCitizen(citizens, citizenUnits[citizenUnit].m_citizen1, flags);
                citizenCount += CheckCitizen(citizens, citizenUnits[citizenUnit].m_citizen2, flags);
                citizenCount += CheckCitizen(citizens, citizenUnits[citizenUnit].m_citizen3, flags);
                citizenCount += CheckCitizen(citizens, citizenUnits[citizenUnit].m_citizen4, flags);

                // Move on to next unit in building.
                citizenUnit = citizenUnits[citizenUnit].m_nextUnit;
            }

            return citizenCount;
        }


        /// <summary>
        /// Checks the given citizen to see if they match any of the provided flags.
        /// </summary>
        /// <param name="citizens">Citizen data buffer instance</param>
        /// <param name="citizenID">Citizen ID</param>
        /// <param name="flags"></param>
        /// <returns>1 if the citizen matches any of the given flags, 0 otherwise</returns>
        private int CheckCitizen(Citizen[] citizens, uint citizenID, Citizen.Flags flags)
        {
            // Ensure valid citizen ID.
            if (citizenID != 0)
            {
                // Check for any matching flags.
                if ((citizens[citizenID].m_flags & flags) != 0)
                {
                    // Flag(s) match - return 1.
                    return 1;
                }
            }

            // If we got here, no match was found - return 0.
            return 0;
        }
    }
}