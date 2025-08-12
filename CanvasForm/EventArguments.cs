using System;
using System.Drawing;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネントイベント引数クラス
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        public ComponentControl Component { get; }

        public ComponentEventArgs(ComponentControl component)
        {
            Component = component;
        }
    }

    /// <summary>
    /// 接続イベント引数クラス
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        public Connection Connection { get; }

        public ConnectionEventArgs(Connection connection)
        {
            Connection = connection;
        }
    }

    /// <summary>
    /// ポートイベント引数クラス
    /// </summary>
    public class PortEventArgs : EventArgs
    {
        public string PortName { get; }

        public PortEventArgs(string portName)
        {
            PortName = portName;
        }
    }
}