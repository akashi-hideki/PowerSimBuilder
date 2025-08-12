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
            AddToDictionary(_connectionsFrom, connection.StartComponent, connection);
            AddToDictionary(_connectionsTo, connection.EndComponent, connection);
        }

        /// <summary>
        /// 接続を削除
        /// </summary>
        public void RemoveConnection(Connection connection)
        {
            RemoveFromDictionary(_connectionsFrom, connection.StartComponent, connection);
            RemoveFromDictionary(_connectionsTo, connection.EndComponent, connection);
        }

        /// <summary>
        /// コンポーネントに関連するすべての接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnections(ComponentControl component)
        {
            var fromList = _connectionsFrom.ContainsKey(component)
                ? _connectionsFrom[component]
                : Enumerable.Empty<Connection>();

            var toList = _connectionsTo.ContainsKey(component)
                ? _connectionsTo[component]
                : Enumerable.Empty<Connection>();

            return fromList.Concat(toList).Distinct();
        }

        /// <summary>
        /// コンポーネントからの接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnectionsFrom(ComponentControl component)
        {
            return _connectionsFrom.ContainsKey(component)
                ? _connectionsFrom[component]
                : Enumerable.Empty<Connection>();
        }

        /// <summary>
        /// コンポーネントへの接続を取得
        /// </summary>
        public IEnumerable<Connection> GetConnectionsTo(ComponentControl component)
        {
            return _connectionsTo.ContainsKey(component)
                ? _connectionsTo[component]
                : Enumerable.Empty<Connection>();
        }

        /// <summary>
        /// 接続が存在するか確認
        /// </summary>
        public bool ConnectionExists(Connection connection)
        {
            return _connectionsFrom.ContainsKey(connection.StartComponent) &&
                   _connectionsFrom[connection.StartComponent].Contains(connection);
        }

        /// <summary>
        /// 特定の接続が存在するか確認
        /// </summary>
        public bool ConnectionExists(ComponentControl start, string startPort,
                                    ComponentControl end, string endPort)
        {
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
            if (!dict.ContainsKey(key))
                dict[key] = new List<Connection>();

            dict[key].Add(connection);
        }

        private void RemoveFromDictionary(Dictionary<ComponentControl, List<Connection>> dict,
                                         ComponentControl key, Connection connection)
        {
            if (dict.ContainsKey(key))
                dict[key].Remove(connection);
        }
    }
}