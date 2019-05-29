using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// Adds <see cref="RenderMesh"> and <see cref="LocalToWorld"> to new <see cref="LineSegment">.
    /// </summary>
    [ExecuteAlways]
    [UpdateBefore(typeof(LineSegmentTransformSystem))]
    public class LineSegmentRegisterSystem : ComponentSystem
    {
        public struct RegisteredState : ISystemStateComponentData { }

        EntityQuery toUnregisterLinesQuery;
        EntityQuery toRegisterLinesQuery;

        protected override void OnCreate()
        {
            var toRegisterLinesQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[]{
                    ComponentType.ReadOnly<LineSegment>(),
                    ComponentType.ReadOnly<LineStyle>(),
                },
                Any = new ComponentType[]{
                },
                None = new ComponentType[]{
                    ComponentType.ReadOnly<RegisteredState>(),
                },
            };
            var toUnregisterLinesQueryDesc = new EntityQueryDesc
            {
                All = new ComponentType[]{
                    ComponentType.ReadOnly<RegisteredState>(),
                },
                Any = new ComponentType[]{
                },
                None = new ComponentType[]{
                    ComponentType.ReadOnly<LineSegment>(),
                    ComponentType.ReadOnly<LineStyle>(),
                },
            };

            toRegisterLinesQuery = GetEntityQuery(toRegisterLinesQueryDesc);
            toUnregisterLinesQuery = GetEntityQuery(toUnregisterLinesQueryDesc);

            CreateMeshIfNotYet();
        }

        protected override void OnUpdate()
        {
            //While not attaching registered state yet, add render mesh to all new members.
            // using (var aca = toRegisterLinesQuery.CreateArchetypeChunkArray(Allocator.TempJob))
            // {
            //     if (aca.Length > 0)
            //     {
            //         NativeList<int> uniqueLineStyleIndexes = new NativeList<int>(Allocator.Temp);
            //         var lineStyleType = GetArchetypeChunkSharedComponentType<LineStyle>();

            //         //TODO : This shouldn't be needed, but somehow the mesh became `null` in editor world??
            //         CreateMeshIfNotYet();

            //         for (int i = 0; i < aca.Length; i++)
            //         {
            //             ArchetypeChunk ac = aca[i];
            //             var index = ac.GetSharedComponentIndex(lineStyleType);
            //             Debug.Log($"Shared index to filter : {index}");
            //             uniqueLineStyleIndexes.Add(index);
            //         }

            //         //Use filter to batch migrate the line style to render mesh.
            //         for (int i = 0; i < uniqueLineStyleIndexes.Length; i++)
            //         {
            //             var ls = EntityManager.GetSharedComponentData<LineStyle>(uniqueLineStyleIndexes[i]);
            //             toRegisterLinesQuery.SetFilter(ls);
            //             EntityManager.AddSharedComponentData(toRegisterLinesQuery, new RenderMesh { mesh = lineMesh, material = ls.lineMaterial });
            //         }
            //         toRegisterLinesQuery.ResetFilter();
            //     }
            // }

            using (var aca = toRegisterLinesQuery.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                if (aca.Length > 0)
                {
                    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

                    var lineStyleType = GetArchetypeChunkSharedComponentType<LineStyle>();
                    var entityType = GetArchetypeChunkEntityType();

                    //TODO : This shouldn't be needed, but somehow the mesh became `null` in editor world??
                    CreateMeshIfNotYet();

                    for (int i = 0; i < aca.Length; i++)
                    {
                        ArchetypeChunk ac = aca[i];
                        var ea = ac.GetNativeArray(entityType);
                        var lineStyle = ac.GetSharedComponentData<LineStyle>(lineStyleType, EntityManager);
                        for (int j = 0; j < ea.Length; j++)
                        {
                            //Must use ECB or else it would invalidate the interating chunks, etc.
                            ecb.AddSharedComponent(ea[j], new RenderMesh { mesh = lineMesh, material = lineStyle.material });
                        }
                    }

                    ecb.Playback(EntityManager);
                }
            }

            //Use EQ operation to prepare other components where they don't need initialization value.
            EntityManager.AddComponent(toRegisterLinesQuery, ComponentType.ReadOnly<Translation>());
            EntityManager.AddComponent(toRegisterLinesQuery, ComponentType.ReadOnly<Rotation>());
            EntityManager.AddComponent(toRegisterLinesQuery, ComponentType.ReadOnly<NonUniformScale>());
            //Unity stopped adding LTW for us without GO conversion.
            EntityManager.AddComponent(toRegisterLinesQuery, ComponentType.ReadOnly<LocalToWorld>()); 

            //This make them not registered again.
            EntityManager.AddComponent(toRegisterLinesQuery, ComponentType.ReadOnly<RegisteredState>());

            //This is for clean up of system state component.
            EntityManager.RemoveComponent(toUnregisterLinesQuery, ComponentType.ReadOnly<RegisteredState>());
        }

        /// <summary>
        /// A rectangle we will always use for all lines.
        /// </summary>
        Mesh lineMesh;

        const float lineDefaultWidth = 1f;
        const float lineDefaultWidthHalf = lineDefaultWidth / 2f;
        const string lineMeshName = "ECSLineMesh";
        private void CreateMeshIfNotYet()
        {
            if (lineMesh == null)
            {
                var mesh = new Mesh();
                mesh.name = lineMeshName;

                var vertices = new Vector3[4];

                vertices[0] = new Vector3(-lineDefaultWidthHalf, 0, 0);
                vertices[1] = new Vector3(lineDefaultWidthHalf, 0, 0);
                vertices[2] = new Vector3(-lineDefaultWidthHalf, 0, 1);
                vertices[3] = new Vector3(lineDefaultWidthHalf, 0, 1);

                mesh.vertices = vertices;

                var tri = new int[6];

                tri[0] = 0;
                tri[1] = 2;
                tri[2] = 1;

                tri[3] = 2;
                tri[4] = 3;
                tri[5] = 1;

                mesh.triangles = tri;

                var normals = new Vector3[4];

                normals[0] = -Vector3.forward;
                normals[1] = -Vector3.forward;
                normals[2] = -Vector3.forward;
                normals[3] = -Vector3.forward;

                mesh.normals = normals;

                var uv = new Vector2[4];

                uv[0] = new Vector2(0, 0);
                uv[1] = new Vector2(1, 0);
                uv[2] = new Vector2(0, 1);
                uv[3] = new Vector2(1, 1);

                mesh.uv = uv;

                lineMesh = mesh;
            }
        }


    }
}
