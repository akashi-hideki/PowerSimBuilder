using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;


namespace PowerSimBuilder
{
    /// <summary>
    /// アプリケーションのメインフォームクラス
    /// 作成日: 2025-08-12
    /// 注意点: ユーザーインターフェイスと主要ロジックを統合
    /// </summary>
    public partial class MainForm : Form
    {
        private CanvasManager _canvasManager;
        private List<ComponentDescriptor> _components;
        private DesignCanvas designCanvas1;
        private ComponentPalette componentPalette1;

        /// <summary>
        /// メインフォームのコンストラクタ
        /// 注意点: UIコンポーネントとイベントハンドラを初期化
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            SetupLanguageMenu();
        }
        /// <summary>
        /// Adds the language selector to the toolbar.
        /// Changing the language writes settings.json and asks the user to
        /// restart (real-time refresh is out of scope; matches StaTable).
        /// </summary>
        private void SetupLanguageMenu()
        {
            if (toolStrip1 == null) return;

            var langMenu = new ToolStripDropDownButton
            {
                Name = "languageButton",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Text = I18n.T("I18n.LangMenu"),
                ToolTipText = "Language / 语言"
            };

            var enItem = new ToolStripMenuItem(I18n.T("I18n.LangEnglish"))
            {
                Checked = I18n.CurrentLanguage == "en"
            };
            enItem.Click += (s, e) => OnChangeLanguage("en");
            langMenu.DropDownItems.Add(enItem);

            var zhItem = new ToolStripMenuItem(I18n.T("I18n.LangChinese"))
            {
                Checked = I18n.CurrentLanguage == "zh"
            };
            zhItem.Click += (s, e) => OnChangeLanguage("zh");
            langMenu.DropDownItems.Add(zhItem);

            toolStrip1.Items.Add(langMenu);
        }

        /// <summary>
        /// Shows a small modal dialog with "Restart" and "Cancel" buttons.
        /// MessageBox cannot rename its OK button, so we build a tiny Form.
        /// </summary>
        private bool ShowRestartConfirmDialog()
        {
            using var form = new Form
            {
                Text = I18n.T("I18n.MsgRestartTitle"),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(440, 170),
                Font = this.Font,
                AutoScaleMode = AutoScaleMode.Font
            };

            var label = new Label
            {
                Text = I18n.T("I18n.MsgRestartToApply"),
                AutoSize = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnRestart = new Button
            {
                Text = I18n.T("I18n.BtnRestart"),
                DialogResult = DialogResult.OK,
                Size = new Size(110, 30),
                Margin = new Padding(6)
            };

            var btnCancel = new Button
            {
                Text = I18n.T("Common.BtnCancel"),
                DialogResult = DialogResult.Cancel,
                Size = new Size(110, 30),
                Margin = new Padding(6)
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 50,
                Padding = new Padding(8)
            };
            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnRestart);

            form.Controls.Add(label);
            form.Controls.Add(buttonPanel);
            form.AcceptButton = btnRestart;
            form.CancelButton = btnCancel;

            return form.ShowDialog(this) == DialogResult.OK;
        }

