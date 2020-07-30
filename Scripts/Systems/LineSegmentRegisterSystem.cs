using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
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
    public class LineSegmentRegisterSystem : SystemBase
    {

        Mesh _lineMesh;

        protected override void OnUpdate ()
        {
            if( _lineMesh==null ) _lineMesh = CreateMesh();
            var bounds = _lineMesh.bounds;
            AABB aabb = new AABB{ Center=bounds.center , Extents = bounds.extents };
            EntityCommandBuffer ecb = new EntityCommandBuffer( Allocator.Temp );

            Entities
                .WithName("add_components_job")
                .WithoutBurst()
                .WithNone<RenderMesh>()
                .ForEach( ( in Entity entity , in LineStyle style , in LineSegment segment ) =>
                {
                    ecb.AddSharedComponent( entity , new RenderMesh{
                        mesh        = _lineMesh ,
                        material    = style.material
                    });
                    ecb.AddComponent( entity , ComponentType.ReadWrite<LocalToWorld>() );
                    ecb.AddComponent( entity , ComponentType.ReadWrite<RenderBounds>() );
                    ecb.SetComponent( entity , new RenderBounds{ Value = aabb });
                    ecb.AddComponent( entity , ComponentType.ReadWrite<WorldRenderBounds>() );
                }).Run();

            ecb.Playback( EntityManager );
            ecb.Dispose();
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
