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
        }

        /// <summary>
        /// コンポーネントに関連するすべての接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnections(ComponentControl component)
        {
            var connections = new List<Connection>();

            if (_connectionsFrom.ContainsKey(component))
                connections.AddRange(_connectionsFrom[component]);

            if (_connectionsTo.ContainsKey(component))
                connections.AddRange(_connectionsTo[component]);

            return connections.Distinct();
        }

        /// <summary>
        /// コンポーネントからの接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnectionsFrom(ComponentControl component)
        {
            return _connectionsFrom.ContainsKey(component)
                ? _connectionsFrom[component].ToList() // コピーを返す
                : Enumerable.Empty<Connection>();
        }

        /// <summary>
        /// コンポーネントへの接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnectionsTo(ComponentControl component)
        {
            return _connectionsTo.ContainsKey(component)
                ? _connectionsTo[component].ToList() // コピーを返す
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
            }

            // コンポーネントへの接続をすべて削除
            if (_connectionsTo.ContainsKey(component))
            {
                var connectionsToRemove = _connectionsTo[component].ToList();
                foreach (var conn in connectionsToRemove)
                {
                    RemoveConnection(conn);
                }
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