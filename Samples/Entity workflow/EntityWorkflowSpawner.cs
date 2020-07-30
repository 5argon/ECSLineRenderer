using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

using E7.ECS.LineRenderer;

public class EntityWorkflowSpawner : MonoBehaviour
{

    [SerializeField] Material _material = null;
    [SerializeField] [Min(0.01f)] float _lineWidth = 0.1f;

    [SerializeField] [Range(3,1000)] int _segments = 1000;
    [SerializeField] float _worldScale = 1f;
    [SerializeField] float _tauFactor = 1f;
    [SerializeField] bool _spherize = false;

    static World _world = null;
    Entity[] _entities = null;


    #if UNITY_EDITOR
    void OnDrawGizmos ()
    {
        float4x4 matrix = transform.localToWorldMatrix;
        float theta = 0;
        for( int i=0 ; i<_segments ; i++ )
        {
            GeneratePoints(
                theta:         ref theta ,
                tauFactor:     _tauFactor ,
                i:              i ,
                segments:       _segments ,
                worldScale:     _worldScale ,
                p0:             out var p0 ,
                p1:             out var p1
            );
            p0 = Mul( matrix , p0 );
            p1 = Mul( matrix , p1 );
            if( _spherize )
            {
                p0 = math.normalize(p0) * _worldScale;
                p1 = math.normalize(p1) * _worldScale;
            }
            Gizmos.DrawLine( p0 , p1 );
        }
    }
    void OnValidate ()
    {
        if( Application.isPlaying && _world!=null && _entities!=null )
        {
            var command = _world.EntityManager;

            // update lineWidth:
            for( int i=0 ; i<_entities.Length ; i++ )
            {
                var segment = command.GetComponentData<LineSegment>( _entities[i] );;
                segment.lineWidth = _lineWidth;
                command.SetComponentData( _entities[i] , segment );
            }

            // update material:
            {
                int styleIndex = command.GetSharedComponentDataIndex<LineStyle>( _entities[0] );
                var styles = new List<LineStyle>(1);
                command.GetAllUniqueSharedComponentData<LineStyle>( styles );
                var style = styles[styleIndex];
                style.material = _material;
                styles[styleIndex] = style;
            }
        }
    }
    #endif

    void Start ()
    {
        if( _material==null )
        {
            Debug.LogWarning($"You forgot to set \'{nameof(_material)}\' field in the inspector!",gameObject);
            return;
        }
        
        if( _world==null )
        {
            _world = new World($"{nameof(EntityWorkflowSpawner)} World");
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(
                    _world ,
                    typeof(LineSegmentRegisterSystem)
                ,   typeof(LineSegmentTransformSystem)
            );
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop( _world , UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop() );
        }

        _entities = InstantiateLines(
            world:      _world ,
            material:   _material ,
            lineWidth:  _lineWidth ,
            segments:   _segments ,
            tauFactor:  _tauFactor ,
            worldScale: _worldScale ,
            matrix:     transform.localToWorldMatrix ,
            spherize:   _spherize
        );

        _world.GetOrCreateSystem<LineSegmentRegisterSystem>().Update();// force processing now (fixes first Update error)
    }

    void Update ()
    {
        if( _world!=null && _entities!=null )
        {
            var command = _world.EntityManager;
            int numEntities = _entities.Length;
            var matrices = new Matrix4x4[ numEntities ];
            for( int i=0 ; i<numEntities ; i++ )
            {
                var ltr = command.GetComponentData<LocalToWorld>( _entities[i] );
                matrices[ i ] = ltr.Value;
            }
            var mesh = _world.GetExistingSystem<LineSegmentRegisterSystem>().lineMesh;
            UnityEngine.Assertions.Assert.IsNotNull(mesh);
            Graphics.DrawMeshInstanced(
                mesh ,
                0 ,
                _material ,
                matrices
            );
        }
    }

    void OnDestroy ()
    {
        if( Application.isPlaying && _world!=null && _world.IsCreated && _entities!=null )
        {
            var command = _world.EntityManager;
            foreach( var entity in _entities )
                command.DestroyEntity( entity );
        }
    }

    static Entity[] InstantiateLines
    (
        World world ,
        Material material ,
        float lineWidth ,
        int segments ,
        float tauFactor ,
        float worldScale ,
        float4x4 matrix ,
        bool spherize
    )
    {
        var command = world.EntityManager;
        
        var prefabArchetype = command.CreateArchetype(
                typeof(LineSegment)
            ,   typeof(LineStyle)
            ,   typeof(Prefab)
        );
        var prefab = command.CreateEntity( prefabArchetype );
        command.SetSharedComponentData( prefab , new LineStyle{
            material = material
        } );

        var instances = command.Instantiate( prefab , segments , Allocator.Temp );
        {
            float theta = 0;
            for( int i=0 ; i<segments ; i++ )
            {
                GeneratePoints(
                    theta:          ref theta ,
                    tauFactor:      tauFactor ,
                    i:              i ,
                    segments:       segments ,
                    worldScale:     worldScale ,
                    p0:             out var p0 ,
                    p1:             out var p1
                );
                p0 = Mul( matrix , p0 );
                p1 = Mul( matrix , p1 );
                if( spherize )
                {
                    p0 = math.normalize(p0) * worldScale;
                    p1 = math.normalize(p1) * worldScale;
                }

                var entity = instances[i];
                command.SetComponentData( entity , new LineSegment{
                    from        = p0 ,
                    to          = p1 ,
                    lineWidth   = lineWidth
                } );
            }
        }
        var arr = instances.ToArray();
        instances.Dispose();
        return arr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void GeneratePoints
    (
        ref float theta ,
        in float tauFactor ,
        in int i ,
        in int segments ,
        in float worldScale ,
        out float3 p0 ,
        out float3 p1
    )
    {
        float theta_step = (float)( math.PI_DBL * 2.0 / segments ) * tauFactor;
        float theta_next = theta + theta_step;
        p0 = new float3{ x=math.cos(theta) , y=0 , z=math.sin(theta) } * (float)i/(float)segments * worldScale;
        p1 = new float3{ x=math.cos(theta_next) , y=0 , z=math.sin(theta_next) } * ((float)i+1f)/(float)segments * worldScale;
        theta += theta_step;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static float3 Mul ( float4x4 matrix , float3 f3 )
    {
        float4 f4 = math.mul( matrix , new float4(f3,1) );
        return new float3{ x=f4.x , y=f4.y , z=f4.z };
    }

}
