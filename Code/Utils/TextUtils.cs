namespace TransferController
{
    /// <summary>
    /// Text-related utilities.
    /// </summary>
    internal static class TextUtils
    {
        /// <summary>
        /// Sanitises a raw prefab name for display.
        /// </summary>
        /// <param name="prefab">Original (raw) prefab</param>
        /// <returns>Cleaned display name</returns>
        internal static string GetDisplayName(PrefabInfo prefab)
        {
            // Null check.
            if (prefab?.name == null)
            {
                return "null";
            }

            // Otherwise, try getting any localized name, omit any package number, and trim off any trailing _Data.
            string localizedName = prefab.GetUncheckedLocalizedTitle();
            int index = localizedName.IndexOf('.');
            return localizedName.Substring(index + 1).Replace("_Data", "");
        }
    }
}