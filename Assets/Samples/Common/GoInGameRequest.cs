using MyGameLib.NetCode;

namespace Samples
{
    public struct GoInGameRequest : IRpcCommand
    {
        public int value;
    }
}