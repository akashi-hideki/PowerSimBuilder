using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    public class CanvasManager
    {
        // イベント
        public event EventHandler CanvasModified;
        public event EventHandler SelectionChanged;

        private readonly List<ComponentControl> _components = new List<ComponentControl>();
        private readonly List<Connection> _connections = new List<Connection>();
        private readonly Stack<CanvasState> _undoStack = new Stack<CanvasState>();
        private readonly Stack<CanvasState> _redoStack = new Stack<CanvasState>();

        // 選択状態
        public ComponentControl SelectedComponent { get; private set; }
        public Connection SelectedConnection { get; private set; }

        // 変更状態フラグ
        public bool IsModified { get; set; }
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public DesignCanvas Canvas { get; }
        public ComponentPalette Palette { get; }

        public CanvasManager(DesignCanvas canvas, ComponentPalette palette)
        {
            Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));

            Canvas.ComponentAdded += Canvas_ComponentAdded;
            Canvas.ComponentRemoved += Canvas_ComponentRemoved;
            Canvas.ComponentSelected += Canvas_ComponentSelected;
            Canvas.ConnectionAdded += Canvas_ConnectionAdded;
            Canvas.ConnectionRemoved += Canvas_ConnectionRemoved;
            Canvas.ConnectionSelected += Canvas_ConnectionSelected;
        }


        public void AddComponent(ComponentDescriptor descriptor, Point location)
        {
            SaveStateForUndo();

            var component = new ComponentControl(descriptor)
            {
                Location = location
            };

            _components.Add(component);
            Canvas.AddComponent(component);

            NotifyCanvasModified();
        }

        public void RemoveComponent(ComponentControl component)
        {
            if (component == null) return;

            SaveStateForUndo();

            var relatedConnections = _connections
                .Where(c => c.StartComponent == component || c.EndComponent == component)
                .ToList();

            foreach (var connection in relatedConnections)
            {
                RemoveConnection(connection);
            }

            _components.Remove(component);
            Canvas.RemoveComponent(component);

            if (SelectedComponent == component)
            {
                SelectedComponent = null;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            NotifyCanvasModified();
        }

        public void MoveComponent(ComponentControl component, Point newLocation)
        {
            if (component == null) return;

            SaveStateForUndo();

            component.Location = newLocation;
            Canvas.Invalidate();
            NotifyCanvasModified();
        }

        public void AddConnection(ComponentControl startComponent, string startPort,
                                ComponentControl endComponent, string endPort)
        {
            if (startComponent == null || endComponent == null)
                throw new ArgumentNullException();

            if (string.IsNullOrEmpty(startPort))
                throw new ArgumentException("Start port must be specified");

            if (string.IsNullOrEmpty(endPort))
                throw new ArgumentException("End port must be specified");

            SaveStateForUndo();

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

        public void RemoveConnection(Connection connection)
        {
            if (connection == null) return;

            SaveStateForUndo();

            _connections.Remove(connection);
            Canvas.RemoveConnection(connection);

            if (SelectedConnection == connection)
            {
                SelectedConnection = null;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            NotifyCanvasModified();
        }

        public void SelectComponent(ComponentControl component)
        {
            if (SelectedComponent == component) return;

            SelectedComponent = component;
            SelectedConnection = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }
        public List<Connection> GetAllConnections()
        {
            return new List<Connection>(_connections);
        }
        public void SelectConnection(Connection connection)
        {
            if (SelectedConnection == connection) return;

            SelectedConnection = connection;
            SelectedComponent = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        public void DeselectAll()
        {
            SelectedComponent = null;
            SelectedConnection = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        public void Undo()
        {
            if (!CanUndo) return;

            var currentState = new CanvasState(this);
            var previousState = _undoStack.Pop();

            _redoStack.Push(currentState);
            ApplyState(previousState);
        }

        public void Redo()
        {
            if (!CanRedo) return;

            var currentState = new CanvasState(this);
            var nextState = _redoStack.Pop();

            _undoStack.Push(currentState);
            ApplyState(nextState);
        }

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

        public void LoadModel(CanvasModel model)
        {
            SaveStateForUndo();

            Clear();

            foreach (var compModel in model.Components)
            {
                var component = new ComponentControl(compModel.Descriptor)
                {
                    Location = compModel.Location,
                    InstanceName = compModel.InstanceName
                };

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

            foreach (var connModel in model.Connections)
            {
                if (connModel.StartComponentIndex >= 0 && connModel.StartComponentIndex < _components.Count &&
                    connModel.EndComponentIndex >= 0 && connModel.EndComponentIndex < _components.Count)
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

            SelectedComponent = model.SelectedComponentIndex >= 0 && model.SelectedComponentIndex < _components.Count
                ? _components[model.SelectedComponentIndex]
                : null;

            SelectedConnection = model.SelectedConnectionIndex >= 0 && model.SelectedConnectionIndex < _connections.Count
                ? _connections[model.SelectedConnectionIndex]
                : null;

            IsModified = false;
            NotifyCanvasModified();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            Canvas.Invalidate();
        }

        public List<ComponentControl> GetAllComponents()
        {
            return new List<ComponentControl>(_components);
        }

        public void Clear()
        {
            _components.Clear();
            _connections.Clear();
            Canvas.Clear(); // Clearメソッドを呼び出す
            SelectedComponent = null;
            SelectedConnection = null;
            IsModified = false;
        }


        private void SaveStateForUndo()
        {
            _undoStack.Push(new CanvasState(this));
            _redoStack.Clear();
        }
        private void ApplyState(CanvasState state)
        {
            _components.Clear();
            foreach (var compModel in state.Components)
            {
                var component = new ComponentControl(compModel.Descriptor)
                {
                    Location = compModel.Location,
                    InstanceName = compModel.InstanceName
                };

                foreach (var param in compModel.Parameters)
                {
                    if (component.Parameters.ContainsKey(param.Key))
                    {
                        component.Parameters[param.Key] = param.Value;
                    }
                }
                for (int i = 0; i < _connections.Count && i < state.ConnectionSelectionStates.Count; i++)
                {
                    _connections[i].IsSelected = state.ConnectionSelectionStates[i];
                }
                _components.Add(component);
            }

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

            // キャンバスにコンポーネントと接続を追加
            Canvas.Clear();
            foreach (var component in _components)
            {
                Canvas.AddComponent(component);
            }
            foreach (var connection in _connections)
            {
                Canvas.AddConnection(connection);
            }

            // 選択状態の復元
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

        private void NotifyCanvasModified()
        {
            IsModified = true;
            CanvasModified?.Invoke(this, EventArgs.Empty);
        }

        private class CanvasState
        {
            
            public List<ComponentModel> Components { get; } = new List<ComponentModel>();
            public List<ConnectionModel> Connections { get; } = new List<ConnectionModel>();
            public int SelectedComponentIndex { get; set; } = -1;
            public int SelectedConnectionIndex { get; set; } = -1;
            public List<bool> ConnectionSelectionStates { get; } = new List<bool>();

            public CanvasState(CanvasManager manager)
            {
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

                SelectedComponentIndex = manager._components.IndexOf(manager.SelectedComponent);
                SelectedConnectionIndex = manager._connections.IndexOf(manager.SelectedConnection);
            }
        }
    }

    public class CanvasModel
    {
        public List<ComponentModel> Components { get; set; } = new List<ComponentModel>();
        public List<ConnectionModel> Connections { get; set; } = new List<ConnectionModel>();
        public int SelectedComponentIndex { get; set; } = -1;
        public int SelectedConnectionIndex { get; set; } = -1;
    }

    public class ComponentModel
    {
        public ComponentDescriptor Descriptor { get; set; }
        public Dictionary<string, object> Parameters { get; set; }

        [JsonConverter(typeof(PointJsonConverter))]
        public Point Location { get; set; }
        public string InstanceName { get; set; }
    }

    public class ConnectionModel
    {
        public int StartComponentIndex { get; set; }
        public string StartPort { get; set; }
        public int EndComponentIndex { get; set; }
        public string EndPort { get; set; }
    }
}