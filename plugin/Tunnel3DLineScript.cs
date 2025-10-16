using Godot;
using System;

public partial class Tunnel3D : Node3D
{
    private class Line
    {

        public static float DistanceLineToPoint(Line line, Vector3 point)
        {
            Vector3 t = line._direction;
            Vector3 l = line._origin;

            float dotProdTCoefficient = t.X * t.X + t.Y * t.Y + t.Z * t.Z;

            float dotProdConstantCoefficient = t.X * (point.X - l.X) + t.Y * (point.Y - l.Y) + t.Z * (point.Z - l.Z);

            float tNormalised = dotProdConstantCoefficient / dotProdTCoefficient;

            Vector3 nearestPointOnLine = l + (t * Math.Clamp(tNormalised, 0, 1));

            return DistancePointToPoint(nearestPointOnLine, point);
        }


        public static float DistanceLineToLine(Line line1, Line line2)
        {
            Vector3 t = line1._direction;
            Vector3 l1 = line1._origin;

            Vector3 u = line2._direction;
            Vector3 l2 = line2._origin;

            // Handle edge-cases where direction is zero

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

            // Create Simultaneous Equation Coefficients

            float TCoef_EQ1 = t.X * t.X + t.Y * t.Y + t.Z * t.Z; 
            float UCoef_EQ1 = -(t.X * u.X + t.Y * u.Y + t.Z * u.Z); // Dot Product

            float TCoef_EQ2 = -UCoef_EQ1; // equivalent to u.X * t.X + u.Y * t.Y + u.Z + t.Z
            float UCoef_EQ2 = -(u.X * u.X + u.Y * u.Y + u.Z * u.Z);

            Vector3 originDifference = (l2 - l1);

            float Const_EQ1 = t.X * originDifference.X + t.Y * originDifference.Y + t.Z * originDifference.Z;
            float Const_EQ2 = u.X * originDifference.X + u.Y * originDifference.Y + u.Z * originDifference.Z;

            // Solve Simultaneously

            // TCoef_EQ1 + UCoef_EQ1 = Const_EQ1   aT + bU = c
            // TCoef_EQ2 + UCoef_EQ2 = Const_EQ2   dT + eU = f

            // Find T

            float EQ2_scalar = UCoef_EQ1 / UCoef_EQ2;

            float scaledTCoef_EQ2 = EQ2_scalar * TCoef_EQ2;
            float scaledConst_EQ2 = EQ2_scalar * Const_EQ2;

            float tValue = (Const_EQ1 - scaledConst_EQ2) / (TCoef_EQ1 - scaledTCoef_EQ2);

            // Find U

            float EQ1_scalar = TCoef_EQ2 / TCoef_EQ1;

            float scaledUCoef_EQ1 = EQ1_scalar * UCoef_EQ1;
            float scaledConst_EQ1 = EQ1_scalar * Const_EQ1;

            float uValue = (Const_EQ2 - scaledConst_EQ1) / (UCoef_EQ2 - scaledUCoef_EQ1);

            // Closest points on line are within the line segment bounds 

            bool tInRange = tValue > 0.0f && tValue < 1.0f;
            bool uInRange = uValue > 0.0f && uValue < 1.0f;

            // NaN values indicate any value is possible (for an unbounded line) or the simultaneous equation failed

            bool equationFailed = false;

            if (float.IsNaN(tValue))
            {
                tValue = 0f;
                equationFailed = true;
            }

            if (float.IsNaN(uValue))
            {
                uValue = 0f;
                equationFailed = true;
            }

            float distanceValue = float.MaxValue;


            if (!equationFailed)
            {
                Vector3 tPoint = l1 + (t * Math.Clamp(tValue, 0.0f, 1.0f));
                Vector3 uPoint = l2 + (u * Math.Clamp(uValue, 0.0f, 1.0f));

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
            else
            {
                float[] tValues = new float[2] { tValue, 1f };
                float[] uValues = new float[2] { uValue, 1f };

                for (int i = 0; i < 4; i++) // Apply for every combination of values
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
            }

            return distanceValue;
        }

        public static float DistancePointToPoint(Vector3 vector1, Vector3 vector2)
        {
            Vector3 diff = vector2 - vector1;

            return MathF.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
        }

        private Vector3 _origin;
        private Vector3 _endpoint;
        private Vector3 _direction;

        public Line(Vector3 origin, Vector3 endpoint)
        {
            _origin = origin;
            _endpoint = endpoint;
            _direction = endpoint - origin;
        }

    }



}
