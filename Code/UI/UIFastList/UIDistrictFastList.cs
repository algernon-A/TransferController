using ColossalFramework.UI;

namespace TransferController
{
    /// <summary>
    /// Fastlist for displaying districts.
    /// </summary>
    public class UIDistrictFastList: UIFastList
    {
        /// <summary>
        /// Use this to create the UIFastList.
        /// Do NOT use AddUIComponent.
        /// I had to do that way because MonoBehaviors classes cannot be generic
        /// </summary>
        /// <typeparam name="T">The type of the row UI component</typeparam>
        /// <typeparam name="U">The type of the UI FastList</typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static U Create<T, U>(UIComponent parent)
            where T : UIPanel, IUIFastListRow
            where U : UIFastList
        {
            UIFastList list = parent.AddUIComponent<U>();
            list.m_rowType = typeof(T);
            return (U)list;
        }

        /// <summary>
        /// Sets the selection to the given district ID.
        /// If no item is found, clears the selection and resets the list.
        /// </summary>
        /// <param name="districtID">DistrictID to find</param>
        public void FindDistrict(int districtID)
        {
            // Iterate through the rows list.
            for (int i = 0; i < m_rowsData.m_buffer.Length; ++i)
            {
                // Look for a match.
                if (m_rowsData.m_buffer[i] is byte thisDistrict && thisDistrict == districtID)
                {
                    // Found a match; set the selected index to this one.
                    selectedIndex = i;

                    // If the selected index is outside the current visibility range, move the to show it.
                    if (selectedIndex < listPosition || selectedIndex > listPosition + m_rows.m_size)
                    {
                        listPosition = selectedIndex;
                    }

                    // Done here; return.
                    return;
                }
            }

            // If we got here, we didn't find a match; clear the selection and reset the list position.
            selectedIndex = -1;
            listPosition = 0f;
            return;
        }
    }
}