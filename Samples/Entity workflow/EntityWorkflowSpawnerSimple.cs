using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

using E7.ECS.LineRenderer;

/// <summary>
/// Instantiates line entities in default World.DefaultGameObjectInjectionWorld
/// </summary>
public class EntityWorkflowSpawnerSimple : MonoBehaviour
{

    [SerializeField] Material _material = null;
    [SerializeField] [Min(0.01f)] float _lineWidth = 0.01f;
    [SerializeField] [Range(3,1000)] int _segments = 400;


    #if UNITY_EDITOR
    void OnDrawGizmos ()
    {
        float theta = 0;
        for( int i=0 ; i<_segments ; i++ )
        {
            GeneratePoints( theta:ref theta , i:i , segments:_segments , p0:out var p0 , p1:out var p1 );
            Gizmos.DrawLine( p0 , p1 );
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

        InstantiateLines( material:_material , lineWidth:_lineWidth , segments:_segments , matrix:transform.localToWorldMatrix );
    }

    static void InstantiateLines
    (
        Material material ,
        float lineWidth ,
        int segments ,
        float4x4 matrix
    )
    {
        var world = World.DefaultGameObjectInjectionWorld;
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
                GeneratePoints( theta:ref theta , i:i , segments:segments , p0:out var p0 , p1:out var p1 );
                var entity = instances[i];
                command.SetComponentData( entity , new LineSegment{
                    from        = p0 ,
                    to          = p1 ,
                    lineWidth   = lineWidth
                } );
            }
        }
        instances.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void GeneratePoints ( ref float theta , in int i , in int segments , out float3 p0 , out float3 p1 )
    {
        float theta_step = (float)( math.PI_DBL * 2.0 / segments ) * 10f;
        float theta_next = theta + theta_step;
        p0 = new float3{ x=math.cos(theta) , y=math.cos(theta)*math.sin(theta) , z=math.sin(theta) } * (float)i/(float)segments;
        p1 = new float3{ x=math.cos(theta_next) , y=math.cos(theta_next)*math.sin(theta_next) , z=math.sin(theta_next) } * ((float)i+1f)/(float)segments;
        theta += theta_step;
    }

}
