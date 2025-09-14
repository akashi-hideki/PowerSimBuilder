using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PowerSimBuilder
{
    /// <summary>
    /// 接続線を描画するための抽象クラス
    /// 作成日: 2025-08-12
    /// 注意点: 各レンダラーで共通のフォールバック処理を実装
    /// </summary>
    public abstract class ConnectionRenderer
    {
        /// <summary>
        /// 接続線を描画する抽象メソッド
        /// </summary>
        public abstract void Render(Graphics g, Connection connection);
        
        /// <summary>
        /// ペンを作成する抽象メソッド
        /// </summary>
        protected abstract Pen CreatePen(Connection connection);

        /// <summary>
        /// 共通のフォールバック接続描画
        /// 注意点: コンポーネントがnullの場合描画しない
        /// </summary>
        protected void RenderFallbackConnection(Graphics g, Connection connection)
        {
            if (connection.StartComponent == null || connection.EndComponent == null)
                return;

            Point start = new Point(
                connection.StartComponent.Left + connection.StartComponent.Width / 2,
                connection.StartComponent.Top + connection.StartComponent.Height / 2
            );

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
    /// 作成日: 2025-08-12
    /// 注意点: パスポイント不足時はフォールバック処理を呼び出す
    /// </summary>
    public class StraightConnectionRenderer : ConnectionRenderer
    {
        /// <summary>
        /// 直線接続を描画
        /// </summary>
        public override void Render(Graphics g, Connection connection)
        {
            if (connection.Visual.PathPoints.Count < 2)
            {
                RenderFallbackConnection(g, connection);
                return;
            }

            using (var pen = CreatePen(connection))
            {
                g.DrawLines(pen, connection.Visual.PathPoints.ToArray());
            }
        }

        /// <summary>
        /// ペンを作成（選択状態で色変更）
        /// </summary>
        protected override Pen CreatePen(Connection connection)
        {
            Color lineColor = connection.Visual.IsSelected ? Color.Red : Color.Blue;
            int lineWidth = connection.Visual.IsSelected ? 3 : 2;
            
            return new Pen(lineColor, lineWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.ArrowAnchor
            };
        }
    }

    /// <summary>
    /// ベジエ曲線で接続線を描画するレンダラー
    /// 作成日: 2025-08-12
    /// 注意点: 制御点の計算ロジックを実装
    /// </summary>
    public class BezierConnectionRenderer : ConnectionRenderer
    {
        /// <summary>
        /// ベジエ曲線で接続を描画
        /// </summary>
        public override void Render(Graphics g, Connection connection)
        {
            if (connection.Visual.PathPoints.Count < 2)
            {
                RenderFallbackConnection(g, connection);
                return;
            }

            // 選択状態に応じた色と太さを使用
            Color lineColor = connection.Visual.IsSelected ? Color.Red : Color.Blue;
            int lineWidth = connection.Visual.IsSelected ? 3 : 2;
            
            using (var pen = new Pen(lineColor, lineWidth))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.ArrowAnchor;
                
                // ベジエ曲線を描画
                if (connection.Visual.PathPoints.Count >= 4)
                {
                    g.DrawBeziers(pen, connection.Visual.PathPoints.ToArray());
                }
                else
                {
                    // ポイント数が足りない場合は直線で描画
                    g.DrawLine(pen, 
                        connection.Visual.PathPoints[0], 
                        connection.Visual.PathPoints[connection.Visual.PathPoints.Count - 1]);
                }
            }
        }

        /// <summary>
        /// ベジエ制御点を計算
        /// 注意点: 単純な中間点を使用した実装
        /// </summary>
        private PointF[] CalculateBezierPoints(List<Point> pathPoints)
        {
            var points = new List<PointF>();
            points.Add(pathPoints[0]);

            for (int i = 1; i < pathPoints.Count - 1; i++)
            {
                points.Add(pathPoints[i]);
                points.Add(pathPoints[i]);
            }

            points.Add(pathPoints[pathPoints.Count - 1]);
            return points.ToArray();
        }

        /// <summary>
        /// ペンを作成（選択状態で色変更）
        /// </summary>
        protected override Pen CreatePen(Connection connection)
        {
            Color lineColor = connection.Visual.IsSelected ? Color.Red : Color.Blue;
            int lineWidth = connection.Visual.IsSelected ? 3 : 2;
            
            return new Pen(lineColor, lineWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.ArrowAnchor
            };
        }
    }

    /// <summary>
    /// 直交線で接続線を描画するレンダラー
    /// 作成日: 2025-08-12
    /// 注意点: 2点間のみの直交変換をサポート
    /// </summary>
    public class OrthogonalConnectionRenderer : ConnectionRenderer
    {
        /// <summary>
        /// 直交接続を描画
        /// </summary>
        public override void Render(Graphics g, Connection connection)
        {
            if (connection.Visual.PathPoints.Count < 2)
            {
                RenderFallbackConnection(g, connection);
                return;
            }

            var points = CalculateOrthogonalPoints(connection.Visual.PathPoints);
            using (var pen = CreatePen(connection))
            {
                g.DrawLines(pen, points);
            }
        }

        /// <summary>
        /// 直交点を計算
        /// 注意点: 2点間のみの簡易実装
        /// </summary>
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

        /// <summary>
        /// ペンを作成（選択状態で色変更）
        /// </summary>
        protected override Pen CreatePen(Connection connection)
        {
            Color lineColor = connection.Visual.IsSelected ? Color.Red : Color.Green;
            int lineWidth = connection.Visual.IsSelected ? 3 : 2;
            
            return new Pen(lineColor, lineWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.ArrowAnchor
            };
        }
    }
}