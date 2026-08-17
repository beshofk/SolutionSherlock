using System.Windows.Forms;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces
{
    /// <summary>
    /// Abstracts WinForms dialog interaction (SaveFileDialog, MessageBox) behind an
    /// interface, since these are otherwise very hard to unit test directly - a test
    /// double can return a canned path/response without a real dialog appearing.
    /// Used by the new export flow only; existing code elsewhere in this project
    /// still calls MessageBox.Show directly, to avoid touching working, already-
    /// verified UI code for this change.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Returns the chosen path, or null if the user cancelled.</summary>
        string ShowSaveFileDialog(IWin32Window owner, string filter, string suggestedFileName, string initialDirectory);

        void ShowError(IWin32Window owner, string title, string message);
        void ShowInfo(IWin32Window owner, string title, string message);
    }
}
