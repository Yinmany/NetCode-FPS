using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace MyGameLib.NetCode
{
    public struct DynamicTypeList
    {
        public const int MaxCapacity = 128;

        public static unsafe void PopulateList<T>(ComponentSystem system,
            NativeArray<GhostComponentSerializer> ghostComponentCollection, bool readOnly, ref T list)
            where T : struct, IDynamicTypeList
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (UnsafeUtility.SizeOf<ArchetypeChunkComponentTypeDynamic32>() !=
                UnsafeUtility.SizeOf<DynamicComponentTypeHandle>() * 32)
                throw new System.Exception("Invalid type size, this will cause undefined behavior");
#endif
            var listLength = ghostComponentCollection.Length;
            DynamicComponentTypeHandle* ghostChunkComponentTypesPtr = list.GetData();
            list.Length = listLength;
            for (int i = 0; i < list.Length; i++)
            {
                var compType = ghostComponentCollection[i].ComponentType;
                if (readOnly)
                    compType.AccessModeType = ComponentType.AccessMode.ReadOnly;
                ghostChunkComponentTypesPtr[i] = system.GetDynamicComponentTypeHandle(compType);
            }
        }
    }

    public unsafe interface IDynamicTypeList
    {
        int Length { get; set; }
        DynamicComponentTypeHandle* GetData();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ArchetypeChunkComponentTypeDynamic8
    {
        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType00;

        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType01;

        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType02;

        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType03;

        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType04;

        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType05;

        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType06;

        [NativeDisableContainerSafetyRestriction]
        public DynamicComponentTypeHandle dynamicType07;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ArchetypeChunkComponentTypeDynamic32
    {
        public ArchetypeChunkComponentTypeDynamic8 dynamicType00_07;
        public ArchetypeChunkComponentTypeDynamic8 dynamicType08_15;
        public ArchetypeChunkComponentTypeDynamic8 dynamicType16_23;
        public ArchetypeChunkComponentTypeDynamic8 dynamicType24_31;
    }

    public struct DynamicTypeList32 : IDynamicTypeList
    {
        public int Length { get; set; }

        private ArchetypeChunkComponentTypeDynamic32 _dynamicTypes;

        public unsafe DynamicComponentTypeHandle* GetData()
        {
            fixed (DynamicComponentTypeHandle* ptr = &_dynamicTypes.dynamicType00_07.dynamicType00)
            {
                return ptr;
            }
        }
    }

    public struct DynamicTypeList64 : IDynamicTypeList
    {
        public int Length { get; set; }

        private ArchetypeChunkComponentTypeDynamic32 _dynamic00_31;
        private ArchetypeChunkComponentTypeDynamic32 _dynamic32_63;

        public unsafe DynamicComponentTypeHandle* GetData()
        {
            fixed (DynamicComponentTypeHandle* ptr = &_dynamic00_31.dynamicType00_07.dynamicType00)
            {
                return ptr;
            }
        }
    }
}