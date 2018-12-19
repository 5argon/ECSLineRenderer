using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System;

namespace E7.ECS.LineRenderer
{
    [Serializable]
    public struct LineSegment : IComponentData
    {
        public float3 from;
        public float3 to;
        public float lineWidth;
        public override string ToString() => $"{from} -> {to} width {lineWidth}";
    }
}
