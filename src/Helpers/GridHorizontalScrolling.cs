using System;
using System.Windows.Forms;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>
    /// Horizontal mouse/touchpad scrolling for a DataGridView: tilt wheel and two-finger horizontal swipes
    /// (WM_MOUSEHWHEEL, which WinForms on .NET Framework ignores) plus Shift+wheel. Moves only the grid's own
    /// HorizontalScrollingOffset; every other message, including plain vertical wheel input, is passed through untouched.
    /// </summary>
    public sealed class GridHorizontalScrolling : NativeWindow
    {
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int MK_SHIFT = 0x0004;
        private const int WHEEL_DELTA = 120;
        // Logical px per wheel notch; touchpads send fractions of a notch per message.
        private const int PixelsPerNotch = 48;

        private readonly DataGridView _grid;

        public static void Enable(DataGridView grid) => new GridHorizontalScrolling(grid);

        private GridHorizontalScrolling(DataGridView grid)
        {
            _grid = grid;
            if (grid.IsHandleCreated) AssignHandle(grid.Handle);
            grid.HandleCreated += (s, e) => AssignHandle(_grid.Handle);
            grid.HandleDestroyed += (s, e) => ReleaseHandle();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEHWHEEL)
            {
                // Positive delta = tilt/swipe right.
                ScrollBy(WheelDelta(m.WParam));
                m.Result = IntPtr.Zero;
                return;
            }

            // Shift+wheel scrolls horizontally only when there is something to scroll; otherwise native handling applies.
            if (m.Msg == WM_MOUSEWHEEL && (LowWord(m.WParam) & MK_SHIFT) != 0 && ScrollBy(-WheelDelta(m.WParam)))
            {
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>Scrolls horizontally, clamped to the grid's scroll range. Returns false when the columns fit (nothing to scroll).</summary>
        private bool ScrollBy(int wheelDelta)
        {
            var columnsWidth = _grid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            var viewportWidth = _grid.DisplayRectangle.Width - (_grid.RowHeadersVisible ? _grid.RowHeadersWidth : 0);
            var maxOffset = columnsWidth - viewportWidth;
            if (maxOffset <= 0) return false;

            var step = (int)Math.Round(wheelDelta * (double)_grid.LogicalToDeviceUnits(PixelsPerNotch) / WHEEL_DELTA);
            if (step == 0) step = Math.Sign(wheelDelta);

            var target = Math.Max(0, Math.Min(maxOffset, _grid.HorizontalScrollingOffset + step));
            if (target != _grid.HorizontalScrollingOffset)
                _grid.HorizontalScrollingOffset = target;
            return true;
        }

        private static int WheelDelta(IntPtr wParam) => (short)(((long)wParam >> 16) & 0xFFFF);

        private static int LowWord(IntPtr wParam) => (int)((long)wParam & 0xFFFF);
    }
}
