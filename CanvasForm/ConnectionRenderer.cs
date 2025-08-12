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
        /// <summary>
        /// 接続線を描画する
        /// </summary>
        public abstract void Render(Graphics g, Connection connection);
    }

    /// <summary>
    /// 直線で接続線を描画するレンダラー
    /// </summary>
    public class StraightConnectionRenderer : ConnectionRenderer
    {
        public override void Render(Graphics g, Connection connection)
        {
            if (connection.Visual.PathPoints.Count < 2) return;

            using (var pen = CreatePen(connection))
            {
                g.DrawLines(pen, connection.Visual.PathPoints.ToArray());
            }
        }

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
            if (connection.Visual.PathPoints.Count < 2) return;

            var points = CalculateBezierPoints(connection.Visual.PathPoints);
            using (var pen = CreatePen(connection))
            {
                g.DrawBeziers(pen, points);
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
            if (connection.Visual.PathPoints.Count < 2) return;

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