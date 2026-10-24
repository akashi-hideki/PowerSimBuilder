#nullable enable
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PowerSimBuilder;

public partial class MainForm
{
    private bool _physicsHooked;

    static MainForm()
    {
        EventHandler? once = null;
        once = (s, e) =>
        {
            Application.Idle -= once;
            foreach (Form f in Application.OpenForms)
            {
                if (f is MainForm mf) { mf.HookPhysicsImport(); break; }
            }
        };
        Application.Idle += once;
    }

    private void HookPhysicsImport()
    {
        if (_physicsHooked) return;
        _physicsHooked = true;

        var ts = FindFirstDescendant<ToolStrip>(this);
        if (ts != null) { AddToolbarButton(ts); return; }
        var ms = FindFirstDescendant<MenuStrip>(this);
        if (ms != null) { AddMenuButton(ms); return; }

        var own = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        AddToolbarButton(own);
        Controls.Add(own);
        Controls.SetChildIndex(own, 0);
    }

    private static T? FindFirstDescendant<T>(Control root) where T : Control
    {
        foreach (Control c in root.Controls)
        {
            if (c is T t) return t;
            var inner = FindFirstDescendant<T>(c);
            if (inner != null) return inner;
        }
        return null;
    }

    private void AddToolbarButton(ToolStrip ts)
    {
        var btn = new ToolStripButton
        {
            Name = "_btnImportPhysics",
            Text = I18n.T("PhysicsImport.ToolbarButton"),
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        btn.Click += OnImportPhysicsClick;
        ts.Items.Add(btn);
    }

    private void AddMenuButton(MenuStrip ms)
    {
        var top = new ToolStripMenuItem(I18n.T("MainForm.MenuTools"));
        var item = new ToolStripMenuItem(I18n.T("PhysicsImport.MenuItem"));
        item.Click += OnImportPhysicsClick;
        top.DropDownItems.Add(item);
        ms.Items.Add(top);
    }

    private void OnImportPhysicsClick(object? sender, EventArgs e)
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "user_physics_models");
        using var dlg = new PhysicsImportForm(root);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        PhysicsComponentScanner.ScanUserPhysicsModels(root);
        TryRefreshPalette();
    }

    private void TryRefreshPalette()
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        string[] candidates = { "RefreshComponentPalette", "RebuildPalette", "RefreshPalette",
                                "LoadPalette", "ReloadPalette", "UpdatePalette", "RefreshComponentList" };
        foreach (var name in candidates)
        {
            var m = GetType().GetMethod(name, flags, null, Type.EmptyTypes, null);
            if (m != null)
            {
                try { m.Invoke(this, null); return; } catch { }
            }
        }
    }
}
