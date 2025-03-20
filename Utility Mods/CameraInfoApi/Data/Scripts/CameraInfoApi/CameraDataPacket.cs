using ProtoBuf;
using VRage;
using VRageMath;

namespace CameraInfoApi.Data.Scripts.CameraInfoApi
{
    [ProtoContract]
    internal class CameraDataPacket
    {
        [ProtoMember(1)] public MatrixD Matrix;
        [ProtoMember(2)] public float FieldOfView;
        [ProtoMember(3)] public long GridId;

        public MyTuple<MatrixD, float> Tuple => new MyTuple<MatrixD, float>(Matrix, FieldOfView);
    }
}
