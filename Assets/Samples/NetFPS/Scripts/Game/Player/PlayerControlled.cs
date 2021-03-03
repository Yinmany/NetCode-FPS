using Samples.MyGameLib.NetCode;
using Unity.Entities;

namespace Samples.NetFPS
{
    public struct PlayerControlledState : IComponentData
    {
        public InputCommand Command;
    }
}