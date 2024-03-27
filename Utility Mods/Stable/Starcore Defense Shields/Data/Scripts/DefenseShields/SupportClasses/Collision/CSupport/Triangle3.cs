using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace DefenseShields.Support
{

    public static class MathUtil
    {

        /// <summary>
        /// Compute barycentric coordinates/weights of vPoint inside triangle (V0,V1,V2). 
        /// If point is in triangle plane and inside triangle, coords will be positive and sum to 1.
        /// ie if result is a, then vPoint = a.x*V0 + a.y*V1 + a.z*V2.
        /// </summary>
        public static Vector3D BarycentricCoords(ref Vector3D vPoint, ref Vector3D V0, ref Vector3D V1, ref Vector3D V2)
        {
            Vector3D kV02 = V0 - V2;
            Vector3D kV12 = V1 - V2;
            Vector3D kPV2 = vPoint - V2;
            double fM00 = kV02.Dot(kV02);
            double fM01 = kV02.Dot(kV12);
            double fM11 = kV12.Dot(kV12);
            double fR0 = kV02.Dot(kPV2);
            double fR1 = kV12.Dot(kPV2);
            double fDet = fM00 * fM11 - fM01 * fM01;
            double fInvDet = 1.0 / fDet;
            double fBary1 = (fM11 * fR0 - fM01 * fR1) * fInvDet;
            double fBary2 = (fM00 * fR1 - fM01 * fR0) * fInvDet;
            double fBary3 = 1.0 - fBary1 - fBary2;
            return new Vector3D(fBary1, fBary2, fBary3);
        }
        public static Vector3D BarycentricCoords(Vector3D vPoint, Vector3D V0, Vector3D V1, Vector3D V2)
        {
            return BarycentricCoords(ref vPoint, ref V0, ref V1, ref V2);
        }
    }

    public struct Triangle3d
    {
        public Vector3D V0, V1, V2;

        public Triangle3d(Vector3D v0, Vector3D v1, Vector3D v2)
        {
            V0 = v0; V1 = v1; V2 = v2;
        }

        public Vector3D this[int key]
        {
            get { return (key == 0) ? V0 : (key == 1) ? V1 : V2; }
            set { if (key == 0) V0 = value; else if (key == 1) V1 = value; else V2 = value; }
        }

        public Vector3D PointAt(double bary0, double bary1, double bary2)
        {
            return bary0 * V0 + bary1 * V1 + bary2 * V2;
        }
        public Vector3D PointAt(Vector3D bary)
        {
            return bary.X* V0 + bary.Y* V1 + bary.Z* V2;
        }

        public Vector3D BarycentricCoords(Vector3D point)
        {
            return MathUtil.BarycentricCoords(ref point, ref V0, ref V1, ref V2);
        }

        // conversion operators
        public static implicit operator Triangle3d(Triangle3f v)
        {
            return new Triangle3d(v.V0, v.V1, v.V2);
        }
        public static explicit operator Triangle3f(Triangle3d v)
        {
            return new Triangle3f(v.V0, v.V1, v.V2);
        }
    }

    public struct Triangle3f
    {
        public Vector3D V0;
        public Vector3D V1;
        public Vector3D V2;

        public Triangle3f(Vector3D v0, Vector3D v1, Vector3D v2)
        {
            V0 = v0; V1 = v1; V2 = v2;
        }

        public Vector3D this[int key]
        {
            get { return (key == 0) ? V0 : (key == 1) ? V1 : V2; }
            set { if (key == 0) V0 = value; else if (key == 1) V1 = value; else V2 = value; }
        }


        public Vector3D PointAt(float bary0, float bary1, float bary2)
        {
            return bary0 * V0 + bary1 * V1 + bary2 * V2;
        }
        public Vector3D PointAt(Vector3D bary)
        {
            return bary.X * V0 + bary.Y * V1 + bary.Z * V2;
        }

        public Vector3D BarycentricCoords(Vector3D point)
        {
            return MathUtil.BarycentricCoords(point, V0, V1, V2);
        }
    }

}
