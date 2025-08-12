using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PowerSimBuilder
{
    /// <summary>
    /// 接続線を描画するための抽象クラス
    /// </summary>
    public abstract class ConnectionRenderer
    {
        public abstract void Render(Graphics g, Connection connection);

        /// <summary>
        /// 共通のフォールバック接続描画
        /// </summary>
        // ConnectionRenderer.cs
        protected void RenderFallbackConnection(Graphics g, Connection connection)
        {
            if (connection.StartComponent == null || connection.EndComponent == null)
                return;

            // 開始点計算
            Point start = new Point(
                connection.StartComponent.Left + connection.StartComponent.Width / 2,
                connection.StartComponent.Top + connection.StartComponent.Height / 2
            );

            // 終了点計算
            Point end = new Point(
                connection.EndComponent.Left + connection.EndComponent.Width / 2,
                connection.EndComponent.Top + connection.EndComponent.Height / 2
            );

            using (var pen = new Pen(Color.Red, 2) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(pen, start, end);
            }

            using (var brush = new SolidBrush(Color.Red))
            {
                g.FillRectangle(brush, start.X - 4, start.Y - 4, 8, 8);
                g.FillRectangle(brush, end.X - 4, end.Y - 4, 8, 8);
            }
        }
    }

    /// <summary>
    /// 直線で接続線を描画するレンダラー
    /// </summary>
    public class StraightConnectionRenderer : ConnectionRenderer
    {
        public override void Render(Graphics g, Connection connection)
        {
            // パスポイントが不足している場合のフォールバック処理
            if (connection.Visual.PathPoints.Count < 2)
            {
                RenderFallbackConnection(g, connection);
                return;
            }

            // 通常の描画処理
            using (var pen = CreatePen(connection))
            {
                g.DrawLines(pen, connection.Visual.PathPoints.ToArray());
            }
        }
        // ... 既存の CreatePen メソッド ...
        protected virtual Pen CreatePen(Connection connection)
        {
            return new Pen(connection.Visual.IsSelected ? Color.Red : Color.Blue, 2)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.ArrowAnchor
            };
        }
    }

    /// <summary>
    /// ベジエ曲線で接続線を描画するレンダラー
    /// </summary>
    public class BezierConnectionRenderer : ConnectionRenderer
    {
        public override void Render(Graphics g, Connection connection)
        {
            // フォールバック処理
            if (connection.Visual.PathPoints.Count < 2)
            {
                RenderFallbackConnection(g, connection);
                return;
            }

            // 通常の描画処理
            var points = CalculateBezierPoints(connection.Visual.PathPoints);
            using (var pen = CreatePen(connection))
            {
                g.DrawBeziers(pen, points);
            }

            // 開始点と終了点を取得
            Point startPoint = connection.Visual.PathPoints[0];
            Point endPoint = connection.Visual.PathPoints[connection.Visual.PathPoints.Count - 1];

            // 選択状態に応じた色
            Color color = connection.IsSelected ? Color.Red : Color.Orange;
            using (var pen = new Pen(color, 2) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(pen, startPoint, endPoint);
            }
        }
        private PointF[] CalculateBezierPoints(List<Point> pathPoints)
        {
            var points = new List<PointF>();

            points.Add(pathPoints[0]);

            for (int i = 1; i < pathPoints.Count - 1; i++)
            {
                points.Add(pathPoints[i]);
                points.Add(pathPoints[i]);
            }

            // C# 7.3 互換の最後の要素アクセス
            points.Add(pathPoints[pathPoints.Count - 1]);

            return points.ToArray();
        }
        // ... 既存の CreatePen メソッド ...
        protected virtual Pen CreatePen(Connection connection)
        {
            return new Pen(connection.Visual.IsSelected ? Color.Red : Color.Blue, 2)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.ArrowAnchor
            };
        }
    }

    /// <summary>
    /// 直交線で接続線を描画するレンダラー
    /// </summary>
    public class OrthogonalConnectionRenderer : ConnectionRenderer
    {
        public override void Render(Graphics g, Connection connection)
        {
            // フォールバック処理
            if (connection.Visual.PathPoints.Count < 2)
            {
                RenderFallbackConnection(g, connection);
                return;
            }

            // 通常の描画処理
            var points = CalculateOrthogonalPoints(connection.Visual.PathPoints);
            using (var pen = CreatePen(connection))
            {
                g.DrawLines(pen, points);
            }
        }
        private Point[] CalculateOrthogonalPoints(List<Point> pathPoints)
        {
            if (pathPoints.Count != 2) 
                return pathPoints.ToArray();

            var start = pathPoints[0];
            var end = pathPoints[1];
            
            int midX = (start.X + end.X) / 2;
            
            return new[]
            {
                start,
                new Point(midX, start.Y),
                new Point(midX, end.Y),
                end
            };
        }

        protected virtual Pen CreatePen(Connection connection)
        {
            return new Pen(connection.Visual.IsSelected ? Color.Red : Color.Green, 2)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.ArrowAnchor
            };
        }
    }
}