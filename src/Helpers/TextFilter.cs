using System;
using System.Linq;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>Match rule shared by the client-side grid search boxes (Solutions and Search Results).</summary>
    public static class TextFilter
    {
        /// <summary>Case-insensitive "contains" match of the trimmed search text against any of the values; blank search text matches everything.</summary>
        public static bool Matches(string searchText, params string[] values)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return true;
            var text = searchText.Trim();
            return values.Any(v => v?.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
