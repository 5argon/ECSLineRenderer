using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// Any new <see cref="LineSegment"/> together with <see cref="LineStyle"/>
    /// get TRS, LTW, and <see cref="RenderMesh"/> so it is ready for rendering.
    ///
    /// <see cref="LineSegmentTransformSystem"/> will put data in <see cref="LineSegment"/>
    /// to TRS, then Unity Transform system put TRS to LTW, then you see the rendering.
    /// </summary>
    [ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class LineSegmentRegisterSystem : JobComponentSystem
    {
        struct RegisteredState : ISystemStateComponentData
        {
        }

        EntityQuery cleanUpQuery;
        EntityQuery newRegisterQuery;

        protected override void OnCreate()
        {
            newRegisterQuery = GetEntityQuery(
                ComponentType.ReadOnly<LineSegment>(),
                ComponentType.ReadOnly<LineStyle>(),
                ComponentType.Exclude<RegisteredState>()
            );

            cleanUpQuery = GetEntityQuery(
                ComponentType.Exclude<LineSegment>(),
                ComponentType.Exclude<LineStyle>(),
                ComponentType.ReadOnly<RegisteredState>()
            );
            CreateMeshIfNotYet();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            //Migrate material on LineStyle to RenderMesh by chunks
            using (var aca = newRegisterQuery.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                if (aca.Length > 0)
                {
                    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

                    var lineStyleType = GetArchetypeChunkSharedComponentType<LineStyle>();

                    //TODO : This shouldn't be needed, but somehow the mesh became `null` in editor world??
                    CreateMeshIfNotYet();

                    for (int i = 0; i < aca.Length; i++)
                    {
                        ArchetypeChunk ac = aca[i];
                        var lineStyle = ac.GetSharedComponentData<LineStyle>(lineStyleType, EntityManager);

                        //Filter to narrow down chunk operation.
                        newRegisterQuery.SetSharedComponentFilter(lineStyle);
                        ecb.AddSharedComponent(newRegisterQuery,
                            new RenderMesh {mesh = lineMesh, material = lineStyle.material});
                    }

                    ecb.Playback(EntityManager);
                    newRegisterQuery.ResetFilter();
                }
            }

            //Use EQ operation to prepare other components where they don't need initialization value.
            EntityManager.AddComponent(newRegisterQuery, ComponentType.ReadOnly<Translation>());
            EntityManager.AddComponent(newRegisterQuery, ComponentType.ReadOnly<Rotation>());
            EntityManager.AddComponent(newRegisterQuery, ComponentType.ReadOnly<NonUniformScale>());
            //Unity stopped adding LTW for us without GO conversion.
            EntityManager.AddComponent(newRegisterQuery, ComponentType.ReadOnly<LocalToWorld>());

            //This make them not registered again.
            EntityManager.AddComponent(newRegisterQuery, ComponentType.ReadOnly<RegisteredState>());

            //This is for clean up of system state component in the case the entity was destroyed.
            EntityManager.RemoveComponent(cleanUpQuery, ComponentType.ReadOnly<RegisteredState>());
            
            return default;
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