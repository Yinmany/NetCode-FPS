using System;
using UnityEngine;

namespace Base.Vehicle.Authoring
{
    public struct VehicleInput
    {
        public float v, h;
    }
    
    public class Vehicle : MonoBehaviour
    {
        public VehicleInput vehicleInput;
        public WheelBaseInfoAuthoring[] wheels;
        public float turn = 4000f;
    }
}