using Unity.Entities;
using UnityEngine;

namespace Samples.NetCube
{
    [GenerateAuthoringComponent]
    public struct SmoothComponent : IComponentData
    {
        public Vector3 PreviousPos;
        public Quaternion PreviousRot;

        public Vector3 CurPos;
        public Quaternion CurRot;

        public Vector3 PosError;
        public Quaternion RotError;
    }
}