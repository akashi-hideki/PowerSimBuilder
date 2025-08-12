using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PowerSimBuilder
{
    /// <summary>
    /// 自動レイアウトエンジン
    /// コンポーネントを自動的に配置する
    /// </summary>
    public class AutoLayoutEngine
    {
        private const int HorizontalSpacing = 150;
        private const int VerticalSpacing = 100;
        private const int Margin = 50;

        /// <summary>
        /// コンポーネントを自動配置する
        /// </summary>
        public void ArrangeComponents(DesignCanvas canvas)
        {
            var components = canvas.Components;
            if (!components.Any()) return;

            var rootComponents = FindRootComponents(canvas);
            if (!rootComponents.Any()) return;

            var layout = new Dictionary<int, List<ComponentControl>>();
            // int currentLevel = 0; // 未使用変数を削除

            var queue = new Queue<(ComponentControl, int)>();
            var visited = new HashSet<ComponentControl>();

            foreach (var root in rootComponents)
            {
                queue.Enqueue((root, 0));
                visited.Add(root);
            }

            while (queue.Count > 0)
            {
                var (current, level) = queue.Dequeue();

                if (!layout.ContainsKey(level))
                    layout[level] = new List<ComponentControl>();

                layout[level].Add(current);

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