        private void OnChangeLanguage(string lang)
        {
            if (I18n.CurrentLanguage == lang) return;

            if (_canvasManager != null && _canvasManager.IsModified)
            {
                var warn = MessageBox.Show(
                    I18n.T("I18n.MsgRestartWithUnsaved"),
                    I18n.T("I18n.MsgRestartTitle"),
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning);
                if (warn != DialogResult.OK) return;
            }

            if (ShowRestartConfirmDialog())
            {
                AppSettings.Language = lang;
                AppSettings.Save();
                Application.Restart();
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Load Event Override
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // デザインキャンバスとコンポーネントパレットの初期化
            designCanvas1 = new DesignCanvas();
            componentPalette1 = new ComponentPalette();

            // コントロールの置換前にサイズを設定
            designCanvas1.Size = canvasPanel.Size;
            componentPalette1.Size = palettePanel.Size;

            // ダブルバッファリングを有効化（ちらつき防止）
            // designCanvas1.DoubleBuffered = true;
            SetDoubleBuffered(componentPalette1);

            // コントロールの置換
            ReplaceControl(canvasPanel, designCanvas1);
            ReplaceControl(palettePanel, componentPalette1);

            // パレット下端にボタン（追加/編集/削除）用の余白を確保
            // これにより末尾コンポーネントがボタンに隠れず、スクロールで表示可能
            componentPalette1.Padding = new Padding(0, 0, 0, 40);

            InitializeRuntimeComponents();
            CheckPythonAtStartup();

            // 境界線の設定
            designCanvas1.BorderStyle = BorderStyle.FixedSingle;
            designCanvas1.BackColor = Color.White;

            // レイアウトを一時的に停止
            this.SuspendLayout();

            // コンポーネントの初期配置
            designCanvas1.ArrangeComponents();

            // レイアウトを再開
            this.ResumeLayout(true);
            this.PerformLayout();
            Test_HandleCommandLineProject();
        }

        // ダブルバッファリングを設定するヘルパーメソッド
        private void SetDoubleBuffered(Control control)
        {
            if (SystemInformation.TerminalServerSession) return;

            System.Reflection.PropertyInfo prop =
                typeof(Control).GetProperty(
                    "DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            if (prop != null)
            {
                prop.SetValue(control, true, null);
            }
        }
        private void ReplaceControl(Panel placeholder, Control realControl)
        {
            // 基本プロパティのコピー
            realControl.Dock = placeholder.Dock;
            realControl.Location = placeholder.Location;
            realControl.Size = placeholder.Size;
            realControl.Anchor = placeholder.Anchor;
            realControl.Margin = placeholder.Margin;
            realControl.Visible = placeholder.Visible;

            // 背景色のコピー
            realControl.BackColor = placeholder.BackColor;

            // Panel の場合のみ BorderStyle をコピー
            if (realControl is Panel panel)
            {
                panel.BorderStyle = placeholder.BorderStyle;
            }
            // FlowLayoutPanel の場合は独自の方法で境界線を処理（後述）

            var parent = placeholder.Parent;
            int index = parent.Controls.IndexOf(placeholder);

            parent.Controls.Remove(placeholder);
            parent.Controls.Add(realControl);
            parent.Controls.SetChildIndex(realControl, index);

            placeholder.Dispose();
        }
        private void InitializeRuntimeComponents()
        {
            InitializeEventHandlers();

            _components = ComponentPaletteStorage.LoadOrCreateDefault();
            componentPalette1.AvailableComponents = _components;

            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            SubscribeToEvents();

            // デザイナーファイルから移動した設定
            designCanvas1.ConnectionRenderer = new BezierConnectionRenderer();
            designCanvas1.ConnectionSelected += DesignCanvas_ConnectionSelected;
        }
        /// <summary>
        /// イベントハンドラを初期化
        /// 注意点: ツールバーボタンとプロパティグリッドのイベントを登録
        /// </summary>

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.D))
            {
                I18nDumpUi.DumpAllForms();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void InitializeEventHandlers()
        {
            addButton.Click += addComponentButton_Click; // 追加: イベントハンドラ登録
            saveButton.Click += saveButton_Click;
            saveAsButton.Click += saveAsButton_Click;
            loadButton.Click += loadButton_Click;
            undoButton.Click += undoButton_Click;
            redoButton.Click += redoButton_Click;
            createSimButton.Click += createSimButton_Click;
            arrangeButton.Click += arrangeButton_Click;
            staTableImportButton.Click += staTableImportButton_Click;
            editButton.Click += editPaletteButton_Click;
            removePalletButton.Click += deletePaletteButton_Click;
            propertyGrid1.PropertyValueChanged += propertyGrid1_PropertyValueChanged;

            // Task 3-C-9: パラメータ編集ボタン（動的追加）
            var paramsEditButton = new ToolStripButton
            {
                Name = "paramsEditButton",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Text = I18n.T("MainForm.BtnParametersEdit"),
            };
            paramsEditButton.Click += paramsEditButton_Click;
            toolStrip1.Items.Add(paramsEditButton);

            // Phase 3b: Save selection as group
            var saveGroupButton = new ToolStripButton
            {
                Name = "saveGroupButton",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Text = I18n.T("MainForm.BtnSaveGroup"),
            };
            saveGroupButton.Click += saveGroupButton_Click;
            toolStrip1.Items.Add(saveGroupButton);

            // Phase 4b: Load group
            var loadGroupButton = new ToolStripButton
            {
                Name = "loadGroupButton",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Text = I18n.T("MainForm.BtnLoadGroup"),
            };
            loadGroupButton.Click += loadGroupButton_Click;
            toolStrip1.Items.Add(loadGroupButton);
        }

        private void saveGroupButton_Click(object sender, EventArgs e)
        {
            var selected = designCanvas1.SelectedComponents;
            if (selected == null || selected.Count == 0)
            {
                MessageBox.Show(this, I18n.T("MainForm.MsgSaveGroupNoSelection"),
                    I18n.T("MainForm.SaveGroupDialogTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new SaveFileDialog
            {
                Title = I18n.T("MainForm.SaveGroupDialogTitle"),
                Filter = "PowerSimBuilder Group (*.psbgroup)|*.psbgroup",
                DefaultExt = GroupManager.Extension,
                AddExtension = true,
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var groupName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                var group = GroupManager.Extract(groupName, selected, _canvasManager.GetAllConnections());
                GroupManager.Save(dlg.FileName, group);
                MessageBox.Show(this, I18n.T("MainForm.MsgSaveGroupOk", dlg.FileName),
                    I18n.T("MainForm.SaveGroupDialogTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message,
                    I18n.T("Common.MsgTitleError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void loadGroupButton_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = I18n.T("MainForm.LoadGroupDialogTitle"),
                Filter = "PowerSimBuilder Group (*.psbgroup)|*.psbgroup",
                DefaultExt = GroupManager.Extension,
                CheckFileExists = true,
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                // Offset: place group 50px to the right/down from canvas origin.
                var offset = new System.Drawing.Point(50, 50);
                var (comps, conns) = GroupManager.LoadAndAppend(
                    dlg.FileName, _canvasManager, offset);
                MessageBox.Show(this,
                    I18n.T("MainForm.MsgLoadGroupOk",
                        string.Format("{0} ({1} components, {2} connections)",
                            System.IO.Path.GetFileName(dlg.FileName), comps, conns)),
                    I18n.T("MainForm.LoadGroupDialogTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message,
                    I18n.T("Common.MsgTitleError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// イベント購読を設定
        /// 注意点: キャンバス管理クラスのイベントを購読
        /// </summary>
        private void SubscribeToEvents()
        {
            _canvasManager.CanvasModified += CanvasManager_CanvasModified;
            _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
            componentPalette1.ComponentSelected += ComponentPalette_ComponentSelected;

        }

        /// <summary>
        /// コンポーネントパレット選択イベント
        /// 注意点: 追加ボタンの有効状態を更新
        /// </summary>
        private void ComponentPalette_ComponentSelected(object sender, EventArgs e)
        {
            editButton.Enabled = (componentPalette1.SelectedComponent != null);
            LogcatTraceHelper.Instance.SimpleMessage("Pallet Event", "pallet Selected ");
        }

        /// <summary>
        /// コンポーネント追加ボタンクリック
        /// 注意点: カスタムコンポーネント追加フォームを表示
        /// </summary>
        private void addComponentButton_Click(object sender, EventArgs e)
        {
            using (var form = new AddComponentForm())
            {
                if (form.ShowDialog() == DialogResult.OK && form.ResultComponent != null)
                {
                    componentPalette1.AddComponent(form.ResultComponent);
                    ComponentPaletteStorage.SaveDescriptors(componentPalette1.AvailableComponents);
                }
            }
        }

        /// <summary>
        /// キャンバス変更イベント
        /// 注意点: 保存状態とUNDO/REDOボタンを更新
        /// </summary>
        private void CanvasManager_CanvasModified(object sender, EventArgs e)
        {
            UpdateSaveStatus();
            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// 選択変更イベント
        /// 注意点: プロパティグリッドに選択オブジェクトを表示
        /// </summary>
        private void CanvasManager_SelectionChanged(object sender, EventArgs e)
        {
            propertyGrid1.SelectedObject = _canvasManager.SelectedComponent?.Parameters;
            UpdateDeleteButton();
        }
        /// <summary>
        /// コンポーネント編集ボタンクリック
        /// 注意点: パレットで選択中のコンポーネントをキャンバスに追加
        /// </summary>

        private void editPaletteButton_Click(object sender, EventArgs e)
        {
            if (componentPalette1.SelectedComponent == null)
            {
                MessageBox.Show(I18n.T("MainForm.MsgSelectToEdit"), I18n.T("Common.MsgTitleInfo"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // キャンバスで使用中のコンポーネントがあるかチェック
            bool isUsedInCanvas = _canvasManager.GetAllComponents()
                .Any(c => c.Descriptor == componentPalette1.SelectedComponent);

            if (isUsedInCanvas)
            {
                MessageBox.Show(I18n.T("MainForm.MsgInUseEdit"), I18n.T("Common.MsgTitleError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var form = new AddComponentForm(componentPalette1.SelectedComponent))
            {
                if (form.ShowDialog() == DialogResult.OK && form.ResultComponent != null)
                {
                    // パレットを更新
                    componentPalette1.UpdateComponent(componentPalette1.SelectedComponent, form.ResultComponent);

                    // 変更を保存
                    ComponentPaletteStorage.SaveDescriptors(componentPalette1.AvailableComponents);

                    MessageBox.Show(I18n.T("MainForm.MsgUpdated"), I18n.T("Common.MsgTitleSuccess"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // パレットアイテム削除
        private void deletePaletteButton_Click(object sender, EventArgs e)
        {
            if (componentPalette1.SelectedComponent == null)
            {
                MessageBox.Show(I18n.T("MainForm.MsgSelectToDelete"), I18n.T("Common.MsgTitleInfo"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // キャンバスで使用中のコンポーネントがあるかチェック
            bool isUsedInCanvas = _canvasManager.GetAllComponents()
                .Any(c => c.Descriptor == componentPalette1.SelectedComponent);

            if (isUsedInCanvas)
            {
                MessageBox.Show(I18n.T("MainForm.MsgInUseDelete"), I18n.T("Common.MsgTitleError"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                I18n.T("MainForm.MsgDeleteConfirm", componentPalette1.SelectedComponent.Name),
                I18n.T("Common.MsgTitleConfirm"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                componentPalette1.RemoveComponent(componentPalette1.SelectedComponent);

                // 変更を保存
                ComponentPaletteStorage.SaveDescriptors(componentPalette1.AvailableComponents);

                MessageBox.Show(I18n.T("MainForm.MsgDeleted"), I18n.T("Common.MsgTitleSuccess"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 保存ボタンクリック
        /// 注意点: JSON形式でシステム構成を保存
        /// </summary>
        private void saveButton_Click(object sender, EventArgs e)
        {
            // Ensure MainForm is active before opening modal dialog.
            this.BringToFront();
            this.Activate();

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = I18n.T("MainForm.SaveDialogTitle");
                dialog.Filter = "PowerSimBuilder Project (*.psbproj)|*.psbproj|JSON (legacy) (*.json)|*.json";
                dialog.DefaultExt = ProjectArchive.Extension;
                dialog.FileName = "project" + ProjectArchive.Extension;
                var workspaceProjects = Path.Combine(AppSettings.WorkspaceRoot, "projects");
                try { Directory.CreateDirectory(workspaceProjects); } catch { }
                dialog.InitialDirectory = Directory.Exists(workspaceProjects)
                    ? workspaceProjects
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    SaveProjectToPath(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(I18n.T("MainForm.MsgSaveFail", ex.Message), I18n.T("Common.MsgTitleError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveAsButton_Click(object sender, EventArgs e)
        {
            // Save As: always enabled, ignores IsModified.
            this.BringToFront();
            this.Activate();
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = I18n.T("MainForm.SaveAsDialogTitle");
                dialog.Filter = "PowerSimBuilder Project (*.psbproj)|*.psbproj|JSON (legacy) (*.json)|*.json";
                dialog.DefaultExt = ProjectArchive.Extension;
                dialog.FileName = "project" + ProjectArchive.Extension;
                var workspaceProjects = Path.Combine(AppSettings.WorkspaceRoot, "projects");
                try { Directory.CreateDirectory(workspaceProjects); } catch { }
                dialog.InitialDirectory = Directory.Exists(workspaceProjects)
                    ? workspaceProjects
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    SaveProjectToPath(dialog.FileName);
                    this.Activate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(I18n.T("MainForm.MsgSaveFail", ex.Message), I18n.T("Common.MsgTitleError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Save current canvas to path (ZIP if .psbproj, JSON otherwise).
        /// Extracted from saveButton_Click for headless automation.
        /// </summary>
        private double _currentTEnd = 5.0;

        private void SaveProjectToPath(string filePath)
        {
            var model = _canvasManager.SaveModel();
            var projectName = Path.GetFileNameWithoutExtension(filePath);

            if (filePath.EndsWith(ProjectArchive.Extension, StringComparison.OrdinalIgnoreCase))
            {
                var v2 = model.FromCanvasModel(projectName);
                v2.Metadata.TEnd = _currentTEnd;
                var archiveOptions = new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = true,
                    IncludeGenerated = true,
                    IncludeFmu = false,
                };
                ProjectArchive.SaveAndExpand(filePath, v2, archiveOptions);
            }
            else
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new PointJsonConverter() }
                };
                var json = JsonSerializer.Serialize(model, options);
                File.WriteAllText(filePath, json);
            }

            _canvasManager.IsModified = false;
            UpdateSaveStatus();
        }

        /// <summary>
        /// Create a demo project programmatically: FirstOrderLag + Dahlquist FMU metadata.
        /// This validates the Canvas -> Save pipeline for FMU-attached components.
        /// Note: actual Python code generation (FMU integration) is a separate concern.
        /// </summary>
        internal void Test_CreateFmuDemoProject(string savePath)
        {
            Test_Log("Test_CreateFmuDemoProject START: " + savePath);

            // 1. Resolve Dahlquist entry via catalog (no manual FmuInfo building)
            var userPhysicsRoot = PowerSimBuilder.Physics.PhysicsModelCatalog.DefaultRoot();
            var entries = PowerSimBuilder.Physics.PhysicsModelCatalog.Scan(userPhysicsRoot);
            var dahlquist = entries.FirstOrDefault(e =>
                e.DisplayName.IndexOf("Dahlquist", StringComparison.OrdinalIgnoreCase) >= 0);
            if (dahlquist == null)
                throw new InvalidOperationException(
                    "Dahlquist not found in catalog under: " + userPhysicsRoot);
            Test_Log("Catalog: Dahlquist at " + dahlquist.FmuRelativePath
                + " / " + dahlquist.Info.FmiType);

            // 2. Clone FirstOrderLag descriptor (avoid mutating the shared dictionary)
            var original = ComponentDescriptorDictionary.Descriptors.Values
                .FirstOrDefault(d => string.Equals(d.Type, "FirstOrderLag", StringComparison.OrdinalIgnoreCase));
            if (original == null)
                throw new InvalidOperationException("FirstOrderLag descriptor not found");

            var descriptor = new ComponentDescriptor
            {
                Name = "plant",
                Type = original.Type,
                Category = original.Category,
                InputPorts = original.InputPorts,
                OutputPorts = original.OutputPorts,
                Parameters = original.Parameters,
                PhysicsModelPath = dahlquist.FmuRelativePath,
                FmiType = dahlquist.Info.FmiType,
            };
            Test_Log("Descriptor: FirstOrderLag + " + descriptor.PhysicsModelPath);

            // 3. Add to canvas
            _canvasManager.AddComponent(descriptor, new Point(200, 150));
            var component = _canvasManager.GetAllComponents().Last();
            component.InstanceName = "plant";
            component.Name = "plant";
            Test_Log("Component added: InstanceName='" + component.InstanceName + "'");

            // 4. Save to .psbproj
            SaveProjectToPath(savePath);
            Test_Log("Test_CreateFmuDemoProject OK: " + savePath);
        }

        internal void Test_SaveProjectToPath(string filePath)
        {
            Test_Log("Test_SaveProjectToPath START: " + filePath);
            SaveProjectToPath(filePath);
            Test_Log("Test_SaveProjectToPath OK: " + filePath);
        }

        /// <summary>
        /// 読み込みボタンクリック
        /// 注意点: JSONファイルからシステム構成を復元
        /// </summary>
        private void loadButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = I18n.T("MainForm.LoadDialogTitle");
                dialog.Filter = "PowerSimBuilder Project (*.psbproj)|*.psbproj|JSON (legacy) (*.json)|*.json|All Files (*.*)|*.*";
                dialog.DefaultExt = ProjectArchive.Extension;
                var workspaceProjectsLoad = Path.Combine(AppSettings.WorkspaceRoot, "projects");
                dialog.InitialDirectory = Directory.Exists(workspaceProjectsLoad)
                    ? workspaceProjectsLoad
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Test_LoadProjectFromPath(dialog.FileName);

                // v1.8.0 Round 3: notify user of missing external FMUs
                if (Test_LastMissingExternalFmus.Count > 0)
                {
                    ShowMissingExternalFmuDialog(Test_LastMissingExternalFmus);
                }
            }
        }

        /// <summary>
        /// v1.8.0 Round 3: Dialog listing FMUs that were excluded from the
        /// archive (size/license) and are not present locally.
        /// </summary>
        private void ShowMissingExternalFmuDialog(
            System.Collections.Generic.List<ExternalFmu> missing)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("This project references FMU(s) that were not bundled in the archive:");
            sb.AppendLine();
            foreach (var ext in missing)
            {
                sb.AppendLine($"  \u2022 {ext.RelativePath}  ({ext.Reason})");
                if (!string.IsNullOrEmpty(ext.LicenseId))
                    sb.AppendLine($"    License: {ext.LicenseId}");
                if (ext.SizeBytes > 0)
                    sb.AppendLine($"    Size: {ext.SizeBytes / 1024} KB");
                if (!string.IsNullOrEmpty(ext.DownloadUrl))
                    sb.AppendLine($"    Download: {ext.DownloadUrl}");
                sb.AppendLine($"    Place under: user_physics_models/{ext.RelativePath}");
                sb.AppendLine();
            }
            sb.AppendLine("The project was loaded, but simulation may fail");
            sb.AppendLine("until these FMUs are available.");
            MessageBox.Show(this, sb.ToString(),
                "Missing External FMU", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// When true, Test_LoadProjectFromPath skips ArrangeComponents(),
        /// preserving loaded positions (used for location round-trip tests).
        /// </summary>
        internal bool Test_DisableArrangeOnLoad { get; set; } = false;

        /// <summary>
        /// v1.8.0 Round 3: Filled after Test_LoadProjectFromPath for UI to display.
        /// Contains manifest.ExternalFmus that were not found under user_physics_models/.
        /// </summary>
        internal System.Collections.Generic.List<ExternalFmu> Test_LastMissingExternalFmus
        { get; private set; } = new();

        internal void Test_LoadProjectFromPath(string path)
        {
            try
            {
                Test_Log("Test_LoadProjectFromPath START: " + path);
                CanvasModel model;
                if (path.EndsWith(ProjectArchive.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    // New: ZIP archive + manifest
                    var (v2, extractDir, manifest) = ProjectArchive.LoadWithManifest(path, extractArtifacts: true);
                    Test_Log($"ProjectArchive.LoadWithManifest OK: {v2.Components.Count} components, extract={extractDir}");
                    _currentTEnd = v2.Metadata.TEnd;
                    model = v2.ToCanvasModel();
                    Test_Log($"ToCanvasModel OK: Components={model.Components.Count}, Connections={model.Connections.Count}");

                    // v1.8.0 Round 3: check external FMUs
                    Test_LastMissingExternalFmus =
                        ProjectArchive.CheckExternalFmus(manifest, extractDir);
                    Test_Log($"Missing external FMUs: {Test_LastMissingExternalFmus.Count}");
                    foreach (var ext in Test_LastMissingExternalFmus)
                        Test_Log($"  - {ext.RelativePath} ({ext.Reason}, license={ext.LicenseId})");

                    // Extract generated/demo_main.py so that Create Simulation / Run can reuse it
                    if (!string.IsNullOrEmpty(extractDir))
                    {
                        var extractedDemo = Path.Combine(extractDir, "generated", "demo_main.py");
                        if (File.Exists(extractedDemo))
                        {
                            var localGenerated = Path.Combine(
                                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
                                "generated");
                            Directory.CreateDirectory(localGenerated);
                            File.Copy(extractedDemo, Path.Combine(localGenerated, "demo_main.py"), overwrite: true);
                            Test_Log("Extracted demo_main.py to generated/");
                        }
                    }
                }
                else
                {
                    // Legacy: single JSON file
                    var json = File.ReadAllText(path);
                    Test_Log("JSON read OK: " + json.Length + " chars");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters = { new PointJsonConverter() }
                    };

                    model = JsonSerializer.Deserialize<CanvasModel>(json, options);
                    Test_Log("Deserialize OK: Components=" + (model?.Components?.Count ?? -1));
                }

                _canvasManager.LoadModel(model);
                Test_Log("LoadModel OK");

                RepairGeneratedPaths(_canvasManager);
                if (!Test_DisableArrangeOnLoad)
                    designCanvas1.ArrangeComponents();
                UpdateSaveStatus();
                UpdateUndoRedoButtons();
                UpdateDeleteButton();
                Test_Log("Test_LoadProjectFromPath COMPLETE");
            }
            catch (Exception ex)
            {
                Test_Log("Test_LoadProjectFromPath FAILED: " + ex.ToString());
                if (!Test_IsTestMode())
                {
                    MessageBox.Show(I18n.T("MainForm.MsgLoadFail", ex.Message), I18n.T("Common.MsgTitleError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static bool Test_IsTestMode()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--test-project") return true;
            }
            return false;
        }

        internal static void Test_Log(string message)
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "PowerSimBuilder_TestLoad.log");
                System.IO.File.AppendAllText(logPath,
                    DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch { }
        }

        private void Test_HandleCommandLineProject()
        {
            var cmdArgs = Environment.GetCommandLineArgs();
            string projectPath = null;
            string openEditorInstance = null;
            for (int i = 0; i < cmdArgs.Length - 1; i++)
            {
                if (cmdArgs[i] == "--test-project" && File.Exists(cmdArgs[i + 1]))
                {
                    projectPath = cmdArgs[i + 1];
                }
                else if (cmdArgs[i] == "--test-open-editor" && !string.IsNullOrEmpty(cmdArgs[i + 1]))
                {
                    openEditorInstance = cmdArgs[i + 1];
                }
            }
            // Phase C: --test-open-role-snippets (boolean flag, may appear anywhere)
            bool openRoleSnippets = false;
            for (int i = 0; i < cmdArgs.Length; i++)
            {
                if (cmdArgs[i] == "--test-open-role-snippets") { openRoleSnippets = true; break; }
            }
            if (projectPath != null)
            {
                Test_LoadProjectFromPath(projectPath);
            }
            if (openEditorInstance != null)
            {
                BeginInvoke(new Action(() =>
                {
                    System.Threading.Thread.Sleep(500);
                    Test_OpenEditorForInstance(openEditorInstance);
                }));
            }
            if (openRoleSnippets)
            {
                BeginInvoke(new Action(() =>
                {
                    System.Threading.Thread.Sleep(1500);
                    Test_OpenRoleSnippetsInEditor();
                }));
            }
        }

        /// <summary>
        /// Test-only: find the PythonEditorForm that is currently open and trigger
        /// its Role Snippets dialog (bypasses flaky context-menu automation).
        /// </summary>
        internal void Test_OpenRoleSnippetsInEditor()
        {
            try
            {
                Test_Log("Test_OpenRoleSnippetsInEditor START");
                foreach (Form f in Application.OpenForms)
                {
                    if (f is PythonEditorForm editor)
                    {
                        Test_Log("  found editor: " + f.Name);
                        editor.Test_ShowRoleSnippetsDialog();
                        Test_Log("  dialog invoked");
                        return;
                    }
                }
                Test_Log("  no PythonEditorForm found");
            }
            catch (System.Exception ex)
            {
                Test_Log("  FAILED: " + ex.Message);
            }
        }

        /// <summary>
        /// Phase B: programmatically build a simulation on the canvas with an
        /// FMU-embedded component (FirstOrderLag + Dahlquist).
        /// </summary>
        /// <summary>
        /// Programmatic equivalent of the "Create Simulation" button.
        /// Builds demo_main.py from the current canvas without showing a dialog.
        /// </summary>
        /// <summary>
        /// Programmatic equivalent of the Run button: execute demo_main.py via Python.
        /// Expects simulation_signals.png to be produced next to the script.
        /// </summary>
        internal void Test_RunSimulation(string scriptPath)
        {
            // Test_RunSimulation v2: robust with retry + detailed diagnostics
            Test_Log("Test_RunSimulation START: " + scriptPath);
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Script not found", scriptPath);

            var scriptDir = Path.GetDirectoryName(scriptPath) ?? ".";
            var pngPath = Path.Combine(scriptDir, "simulation_signals.png");

            // Delete existing PNG so we verify fresh production
            try { File.Delete(pngPath); Test_Log("Deleted old PNG: " + pngPath); } catch { }

            // Log resolved Python path (diagnose PATH differences)
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = "python",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var wp = System.Diagnostics.Process.Start(psi);
                if (wp != null)
                {
                    var whereOut = wp.StandardOutput.ReadToEnd().Trim();
                    wp.WaitForExit(5000);
                    Test_Log("where python: " + whereOut.Replace("\r\n", " | ").Replace("\n", " | "));
                }
            }
            catch (Exception ex) { Test_Log("where python failed: " + ex.Message); }

            var executor = new PythonExecutor { SilentMode = true, ForceHeadlessBackend = true };
            var runSw = System.Diagnostics.Stopwatch.StartNew();
            var ok = executor.Run(scriptPath);
            Test_Log("PythonExecutor.Run returned: " + ok);
            if (ok)
            {
                // 50s timeout (xUnit outer 60s cushion)
                var exited = executor.WaitForExit(50000);
                runSw.Stop();
                Test_Log($"WaitForExit: {exited} ({runSw.ElapsedMilliseconds}ms)");
                if (!exited)
                    throw new InvalidOperationException(
                        $"Python process did not exit 0 within timeout ({runSw.ElapsedMilliseconds}ms)");
            }

            if (!ok)
                throw new InvalidOperationException("PythonExecutor.Run returned false");

            // Retry for up to 10s (filesystem flush + slow env)
            for (int i = 0; i < 100; i++)
            {
                if (File.Exists(pngPath) && new FileInfo(pngPath).Length > 0) break;
                System.Threading.Thread.Sleep(100);
            }

            if (!File.Exists(pngPath))
            {
                var files = Directory.Exists(scriptDir)
                    ? string.Join(", ", Directory.GetFiles(scriptDir).Select(Path.GetFileName))
                    : "(dir missing)";
                throw new FileNotFoundException(
                    $"simulation_signals.png not produced. Files in {scriptDir}: {files}",
                    pngPath);
            }

            var size = new FileInfo(pngPath).Length;
            Test_Log($"Test_RunSimulation OK: png={pngPath} ({size} bytes)");
        }

        internal void Test_GenerateSimulation(string outputFilePath)
        {
            Test_Log("Test_GenerateSimulation START: " + outputFilePath);
            try
            {
                List<ComponentDescriptor> components = _canvasManager.UsedComponentDescriptors;
                List<ComponentControl> componentInstances = _canvasManager.GetAllComponents();
                List<Connection> connections = _canvasManager.Connections;
                Test_Log($"  components={components.Count}, instances={componentInstances.Count}, connections={connections.Count}");

                var builder = new PythonSimulationMainBuilder();
                builder.FilePath = outputFilePath;
                builder.TEnd = _currentTEnd;
                builder.Components = components;
                builder.ComponentInstances = componentInstances;
                builder.Connections = connections;
                builder.ErrorHandler = (category, message) =>
                    Test_Log($"  GEN_ERR[{category}]: {message}");
                builder.BuildPythonSimulationFile();
                Test_Log("Test_GenerateSimulation OK: " + outputFilePath);
            }
            catch (Exception ex)
            {
                Test_Log("Test_GenerateSimulation FAIL: " + ex.Message);
                throw;
            }
        }

        internal void Test_CreateDemoSimulation()
        {
            Test_Log("Test_CreateDemoSimulation START");
            _canvasManager.Clear();
            Test_Log("Canvas cleared");

            var descriptor = new ComponentDescriptor
            {
                Name = "FirstOrderLag",
                Type = "FirstOrderLag",
                Category = "Simulation",
                InputPorts = new[] { "input" },
                OutputPorts = new[] { "output" },
                PhysicsModelPath = "Dahlquist/source/Dahlquist.fmu",
                FmiType = "CoSimulation",
                Parameters = new List<ParameterDescriptor>
                {
                    new() { Name = "time_constant", Type = "float", DefaultValue = 1.0 },
                    new() { Name = "initial_output", Type = "float", DefaultValue = 0.0 },
                },
            };

            _canvasManager.AddComponent(descriptor, new System.Drawing.Point(200, 100));
            var components = _canvasManager.GetAllComponents();
            // Improve visibility: set a meaningful instance name so demo_main.py
            // uses "first order lag" as the dictionary key (matches demo script).
            if (components.Count > 0)
            {
                var added = components[components.Count - 1];
                added.InstanceName = "first order lag";
                added.Name = "first order lag";
            }
            Test_Log("Added component: count=" + components.Count);
            if (components.Count > 0)
            {
                var last = components[components.Count - 1];
                Test_Log("  InstanceName=" + (last.InstanceName ?? "(null)"));
                Test_Log("  PhysicsModelPath=" + (last.Descriptor.PhysicsModelPath ?? "(null)"));
                Test_Log("  FmiType=" + (last.Descriptor.FmiType ?? "(null)"));
            }

            Test_Log("Test_CreateDemoSimulation OK");
        }

        /// <summary>
        /// Phase B-2: programmatically build a 2-component project with one
        /// connection (Gain.output -> FirstOrderLag.input) to verify that
        /// name-based wiring survives round-trip.
        /// </summary>
        internal void Test_CreateTwoComponentProject()
        {
            Test_Log("Test_CreateTwoComponentProject START");
            _canvasManager.Clear();
            Test_Log("Canvas cleared");

            // Component 1: Gain (source)
            var gainDesc = new ComponentDescriptor
            {
                Name = "Gain",
                Type = "Gain",
                Category = "Math",
                InputPorts = new[] { "input" },
                OutputPorts = new[] { "output" },
                Parameters = new List<ParameterDescriptor>
                {
                    new() { Name = "gain", Type = "float", DefaultValue = 2.0 },
                },
            };
            _canvasManager.AddComponent(gainDesc, new System.Drawing.Point(100, 100));

            // Component 2: FirstOrderLag (plant) with Dahlquist FMU
            var lagDesc = new ComponentDescriptor
            {
                Name = "FirstOrderLag",
                Type = "FirstOrderLag",
                Category = "Simulation",
                InputPorts = new[] { "input" },
                OutputPorts = new[] { "output" },
                PhysicsModelPath = "Dahlquist/source/Dahlquist.fmu",
                FmiType = "CoSimulation",
                Parameters = new List<ParameterDescriptor>
                {
                    new() { Name = "time_constant", Type = "float", DefaultValue = 1.0 },
                    new() { Name = "initial_output", Type = "float", DefaultValue = 0.0 },
                },
            };
            _canvasManager.AddComponent(lagDesc, new System.Drawing.Point(350, 100));

            var comps = _canvasManager.GetAllComponents();
            Test_Log("Added components: count=" + comps.Count);
            if (comps.Count >= 2)
            {
                comps[0].InstanceName = "gain";
                comps[0].Name = "gain";
                comps[0].Parameters["gain"] = 3.7;  // B-2c: non-default
                comps[0].Location = new System.Drawing.Point(123, 456);  // B-2d

                comps[1].InstanceName = "first order lag";
                comps[1].Name = "first order lag";
                comps[1].Parameters["time_constant"] = 2.5;  // B-2c: non-default
                comps[1].Parameters["initial_output"] = 0.5;  // B-2c: non-default
                comps[1].Location = new System.Drawing.Point(789, 321);  // B-2d
            }

            // Connection: gain.output -> first order lag.input
            _canvasManager.AddConnection(comps[0], "output", comps[1], "input");
            Test_Log("Added connection: count=" + _canvasManager.Connections.Count);

            Test_Log("Test_CreateTwoComponentProject OK");
        }

        /// <summary>
        /// Phase B-2e: closed-loop project (controller + plant + 2 wires).
        /// Verifies bidirectional wiring survives round-trip.
        /// </summary>
        internal void Test_CreateClosedLoopProject()
        {
            Test_Log("Test_CreateClosedLoopProject START");
            _canvasManager.Clear();

            // Controller (PID)
            var ctrlDesc = new ComponentDescriptor
            {
                Name = "ControllerBox",
                Type = "ControllerBox",
                Category = "Control",
                InputPorts = new[] { "measurement" },
                OutputPorts = new[] { "control" },
                Parameters = new List<ParameterDescriptor>
                {
                    new() { Name = "kp", Type = "float", DefaultValue = 2.0 },
                    new() { Name = "ki", Type = "float", DefaultValue = 0.5 },
                    new() { Name = "kd", Type = "float", DefaultValue = 0.05 },
                },
            };
            _canvasManager.AddComponent(ctrlDesc, new System.Drawing.Point(100, 100));

            // Plant (FirstOrderLag + Dahlquist FMU)
            var plantDesc = new ComponentDescriptor
            {
                Name = "FirstOrderLag",
                Type = "FirstOrderLag",
                Category = "Simulation",
                InputPorts = new[] { "input" },
                OutputPorts = new[] { "output" },
                PhysicsModelPath = "Dahlquist/source/Dahlquist.fmu",
                FmiType = "CoSimulation",
                Parameters = new List<ParameterDescriptor>
                {
                    new() { Name = "time_constant", Type = "float", DefaultValue = 1.0 },
                    new() { Name = "initial_output", Type = "float", DefaultValue = 0.0 },
                },
            };
            _canvasManager.AddComponent(plantDesc, new System.Drawing.Point(400, 100));

            var comps = _canvasManager.GetAllComponents();
            Test_Log("Closed loop: components=" + comps.Count);
            if (comps.Count >= 2)
            {
                comps[0].InstanceName = "controller";
                comps[0].Name = "controller";
                comps[0].Parameters["kp"] = 2.5;
                comps[0].Parameters["ki"] = 0.75;
                comps[0].Parameters["kd"] = 0.1;
                comps[0].Location = new System.Drawing.Point(100, 200);

                comps[1].InstanceName = "plant";
                comps[1].Name = "plant";
                comps[1].Parameters["time_constant"] = 1.5;
                comps[1].Parameters["initial_output"] = 0.0;
                comps[1].Location = new System.Drawing.Point(500, 200);
            }

            // Closed loop:
            //   controller.control -> plant.input
            //   plant.output       -> controller.measurement
            _canvasManager.AddConnection(comps[0], "control", comps[1], "input");
            _canvasManager.AddConnection(comps[1], "output", comps[0], "measurement");
            Test_Log("Closed loop: connections=" + _canvasManager.Connections.Count);

            Test_Log("Test_CreateClosedLoopProject OK");
        }

        internal void Test_OpenEditorForInstance(string instanceName)
        {
            if (string.IsNullOrEmpty(instanceName)) return;
            Test_Log("Test_OpenEditorForInstance START: " + instanceName);
            try
            {
                var components = _canvasManager.GetAllComponents();
                if (components == null) { Test_Log("  components is null"); return; }
                ComponentControl target = null;
                foreach (var c in components)
                {
                    if (c != null && c.InstanceName == instanceName) { target = c; break; }
                }
                if (target == null)
                {
                    Test_Log("  instance not found: " + instanceName);
                    return;
                }
                Test_Log("  found, opening editor");
                Test_Log("  BEFORE: OpenForms count=" + System.Windows.Forms.Application.OpenForms.Count);
                foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms)
                {
                    Test_Log("    form: name='" + f.Name + "' handle=" + f.Handle + " visible=" + f.Visible);
                }

                // Diagnostic: monitor OpenForms during ShowDialog
                var monitor = new System.Windows.Forms.Timer { Interval = 1000 };
                int tickCount = 0;
                monitor.Tick += (s, e) =>
                {
                    tickCount++;
                    try
                    {
                        var forms = System.Windows.Forms.Application.OpenForms;
                        Test_Log("  [MON " + tickCount + "] OpenForms=" + forms.Count);
                        foreach (System.Windows.Forms.Form f in forms)
                        {
                            string name = ""; string text = ""; bool vis = false; long h = 0;
                            try { name = f.Name ?? ""; } catch { }
                            try { text = f.Text ?? ""; } catch { }
                            try { vis = f.Visible; } catch { }
                            try { h = f.Handle.ToInt64(); } catch { }
                            Test_Log("    MON form: name='" + name + "' text='" + text + "' handle=" + h + " visible=" + vis);
                        }
                    }
                    catch (Exception ex) { Test_Log("  MON err: " + ex.Message); }
                    if (tickCount >= 15) { try { monitor.Stop(); monitor.Dispose(); } catch { } }
                };
                monitor.Start();

                designCanvas1.Test_OpenEditorForComponent(target);
                Test_Log("  editor closed (ticks=" + tickCount + ")");
                try { monitor.Stop(); monitor.Dispose(); } catch { }
            }
            catch (Exception ex)
            {
                Test_Log("  FAILED: " + ex.ToString());
            }
        }

        // ============================================================
        // Task 3-C-10: generated_path 自動修復（データ汚染対策）
        // ============================================================

        /// <summary>
        /// ロード時に ControllerBox の generated_path を検証し、
        /// 無効なら Descriptor.DefaultValue にフォールバックする。
        /// 過去バージョンで誤って保存された汚染データを自動修復する。
        /// </summary>
        private static void RepairGeneratedPaths(CanvasManager manager)
        {
            if (manager == null) return;
            var components = manager.GetAllComponents();
            if (components == null) return;
            foreach (var comp in components)
            {
                if (comp?.Descriptor == null) continue;
                if (comp.Descriptor.Type != "ControllerBox") continue;
                if (comp.Parameters == null) continue;
                if (!comp.Parameters.ContainsKey("generated_path")) continue;

                // Read via JsonElement-aware converter (psbproj load returns JsonElement).
                var raw = comp.Parameters["generated_path"];
                var path = AsStringOrNull(raw);
                if (IsValidControllerModule(path)) continue;

                var defRaw = comp.Descriptor.Parameters?
                    .FirstOrDefault(p => p.Name == "generated_path")?
                    .DefaultValue;
                var defVal = AsStringOrNull(defRaw);
                var fallback = IsValidControllerModule(defVal) ? defVal : "";
                comp.Parameters["generated_path"] = fallback;
                try
                {
                    LogcatTraceHelper.Instance.SimpleMessage(
                        "Load",
                        $"{comp.InstanceName}: generated_path repaired ({path} -> {fallback})");
                }
                catch { /* logging is best-effort */ }
            }
        }

        /// <summary>
        /// 指定パスが有効な ControllerBox モジュール（build_runtime() を持つ .py）か判定。
        /// </summary>
        /// <summary>
        /// Convert a parameter value (possibly JsonElement) to string.
        /// Mirrors DesignCanvas.AsStringOrNull, needed because psbproj load
        /// deserializes Parameters into System.Text.Json.JsonElement.
        /// </summary>
        private static string AsStringOrNull(object value)
        {
            if (value == null) return null;
            if (value is string s) return s;
            if (value is System.Text.Json.JsonElement je)
            {
                return je.ValueKind == System.Text.Json.JsonValueKind.String
                    ? je.GetString()
                    : je.ToString();
            }
            return value.ToString();
        }

        /// <summary>
        /// Resolve a possibly-relative path against CWD and the repo root
        /// (walks up to PowerSimBuilder.sln). Returns null if not found.
        /// </summary>
        private static string ResolveRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                if (System.IO.File.Exists(path)) return path;
                var dir = System.IO.Directory.GetCurrentDirectory();
                var roots = new System.Collections.Generic.List<string> { dir };
                while (!string.IsNullOrEmpty(dir))
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(dir, "PowerSimBuilder.sln")))
                    {
                        if (!roots.Contains(dir)) roots.Add(dir);
                        break;
                    }
                    var parent = System.IO.Path.GetDirectoryName(dir);
                    if (parent == dir) break;
                    dir = parent;
                }
                foreach (var root in roots)
                {
                    var candidate = System.IO.Path.Combine(root, path);
                    if (System.IO.File.Exists(candidate)) return candidate;
                }
                return null;
            }
            catch { return null; }
        }

        private static bool IsValidControllerModule(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                // Resolve relative paths against CWD and repo root (Trap #78).
                var resolved = ResolveRelativePath(path);
                if (resolved == null) return false;
                var content = System.IO.File.ReadAllText(resolved, System.Text.Encoding.UTF8);
                return content.Contains("def build_runtime");
            }
            catch { return false; }
        }

        /// <summary>
        /// UNDOボタンクリック
        /// 注意点: 直前の操作を取り消し
        /// </summary>
        private void undoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Undo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// REDOボタンクリック
        /// 注意点: 取り消した操作を再実行
        /// </summary>
        private void redoButton_Click(object sender, EventArgs e)
        {
            _canvasManager.Redo();
            designCanvas1.Invalidate();
        }

        /// <summary>
        /// 配置整理ボタンクリック
        /// 注意点: 自動レイアウトを実行
        /// </summary>
        private void arrangeButton_Click(object sender, EventArgs e)
        {
            designCanvas1.ArrangeComponents();
        }

        /// <summary>
        /// 実行ボタンクリック
        /// 注意点: Pythonシミュレーションコードを生成・実行
        /// </summary>
        // ============================================================
        // Task 3-C-9: パラメータ編集ハンドラ
        // ============================================================
        private void paramsEditButton_Click(object sender, EventArgs e)
        {
            var selected = _canvasManager.SelectedComponent;
            if (selected == null)
            {
                MessageBox.Show(this,
                    I18n.T("MainForm.MsgParamSelectFirst"),
                    I18n.T("Common.MsgTitleInfo"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new ParametersEditorForm(selected))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _canvasManager.IsModified = true;
                    designCanvas1.Invalidate();
                    propertyGrid1.Refresh();
                }
            }
        }
        private void staTableImportButton_Click(object sender, EventArgs e)
        {
            using (var form = new StaTableImportForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                // Task 3-C-6: 生成された .py からプロジェクト固有の Descriptor を構築
                try
                {
                    var project = form.ImportedProject;
                    var genPath = form.ImportedGeneratedPath;
                    var srcXml = form.ImportedSourceXmlPath;
                    if (project == null || string.IsNullOrEmpty(genPath)) return;

                    var descriptor = ControllerBoxDescriptorBuilder.Build(
                        project.Name, genPath, srcXml);

                    // 既存チェック（同じプロジェクト名があれば上書き）
                    var existing = componentPalette1.AvailableComponents
                        .FirstOrDefault(d => d.Name == descriptor.Name
                                          && d.Type == "ControllerBox");
                    if (existing != null)
                    {
                        // パレット上の Descriptor を更新（ポート構成を最新化）
                        componentPalette1.UpdateComponent(existing, descriptor);
                        Console.WriteLine($"[ControllerBox] Updated descriptor: {descriptor.Name}");
                    }
                    else
                    {
                        componentPalette1.AddComponent(descriptor);
                        // NOTE: _components は componentPalette1.AvailableComponents と
                        // 同一リストなので、ここで追加すると二重追加になる（Fix 3-C-7b）
                        Console.WriteLine($"[ControllerBox] Added descriptor: {descriptor.Name}");
                    }

                    ComponentPaletteStorage.SaveDescriptors(
                        componentPalette1.AvailableComponents);

                    MessageBox.Show(this,
                        I18n.T("MainForm.MsgPaletteAdded", project.Name, descriptor.InputPorts.Length, descriptor.OutputPorts.Length),
                        "ControllerBox", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        I18n.T("MainForm.MsgDescriptorFail", ex.Message),
                        I18n.T("Common.MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Warns the user at startup if Python is not on PATH.
        /// Disables the Create Simulation button. Code generation still works.
        /// </summary>
        private void CheckPythonAtStartup()
        {
            if (PythonExecutor.IsPythonAvailable())
                return;

            if (createSimButton != null)
            {
                createSimButton.Enabled = false;
                createSimButton.ToolTipText = I18n.T("MainForm.MsgPythonMissingAtStartup");
            }

            MessageBox.Show(
                I18n.T("MainForm.MsgPythonMissingAtStartup"),
                I18n.T("Common.MsgTitleWarning"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void createSimButton_Click(object sender, EventArgs e)
        {
            // Auto-save to generated/demo_main.py (no SaveFileDialog).
            string file_path = BuildPythonSimulationAutoSave(_canvasManager);
            if (string.IsNullOrEmpty(file_path) || !File.Exists(file_path))
                return;

            // Non-modal editor: MainForm stays interactive (Save always clickable).
            var editorForm = new PythonEditorForm(file_path, true, initialTEnd: _currentTEnd);
            editorForm.SimTimeChanged += (s, v) => _currentTEnd = v;
            editorForm.Executor = new PythonExecutor { SilentMode = true };
            if (Test_IsTestMode()) editorForm.SuppressDialogs = true;
            editorForm.FormClosed += (s, e) => this.Activate();
            editorForm.Show(this);
        }
        /// <summary>
        /// Pythonシミュレーションコードの出力ディレクトリパスを取得します
        /// 処理内容:
        ///   - アプリケーションデータフォルダ下に専用ディレクトリを構成
        ///   - ディレクトリが存在しない場合は新規作成
        /// 注意点:
        ///   - 環境ごとの適切なアプリケーションデータフォルダを使用
        ///   - マルチユーザー環境でも安全に動作
        /// 出力例:
        ///   - Windows: C:\Users\[ユーザー名]\AppData\Roaming\PowerSimBuilder\PythonOutput\
        ///   - macOS: /Users/[ユーザー名]/.config/PowerSimBuilder/PythonOutput/
        ///   - Linux: /home/[ユーザー名]/.config/PowerSimBuilder/PythonOutput/
        /// </summary>
        /// <returns>Pythonコードファイルの保存先ディレクトリのフルパス</returns>
        private string GetPythonOutputDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string outputDir = Path.Combine(appData, "PowerSimBuilder", "PythonOutput");

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            return outputDir;
        }

        /// <summary>
        /// 接続選択イベントハンドラ
        /// 注意点: プロパティグリッドに接続プロパティを表示
        /// </summary>
        private void DesignCanvas_ConnectionSelected(object sender, ConnectionEventArgs e)
        {
            if (e.Connection != null)
            {
                propertyGrid1.SelectedObject = new ConnectionProperties(e.Connection);
            }
            else
            {
                propertyGrid1.SelectedObject = null;
            }
        }

        /// <summary>
        /// プロパティ値変更イベント
        /// 注意点: 故障監視システムの条件編集をトリガー
        /// </summary>
        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (_canvasManager.SelectedComponent?.Descriptor.Type == "FaultMonitor" &&
                e.ChangedItem.Label == "Conditions")
            {
                OpenFaultConditionEditor();
            }
        }

        /// <summary>
        /// 故障条件エディタを開く
        /// 注意点: 選択中の故障監視システムに対して実行
        /// </summary>
        public void OpenFaultConditionEditor()
        {
            var components = _canvasManager.GetAllComponents();
            using (var form = new FaultConditionEditorForm(components))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var faultMonitor = _canvasManager.SelectedComponent;
                    if (faultMonitor != null)
                    {
                        faultMonitor.Parameters["Conditions"] = form.Conditions;
                    }
                }
            }
        }

        /// <summary>
        /// 保存状態を更新
        /// 注意点: 保存ボタンの有効状態を制御
        /// </summary>
        private void UpdateSaveStatus()
        {
            saveButton.Enabled = _canvasManager.IsModified;
            if (saveAsButton != null) saveAsButton.Enabled = true;
        }

        /// <summary>
        /// UNDO/REDOボタン状態を更新
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            undoButton.Enabled = _canvasManager.CanUndo;
            redoButton.Enabled = _canvasManager.CanRedo;
        }

        /// <summary>
        /// 削除ボタン状態を更新
        /// 注意点: 選択対象がある場合のみ有効化
        /// </summary>
        private void UpdateDeleteButton()
        {
            removePalletButton.Enabled =
                _canvasManager.SelectedComponent != null ||
                _canvasManager.SelectedConnection != null;
        }
        /// <summary>
        /// CanvasManagerからコンポーネントと接続情報を取得し、SaveFileDialogを使用して
        /// Pythonシミュレーションファイルを生成します
        /// </summary>
        /// <param name="manager">コンポーネントと接続情報を含むCanvasManagerインスタンス</param>
        /// <returns>生成されたPythonファイルのパス。ユーザーがキャンセルした場合やエラーが発生した場合は空文字列を返します</returns>
        /// <summary>
        /// Auto-save Python simulation to generated/demo_main.py (no dialog).
        /// Used by Create Simulation button to avoid double-dialog UX.
        /// </summary>
        public string BuildPythonSimulationAutoSave(CanvasManager manager)
        {
            string outputPath = Path.Combine(
                Directory.GetCurrentDirectory(), "generated", "demo_main.py");
            string? outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            try
            {
                List<ComponentDescriptor> components = manager.UsedComponentDescriptors;
                List<ComponentControl> componentInstances = manager.GetAllComponents();
                List<Connection> connections = manager.Connections;

                var builder = new PythonSimulationMainBuilder();
                builder.FilePath = outputPath;
                builder.TEnd = _currentTEnd;
                builder.Components = components;
                builder.ComponentInstances = componentInstances;
                builder.Connections = connections;
                builder.ErrorHandler = (category, message) =>
                {
                    MessageBox.Show(I18n.T("MainForm.MsgErrCategory", category, message),
                        I18n.T("Common.MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                builder.BuildPythonSimulationFile();
                return outputPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show(I18n.T("MainForm.MsgGenerateFail", ex.Message),
                    I18n.T("Common.MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        public string BuildPythonSimulationWithFileDialog(CanvasManager manager)
        {
            List<ComponentDescriptor> components = manager.UsedComponentDescriptors;
            List<ComponentControl> componentInstances = manager.GetAllComponents();  // Task 3-C-8
            List<Connection> connections = manager.Connections;
            string pyfile = string.Empty;
            try
            {
                // SaveFileDialogのインスタンスを作成（Windows FormsではOpenFileDialogではなくSaveFileDialogを使用）
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                // ダイアログの設定
                saveFileDialog.Title = I18n.T("MainForm.SimSaveTitle");
                saveFileDialog.Filter = I18n.T("Common.FilterPython");
                saveFileDialog.DefaultExt = ".py";
                saveFileDialog.AddExtension = true;

                // 初期ディレクトリを設定（オプション）
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // ダイアログを表示し、ユーザーがOKをクリックしたか確認
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // PythonSimulationMainBuilderのインスタンスを作成
                    PythonSimulationMainBuilder builder = new PythonSimulationMainBuilder();

                    // ファイルパスを設定
                    builder.FilePath = saveFileDialog.FileName;
                    builder.TEnd = _currentTEnd;
                    pyfile = builder.FilePath;

                    // コンポーネントと接続を引数から設定
                    builder.Components = components;
                    builder.ComponentInstances = componentInstances;  // Task 3-C-8
                    builder.Connections = connections;

                    // エラーハンドラーを設定（オプション）
                    builder.ErrorHandler = (category, message) =>
                    {
                        MessageBox.Show(I18n.T("MainForm.MsgErrCategory", category, message), I18n.T("Common.MsgTitleError"),
                                      MessageBoxButtons.OK, MessageBoxIcon.Error);
                    };

                    // ファイルをビルド
                    builder.BuildPythonSimulationFile();

                    // Task 3-C-11: 「生成成功」MessageBox は削除（PythonEditorForm がすぐ開く）
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(I18n.T("MainForm.MsgGenerateFail", ex.Message),
                              I18n.T("Common.MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return pyfile;
        }
    }
}
