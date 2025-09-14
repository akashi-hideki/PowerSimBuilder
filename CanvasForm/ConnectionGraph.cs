using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerSimBuilder
{
    /// <summary>
    /// 接続関係をグラフ構造で管理するクラス
    /// </summary>
    public class ConnectionGraph
    {
        private readonly Dictionary<ComponentControl, List<Connection>> _connectionsFrom
            = new Dictionary<ComponentControl, List<Connection>>();

        private readonly Dictionary<ComponentControl, List<Connection>> _connectionsTo
            = new Dictionary<ComponentControl, List<Connection>>();

        /// <summary>
        /// 接続を追加
        /// </summary>
        public void AddConnection(Connection connection)
        {
            if (connection == null || connection.StartComponent == null || connection.EndComponent == null)
                return;

            AddToDictionary(_connectionsFrom, connection.StartComponent, connection);
            AddToDictionary(_connectionsTo, connection.EndComponent, connection);
        }

        /// <summary>
        /// 接続を削除
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            if (connection == null) return;

            RemoveFromDictionary(_connectionsFrom, connection.StartComponent, connection);
            RemoveFromDictionary(_connectionsTo, connection.EndComponent, connection);

            // 空のリストを削除してメモリリークを防止
            CleanupEmptyEntries();
        }

        /// <summary>
        /// コンポーネントに関連するすべての接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnections(ComponentControl component)
        {
            var connections = new List<Connection>();

            if (component != null && _connectionsFrom.ContainsKey(component))
                connections.AddRange(_connectionsFrom[component]);

            if (component != null && _connectionsTo.ContainsKey(component))
                connections.AddRange(_connectionsTo[component]);

            return connections.Distinct();
        }

        /// <summary>
        /// コンポーネントからの接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnectionsFrom(ComponentControl component)
        {
            return (component != null && _connectionsFrom.ContainsKey(component))
                ? _connectionsFrom[component].ToList()
                : Enumerable.Empty<Connection>();
        }

        /// <summary>
        /// コンポーネントへの接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnectionsTo(ComponentControl component)
        {
            return (component != null && _connectionsTo.ContainsKey(component))
                ? _connectionsTo[component].ToList()
                : Enumerable.Empty<Connection>();
        }

        /// <summary>
        /// 接続が存在するか確認
        /// </summary>
        public bool ConnectionExists(Connection connection)
        {
            if (connection == null || connection.StartComponent == null)
                return false;

            return _connectionsFrom.ContainsKey(connection.StartComponent) &&
                   _connectionsFrom[connection.StartComponent].Contains(connection);
        }

        /// <summary>
        /// 特定の接続が存在するか確認
        /// </summary>
        public bool ConnectionExists(ComponentControl start, string startPort,
                                    ComponentControl end, string endPort)
        {
            if (start == null || end == null)
                return false;

            return _connectionsFrom.ContainsKey(start) &&
                   _connectionsFrom[start].Any(c =>
                       c.StartPort == startPort &&
                       c.EndComponent == end &&
                       c.EndPort == endPort);
        }

        /// <summary>
        /// すべての接続をクリア
        /// </summary>
        public void Clear()
        {
            _connectionsFrom.Clear();
            _connectionsTo.Clear();
        }

        private void AddToDictionary(Dictionary<ComponentControl, List<Connection>> dict,
                                    ComponentControl key, Connection connection)
        {
            if (key == null) return;

            if (!dict.ContainsKey(key))
                dict[key] = new List<Connection>();

            // 重複チェック
            if (!dict[key].Contains(connection))
                dict[key].Add(connection);
        }

        private void RemoveFromDictionary(Dictionary<ComponentControl, List<Connection>> dict,
                                         ComponentControl key, Connection connection)
        {
            if (key != null && dict.ContainsKey(key))
                dict[key].Remove(connection);
        }

        /// <summary>
        /// 空のエントリをクリーンアップ
        /// </summary>
        private void CleanupEmptyEntries()
        {
            // 空のリストを削除
            var emptyFromKeys = _connectionsFrom.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
            foreach (var key in emptyFromKeys)
            {
                _connectionsFrom.Remove(key);
            }

            var emptyToKeys = _connectionsTo.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
            foreach (var key in emptyToKeys)
            {
                _connectionsTo.Remove(key);
            }
        }

        /// <summary>
        /// コンポーネントに関連するすべての接続を強制的に削除
        /// </summary>
        public void RemoveAllConnections(ComponentControl component)
        {
            if (component == null) return;

            // コンポーネントからの接続をすべて削除
            if (_connectionsFrom.ContainsKey(component))
            {
                var connectionsToRemove = _connectionsFrom[component].ToList();
                foreach (var conn in connectionsToRemove)
                {
                    RemoveConnection(conn);
                }
                _connectionsFrom.Remove(component); // コンポーネント自体も削除
            }

            // コンポーネントへの接続をすべて削除
            if (_connectionsTo.ContainsKey(component))
            {
                var connectionsToRemove = _connectionsTo[component].ToList();
                foreach (var conn in connectionsToRemove)
                {
                    RemoveConnection(conn);
                }
                _connectionsTo.Remove(component); // コンポーネント自体も削除
            }
        }
        /// <summary>
        /// 接続グラフと接続リストの同期を確認
        /// </summary>
        /// <summary>
        /// 接続グラフと接続リストの同期を確認
        /// </summary>
        public void SynchronizeConnections(List<Connection> currentConnections)
        {
            try
            {
                // グラフに存在するが現在の接続リストにない接続を削除
                var allGraphConnections = _connectionsFrom.SelectMany(kvp => kvp.Value)
                    .Concat(_connectionsTo.SelectMany(kvp => kvp.Value))
                    .Distinct()
                    .ToList();

                var orphanedConnections = allGraphConnections
                    .Where(conn => !currentConnections.Contains(conn))
                    .ToList();

                foreach (var conn in orphanedConnections)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("ConnectionGraph",
                        $"Removing orphaned connection: {conn.StartComponent?.InstanceName} -> {conn.EndComponent?.InstanceName}");
                    RemoveConnection(conn);
                }

                // 現在の接続リストに存在するがグラフにない接続を追加
                var missingConnections = currentConnections
                    .Where(conn => !allGraphConnections.Contains(conn) &&
                                  conn.StartComponent != null && conn.EndComponent != null)
                    .ToList();

                foreach (var conn in missingConnections)
                {
                    LogcatTraceHelper.Instance.SimpleMessage("ConnectionGraph",
                        $"Adding missing connection: {conn.StartComponent?.InstanceName} -> {conn.EndComponent?.InstanceName}");
                    AddConnection(conn);
                }
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("ConnectionGraph",
                    $"Error synchronizing connections: {ex.Message}");
            }
        }
        /// <summary>
        /// グラフの整合性を検証
        /// </summary>
        public void ValidateIntegrity(List<ComponentControl> allComponents)
        {
            var componentSet = new HashSet<ComponentControl>(allComponents);

            // 存在しないコンポーネントへの参照を削除
            var invalidFromKeys = _connectionsFrom.Keys.Where(key => !componentSet.Contains(key)).ToList();
            foreach (var key in invalidFromKeys)
            {
                _connectionsFrom.Remove(key);
            }

            var invalidToKeys = _connectionsTo.Keys.Where(key => !componentSet.Contains(key)).ToList();
            foreach (var key in invalidToKeys)
            {
                _connectionsTo.Remove(key);
            }

            // 各接続のコンポーネント参照を検証
            foreach (var connections in _connectionsFrom.Values)
            {
                connections.RemoveAll(conn =>
                    conn.StartComponent == null ||
                    conn.EndComponent == null ||
                    !componentSet.Contains(conn.StartComponent) ||
                    !componentSet.Contains(conn.EndComponent));
            }

            foreach (var connections in _connectionsTo.Values)
            {
                connections.RemoveAll(conn =>
                    conn.StartComponent == null ||
                    conn.EndComponent == null ||
                    !componentSet.Contains(conn.StartComponent) ||
                    !componentSet.Contains(conn.EndComponent));
            }
        }
        /// <summary>
        /// 接続元のデータを取得（デバッグ用）
        /// </summary>
        public Dictionary<ComponentControl, List<Connection>> GetConnectionsFromData()
        {
            return _connectionsFrom.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }

        /// <summary>
        /// 接続先のデータを取得（デバッグ用）
        /// </summary>
        public Dictionary<ComponentControl, List<Connection>> GetConnectionsToData()
        {
            return _connectionsTo.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }
    }
 
}