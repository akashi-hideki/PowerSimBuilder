using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// キャンバスの状態と操作を管理するクラス
    /// 作成日: 2025-08-12
    /// 注意点: UNDO/REDO機能、保存/読み込み機能を実装
    /// </summary>
    public class CanvasManager
    {
        // イベント定義
        public event EventHandler CanvasModified;
        public event EventHandler SelectionChanged;

        // 状態管理用フィールド
        private readonly List<ComponentControl> _components = new List<ComponentControl>();
        private readonly List<Connection> _connections = new List<Connection>();
        private readonly Stack<CanvasState> _undoStack = new Stack<CanvasState>();
        private readonly Stack<CanvasState> _redoStack = new Stack<CanvasState>();

        /// <summary>
        /// 選択中のコンポーネント
        /// 注意点: 単一選択のみ対応
        /// </summary>
        public ComponentControl SelectedComponent { get; private set; }

        /// <summary>
        /// 選択中の接続
        /// 注意点: 単一選択のみ対応
        /// </summary>
        public Connection SelectedConnection { get; private set; }

        /// <summary>
        /// 変更状態フラグ
        /// 注意点: 保存時にfalseにリセット
        /// </summary>
        public bool IsModified { get; set; }

        /// <summary>
        /// UNDO可能状態
        /// 注意点: ツールバーボタンの有効化に使用
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;

        /// <summary>
        /// REDO可能状態
        /// 注意点: ツールバーボタンの有効化に使用
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>
        /// デザインキャンバス
        /// 注意点: 操作対象のキャンバス
        /// </summary>
        public DesignCanvas Canvas { get; }

        /// <summary>
        /// コンポーネントパレット
        /// 注意点: コンポーネント追加時に使用
        /// </summary>
        public ComponentPalette Palette { get; }
        /// <summary>
        /// キャンバス上で使用されているコンポーネントのディスクリプタリスト
        /// 注意点: 実際に配置されているコンポーネントの情報のみ含む
        /// </summary>
        public List<ComponentDescriptor> UsedComponentDescriptors
        {
            get
            {
                return _components.Select(c => c.Descriptor).ToList();
            }
        }
        /// <summary>
        /// キャンバス上の接続情報
        /// </summary>
        public List<Connection> Connections => Canvas.Connections;
        /// <summary>
        /// コンストラクタ
        /// 注意点: イベントハンドラを登録
        /// </summary>
        public CanvasManager(DesignCanvas canvas, ComponentPalette palette)
        {
            Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));

            // キャンバスイベント購読
            Canvas.ComponentAdded += Canvas_ComponentAdded;
            Canvas.ComponentRemoved += Canvas_ComponentRemoved;
            Canvas.ComponentSelected += Canvas_ComponentSelected;
            Canvas.ConnectionAdded += Canvas_ConnectionAdded;
            Canvas.ConnectionRemoved += Canvas_ConnectionRemoved;
            Canvas.ConnectionSelected += Canvas_ConnectionSelected;
        }

        /// <summary>
        /// コンポーネントを追加
        /// 注意点: UNDO用状態保存を実行
        /// </summary>
        public void AddComponent(ComponentDescriptor descriptor, Point location)
        {
            SaveStateForUndo();

            var component = new ComponentControl(descriptor) { Location = location };
            _components.Add(component);
            Canvas.AddComponent(component);
            NotifyCanvasModified();
        }

        /// <summary>
        /// コンポーネントを削除
        /// 注意点: 関連接続も同時削除
        /// </summary>
        public void RemoveComponent(ComponentControl component)
        {
            if (component == null) return;

            SaveStateForUndo();

            // 関連接続を削除
            var relatedConnections = _connections
                .Where(c => c.StartComponent == component || c.EndComponent == component)
                .ToList();

            foreach (var connection in relatedConnections)
            {
                RemoveConnection(connection);
            }

            // コンポーネント削除
            _components.Remove(component);
            Canvas.RemoveComponent(component);

            // 選択状態更新
            if (SelectedComponent == component)
            {
                SelectedComponent = null;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            NotifyCanvasModified();
        }

        /// <summary>
        /// コンポーネントを移動
        /// 注意点: UNDO用状態保存を実行
        /// </summary>
        public void MoveComponent(ComponentControl component, Point newLocation)
        {
            if (component == null) return;

            SaveStateForUndo();
            component.Location = newLocation;
            Canvas.Invalidate();
            NotifyCanvasModified();
        }

        /// <summary>
        /// 接続を追加
        /// 注意点: 重複接続は追加しない
        /// </summary>
        public void AddConnection(ComponentControl startComponent, string startPort,
                                ComponentControl endComponent, string endPort)
        {
            if (startComponent == null || endComponent == null)
                throw new ArgumentNullException();

            if (string.IsNullOrEmpty(startPort) || string.IsNullOrEmpty(endPort))
                throw new ArgumentException(I18n.T("CanvasManager.MsgPortNameRequired"));

            SaveStateForUndo();

            // 重複チェック
            if (_connections.Any(c =>
                c.StartComponent == startComponent && c.StartPort == startPort &&
                c.EndComponent == endComponent && c.EndPort == endPort))
            {
                return;
            }

            var connection = new Connection
            {
                StartComponent = startComponent,
                StartPort = startPort,
                EndComponent = endComponent,
                EndPort = endPort
            };

            _connections.Add(connection);
            Canvas.AddConnection(connection);
            NotifyCanvasModified();
        }

        /// <summary>
        /// 接続を削除
        /// 注意点: 選択状態も更新
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            if (connection == null) return;

            SaveStateForUndo();
            _connections.Remove(connection);
            Canvas.RemoveConnection(connection);

            // 選択状態更新
            if (SelectedConnection == connection)
            {
                SelectedConnection = null;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            NotifyCanvasModified();
        }

        /// <summary>
        /// コンポーネントを選択
        /// 注意点: 既存選択を解除
        /// </summary>
        public void SelectComponent(ComponentControl component)
        {
            if (SelectedComponent == component) return;

            SelectedComponent = component;
            SelectedConnection = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        /// <summary>
        /// 全接続を取得
        /// 注意点: 外部からの読み取り専用アクセス用
        /// </summary>
        public List<Connection> GetAllConnections()
        {
            return new List<Connection>(_connections);
        }

        /// <summary>
        /// 接続を選択
        /// 注意点: 既存選択を解除
        /// </summary>
        public void SelectConnection(Connection connection)
        {
            if (SelectedConnection == connection) return;

            SelectedConnection = connection;
            SelectedComponent = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        /// <summary>
        /// 全選択解除
        /// 注意点: コンポーネントと接続の選択を解除
        /// </summary>
        public void DeselectAll()
        {
            SelectedComponent = null;
            SelectedConnection = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        /// <summary>
        /// UNDO操作
        /// 注意点: REDOスタックに現在状態を保存
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            var currentState = new CanvasState(this);
            var previousState = _undoStack.Pop();

            _redoStack.Push(currentState);
            ApplyState(previousState);
        }

        /// <summary>
        /// REDO操作
        /// 注意点: UNDOスタックに現在状態を保存
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            var currentState = new CanvasState(this);
            var nextState = _redoStack.Pop();

            _undoStack.Push(currentState);
            ApplyState(nextState);
        }

        /// <summary>
        /// モデルを保存
        /// 注意点: シリアライズ可能な形式で出力
        /// </summary>
        public CanvasModel SaveModel()
        {
            return new CanvasModel
            {
                Components = _components.Select(c => new ComponentModel
                {
                    Descriptor = c.Descriptor,
                    Parameters = c.Parameters.ToDictionary(p => p.Key, p => p.Value),
                    Location = c.Location,
                    InstanceName = c.InstanceName
                }).ToList(),

                Connections = _connections.Select(c => new ConnectionModel
                {
                    StartComponentIndex = _components.IndexOf(c.StartComponent),
                    StartPort = c.StartPort,
                    EndComponentIndex = _components.IndexOf(c.EndComponent),
                    EndPort = c.EndPort
                }).ToList(),

                SelectedComponentIndex = _components.IndexOf(SelectedComponent),
                SelectedConnectionIndex = _connections.IndexOf(SelectedConnection)
            };
        }

        /// <summary>
        /// モデルを読み込み
        /// 注意点: UNDOスタックをクリア
        /// </summary>
        public void LoadModel(CanvasModel model)
        {
            SaveStateForUndo();
            Clear();

            // コンポーネント復元
            foreach (var compModel in model.Components)
            {
                var component = new ComponentControl(compModel.Descriptor)
                {
                    Location = compModel.Location,
                    InstanceName = compModel.InstanceName
                };
                component.Name = component.InstanceName;

                // パラメータ復元
                foreach (var param in compModel.Parameters)
                {
                    if (component.Parameters.ContainsKey(param.Key))
                    {
                        component.Parameters[param.Key] = param.Value;
                    }
                }

                _components.Add(component);
                Canvas.AddComponent(component);
            }

            // 接続復元
            foreach (var connModel in model.Connections)
            {
                if (connModel.StartComponentIndex >= 0 &&
                    connModel.StartComponentIndex < _components.Count &&
                    connModel.EndComponentIndex >= 0 &&
                    connModel.EndComponentIndex < _components.Count)
                {
                    var startComponent = _components[connModel.StartComponentIndex];
                    var endComponent = _components[connModel.EndComponentIndex];

                    var connection = new Connection
                    {
                        StartComponent = startComponent,
                        StartPort = connModel.StartPort,
                        EndComponent = endComponent,
                        EndPort = connModel.EndPort
                    };

                    _connections.Add(connection);
                    Canvas.AddConnection(connection);
                }
            }

            // 選択状態復元
            SelectedComponent = model.SelectedComponentIndex >= 0 &&
                model.SelectedComponentIndex < _components.Count
                ? _components[model.SelectedComponentIndex]
                : null;

            SelectedConnection = model.SelectedConnectionIndex >= 0 &&
                model.SelectedConnectionIndex < _connections.Count
                ? _connections[model.SelectedConnectionIndex]
                : null;

            IsModified = false;
            NotifyCanvasModified();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        /// <summary>
        /// 全コンポーネントを取得
        /// 注意点: 外部からの読み取り専用アクセス用
        /// </summary>
        public List<ComponentControl> GetAllComponents()
        {
            return new List<ComponentControl>(_components);
        }

        /// <summary>
        /// キャンバスをクリア
        /// 注意点: UNDO/REDOスタックは保持
        /// </summary>
        public void Clear()
        {
            _components.Clear();
            _connections.Clear();
            Canvas.Clear();
            SelectedComponent = null;
            SelectedConnection = null;
            IsModified = false;
        }

        /// <summary>
        /// UNDO用状態保存
        /// 注意点: REDOスタックをクリア
        /// </summary>
        private void SaveStateForUndo()
        {
            _undoStack.Push(new CanvasState(this));
            _redoStack.Clear();
        }

        /// <summary>
        /// 状態を適用
        /// 注意点: モデル全体を再構築
        /// </summary>
        private void ApplyState(CanvasState state)
        {
            // コンポーネント復元
            _components.Clear();
            foreach (var compModel in state.Components)
            {
                var component = new ComponentControl(compModel.Descriptor)
                {
                    Location = compModel.Location,
                    InstanceName = compModel.InstanceName
                };
                component.Name = component.InstanceName;

                // パラメータ復元
                foreach (var param in compModel.Parameters)
                {
                    if (component.Parameters.ContainsKey(param.Key))
                    {
                        component.Parameters[param.Key] = param.Value;
                    }
                }
                _components.Add(component);
            }

            // 接続復元
            _connections.Clear();
            foreach (var connModel in state.Connections)
            {
                if (connModel.StartComponentIndex >= 0 &&
                    connModel.StartComponentIndex < _components.Count &&
                    connModel.EndComponentIndex >= 0 &&
                    connModel.EndComponentIndex < _components.Count)
                {
                    var startComponent = _components[connModel.StartComponentIndex];
                    var endComponent = _components[connModel.EndComponentIndex];

                    var connection = new Connection
                    {
                        StartComponent = startComponent,
                        StartPort = connModel.StartPort,
                        EndComponent = endComponent,
                        EndPort = connModel.EndPort
                    };

                    _connections.Add(connection);
                }
            }

            // キャンバスに再追加
            Canvas.Clear();
            foreach (var component in _components)
            {
                Canvas.AddComponent(component);
            }
            foreach (var connection in _connections)
            {
                Canvas.AddConnection(connection);
            }

            // 選択状態復元
            SelectedComponent = state.SelectedComponentIndex >= 0 &&
                state.SelectedComponentIndex < _components.Count
                ? _components[state.SelectedComponentIndex]
                : null;

            SelectedConnection = state.SelectedConnectionIndex >= 0 &&
                state.SelectedConnectionIndex < _connections.Count
                ? _connections[state.SelectedConnectionIndex]
                : null;

            IsModified = true;
            NotifyCanvasModified();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        #region イベントハンドラ
        private void Canvas_ComponentAdded(object sender, ComponentEventArgs e)
        {
            if (!_components.Contains(e.Component))
            {
                _components.Add(e.Component);
                NotifyCanvasModified();
            }
        }

        private void Canvas_ComponentRemoved(object sender, ComponentEventArgs e)
        {
            _components.Remove(e.Component);
            NotifyCanvasModified();
        }

        private void Canvas_ComponentSelected(object sender, ComponentEventArgs e)
        {
            SelectComponent(e.Component);
        }

        private void Canvas_ConnectionAdded(object sender, ConnectionEventArgs e)
        {
            if (!_connections.Contains(e.Connection))
            {
                _connections.Add(e.Connection);
                NotifyCanvasModified();
            }
        }

        private void Canvas_ConnectionRemoved(object sender, ConnectionEventArgs e)
        {
            _connections.Remove(e.Connection);
            NotifyCanvasModified();
        }

        private void Canvas_ConnectionSelected(object sender, ConnectionEventArgs e)
        {
            SelectConnection(e.Connection);
        }
        #endregion

        /// <summary>
        /// 変更通知
        /// 注意点: 変更フラグを設定しイベント発行
        /// </summary>
        private void NotifyCanvasModified()
        {
            IsModified = true;
            CanvasModified?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// キャンバス状態保持クラス
        /// 注意点: シリアライズ可能な形式で状態保存
        /// </summary>
        private class CanvasState
        {
            public List<ComponentModel> Components { get; } = new List<ComponentModel>();
            public List<ConnectionModel> Connections { get; } = new List<ConnectionModel>();
            public int SelectedComponentIndex { get; set; } = -1;
            public int SelectedConnectionIndex { get; set; } = -1;
            public List<bool> ConnectionSelectionStates { get; } = new List<bool>();

            public CanvasState(CanvasManager manager)
            {
                // コンポーネント状態保存
                foreach (var component in manager._components)
                {
                    Components.Add(new ComponentModel
                    {
                        Descriptor = component.Descriptor,
                        Parameters = component.Parameters.ToDictionary(p => p.Key, p => p.Value),
                        Location = component.Location,
                        InstanceName = component.InstanceName
                    });
                }

                // 接続状態保存
                foreach (var connection in manager._connections)
                {
                    Connections.Add(new ConnectionModel
                    {
                        StartComponentIndex = manager._components.IndexOf(connection.StartComponent),
                        StartPort = connection.StartPort,
                        EndComponentIndex = manager._components.IndexOf(connection.EndComponent),
                        EndPort = connection.EndPort
                    });
                    ConnectionSelectionStates.Add(connection.IsSelected);
                }

                // 選択状態保存
                SelectedComponentIndex = manager._components.IndexOf(manager.SelectedComponent);
                SelectedConnectionIndex = manager._connections.IndexOf(manager.SelectedConnection);
            }
        }
    }

    #region モデルクラス
    /// <summary>
    /// キャンバスモデルクラス
    /// 注意点: シリアライズ用データ構造
    /// </summary>
    public class CanvasModel
    {
        public List<ComponentModel> Components { get; set; } = new List<ComponentModel>();
        public List<ConnectionModel> Connections { get; set; } = new List<ConnectionModel>();
        public int SelectedComponentIndex { get; set; } = -1;
        public List<int> SelectedComponentIndices { get; set; } = new List<int>();
        public int SelectedConnectionIndex { get; set; } = -1;
    }

    /// <summary>
    /// コンポーネントモデルクラス
    /// 注意点: シリアライズ用データ構造
    /// </summary>
    public class ComponentModel
    {
        public ComponentDescriptor Descriptor { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        [JsonConverter(typeof(PointJsonConverter))]
        public Point Location { get; set; }
        public string InstanceName { get; set; }
    }

    /// <summary>
    /// 接続モデルクラス
    /// 注意点: インデックス参照でコンポーネントを特定
    /// </summary>
    public class ConnectionModel
    {
        public int StartComponentIndex { get; set; }
        public string StartPort { get; set; }
        public int EndComponentIndex { get; set; }
        public string EndPort { get; set; }
    }
    #endregion
}
