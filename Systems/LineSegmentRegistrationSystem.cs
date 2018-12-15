using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// Adds MeshInstanceRenderer and LocalToWorld to new LineSegment.
    /// </summary>
    [ExecuteInEditMode]
    public class LineSegmentRegisterSystem : ComponentSystem
    {
        public struct RegisteredState : ISystemStateComponentData { }
        private struct NewRegister : IComponentData { }
        private struct Unregister : IComponentData { }

        ComponentGroup cg;
        ComponentGroup newRegisterCg;
        ComponentGroup unregisterCg;

        protected override void OnCreateManager()
        {
            var query = new EntityArchetypeQuery
            {
                All = new ComponentType[]{
                    ComponentType.Create<LineSegment>(),
                    ComponentType.Create<LineStyle>(),
                },
                Any = new ComponentType[]{
                },
                None = new ComponentType[]{
                    ComponentType.Create<RegisteredState>(),
                },
            };
            var query2 = new EntityArchetypeQuery
            {
                All = new ComponentType[]{
                    ComponentType.Create<RegisteredState>(),
                },
                Any = new ComponentType[]{
                },
                None = new ComponentType[]{
                    ComponentType.Create<LineSegment>(),
                    ComponentType.Create<LineStyle>(),
                },
            };
            cg = GetComponentGroup(query, query2);
            newRegisterCg = GetComponentGroup(ComponentType.Create<NewRegister>());
            unregisterCg = GetComponentGroup(ComponentType.Create<Unregister>());
            CreateMesh();
        }

        Mesh lineMesh;
        const float lineDefaultWidth = 1f;
        const float lineDefaultWidthHalf = lineDefaultWidth / 2f;
        const string lineMeshName = "ECSLineMesh";
        private void CreateMesh()
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

        protected override void OnUpdate()
        {
            using (var ecb = new EntityCommandBuffer(Allocator.Temp))
            using (var aca = cg.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                var registeredType = GetArchetypeChunkComponentType<RegisteredState>();
                var lineSegmentType = GetArchetypeChunkComponentType<LineSegment>(isReadOnly: true);
                var et = GetArchetypeChunkEntityType();

                for (int i = 0; i < aca.Length; i++)
                {
                    ArchetypeChunk ac = aca[i];
                    if (!ac.Has(registeredType) && ac.Has(lineSegmentType))
                    {
                        var ea = ac.GetNativeArray(et);
                        for (int j = 0; j < ea.Length; j++)
                        {
                            Entity e = ea[j];
                            ecb.AddComponent(e, new NewRegister());
                        }
                    }
                    else if (ac.Has(registeredType) && !ac.Has(lineSegmentType))
                    {
                        var ea = ac.GetNativeArray(et);
                        for (int j = 0; j < ea.Length; j++)
                        {
                            Entity e = ea[j];
                            ecb.AddComponent(e, new Unregister());
                        }
                    }
                }
                ecb.Playback(EntityManager);
            }

            //Main thread operations, we could invalidate as we like here

            if (newRegisterCg.CalculateLength() > 0)
            {
                var ea = newRegisterCg.GetEntityArray().ToArray();
                for (int i = 0; i < ea.Length; i++)
                {
                    Entity e = ea[i];
                    var ls = EntityManager.GetSharedComponentData<LineStyle>(e);

                    EntityManager.AddSharedComponentData(e, new MeshInstanceRenderer {  mesh = lineMesh, material  = ls.lineMaterial });
                    EntityManager.AddComponentData(e, new LocalToWorld());

                    EntityManager.AddComponentData(e, new RegisteredState());
                    EntityManager.RemoveComponent<NewRegister>(e);
                }
            }

            if (unregisterCg.CalculateLength() > 0)
            {
                var ea = unregisterCg.GetEntityArray().ToArray();
                for (int i = 0; i < ea.Length; i++)
                {
                    Entity e = ea[i];
                    EntityManager.RemoveComponent<Unregister>(e);
                    EntityManager.RemoveComponent<RegisteredState>(e);
                }
            }
        }

    }
}
