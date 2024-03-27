using System;
using VRageMath;

// change these to single-point compatible types to create a single precision version
using ConvexShape=GjkShapes.IConvex;
using Scalar=System.Double;
using Vec3=VRageMath.Vector3D;

// ReSharper disable BuiltInTypeReferenceStyle

namespace GjkShapes
{
    public static class Gjk
    {
        private struct Simplex
        {
            // When triangle: ABC
            // When tetrahedron: Tri BCD w/ norm pointing towards A, origin between BCD and A.
            public Vec3 A, B, C, D;
            public int Dimension;

            /// <summary>
            /// Flushes A into the simplex
            /// </summary>
            /// <param name="direction">next search direction</param>
            private bool Update34(out Vec3 direction)
            {
                if (Dimension == 4)
                    return Update4(out direction);
                // assert Dimension == 3.
                Update3(out direction);
                return false;
            }
            
            public void Update3(out Vec3 searchDirection)
            {
                var ba = B - A;
                var ca = C - A;

                Vec3 norm;
                Vec3.Cross(ref ba, ref ca, out norm);
                var origin = -A;

                Scalar dotRes;
                Vec3 crossRes;
                Vec3.Cross(ref ba, ref norm, out crossRes);
                Vec3.Dot(ref crossRes, ref origin, out dotRes);
                if (dotRes > 0) // origin outside the triangle, BA closest.
                {
                    Dimension = 2;
                    C = A;
                    Vec3.Cross(ref ba, ref origin, out crossRes);
                    Vec3.Cross(ref crossRes, ref ba, out searchDirection);
                    return;
                }

                Vec3.Cross(ref norm, ref ca, out crossRes);
                Vec3.Dot(ref crossRes, ref origin, out dotRes);
                if (dotRes > 0) // origin outside the triangle, CA closest
                {
                    Dimension = 2;
                    B = A;
                    Vec3.Cross(ref ca, ref origin, out crossRes);
                    Vec3.Cross(ref crossRes, ref ca, out searchDirection);
                    return;
                }

                Dimension = 3;
                Vec3.Dot(ref norm, ref origin, out dotRes);
                if (dotRes < 0) // origin below the triangle.
                {
                    // reverse winding order, BCD = BAC
                    D = C;
                    C = A;
                    searchDirection = -norm;
                    return;
                }

                D = C;
                C = B;
                B = A;
                searchDirection = norm;
            }

            public bool Update4(out Vec3 searchDirection)
            {
                var abc = Vec3.Cross(B - A, C - A);

                var origin = -A;
                
                Scalar dotRes;
                Vec3.Dot(ref abc, ref origin, out dotRes);
                if (dotRes > 0) // outside, ABC closest
                {
                    Dimension = 3;
                    // BCD = BCA
                    D = A;
                    searchDirection = abc;
                    return false;
                }
                var acd = Vec3.Cross(C - A, D - A);
                Vec3.Dot(ref acd, ref origin, out dotRes);
                if (dotRes > 0) // outside, ACD closest
                {
                    Dimension = 3;
                    // BCD = ACD
                    B = A;
                    searchDirection = acd;
                    return false;
                }
                var adb = Vec3.Cross(D - A, B - A);
                Vec3.Dot(ref adb, ref origin, out dotRes);
                if (dotRes > 0) // outside, ADB closest
                {
                    Dimension = 3;
                    // BCD = BAD
                    C = A;
                    searchDirection = adb;
                    return false;
                }
                
                // skip checking BCD dot origin, we know it's on the inside w.r.t it
                searchDirection = Vec3.Zero;
                return true;
            }
        }

        public static bool Intersects<T1, T2>(ref T1 shape1, ref T2 shape2, int iterations = 64) where T1 : struct, ConvexShape where T2 : struct, ConvexShape
        {
            Vec3 searchDirection;
            Scalar dotResult;
            
            var simplex = new Simplex();
            // Initialize the simplex to a line, with searchDirection for next point. 
            {
                searchDirection = shape2.Center - shape1.Center;
                if (Vec3.IsZero(searchDirection))
                    searchDirection = Vec3.UnitX;

                // Initial point
                simplex.C = shape2.FarthestInDirection(searchDirection) - shape1.FarthestInDirection(-searchDirection);
                searchDirection = -simplex.C;
                simplex.Dimension++;
                
                // Second point, crossing origin
                simplex.B = shape2.FarthestInDirection(searchDirection) - shape1.FarthestInDirection(-searchDirection);
                simplex.Dimension++;
                
                // Check if we made progress
                Vec3.Dot(ref simplex.B, ref searchDirection, out dotResult);
                if (dotResult < 0)
                    return false;

                var bc = simplex.C - simplex.B;
                Vec3.Cross(ref bc, ref simplex.B, out searchDirection);
                Vec3.Cross(ref bc, ref searchDirection, out searchDirection);
                if (Vec3.IsZero(searchDirection))
                {
                    // create perp vector to BC
                    if (Math.Abs(bc.Y + bc.Z) > 1e-5 || Math.Abs(bc.X) > 1e-5)
                        searchDirection = new Vector3D(-(bc.Y + bc.Z), bc.X, bc.X);
                    else
                        searchDirection = new Vector3D(bc.Z, bc.Z, -(bc.X + bc.Y));
                }
            }
            for (var i = 0; i < iterations; i++)
            {
                simplex.A = shape2.FarthestInDirection(searchDirection) - shape1.FarthestInDirection(-searchDirection);
                simplex.Dimension++;
                
                // Check if we made progress
                Vec3.Dot(ref simplex.A, ref searchDirection, out dotResult);
                if (dotResult < 0)
                    return false;
                
                if (simplex.Dimension == 3)
                    simplex.Update3(out searchDirection);
                else
                {
                    // assert Dimension == 4
                    if (simplex.Update4(out searchDirection))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}