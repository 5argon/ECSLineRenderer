using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [ExecuteAlways]
    public class LineRendererSimulationGroup : ComponentSystemGroup
    {
    }
}