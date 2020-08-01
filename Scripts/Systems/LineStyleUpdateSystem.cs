using System.Collections.Generic;
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
    /// Forwards changes in <see cref="LineStyle"/> component to <see cref="RenderMesh"/> component.
    /// NOTE: System is started 100% manually until it's performace is sorted out.
    ///       world.GetOrCreateSystem<LineStyleUpdateSystem>().Update();
    /// </summary>
    [WorldSystemFilter( 0 )]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class LineStyleUpdateSystem : SystemBase
    {
        
        EntityQuery _query;
        List<LineStyle> _styles = new List<LineStyle>(10);

        protected override void OnCreate ()
        {
            _query = EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<LineStyle>()
                ,   ComponentType.ReadWrite<RenderMesh>()
            );
        }

        protected override void OnUpdate ()
        {
            var command = EntityManager;
            var cmd = new EntityCommandBuffer( Allocator.Temp );

            _styles.Clear();
            command.GetAllUniqueSharedComponentData( _styles );

            var mesh = LineSegmentRegisterSystem.lineMesh;
            foreach( var style in _styles )
            {
                _query.SetSharedComponentFilter( style );
                command.SetSharedComponentData( _query , new RenderMesh{
                    mesh        = mesh ,
                    material    = style.material
                } );
            }
        }
    }
}
