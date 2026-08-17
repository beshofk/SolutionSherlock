using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock
{
    /// <summary>
    /// MEF entry point. XrmToolBox scans every assembly in its Plugins folder for a
    /// class exported as IXrmToolBoxPlugin and uses the ExportMetadata below to build
    /// the tool's tile in the plugin list. This class does no work itself beyond
    /// handing back the UserControl in GetControl().
    /// </summary>
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "SolutionSherlock"),
     ExportMetadata("Description", "Search across all solutions in an environment to find which ones contain a given entity, field, or other component."),
     // Replace with Base64 of a 32x32 PNG (see https://www.base64-image.de/) before distributing.
     ExportMetadata("SmallImageBase64", null),
     // Replace with Base64 of an 80x80 PNG before distributing.
     ExportMetadata("BigImageBase64", null),
     ExportMetadata("BackgroundColor", "White"),
     ExportMetadata("PrimaryFontColor", "Black"),
     ExportMetadata("SecondaryFontColor", "Gray")]
    public class SolutionSherlockPlugin : PluginBase
    {
        public override IXrmToolBoxPluginControl GetControl()
        {
            return new SolutionSherlockControl();
        }
    }
}
