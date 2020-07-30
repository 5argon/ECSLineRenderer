using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    [ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class LineSegmentTransformSystem : SystemBase
    {
        protected override void OnUpdate ()
		{
			Camera camera = Camera.main;
			if( camera==null ) return;
			Transform cameraTransform = camera.transform;
			if( !camera.orthographic )// perspective-projection camera code path
			{
				float3 cameraPosition = cameraTransform.position;
				Entities
					.WithName("TRS_update_job")
					.ForEach( ( ref LocalToWorld ltr , in LineSegment segment ) =>
					{
						float3 lineVec = segment.to - segment.from;
						var rot = quaternion.LookRotation( math.normalize(lineVec) , math.normalize(cameraPosition-segment.from) );
						var pos = segment.from;
						var scale = new float3{ x=segment.lineWidth , y=1f , z=math.length(lineVec) };
						ltr.Value = float4x4.TRS( pos , rot , scale );
					}).ScheduleParallel();
				}
			else// orthographic-projection camera
			{
				quaternion cameraRotation = cameraTransform.rotation;
				Entities
					.WithName("TRS_orthographic_update_job")
					.ForEach( ( ref LocalToWorld ltr , in LineSegment segment ) =>
					{
						float3 lineVec = segment.to - segment.from;
						var rot = quaternion.LookRotation( math.normalize(lineVec) , math.mul(cameraRotation,new float3{z=-1}) );
						var pos = segment.from;
						var scale = new float3{ x=segment.lineWidth , y=1f , z=math.length(lineVec) };
						ltr.Value = float4x4.TRS( pos , rot , scale );
					}).ScheduleParallel();
			}
		}

        public static float4x4 SimpleMatrix ( LineSegment segment )
        {
            float3 lineVec = segment.to - segment.from;
            var rot = quaternion.identity;
            var pos = segment.from;
            var scale = new float3{ x=segment.lineWidth , y=1f , z=math.length(lineVec) };
            return float4x4.TRS( pos , rot , scale );
        }

	}
}
