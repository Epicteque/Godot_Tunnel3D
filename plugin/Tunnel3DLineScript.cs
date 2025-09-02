using Godot;
using System;

public partial class Tunnel3D : Node3D
{
    private class Line
    {

        public static float DistanceLineToPoint(Line line, Vector3 point)
        {
            Vector3 t = line._normal;
            Vector3 l = line._origin;

            float dotProdTCoef = t.X * t.X + t.Y * t.Y + t.Z * t.Z;

            float dotProdConstCoef = t.X * (point.X - l.X) + t.Y * (point.Y - l.Y) + t.Z * (point.Z - l.Z);

            float t_UNIT = dotProdConstCoef / dotProdTCoef;

            Vector3 nearestPointOnLine = l + (t * Math.Clamp(t_UNIT, 0, 1));

            return DistancePointToPoint(nearestPointOnLine, point);
        }


        public static float DistanceLineToLine(Line line1, Line line2)
        {
            Vector3 t = line1._normal;
            Vector3 l1 = line1._origin;

            Vector3 u = line2._normal;
            Vector3 l2 = line2._origin;

            if (t == Vector3.Zero && u == Vector3.Zero)
            {
                return DistancePointToPoint(l1, l2);
            }
            else if (t == Vector3.Zero)
            {
                return DistanceLineToPoint(line2, l1);
            }
            else if (u == Vector3.Zero)
            {
                return DistanceLineToPoint(line1, l2);
            }

            // TCoef_EQ1 + UCoef_EQ1 = Const_EQ1   aT + bU = c
            // TCoef_EQ2 + UCoef_EQ2 = Const_EQ2   dT + eU = f

            float TCoef_EQ1 = t.X * t.X + t.Y * t.Y + t.Z * t.Z;
            float UCoef_EQ1 = -(t.X * u.X + t.Y * u.Y + t.Z * u.Z);

            float TCoef_EQ2 = -UCoef_EQ1; // u.X * t.X + u.Y * t.Y + u.Z + t.Z
            float UCoef_EQ2 = -(u.X * u.X + u.Y * u.Y + u.Z * u.Z);

            float Const_EQ1 = t.X * (l2.X - l1.X) + t.Y * (l2.Y - l1.Y) + t.Z * (l2.Z - l1.Z);
            float Const_EQ2 = u.X * (l2.X - l1.X) + u.Y * (l2.Y - l1.Y) + u.Z * (l2.Z - l1.Z);

            // Solve Simultaneously

            // Find T

            float EQ2_scalar = UCoef_EQ1 / UCoef_EQ2;

            float scaledTCoef_EQ2 = EQ2_scalar * TCoef_EQ2;
            float scaledConst_EQ2 = EQ2_scalar * Const_EQ2;

            float tValue = (Const_EQ1 - scaledConst_EQ2) / (TCoef_EQ1 - scaledTCoef_EQ2);

            // Find U

            //float uValue = (TCoef_EQ1 * tValue - Const_EQ1) / -UCoef_EQ1;

            float EQ1_scalar = TCoef_EQ2 / TCoef_EQ1;

            float scaledUCoef_EQ1 = EQ1_scalar * UCoef_EQ1;
            float scaledConst_EQ1 = EQ1_scalar * Const_EQ1;

            float uValue = (Const_EQ2 - scaledConst_EQ1) / (UCoef_EQ2 - scaledUCoef_EQ1);


            // Closest points on line are within the line segment bounds 

            bool tInRange = tValue > 0.0f && tValue < 1.0f;
            bool uInRange = uValue > 0.0f && uValue < 1.0f;


            // NaN values indicate any value is possible (for an unbounded line) - The lines are coincident/parallel

            int iters = 1;

            float tValue2 = 1f;
            float uValue2 = 1f;

            if (float.IsNaN(tValue))
            {
                tValue = 0f;
                iters = 4;
            }

            if (float.IsNaN(uValue))
            {
                uValue = 0f;
                iters = 4;
            }


            float distanceValue = float.MaxValue;

            float[] tValues = new float[2] { tValue, tValue2 };
            float[] uValues = new float[2] { uValue, uValue2 };

            for (int i = 0; i < iters; i++)
            {
                Vector3 tPoint = l1 + (t * Math.Clamp(tValues[i / 2], 0.0f, 1.0f));
                Vector3 uPoint = l2 + (u * Math.Clamp(uValues[i % 2], 0.0f, 1.0f));

                distanceValue = Math.Min(distanceValue, DistancePointToPoint(tPoint, uPoint));

                if (!uInRange)
                {
                    distanceValue = Math.Min(distanceValue, DistanceLineToPoint(line1, uPoint));
                }

                if (!tInRange)
                {
                    distanceValue = Math.Min(distanceValue, DistanceLineToPoint(line2, tPoint));
                }
            }

            return distanceValue;
        }

