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
    [ExecuteAlways]
    public class LineSegmentTransformSystem : JobComponentSystem
    {
        ComponentGroup lineSegmentGroup;
        ComponentGroup billboardCameraGroup;

        protected override void OnCreateManager()
        {
            var lineSegmentQuery = new EntityArchetypeQuery
            {
                All = new ComponentType[]{
                    ComponentType.ReadOnly<LineSegment>(),
                    ComponentType.ReadWrite<LocalToWorld>(),
                },
                Any = new ComponentType[]{
                },
                None = new ComponentType[]{
                },
            };
            lineSegmentGroup = GetComponentGroup(lineSegmentQuery);
            billboardCameraGroup = GetComponentGroup(
                ComponentType.ReadOnly<BillboardCamera>(),
                ComponentType.ReadOnly<LocalToWorld>()
            );
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var aca = billboardCameraGroup.CreateArchetypeChunkArray(Allocator.TempJob);

            var linePositioningJobHandle = new LinePositioningJob
            {
                cameraAca = aca,
                lastSystemVersion = LastSystemVersion,
                lineSegmentType = GetArchetypeChunkComponentType<LineSegment>(isReadOnly: true),
                ltwType = GetArchetypeChunkComponentType<LocalToWorld>(isReadOnly: false),
            }.Schedule(lineSegmentGroup, inputDeps);

            return linePositioningJobHandle;
        }

        [BurstCompile]
        struct LinePositioningJob : IJobChunk
        {
            [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> cameraAca;
            [ReadOnly] public ArchetypeChunkComponentType<LineSegment> lineSegmentType;
            public uint lastSystemVersion;

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

                    if (seg.from.Equals(seg.to))
                    {
                        continue;
                    }

                    float3 forward = seg.to - seg.from;

                    //We will use length too so not using normalize here
                    float lineLength = math.length(forward);
                    float3 forwardUnit = forward / lineLength;

                    //billboard rotation

                    //If forward and toCamera is collinear the cross product is 0
                    //and it will gives quaternion with tons of NaN
                    //So we have to check for that and do nothing if that is the case
                    quaternion rotation = quaternion.identity;
                    if (cameraAca.Length != 0)
                    {
                        var cameraLtws = cameraAca[0].GetNativeArray(ltwType);
                        var cameraPos = cameraLtws[0].Position;
                        float3 toCamera = math.normalize(cameraPos - seg.from);

                        if ((seg.from.Equals(cameraPos) || math.cross(forwardUnit, toCamera).Equals(float3.zero)) == false)
                        {
                            rotation = quaternion.LookRotation(forwardUnit, toCamera);
                        }
                    }

                    var mat = math.mul(
                            new float4x4(rotation, seg.from),
                            float4x4.Scale(seg.lineWidth, 1, lineLength));

                    ltws[i] = new LocalToWorld { Value = mat };
                }
            }
        }
    }
}
