using Microsoft.Xrm.Sdk.Metadata;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>
    /// EntityMetadata.DisplayName / AttributeMetadata.DisplayName / EntityKeyMetadata.DisplayName
    /// are Label objects (localizable, can be null for some system components) - these
    /// helpers collapse that down to a single display string with a safe fallback to
    /// the logical name.
    /// </summary>
    public static class MetadataExtensions
    {
        public static string GetDisplayLabel(this EntityMetadata metadata)
        {
            return metadata?.DisplayName?.UserLocalizedLabel?.Label ?? metadata?.LogicalName;
        }

        public static string GetDisplayLabel(this AttributeMetadata metadata)
        {
            return metadata?.DisplayName?.UserLocalizedLabel?.Label ?? metadata?.LogicalName;
        }

        public static string GetDisplayLabel(this EntityKeyMetadata metadata)
        {
            return metadata?.DisplayName?.UserLocalizedLabel?.Label ?? metadata?.LogicalName;
        }
    }
}
