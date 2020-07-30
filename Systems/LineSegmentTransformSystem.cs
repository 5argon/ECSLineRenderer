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
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class LineSegmentTransformSystem : JobComponentSystem
    {
        EntityQuery lineSegmentQuery;
        EntityQuery billboardCameraQuery;

        protected override void OnCreate()
        {
            lineSegmentQuery = GetEntityQuery(
                ComponentType.ReadOnly<LineSegment>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<NonUniformScale>()
            );
            billboardCameraQuery = GetEntityQuery(
                ComponentType.ReadOnly<BillboardCamera>(),
                ComponentType.ReadWrite<LocalToWorld>()
            );
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var cameraAca = billboardCameraQuery.CreateArchetypeChunkArray(Allocator.TempJob);

            var linePositioningJobHandle = new LinePositioningJob
            {
                cameraAca = cameraAca,
                lastSystemVersion = LastSystemVersion,
                lineSegmentType = GetArchetypeChunkComponentType<LineSegment>(isReadOnly: true),
                ltwType = GetArchetypeChunkComponentType<LocalToWorld>(isReadOnly: true),
                translationType = GetArchetypeChunkComponentType<Translation>(isReadOnly: false),
                rotationType = GetArchetypeChunkComponentType<Rotation>(isReadOnly: false),
                scaleType = GetArchetypeChunkComponentType<NonUniformScale>(isReadOnly: false),
            }.Schedule(lineSegmentQuery, inputDeps);

            return linePositioningJobHandle;
        }

        [BurstCompile]
        struct LinePositioningJob : IJobChunk
        {
            [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> cameraAca;
            [ReadOnly] public ArchetypeChunkComponentType<LineSegment> lineSegmentType;
            public uint lastSystemVersion;

            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> ltwType;

            public ArchetypeChunkComponentType<Translation> translationType;
            public ArchetypeChunkComponentType<Rotation> rotationType;
            public ArchetypeChunkComponentType<NonUniformScale> scaleType;

            public void Execute(ArchetypeChunk ac, int chunkIndex, int firstEntityIndex)
            {
                //Do not commit to change if possible

                bool lineChunkChanged = ac.DidChange(lineSegmentType, lastSystemVersion);
                bool cameraMovedOrRotated = cameraAca.Length != 0 && cameraAca[0].DidChange(ltwType, lastSystemVersion);

                if (!lineChunkChanged && !cameraMovedOrRotated) return;

                //These gets will commit a version bump
                var segs = ac.GetNativeArray(lineSegmentType);
                var trans = ac.GetNativeArray(translationType);
                var rots = ac.GetNativeArray(rotationType);
                var scales = ac.GetNativeArray(scaleType);

                for (int i = 0; i < segs.Length; i++)
                {
                    var seg = segs[i];

                    var tran = trans[i];
                    var rot = rots[i];
                    var scale = scales[i];

                    if (seg.from.Equals(seg.to))
                    {
                        continue;
                    }

                    float3 forward = seg.to - seg.from;

                    //We will use length too so not using normalize here
                    float lineLength = math.length(forward);
                    float3 forwardUnit = forward / lineLength;

                    //Billboard rotation

                    quaternion rotation = quaternion.identity;
                    if (cameraAca.Length != 0)
                    {
                        var cameraTranslations = cameraAca[0].GetNativeArray(ltwType);

                        //TODO: Better support for multiple cameras. It would be via `alignWithCamera` on the LineStyle?

                        var cameraRigid = math.RigidTransform(cameraTranslations[0].Value);
                        var cameraTranslation = cameraRigid.pos;
                        
                        //TODO : use this somehow? Currently billboard is wrong.
                        // If anyone understand http://www.opengl-tutorial.org/intermediate-tutorials/billboards-particles/billboards/
                        // please tell me how to do this..
                        var cameraRotation = cameraRigid.rot; 

                        float3 toCamera = math.normalize(cameraTranslation - seg.from);

                        //If forward and toCamera is collinear the cross product is 0
                        //and it will gives quaternion with tons of NaN
                        //So we rather do nothing if that is the case
                        if ((seg.from.Equals(cameraTranslation) ||
                             math.cross(forwardUnit, toCamera).Equals(float3.zero)) == false)
                        {
                            //This is wrong because it only taken account of the camera's position, not also its rotation.
                            rotation = quaternion.LookRotation(forwardUnit, toCamera);
                            //Debug.Log($"ROTATING {rotation} to {cameraTranslation}");
                        }
                    }

                    trans[i] = new Translation {Value = seg.from};
                    rots[i] = new Rotation {Value = rotation};
                    scales[i] = new NonUniformScale {Value = math.float3(seg.lineWidth, 1, lineLength)};
                }
            }
        }
    }
}