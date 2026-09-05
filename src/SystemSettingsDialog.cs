using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Helpers;
using BeshoyFanous.XrmToolBox.SolutionSherlock.Models;

namespace BeshoyFanous.XrmToolBox.SolutionSherlock
{
    /// <summary>
    /// Modal "Export System Settings (Advanced)" configuration dialog. Operates on
    /// a working COPY of the caller's <see cref="SystemSettingsSelection"/> so
    /// Cancel discards changes and Apply is the only path that commits back. Rows
    /// are generated from <see cref="SystemSettingsSelection.Definitions"/> - the
    /// single source of truth for the nine supported settings - so adding a new
    /// setting only requires updating that Definitions array.
    /// </summary>
    internal sealed class SystemSettingsDialog : Form
    {
        private readonly SystemSettingsSelection _workingCopy;
        private readonly List<(SystemSettingDefinition Definition, CheckBox CheckBox)> _bindings
            = new List<(SystemSettingDefinition, CheckBox)>();

        /// <summary>The Apply'd result. Null until the user clicks Apply.</summary>
        public SystemSettingsSelection Result { get; private set; }

        public SystemSettingsDialog(SystemSettingsSelection currentSelection)
        {
            _workingCopy = (currentSelection ?? new SystemSettingsSelection()).Clone();

            Text = "Export System Settings (Advanced)";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(420, 420);
            Padding = new Padding(16);
            BackColor = UiTheme.Surface;
            Font = new Font("Segoe UI", 9f);

            BuildLayout();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = UiTheme.Surface,
                AutoSize = false
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblIntro = new Label
            {
                AutoSize = true,
                Text = "Select the system settings you want to include:",
                ForeColor = UiTheme.TextPrimary,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 0, 0, 8)
            };
            root.Controls.Add(lblIntro, 0, 0);

            var pnlChecks = BuildCheckboxPanel();
            root.Controls.Add(pnlChecks, 0, 1);

            var pnlSelectionButtons = BuildSelectionButtonsPanel();
            root.Controls.Add(pnlSelectionButtons, 0, 2);

            var pnlDialogButtons = BuildDialogButtonsPanel();
            root.Controls.Add(pnlDialogButtons, 0, 3);

            Controls.Add(root);
        }

        private FlowLayoutPanel BuildCheckboxPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = UiTheme.Surface,
                Padding = new Padding(4, 4, 4, 8)
            };

            foreach (var definition in SystemSettingsSelection.Definitions)
            {
                var chk = new CheckBox
                {
                    Text = definition.Label,
                    AutoSize = true,
                    Checked = definition.Getter(_workingCopy),
                    ForeColor = UiTheme.TextPrimary,
                    Margin = new Padding(0, 2, 0, 2),
                    UseVisualStyleBackColor = true
                };

                // Local copy so each closure keeps its own definition reference.
                var def = definition;
                chk.CheckedChanged += (s, e) => def.Setter(_workingCopy, chk.Checked);

                panel.Controls.Add(chk);
                _bindings.Add((definition, chk));
            }

            return panel;
        }

        private FlowLayoutPanel BuildSelectionButtonsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = UiTheme.Surface,
                Padding = new Padding(0, 8, 0, 8),
                Margin = new Padding(0),
            };

            var btnSelectAll = new Button { Text = "Select All", AutoSize = true, MinimumSize = new Size(90, 28), Margin = new Padding(0, 0, 8, 0) };
            var btnClearAll = new Button { Text = "Clear All",  AutoSize = true, MinimumSize = new Size(90, 28), Margin = new Padding(0) };

            UiTheme.StyleSecondaryButton(btnSelectAll);
            UiTheme.StyleSecondaryButton(btnClearAll);

            btnSelectAll.Click += (s, e) => ApplyBulk(true);
            btnClearAll.Click += (s, e) => ApplyBulk(false);

            panel.Controls.Add(btnSelectAll);
            panel.Controls.Add(btnClearAll);

            return panel;
        }

        private FlowLayoutPanel BuildDialogButtonsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = UiTheme.Surface,
                Padding = new Padding(0, 8, 0, 0),
                Margin = new Padding(0)
            };

            var btnApply = new Button
            {
                Text = "Apply",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(90, 30),
                Margin = new Padding(8, 0, 0, 0)
            };
            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                MinimumSize = new Size(90, 30),
                Margin = new Padding(0)
            };

            UiTheme.StylePrimaryButton(btnApply);
            UiTheme.StyleSecondaryButton(btnCancel);

            btnApply.Click += (s, e) => Result = _workingCopy.Clone();

            AcceptButton = btnApply;
            CancelButton = btnCancel;

            // FlowDirection is RightToLeft: first added -> rightmost. Add Apply first so it sits at the right edge.
            panel.Controls.Add(btnApply);
            panel.Controls.Add(btnCancel);

            return panel;
        }

        private void ApplyBulk(bool value)
        {
            foreach (var (definition, checkBox) in _bindings)
            {
                checkBox.Checked = value;
                // CheckedChanged handler above also writes to _workingCopy; explicit
                // Setter call here keeps the model consistent even if the event
                // ordering ever changes.
                definition.Setter(_workingCopy, value);
            }
        }
    }
}
