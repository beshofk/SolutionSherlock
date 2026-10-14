using System.Linq;
using System.Windows.Forms;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>
    /// Adds cell-level copy to a FullRowSelect grid without changing its selection model:
    /// Ctrl+C copies the focused cell's full (untruncated) value, and "Copy Cell"/"Copy Row"
    /// context-menu items act on the right-clicked cell.
    /// </summary>
    public static class GridClipboard
    {
        public static void Enable(DataGridView grid, ContextMenuStrip menu)
        {
            DataGridViewCell target = null;
            var openedByMouse = false;

            var copyCell = new ToolStripMenuItem("Copy Cell") { ShortcutKeyDisplayString = "Ctrl+C" };
            var copyRow = new ToolStripMenuItem("Copy Row");
            if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(copyCell);
            menu.Items.Add(copyRow);
            grid.ContextMenuStrip = menu;

            grid.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                var hit = grid.HitTest(e.X, e.Y);
                target = hit.Type == DataGridViewHitTestType.Cell ? grid[hit.ColumnIndex, hit.RowIndex] : null;
                openedByMouse = true;
            };
            menu.Opening += (s, e) =>
            {
                // Keyboard-opened menu (Apps key / Shift+F10) has no clicked cell, so it acts on the focused one.
                if (!openedByMouse) target = grid.CurrentCell;
                openedByMouse = false;
                copyCell.Enabled = copyRow.Enabled = target != null;
            };
            copyCell.Click += (s, e) => { if (target?.DataGridView != null) SetClipboard(CellText(target)); };
            copyRow.Click += (s, e) => { if (target?.DataGridView != null) SetClipboard(RowText(target.OwningRow)); };

            grid.KeyDown += (s, e) =>
            {
                if (e.KeyData != (Keys.Control | Keys.C)) return;
                var cell = grid.CurrentCell;
                // Focused cell not in the highlighted row: fall through to the grid's native selected-row copy.
                if (cell == null || !cell.Selected) return;
                SetClipboard(CellText(cell));
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
        }

        // Trim drops the indentation the Components grid adds to show grouping.
        private static string CellText(DataGridViewCell cell) => cell.FormattedValue?.ToString().Trim() ?? string.Empty;

        private static string RowText(DataGridViewRow row)
        {
            // Button/link columns only show fixed caption text ("Actions", "Open solution"), not data.
            return string.Join("\t", row.Cells.Cast<DataGridViewCell>()
                .Where(c => c.OwningColumn.Visible && !(c is DataGridViewButtonCell) && !(c is DataGridViewLinkCell))
                .OrderBy(c => c.OwningColumn.DisplayIndex)
                .Select(CellText));
        }

        private static void SetClipboard(string text)
        {
            if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
        }
    }
}
