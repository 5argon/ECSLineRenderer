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

        private float3 cameraPosition;
        public void RememberCamera(Camera c)
        {
            cameraPosition = c.transform.position;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new LinePositioningJob
            {
                cameraPosition = cameraPosition,
                lastSystemVersion = LastSystemVersion,
                lineSegmentType = GetArchetypeChunkComponentType<LineSegment>(isReadOnly: true),
                ltwType = GetArchetypeChunkComponentType<LocalToWorld>(isReadOnly: false),
            }.Schedule(cg, inputDeps);
        }

        [BurstCompile]
        struct LinePositioningJob : IJobChunk
        {
            public float3 cameraPosition;
            public uint lastSystemVersion;
            [ReadOnly] public ArchetypeChunkComponentType<LineSegment> lineSegmentType;
            public ArchetypeChunkComponentType<LocalToWorld> ltwType;

            public void Execute(ArchetypeChunk ac, int chunkIndex, int firstEntityIndex)
            {
                if (!ac.DidChange(lineSegmentType, lastSystemVersion)) return;

                var segs = ac.GetNativeArray(lineSegmentType);
                var ltws = ac.GetNativeArray(ltwType);
                for (int i = 0; i < segs.Length; i++)
                {
                    var seg = segs[i];
                    var ltw = ltws[i];

                    if (seg.from.Equals(seg.to) || seg.from.Equals(cameraPosition))
                    {
                        continue;
                    }

                    float3 forward = seg.to - seg.from;

                    //We will use length too so not using normalize here
                    float lineLength = math.length(forward);
                    float3 forwardUnit = forward / lineLength;

                    //billboard rotation
                    float3 toCamera = math.normalize(cameraPosition - seg.from);

                    //If forward and toCamera is collinear the cross product is 0
                    //and it will gives quaternion with tons of NaN
                    //So we have to check for that and do nothing if that is the case
                    if (math.cross(forwardUnit, toCamera).Equals(float3.zero))
                    {
                        continue;
                    }

                    quaternion rotation = quaternion.LookRotation(forwardUnit, toCamera);

                    var mat = math.mul(
                            new float4x4(rotation, seg.from),
                            float4x4.Scale(seg.lineWidth, 1, lineLength));

                    ltws[i] = new LocalToWorld { Value = mat };
                }
            }
        }
    }
}
