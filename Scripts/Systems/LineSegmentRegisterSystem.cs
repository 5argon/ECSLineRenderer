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
        
        protected override void OnUpdate ()
        {
            var mesh = Internal.MeshProvider.lineMesh;
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

    }
}
