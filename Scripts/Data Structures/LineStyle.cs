using Unity.Entities;
using System;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// This is some properties of the line that could be shared between different lines.
    /// When line corners and line caps are supported, this component could also be used with them.
    /// </summary>
    [Serializable]
    public struct LineStyle : ISharedComponentData, IEquatable<LineStyle>
    {
        public Material material;

        // /// <summary>
        // /// WIP : Override billboard camera target.
        // /// </summary>
        // public Camera alignWithCamera;

        // /// <summary>
        // /// WIP
        // /// </summary>
        // public int endCapVertices;

        // /// <summary>
        // /// WIP
        // /// </summary>
        // public int cornerVertices;

        public bool Equals(LineStyle other) 
            => ReferenceEquals(material, other.material);

        public override int GetHashCode()
            => ReferenceEquals(material, null) ? material.GetHashCode() : 0;
    }

}
