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

        Mesh _lineMesh;
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
        }

        protected override void OnStartRunning ()
        {
            if( _lineMesh==null )
                _lineMesh = CreateMesh();
        }

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            //Migrate material on LineStyle to RenderMesh by chunks
            using (var aca = newRegisterQuery.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                if (aca.Length > 0)
                {
                    EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

                    var lineStyleType = GetArchetypeChunkSharedComponentType<LineStyle>();

                    for (int i = 0; i < aca.Length; i++)
                    {
                        ArchetypeChunk ac = aca[i];
                        var lineStyle = ac.GetSharedComponentData<LineStyle>(lineStyleType, EntityManager);

                        //Filter to narrow down chunk operation.
                        newRegisterQuery.SetSharedComponentFilter(lineStyle);
                        ecb.AddSharedComponent( newRegisterQuery , new RenderMesh{
                            mesh = _lineMesh ,
                            material = lineStyle.material
                        });
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

        static Mesh CreateMesh ()
		{
			var mesh = new Mesh();
			mesh.name = "quad 1x1, pivot at bottom center";
			mesh.vertices = new Vector3[4]{ new Vector3{ x=-0.5f } , new Vector3{ x=0.5f } , new Vector3{ x=-0.5f , z=1 } , new Vector3{ x=0.5f , z=1 } };
			mesh.triangles = new int[6]{ 0 , 2 , 1 , 2 , 3 , 1 };
			mesh.normals = new Vector3[4]{ -Vector3.forward , -Vector3.forward , -Vector3.forward , -Vector3.forward };
			mesh.uv = new Vector2[4]{ new Vector2{ x=0 , y=0 } , new Vector2{ x=1 , y=0 } , new Vector2{ x=0 , y=1 } , new Vector2{ x=1 , y=1 } };
			return mesh;
		}

    }
}
