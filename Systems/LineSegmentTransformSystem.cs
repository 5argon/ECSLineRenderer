using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace E7.ECS.LineRenderer
{
    [ExecuteInEditMode]
    [UpdateBefore(typeof(EndFrameTransformSystem))]
    public class LineSegmentTransformSystem : JobComponentSystem
    {
        ComponentGroup cg;
        protected override void OnCreateManager()
        {
            var query = new EntityArchetypeQuery
            {
                All = new ComponentType[]{
                    ComponentType.Create<LineSegment>(),
                    ComponentType.Create<LocalToWorld>(),
                },
                Any = new ComponentType[]{
                },
                None = new ComponentType[]{
                },
            };
            cg = GetComponentGroup(query);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new LinePositioningJob
            {
                lastSystemVersion = LastSystemVersion,
                lineSegmentType = GetArchetypeChunkComponentType<LineSegment>(isReadOnly: true),
                ltwType = GetArchetypeChunkComponentType<LocalToWorld>(isReadOnly: false),
            }.Schedule(cg, inputDeps);
        }

        [BurstCompile]
        struct LinePositioningJob : IJobChunk
        {
            public uint lastSystemVersion;
            [ReadOnly] public ArchetypeChunkComponentType<LineSegment> lineSegmentType;
            public ArchetypeChunkComponentType<LocalToWorld> ltwType;

            public void Execute(ArchetypeChunk ac, int ci)
            {
                if (!ac.DidAddOrChange(lineSegmentType, lastSystemVersion)) return;

                var segs = ac.GetNativeArray(lineSegmentType);
                var ltws = ac.GetNativeArray(ltwType);
                for (int i = 0; i < segs.Length; i++)
                {
                    var seg = segs[i];
                    var ltw = ltws[i];

                    if (float3.Equals(seg.from, seg.to))
                    {
                        continue;
                    }

                    float3 forward = seg.to - seg.from;

                    //We will use length too so not using normalize here
                    float lineLength = math.length(forward);
                    float3 forwardUnit = forward / lineLength;

                    //Find any perpendicular vector of forward because line has no facing
                    //TODO : proper billboarding
                    float3 perpendicular = 
                    forwardUnit.x != 0 ? 
                    new float3(1, 1, (-forwardUnit.y - forwardUnit.z) / forwardUnit.x) :
                    forwardUnit.y != 0 ? 
                    new float3(1, 1, (-forwardUnit.x - forwardUnit.z) / forwardUnit.y) :
                    forwardUnit.z != 0 ? 
                    new float3(1, 1, (-forwardUnit.x - forwardUnit.y) / forwardUnit.z) :
                    float3.zero;
                    
                    float3 perpendicularUnit = math.normalize(perpendicular);

                    quaternion rotation = quaternion.LookRotation(forwardUnit, perpendicularUnit);

                    var mat = math.mul(
                            new float4x4(rotation, seg.from),
                            float4x4.Scale(seg.lineWidth, 1, lineLength));

                    ltws[i] = new LocalToWorld { Value = mat };
                }
            }
        }
    }
}
