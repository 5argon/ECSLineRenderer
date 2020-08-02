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
    /// </summary>
    [ExecuteAlways]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class LineSegmentRegisterSystem : SystemBase
    {
        
        public static Mesh lineMesh { get; private set; }

        protected override void OnUpdate ()
        {
            var mesh = lineMesh;
            var bounds = mesh.bounds;
            AABB aabb = new AABB{ Center=bounds.center , Extents = bounds.extents };
            EntityCommandBuffer ecb = new EntityCommandBuffer( Allocator.Temp );

            Entities
                .WithName("add_components_job")
                .WithoutBurst()
                .WithNone<RenderMesh>()
                .ForEach( ( in Entity entity , in LineStyle style , in LineSegment segment ) =>
                {
                    ecb.AddSharedComponent( entity , new RenderMesh{
                        mesh        = mesh ,
                        material    = style.material
                    });
                    ecb.AddComponent( entity , ComponentType.ReadWrite<LocalToWorld>() );
                    ecb.AddComponent( entity , ComponentType.ReadWrite<RenderBounds>() );
                    ecb.SetComponent( entity , new RenderBounds{
                        Value = aabb
                    });
                    ecb.AddComponent( entity , ComponentType.ReadWrite<WorldRenderBounds>() );
                }).Run();

            ecb.Playback( EntityManager );
            ecb.Dispose();
        }


        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        #endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void InitializeType ()
        {
            lineMesh = new Mesh();
            lineMesh.name = "quad 1x1, pivot at bottom center";
            lineMesh.vertices = new Vector3[4]{ new Vector3{ x=-0.5f } , new Vector3{ x=0.5f } , new Vector3{ x=-0.5f , z=1 } , new Vector3{ x=0.5f , z=1 } };
            lineMesh.triangles = new int[6]{ 0 , 2 , 1 , 2 , 3 , 1 };
            lineMesh.normals = new Vector3[4]{ -Vector3.forward , -Vector3.forward , -Vector3.forward , -Vector3.forward };
            lineMesh.uv = new Vector2[4]{ new Vector2{ x=0 , y=0 } , new Vector2{ x=1 , y=0 } , new Vector2{ x=0 , y=1 } , new Vector2{ x=1 , y=1 } };
        }

    }
}
