using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>
    /// Column sizing for a result grid in AutoSizeColumnsMode.None: every column keeps its own width and the
    /// grid scrolls horizontally on overflow, but the rightmost column stretches to the grid's right edge
    /// whenever the columns would otherwise leave empty space. Only the last column is ever adjusted, and only
    /// by the space it auto-absorbed - the other columns and any width the user gave it are never touched.
    /// </summary>
    public sealed class GridColumnLayout
    {
        // Initial-layout limits (logical px): one long value can't make a column huge, and spare width
        // never stretches a column past twice its content width.
        private const int MaxContentWidth = 400;
        private const int MaxGrownWidth = 520;
        private const int ContentPadding = 12;
        // Above this many rows, measure only the rows on screen to keep a reset fast.
        private const int MaxRowsToMeasure = 1000;

        private readonly DataGridView _grid;
        private readonly Dictionary<string, int> _defaultWidths;
        private readonly HashSet<string> _growableColumns;
        private DataGridViewColumn _lastColumn;
        // Last column's own width (default or user-set), excluding space auto-added to reach the right edge.
        private int _lastColumnOwnWidth;
        private bool _adjusting;

        // Column whose right divider the user started dragging, for widening past the grid's right edge.
        private DataGridViewColumn _dragColumn;
        private int _dragStartX;
        private int _dragStartWidth;

        /// <summary>
        /// Call once the grid's columns are configured; their current widths are the fallback layout for an empty grid.
        /// <paramref name="growableColumns"/> (column names) share any spare width on a reset - typically the main name columns.
        /// </summary>
        public GridColumnLayout(DataGridView grid, params string[] growableColumns)
        {
            _grid = grid;
            _defaultWidths = grid.Columns.Cast<DataGridViewColumn>().ToDictionary(c => c.Name, c => c.Width);
            _growableColumns = new HashSet<string>(growableColumns);

            // DataGridView commits a header-drag resize on mouse-up, so this never fights an in-progress drag.
            grid.ColumnWidthChanged += (s, e) =>
            {
                if (_adjusting) return;
                if (e.Column == _lastColumn) _lastColumnOwnWidth = e.Column.Width;
                FitLastColumn();
            };
            grid.SizeChanged += (s, e) => FitLastColumn();
            grid.MouseDown += OnMouseDown;
            grid.MouseUp += OnMouseUp;

            // The vertical scrollbar appearing/disappearing changes the usable width without a SizeChanged;
            // deferred so the grid finishes its own layout pass first.
            var verticalScrollBar = grid.Controls.OfType<VScrollBar>().FirstOrDefault();
            if (verticalScrollBar != null)
                verticalScrollBar.VisibleChanged += (s, e) => { if (grid.IsHandleCreated) grid.BeginInvoke((Action)FitLastColumn); };
        }

        /// <summary>
        /// Initial layout for a newly loaded result set: each column sized to its content (header + cells, capped),
        /// spare width shared among the growable columns (capped), then the last column fills any remainder.
        /// </summary>
        public void ResetToDefaults()
        {
            var columns = _grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
            var widths = columns.ToDictionary(c => c, InitialWidth);

            var spare = _grid.DisplayRectangle.Width - widths.Values.Sum() - (_grid.RowHeadersVisible ? _grid.RowHeadersWidth : 0);
            var growable = columns.Where(c => _growableColumns.Contains(c.Name)).ToList();
            var growableTotal = growable.Sum(c => widths[c]);
            if (spare > 0 && growableTotal > 0)
            {
                foreach (var column in growable)
                {
                    var content = widths[column];
                    var limit = Math.Max(content, Math.Min(content * 2, _grid.LogicalToDeviceUnits(MaxGrownWidth)));
                    widths[column] = Math.Min(limit, content + (int)((long)spare * content / growableTotal));
                }
            }

            _adjusting = true;
            try
            {
                foreach (var pair in widths)
                    pair.Key.Width = pair.Value;
            }
            finally
            {
                _adjusting = false;
            }

            _lastColumn = null; // re-read the last column's own width from the restored default
            FitLastColumn();
        }

        private int InitialWidth(DataGridViewColumn column)
        {
            if (_grid.Rows.Count == 0)
                return _defaultWidths.TryGetValue(column.Name, out var fallback) ? fallback : column.Width;

            var mode = _grid.Rows.Count <= MaxRowsToMeasure
                ? DataGridViewAutoSizeColumnMode.AllCells
                : DataGridViewAutoSizeColumnMode.DisplayedCells;
            var content = column.GetPreferredWidth(mode, true) + _grid.LogicalToDeviceUnits(ContentPadding);
            return Math.Max(column.MinimumWidth, Math.Min(content, _grid.LogicalToDeviceUnits(MaxContentWidth)));
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _dragColumn = null;
            // DataGridView shows the SizeWE cursor exactly when the pointer is on a column divider it will resize.
            if (e.Button != MouseButtons.Left || e.Y >= _grid.ColumnHeadersHeight || _grid.Cursor != Cursors.SizeWE) return;

            foreach (DataGridViewColumn column in _grid.Columns)
            {
                if (!column.Displayed) continue;
                var right = _grid.GetColumnDisplayRectangle(column.Index, false).Right;
                if (e.X >= right - 6 && e.X <= right + 2)
                {
                    _dragColumn = column;
                    _dragStartX = e.X;
                    _dragStartWidth = column.Width;
                    return;
                }
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            var column = _dragColumn;
            _dragColumn = null;
            if (column == null || e.X <= _grid.DisplayRectangle.Right) return;

            // DataGridView stops a divider drag at the visible edge; honour the full distance the pointer travelled.
            // Deferred so the grid commits its own (clamped) width first.
            var desired = _dragStartWidth + (e.X - _dragStartX);
            _grid.BeginInvoke((Action)(() =>
            {
                if (!_grid.IsDisposed && column.DataGridView == _grid && desired > column.Width) column.Width = desired;
            }));
        }

        private void FitLastColumn()
        {
            if (_adjusting || _grid.IsDisposed) return;

            var last = _grid.Columns.GetLastColumn(DataGridViewElementStates.Visible, DataGridViewElementStates.None);
            if (last == null) return;
            if (last != _lastColumn)
            {
                _lastColumn = last;
                _lastColumnOwnWidth = last.Width;
            }

            var othersWidth = _grid.RowHeadersVisible ? _grid.RowHeadersWidth : 0;
            foreach (DataGridViewColumn column in _grid.Columns)
                if (column.Visible && column != last) othersWidth += column.Width;

            var target = Math.Max(_lastColumnOwnWidth, _grid.DisplayRectangle.Width - othersWidth);
            if (last.Width == target) return;

            _adjusting = true;
            try { last.Width = target; }
            finally { _adjusting = false; }
        }
    }
}
