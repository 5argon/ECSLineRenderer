using Unity.Entities;
using System;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    [Serializable]
    public struct LineStyle : ISharedComponentData
    {
        public Material lineMaterial;
    }

}
