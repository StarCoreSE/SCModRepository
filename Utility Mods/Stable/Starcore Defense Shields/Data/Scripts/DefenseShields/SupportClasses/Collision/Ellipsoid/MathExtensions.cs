using System;
using VRageMath;

namespace DefenseShields
{
    public static class MathExtensions
    {
        /// <summary>
        /// Solves a quadratic equation. The results are returned sorted by value.
        /// </summary>
        /// <param name="a">Parameter a of the equation</param>
        /// <param name="b">Parameter b of the equation</param>
        /// <param name="c">Parameter c of the equation</param>
        /// <returns>
        /// A nulable Vector2 with the two solutions sorted by value,
        /// or null if there is no solution.
        /// </returns>
        public static Vector2D? SolveQuadricEquation(double a, double b, double c)
        {
            double delta = b * b - 4 * a * c;
            if (delta < 0.0d)
            {
                return null;
            }
            double sqrtD = (double)Math.Sqrt(delta);
            double r1 = (-b + sqrtD) / (2 * a);
            double r2 = (-b - sqrtD) / (2 * a);

            MinMax(ref r1, ref r2);

            return new Vector2D(r1, r2);
        }

        /// <summary>
        /// Swaps the numbers if min > max.
        /// </summary>
        /// <param name="min">After calling the method - the lower number of the two</param>
        /// <param name="max">After calling the method - the higher number of the two</param>
        public static void MinMax(ref double min, ref double max)
        {
            if (min > max)
            {
                double temp = max;
                max = min;
                min = temp;
            }
        }

        /// <summary>
        /// Checks whether the given number is between min (inclusive) and max (inclusive).
        /// </summary>
        /// <param name="num">The number to check if in range</param>
        /// <param name="min">The minimum of the range</param>
        /// <param name="max">The maximum of the range</param>
        /// <returns>True if the given number is in range, false otherwise</returns>
        public static bool InRange(this double num, double min, double max)
        {
            return num >= min && num <= max;
        }
    }
}
