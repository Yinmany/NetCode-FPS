using System;
using Unity.Burst;

namespace MyGameLib.NetCode
{
    public struct PortableFunctionPointer<T> where T : Delegate
    {
        public PortableFunctionPointer(T executeDelegate)
        {
#if !UNITY_DOTSPLAYER
            Ptr = BurstCompiler.CompileFunctionPointer(executeDelegate);
#else
            Ptr = executeDelegate;
#endif
        }

#if !UNITY_DOTSPLAYER
        public readonly FunctionPointer<T> Ptr;
#else
        internal readonly T Ptr;
#endif
    }
}