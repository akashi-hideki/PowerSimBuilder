using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// ControllerBox の Input/Output ポートを globals 候補から選択する UserControl。
    /// SPEC: docs/SPEC_PORT_SELECTION.md
    /// </summary>
    public class PortSelectorControl : UserControl
    {
        private CheckedListBox _lstInputs;
        private CheckedListBox _lstOutputs;
        private Label _lblInputsHeader;
        private Label _lblOutputsHeader;
        private List<string> _allGlobals = new List<string>();

        public event EventHandler SelectionChanged;

        public PortSelectorControl()
        {
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            SuspendLayout();

            // メインの SplitContainer (上下 2 ペイン)
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 220,
                IsSplitterFixed = false,
            };

            // ---- 上段: Input Ports ----
            var pnlIn = new Panel { Dock = DockStyle.Fill };

            var btnInRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            var btnInAuto = new Button { Text = "Auto-suggest", AutoSize = true };
            var btnInAll = new Button { Text = "Select All", AutoSize = true };
            var btnInClear = new Button { Text = "Clear", AutoSize = true };
            btnInAuto.Click += (s, e) => ApplyAutoSuggestToInputs();
            btnInAll.Click += (s, e) => SetAllChecked(_lstInputs, true);
            btnInClear.Click += (s, e) => SetAllChecked(_lstInputs, false);
            btnInRow.Controls.AddRange(new Control[] { btnInAuto, btnInAll, btnInClear });

            _lblInputsHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "Input Ports (globals から選択)",
                Font = new Font(Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _lstInputs = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
            };
            _lstInputs.ItemCheck += (s, e) => { if (IsHandleCreated) BeginInvoke((Action)(() => RaiseSelectionChanged())); };

            // 順序: Fill → Bottom → Top (WinForms の dock 順序)
            pnlIn.Controls.Add(_lstInputs);
            pnlIn.Controls.Add(btnInRow);
            pnlIn.Controls.Add(_lblInputsHeader);
            split.Panel1.Controls.Add(pnlIn);

            // ---- 下段: Output Ports ----
            var pnlOut = new Panel { Dock = DockStyle.Fill };

            var btnOutRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
            };
            var btnOutAuto = new Button { Text = "Auto-suggest", AutoSize = true };
            var btnOutAll = new Button { Text = "Select All", AutoSize = true };
            var btnOutClear = new Button { Text = "Clear", AutoSize = true };
            btnOutAuto.Click += (s, e) => ApplyAutoSuggestToOutputs();
            btnOutAll.Click += (s, e) => SetAllChecked(_lstOutputs, true);
            btnOutClear.Click += (s, e) => SetAllChecked(_lstOutputs, false);
            btnOutRow.Controls.AddRange(new Control[] { btnOutAuto, btnOutAll, btnOutClear });

            _lblOutputsHeader = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = "Output Ports (globals から選択)",
                Font = new Font(Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _lstOutputs = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
            };
            _lstOutputs.ItemCheck += (s, e) => { if (IsHandleCreated) BeginInvoke((Action)(() => RaiseSelectionChanged())); };

            pnlOut.Controls.Add(_lstOutputs);
            pnlOut.Controls.Add(btnOutRow);
            pnlOut.Controls.Add(_lblOutputsHeader);
            split.Panel2.Controls.Add(pnlOut);

            Controls.Add(split);

            ResumeLayout(false);
        }

        // -----------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------

        /// <summary>globals 抽出結果を候補として渡す。</summary>
        public void SetCandidates(IEnumerable<string> allGlobals)
        {
            _allGlobals = (allGlobals ?? Enumerable.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            RebuildLists();
            ApplyAutoSuggestToInputs();
            ApplyAutoSuggestToOutputs();
        }

        /// <summary>既存選択を復元 (ControllerBox 編集時)。</summary>
        public void SetSelection(IEnumerable<string> selectedInputs, IEnumerable<string> selectedOutputs)
        {
            var inSet = new HashSet<string>(selectedInputs ?? Enumerable.Empty<string>());
            var outSet = new HashSet<string>(selectedOutputs ?? Enumerable.Empty<string>());

            for (int i = 0; i < _lstInputs.Items.Count; i++)
                _lstInputs.SetItemChecked(i, inSet.Contains((string)_lstInputs.Items[i]));
            for (int i = 0; i < _lstOutputs.Items.Count; i++)
                _lstOutputs.SetItemChecked(i, outSet.Contains((string)_lstOutputs.Items[i]));

            RaiseSelectionChanged();
        }

        public IReadOnlyList<string> GetSelectedInputs()
        {
            return _lstInputs.CheckedItems.Cast<string>().ToList();
        }

        public IReadOnlyList<string> GetSelectedOutputs()
        {
            return _lstOutputs.CheckedItems.Cast<string>().ToList();
        }

        // -----------------------------------------------------------------
        // 内部
        // -----------------------------------------------------------------

        private void RebuildLists()
        {
            _lstInputs.Items.Clear();
            _lstOutputs.Items.Clear();
            foreach (var name in _allGlobals)
            {
                _lstInputs.Items.Add(name, false);
                _lstOutputs.Items.Add(name, false);
            }
        }

        private void ApplyAutoSuggestToInputs()
        {
            for (int i = 0; i < _lstInputs.Items.Count; i++)
            {
                var name = (string)_lstInputs.Items[i];
                _lstInputs.SetItemChecked(i, SuggestAsInput(name));
            }
        }

        private void ApplyAutoSuggestToOutputs()
        {
            for (int i = 0; i < _lstOutputs.Items.Count; i++)
            {
                var name = (string)_lstOutputs.Items[i];
                _lstOutputs.SetItemChecked(i, SuggestAsOutput(name));
            }
        }

        private void SetAllChecked(CheckedListBox list, bool @checked)
        {
            for (int i = 0; i < list.Items.Count; i++)
                list.SetItemChecked(i, @checked);
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        // -----------------------------------------------------------------
        // ヒューリスティック (SPEC §4)
        // -----------------------------------------------------------------

        /// <summary>除外パターン: g_system_tick, *_count, seq_*, stage_*, rx_buf</summary>
        internal static bool IsExcluded(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            if (name == "g_system_tick") return true;
            if (name == "rx_buf") return true;
            if (name.EndsWith("_count", StringComparison.Ordinal)) return true;
            if (name.StartsWith("seq_", StringComparison.Ordinal)) return true;
            if (name.StartsWith("stage_", StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Input ヒューリスティック。</summary>
        internal static bool SuggestAsInput(string name)
        {
            if (IsExcluded(name)) return false;
            if (name.StartsWith("thermistor_", StringComparison.Ordinal)) return true;
            if (name.EndsWith("_sensor", StringComparison.Ordinal)) return true;
            if (name.Contains("_current_")) return true;
            if (name.StartsWith("door_", StringComparison.Ordinal)) return true;
            if (name.EndsWith("_command", StringComparison.Ordinal)) return true;
            if (name.EndsWith("_setpoint", StringComparison.Ordinal)) return true;
            if (name.Contains("_target_")) return true;
            return false;
        }

        /// <summary>Output ヒューリスティック。</summary>
        internal static bool SuggestAsOutput(string name)
        {
            if (IsExcluded(name)) return false;
            if (name.EndsWith("_duty", StringComparison.Ordinal)) return true;
            if (name.EndsWith("_on", StringComparison.Ordinal)) return true;
            if (name.EndsWith("_off", StringComparison.Ordinal)) return true;
            if (name.EndsWith("_power", StringComparison.Ordinal)) return true;
            if (name == "error_code") return true;
            if (name.EndsWith("_fault", StringComparison.Ordinal)) return true;
            return false;
        }
    }
}