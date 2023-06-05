using System;
using System.Collections.Generic;
using System.Drawing;

namespace SpringChallenge2022
{
    public static class Helpers
    {
        public static bool IsCircleCollision(Point center1, int radius1, Point center2, int radius2)
        {
            int dx = center2.X - center1.X;
            int dy = center2.Y - center1.Y;

            double D = Math.Sqrt(dx * dx + dy * dy);
            return Math.Abs(radius1 - radius2) < D && D < radius1 + radius2;
        }

        public static Point PointFromRounding(double x, double y)
        {
            if (x > 8815)
            {
                x = Math.Ceiling(x);
            }
            else
            {
                x = Math.Floor(x);
            }

            if (y > 4500)
            {
                y = Math.Ceiling(y);
            }
            else
            {
                y = Math.Floor(y);
            }

            return new Point((int)x, (int)y);
        }

        public static List<Point> CircleIntersections(Point center1, double radius1, Point center2, double radius2)
        {
            List<Point> ret = new();
            double dx = center1.X - center2.X;
            double dy = center1.Y - center2.Y;
            double d = Math.Sqrt(dx * dx + dy * dy);

            if (d > (radius1 + radius2) || (d == 0 && radius1 == radius2) || (d + Math.Min(radius1, radius2)) < Math.Max(radius1, radius2))
            {
                return ret;
            }
            else
            {
                double a = (radius1 * radius1 - radius2 * radius2 + d * d) / (2 * d);
                double h = Math.Sqrt(radius1 * radius1 - a * a);

                double p2x = center1.X + a * (center2.X - center1.X) / d;
                double p2y = center1.Y + a * (center2.Y - center1.Y) / d;

                Point i1 = PointFromRounding(p2x + h * (center2.Y - center1.Y) / d, p2y - h * (center2.X - center1.X) / d);
                Point i2 = PointFromRounding(p2x - h * (center2.Y - center1.Y) / d, p2y + h * (center2.X - center1.X) / d);

                if (d == (radius1 + radius2))
                {
                    ret.Add(i1);
                }
                else
                {
                    ret.Add(i1);
                    ret.Add(i2);
                }

                return ret;
            }
        }

        public static double AngleFromPoint(Point center, Point target)
        {
            return Math.Atan2(target.Y - center.Y, target.X - center.X) * (180 / Math.PI);
        }

        public static Point PointOnCircle(Point center, int radius, double angle)
        {
            return PointFromRounding(center.X + (radius * Math.Cos(angle * (Math.PI / 180))), center.Y + (radius * Math.Sin(angle * (Math.PI / 180))));
        }

        public static int GetCircleRadius(Point center, Point onCircle)
        {
            return (int)Math.Sqrt(Math.Pow(center.X - onCircle.X, 2) + Math.Pow(center.Y - onCircle.Y, 2));
        }

        public static Point GetNextPosition(Point currentPosition, int range, Point target)
        {
            double theta = Math.Atan2(target.Y - currentPosition.Y, target.X - currentPosition.X);
            return PointFromRounding(Math.Min(Math.Max(0, currentPosition.X + (range * Math.Cos(theta))), 17630), Math.Min(Math.Max(0, currentPosition.Y + (range * Math.Sin(theta))), 9000));
        }

        public static Point CoordsConverter(Point source, Point distance)
        {
            if (Game.IsTopLeft)
            {
                return new Point(source.X + distance.X, source.Y + distance.Y);
            }
            else
            {
                return new Point(source.X - distance.X, source.Y - distance.Y);
            }
        }

        public static bool IsInRange(Point source, Point center, int range)
        {
            return Math.Pow(source.X - center.X, 2) + Math.Pow(source.Y - center.Y, 2) <= Math.Pow(range, 2);
        }
    }
}
