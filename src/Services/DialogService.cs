using System.IO;
using System.Windows.Forms;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Interfaces;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Services
{
    /// <summary>Thin wrapper over WinForms dialogs so they can be mocked in tests.</summary>
    public class DialogService : IDialogService
    {
        public string ShowSaveFileDialog(IWin32Window owner, string filter, string suggestedFileName, string initialDirectory)
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = suggestedFileName,
                // OverwritePrompt defaults to true, but set explicitly so the
                // "confirm overwrite" requirement is visible in code, not just relied
                // on as a default that could silently change.
                OverwritePrompt = true,
                AddExtension = true
            })
            {
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                    dialog.InitialDirectory = initialDirectory;

                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
            }
        }

        public void ShowError(IWin32Window owner, string title, string message) =>
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        public void ShowInfo(IWin32Window owner, string title, string message) =>
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
