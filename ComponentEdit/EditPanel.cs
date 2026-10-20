using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace PowerSimBuilder.ComponentEdit
{
    /// <summary>
    /// Left-hand side panel for PythonEditorForm.
    /// Lists Components (bundled / override / adapter) and RoleFunctions
    /// (implemented / stub) parsed from the current Python source.
    /// Language support: ApplyI18n() reads I18n.T values at construction time.
    /// Runtime language switch requires app restart (same as other forms).
    /// </summary>
    public partial class EditPanel : UserControl
    {
        public event EventHandler<RoleFunctionInfo> JumpRequested;
        public event EventHandler<ComponentInfo> OverrideRequested;
        public event EventHandler AdapterRequested;
        public event EventHandler ImportCRequested;

        public string ProjectDir { get; set; }

        public EditPanel()
        {
            InitializeComponent();
            ApplyI18n();
            WireEvents();
        }

        public void ApplyI18n()
        {
            grpComponents.Text    = I18n.T("EditPanel.GroupComponents");
            grpRoleFunctions.Text = I18n.T("EditPanel.GroupRoleFunctions");
            btnOverride.Text      = I18n.T("EditPanel.BtnOverride");
            btnAdapter.Text       = I18n.T("EditPanel.BtnAdapter");
            btnImportC.Text       = I18n.T("EditPanel.BtnImportC");
            UpdateRoleCountLabel(0, 0);
        }

        private void UpdateRoleCountLabel(int implemented, int total)
        {
            lblRoleCount.Text = I18n.T("EditPanel.RoleCount", implemented, total);
        }

        private void WireEvents()
        {
            treeRoleFunctions.NodeMouseDoubleClick += (s, e) =>
            {
                var info = e.Node?.Tag as RoleFunctionInfo;
                if (info != null) JumpRequested?.Invoke(this, info);
            };
            treeRoleFunctions.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F3)
                {
                    JumpToNextStub();
                    e.Handled = true;
                }
            };
            treeComponents.NodeMouseDoubleClick += (s, e) =>
            {
                var info = e.Node?.Tag as ComponentInfo;
                if (info != null && info.Source == ComponentSourceKind.Bundled)
                    OverrideRequested?.Invoke(this, info);
            };
            btnOverride.Click += OnOverrideClick;
            btnAdapter.Click += (s, e) => AdapterRequested?.Invoke(this, EventArgs.Empty);
            btnImportC.Click += (s, e) => ImportCRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnOverrideClick(object sender, EventArgs e)
        {
            var info = treeComponents.SelectedNode?.Tag as ComponentInfo;
            if (info == null || info.Source != ComponentSourceKind.Bundled) return;
            OverrideRequested?.Invoke(this, info);
        }

        public void LoadComponents(IReadOnlyList<ComponentInfo> comps)
        {
            treeComponents.BeginUpdate();
            try
            {
                treeComponents.Nodes.Clear();
                if (comps == null) return;
                foreach (var c in comps)
                {
                    var node = new TreeNode(c.DisplayName) { Tag = c };
                    treeComponents.Nodes.Add(node);
                }
            }
            finally { treeComponents.EndUpdate(); }
        }

        public void LoadRoleFunctions(IReadOnlyList<RoleFunctionInfo> roles)
        {
            treeRoleFunctions.BeginUpdate();
            try
            {
                treeRoleFunctions.Nodes.Clear();
                int impl = 0, stub = 0;
                if (roles != null)
                {
                    foreach (var r in roles)
                    {
                        var node = new TreeNode(r.DisplayName) { Tag = r };
                        treeRoleFunctions.Nodes.Add(node);
                        if (r.Kind == RoleFunctionKind.Implemented) impl++;
                        else stub++;
                    }
                }
                UpdateRoleCountLabel(impl, impl + stub);
            }
            finally { treeRoleFunctions.EndUpdate(); }
        }

        public void JumpToNextStub()
        {
            if (treeRoleFunctions.Nodes.Count == 0)
            {
                AppServices.MessageBox.ShowInfo(this, I18n.T("EditPanel.MsgJumpNoFunc"), I18n.T("Common.MsgTitleInfo"));
                return;
            }
            int start = treeRoleFunctions.SelectedNode?.Index ?? -1;
            for (int i = 1; i <= treeRoleFunctions.Nodes.Count; i++)
            {
                int idx = (start + i) % treeRoleFunctions.Nodes.Count;
                var node = treeRoleFunctions.Nodes[idx];
                var info = node.Tag as RoleFunctionInfo;
                if (info != null && info.Kind == RoleFunctionKind.Stub)
                {
                    treeRoleFunctions.SelectedNode = node;
                    node.EnsureVisible();
                    JumpRequested?.Invoke(this, info);
                    return;
                }
            }
        }

        // -------- Test hooks (not for production use) --------
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TreeView Test_ComponentsTree { get { return treeComponents; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public TreeView Test_RoleFunctionsTree { get { return treeRoleFunctions; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Label Test_RoleCountLabel { get { return lblRoleCount; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Button Test_OverrideButton { get { return btnOverride; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Button Test_AdapterButton { get { return btnAdapter; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Button Test_ImportCButton { get { return btnImportC; } }
    }
}