        public static Vector3[] PositionsLineToLine(Line line1, Line line2, float[] bounds) // Function Only Used in Prototype Demonstration
        {

            Vector3 PositionLineToPoint(Line line, Vector3 point)
            {
                Vector3 t = line._normal;
                Vector3 l = line._origin;


                float dotProdTCoef = t.X * t.X + t.Y * t.Y + t.Z * t.Z;

                float dotProdConstCoef = t.X * (point.X - l.X) + t.Y * (point.Y - l.Y) + t.Z * (point.Z - l.Z);


                float t_UNIT = dotProdConstCoef / dotProdTCoef;

                Vector3 nearestPointOnLine = l + (t * Mathf.Clamp(t_UNIT, 0, 1));


                return nearestPointOnLine;
            }


            Vector3 t = line1._normal;
            Vector3 l1 = line1._origin;

            Vector3 u = line2._normal;
            Vector3 l2 = line2._origin;

            if (t == Vector3.Zero && u == Vector3.Zero)
            {
                return new Vector3[] { l1, l2 };
            }
            else if (t == Vector3.Zero)
            {
                return new Vector3[] { l1, PositionLineToPoint(line2, l1) };
            }
            else if (u == Vector3.Zero)
            {
                return new Vector3[] { l2, PositionLineToPoint(line1, l2) };
            }



            // TCoef_EQ1 + UCoef_EQ1 = Const_EQ1   aT + bU = c
            // TCoef_EQ2 + UCoef_EQ2 = Const_EQ2   dT + eU = f

            float TCoef_EQ1 = t.X * t.X + t.Y * t.Y + t.Z * t.Z;
            float UCoef_EQ1 = -(t.X * u.X + t.Y * u.Y + t.Z * u.Z);

            float TCoef_EQ2 = -UCoef_EQ1; // u.X * t.X + u.Y * t.Y + u.Z + t.Z
            float UCoef_EQ2 = -(u.X * u.X + u.Y * u.Y + u.Z * u.Z);

            float Const_EQ1 = t.X * (l2.X - l1.X) + t.Y * (l2.Y - l1.Y) + t.Z * (l2.Z - l1.Z);
            float Const_EQ2 = u.X * (l2.X - l1.X) + u.Y * (l2.Y - l1.Y) + u.Z * (l2.Z - l1.Z);

            // Solve Simultaneously

            // Find T

            float EQ2_scalar = UCoef_EQ1 / UCoef_EQ2;

            float scaledTCoef_EQ2 = EQ2_scalar * TCoef_EQ2;
            float scaledConst_EQ2 = EQ2_scalar * Const_EQ2;

            float tValue = (Const_EQ1 - scaledConst_EQ2) / (TCoef_EQ1 - scaledTCoef_EQ2);

            // Find U

            //float uValue = (TCoef_EQ1 * tValue - Const_EQ1) / -UCoef_EQ1;

            float EQ1_scalar = TCoef_EQ2 / TCoef_EQ1;

            float scaledUCoef_EQ1 = EQ1_scalar * UCoef_EQ1;
            float scaledConst_EQ1 = EQ1_scalar * Const_EQ1;

            float uValue = (Const_EQ2 - scaledConst_EQ1) / (UCoef_EQ2 - scaledUCoef_EQ1);


            // Closest points on line are within the line segment bounds 

            bool tInRange = tValue > 0.0f && tValue < 1.0f;
            bool uInRange = uValue > 0.0f && uValue < 1.0f;

            // NaN values indicate any value is possible - The lines are coincident

            int iters = 1;

            float tValue2 = 1f;
            float uValue2 = 1f;

            if (float.IsNaN(tValue))
            {
                tValue = 0f;
                iters = 4;
            }

            if (float.IsNaN(uValue))
            {
                uValue = 0f;
                iters = 4;
            }


            float distanceValue = float.MaxValue;

            Vector3 closestPoint1 = Vector3.Zero;
            Vector3 closestPoint2 = Vector3.Zero;



            GD.Print(distanceValue);

            float[] tValues = new float[2] { tValue, tValue2 };

            float[] uValues = new float[2] { uValue, uValue2 };

            for (int i = 0; i < iters; i++) // In cases where T and U are NaN, all bounding points must be checked, otherwise continues as normal.
            {
                Vector3 tPoint = l1 + (t * Mathf.Clamp(tValues[i / 2], 0.0f, 1.0f));
                Vector3 uPoint = l2 + (u * Mathf.Clamp(uValues[i % 2], 0.0f, 1.0f));

                if (i == 0) // sets variables during first iteration.
                {
                    closestPoint1 = tPoint;
                    closestPoint2 = uPoint;
                }


                distanceValue = Math.Min(distanceValue, DistancePointToPoint(tPoint, uPoint));

                if (!uInRange)
                {

                    Vector3 p = PositionLineToPoint(line1, uPoint);
                    if (DistancePointToPoint(uPoint, p) < distanceValue)
                    {
                        closestPoint1 = p;

                        closestPoint2 = uPoint;

                        distanceValue = Math.Min(distanceValue, DistancePointToPoint(uPoint, p));
                    }

                }

                if (!tInRange)
                {

                    Vector3 p = PositionLineToPoint(line2, tPoint);

                    //GD.Print(p);

                    if (DistancePointToPoint(tPoint, p) < distanceValue)
                    {
                        closestPoint1 = tPoint;
                        closestPoint2 = p;

                        distanceValue = Math.Min(distanceValue, DistancePointToPoint(tPoint, p));

                    }


                }

            }

            //GD.Print($"{closestPoint1} {closestPoint2}");


            //GD.Print($"T{tInRange} U{uInRange} RDist: {distanceValue} PDist:{DistancePointToPoint(closestPoint1, closestPoint2)}");



            return new Vector3[] { closestPoint1, closestPoint2 };
        }

        public static float DistancePointToPoint(Vector3 v1, Vector3 v2)
        {
            Vector3 diff = v2 - v1;

            return MathF.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
        }

        private Vector3 _origin;
        private Vector3 _endpoint;
        private Vector3 _normal;

        public Line(Vector3 origin, Vector3 endpoint)
        {
            _origin = origin;
            _endpoint = endpoint;
            _normal = endpoint - origin;
        }

    }



}
