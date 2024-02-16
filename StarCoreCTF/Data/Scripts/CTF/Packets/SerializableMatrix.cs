using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF
{
    [ProtoContract]
    public class SerializableMatrix
    {
        [ProtoMember(100)]
        public double M11;

        [ProtoMember(102)]
        public double M12;

        [ProtoMember(103)]
        public double M13;

        [ProtoMember(104)]
        public double M14;

        [ProtoMember(105)]
        public double M21;

        [ProtoMember(106)]
        public double M22;

        [ProtoMember(107)]
        public double M23;

        [ProtoMember(108)]
        public double M24;

        [ProtoMember(109)]
        public double M31;

        [ProtoMember(110)]
        public double M32;

        [ProtoMember(111)]
        public double M33;

        [ProtoMember(112)]
        public double M34;

        [ProtoMember(113)]
        public double M41;

        [ProtoMember(114)]
        public double M42;

        [ProtoMember(115)]
        public double M43;

        [ProtoMember(116)]
        public double M44;

        public SerializableMatrix()
        {

        }

        public SerializableMatrix(MatrixD matrix)
        {
            M11 = matrix.M11;
            M12 = matrix.M12;
            M13 = matrix.M13;
            M14 = matrix.M14;
            M21 = matrix.M21;
            M22 = matrix.M22;
            M23 = matrix.M23;
            M24 = matrix.M24;
            M31 = matrix.M31;
            M32 = matrix.M32;
            M33 = matrix.M33;
            M34 = matrix.M34;
            M41 = matrix.M41;
            M42 = matrix.M42;
            M43 = matrix.M43;
            M44 = matrix.M44;
        }

        public static implicit operator SerializableMatrix(MatrixD matrix)
        {
            return new SerializableMatrix(matrix);
        }

        public static implicit operator MatrixD(SerializableMatrix v)
        {
            if (v == null)
                return new MatrixD();
            return new MatrixD(v.M11, v.M12, v.M13, v.M14, v.M21, v.M22, v.M23, v.M24, v.M31, v.M32, v.M33, v.M34, v.M41, v.M42, v.M43, v.M44);
        }
    }
}
