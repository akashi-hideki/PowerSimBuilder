using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PowerSimBuilder
{
    /// <summary>
    /// 自動レイアウトエンジン
    /// 作成日: 2025-08-12
    /// 注意点: グラフ理論に基づいたコンポーネント配置アルゴリズムを実装
    /// </summary>
    public class AutoLayoutEngine
    {
        private const int HorizontalSpacing = 150;
        private const int VerticalSpacing = 100;
        private const int Margin = 50;

        /// <summary>
        /// コンポーネントを自動配置する
        /// 処理内容:
        ///   1. ルートコンポーネント（入力のないコンポーネント）を検出
        ///   2. BFS（幅優先探索）でコンポーネントの階層を決定
        ///   3. 各階層のコンポーネントを水平方向に均等配置
        /// 注意点:
        ///   - 接続関係に基づいた配置のため、事前に接続情報が必要
        ///   - ルートコンポーネントがない場合、配置は実行されない
        /// </summary>
        public void ArrangeComponents(DesignCanvas canvas)
        {
            var components = canvas.Components;
            if (!components.Any()) return;

            var rootComponents = FindRootComponents(canvas);
            if (!rootComponents.Any()) return;

            var layout = new Dictionary<int, List<ComponentControl>>();
            var queue = new Queue<(ComponentControl, int)>();
            var visited = new HashSet<ComponentControl>();

            // ルートコンポーネントから探索開始
            foreach (var root in rootComponents)
            {
                queue.Enqueue((root, 0));
                visited.Add(root);
            }

            // BFSで階層構造を構築
            while (queue.Count > 0)
            {
                var (current, level) = queue.Dequeue();

                if (!layout.ContainsKey(level))
                    layout[level] = new List<ComponentControl>();

                layout[level].Add(current);

                // 下流コンポーネントを探索
                var nextComponents = canvas.ConnectionGraph
                    .GetConnectionsFrom(current)
                    .Select(c => c.EndComponent)
                    .Where(c => !visited.Contains(c));

                foreach (var next in nextComponents)
                {
                    queue.Enqueue((next, level + 1));
                    visited.Add(next);
                }
            }

            int maxWidth = canvas.Width - Margin * 2;

            // 各階層のコンポーネントを配置
            for (int level = 0; level < layout.Count; level++)
            {
                var levelComponents = layout[level];
                int levelWidth = levelComponents.Count * HorizontalSpacing;
                int startX = (canvas.Width - levelWidth) / 2;

                for (int i = 0; i < levelComponents.Count; i++)
                {
                    var component = levelComponents[i];
                    component.Location = new Point(
                        startX + i * HorizontalSpacing,
                        Margin + level * VerticalSpacing
                    );
                }
            }
        }

        /// <summary>
        /// ルートコンポーネントを検出（入力ポートがないコンポーネント）
        /// 処理内容:
        ///   - 全コンポーネントから入力ポートを持たないものを抽出
        /// 注意点:
        ///   - 入力ポート配列がnullまたは空の場合をルートと判定
        /// </summary>
        private List<ComponentControl> FindRootComponents(DesignCanvas canvas)
        {
            return canvas.Components
                .Where(c => 
                    c.Descriptor.InputPorts == null || 
                    c.Descriptor.InputPorts.Length == 0)
                .ToList();
        }
    }
}