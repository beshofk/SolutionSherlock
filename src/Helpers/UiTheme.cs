using System.Drawing;
using System.Windows.Forms;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers
{
    /// <summary>
    /// Single source of truth for the plugin's modern color/typography system, shared
    /// by both tabs. Purely cosmetic (colors/fonts/borders) - never touches layout
    /// (Dock/Anchor/sizing) or event wiring, so it can be applied on top of the
    /// existing Designer-defined control tree without changing behavior.
    /// </summary>
    public static class UiTheme
    {
        // ---- Palette ----
        public static readonly Color Primary = Color.FromArgb(0, 99, 177);
        public static readonly Color PrimaryDark = Color.FromArgb(0, 78, 143);
        public static readonly Color PrimaryLight = Color.FromArgb(230, 240, 253);
        public static readonly Color Secondary = Color.FromArgb(98, 100, 167);

        public static readonly Color AppBackground = Color.FromArgb(247, 248, 250);
        public static readonly Color Surface = Color.White;
        public static readonly Color Border = Color.FromArgb(224, 227, 231);

        public static readonly Color TextPrimary = Color.FromArgb(31, 35, 40);
        public static readonly Color TextSecondary = Color.FromArgb(91, 100, 114);

        public static readonly Color Success = Color.FromArgb(23, 120, 62);
        public static readonly Color Warning = Color.FromArgb(158, 106, 3);
        public static readonly Color Error = Color.FromArgb(179, 38, 30);

        public static readonly Color RowAlternate = Color.FromArgb(249, 250, 251);
        public static readonly Color NestedGroupHeaderBackColor = Color.FromArgb(238, 241, 245);

        public static readonly Font SectionHeaderFont = new Font("Segoe UI Semibold", 9.75f);

        /// <summary>Applies consistent modern header/selection/gridline/alternating-row styling to a result grid without touching its columns, data source, or event handlers.</summary>
        public static void StyleGrid(DataGridView grid)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Surface;
            grid.GridColor = Border;
            grid.RowHeadersVisible = false;

            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 32;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AppBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(grid.Font, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppBackground;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

            grid.RowTemplate.Height = 24;
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Primary;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = RowAlternate;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Primary;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        }

        /// <summary>Styles a Button as the primary/high-emphasis action (solid accent fill, white text).</summary>
        public static void StylePrimaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = PrimaryDark;
            button.FlatAppearance.MouseDownBackColor = PrimaryDark;
            button.BackColor = Primary;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 9f);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        /// <summary>Styles a Button as a secondary/low-emphasis action (light surface, bordered).</summary>
        public static void StyleSecondaryButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.MouseOverBackColor = PrimaryLight;
            button.FlatAppearance.MouseDownBackColor = Border;
            button.BackColor = Surface;
            button.ForeColor = TextPrimary;
            button.Font = new Font("Segoe UI", 9f);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }

        /// <summary>Restyles a GroupBox's native caption as a bold accent-colored section title, without changing its layout, border, or contained controls.</summary>
        public static void StyleSectionHeader(GroupBox groupBox)
        {
            groupBox.ForeColor = Primary;
            groupBox.Font = SectionHeaderFont;
        }

        /// <summary>Styles a dock-panel title bar as a Visual Studio-style tool-window header: subtle accent edge, compact bold caption, and a flat unobtrusive collapse glyph button (never a large colored action button).</summary>
        public static void StyleDockHeader(Panel header, Panel accentEdge, Label title, Button collapseButton)
        {
            header.BackColor = NestedGroupHeaderBackColor;
            accentEdge.BackColor = Primary;

            title.Font = new Font("Segoe UI Semibold", 8.5f);
            title.ForeColor = TextPrimary;

            collapseButton.FlatStyle = FlatStyle.Flat;
            collapseButton.FlatAppearance.BorderSize = 0;
            collapseButton.FlatAppearance.MouseOverBackColor = Border;
            collapseButton.FlatAppearance.MouseDownBackColor = Border;
            collapseButton.BackColor = NestedGroupHeaderBackColor;
            collapseButton.ForeColor = TextSecondary;
            collapseButton.Font = new Font("Segoe UI", 9f);
            collapseButton.Cursor = Cursors.Hand;
            collapseButton.UseVisualStyleBackColor = false;
        }

        /// <summary>Styles the compact "restore" tab shown in place of a dock panel while it's collapsed (Visual Studio auto-hide-tab look).</summary>
        public static void StyleDockTab(Label tab)
        {
            tab.BackColor = NestedGroupHeaderBackColor;
            tab.ForeColor = TextSecondary;
            tab.Font = new Font("Segoe UI Semibold", 8.5f);
            tab.BorderStyle = BorderStyle.Fixed3D;
            tab.Cursor = Cursors.SizeWE;
        }

        /// <summary>Styles a dock header's pin toggle: a bold accent-filled chip when pinned (panel forced always-visible), a subdued outline look when unpinned.</summary>
        public static void StyleDockPinButton(Button pinButton, bool pinned)
        {
            pinButton.FlatStyle = FlatStyle.Flat;
            pinButton.FlatAppearance.BorderSize = 0;
            pinButton.Font = new Font("Segoe UI", 9f);
            pinButton.Cursor = Cursors.Hand;
            pinButton.UseVisualStyleBackColor = false;

            if (pinned)
            {
                pinButton.BackColor = Primary;
                pinButton.ForeColor = Color.White;
                pinButton.FlatAppearance.MouseOverBackColor = PrimaryDark;
                pinButton.FlatAppearance.MouseDownBackColor = PrimaryDark;
            }
            else
            {
                pinButton.BackColor = NestedGroupHeaderBackColor;
                pinButton.ForeColor = TextSecondary;
                pinButton.FlatAppearance.MouseOverBackColor = Border;
                pinButton.FlatAppearance.MouseDownBackColor = Border;
            }
        }

        /// <summary>Flat, low-noise ToolStrip renderer replacing the default gradient/3D system look.</summary>
        public static ToolStripRenderer CreateToolStripRenderer()
        {
            return new ToolStripProfessionalRenderer(new ModernColorTable());
        }

        private sealed class ModernColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => Surface;
            public override Color ToolStripGradientMiddle => Surface;
            public override Color ToolStripGradientEnd => Surface;
            public override Color ImageMarginGradientBegin => Surface;
            public override Color ImageMarginGradientMiddle => Surface;
            public override Color ImageMarginGradientEnd => Surface;
            public override Color MenuBorder => Border;
            public override Color MenuItemBorder => PrimaryLight;
            public override Color ButtonSelectedHighlight => PrimaryLight;
            public override Color ButtonSelectedBorder => Primary;
            public override Color ButtonPressedHighlight => PrimaryLight;
            public override Color MenuItemSelected => PrimaryLight;
            public override Color MenuItemSelectedGradientBegin => PrimaryLight;
            public override Color MenuItemSelectedGradientEnd => PrimaryLight;
            public override Color SeparatorDark => Border;
            public override Color SeparatorLight => Surface;
        }
    }
}
