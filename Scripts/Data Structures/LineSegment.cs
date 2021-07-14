using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// This component is not intended to be a fully-fledged line, but rather a part of it.
    /// A complete line consists of multiple segments of no-cap lines,
    /// joined with "corner segment" then end with "line cap". (No work done on those yet)
    /// </summary>
    [Serializable]
    public struct LineSegment : IComponentData
    {
        public float3 from;
        public float3 to;
        public float lineWidth;
        public override string ToString() => $"{from} -> {to} width {lineWidth}";

        public LineSegment(float3 from, float3 to, float lineWidth = 0.1f)
        {
            this.from = from;
            this.to = to;
            this.lineWidth = lineWidth;
        }
    }
}
