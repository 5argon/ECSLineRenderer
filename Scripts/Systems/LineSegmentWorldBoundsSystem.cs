using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

namespace E7.ECS.LineRenderer
{
	/// <summary>
	/// Calculates <see cref="WorldRenderBounds" from <see cref="RenderBounds" and <see cref="LineSegment"/>.
	/// Start this system when you need <see cref="WorldRenderBounds"/> but no other system calculates it and you still want to do a culling pass.
	/// </summary>
	[WorldSystemFilter(0)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class LineSegmentWorldRenderBoundsSystem : SystemBase
    {
        protected override void OnUpdate ()
		{
			Entities
				.WithName("WorldRenderBounds_update_job")
				.ForEach( ( ref WorldRenderBounds wrb , in RenderBounds bounds , in LineSegment segment ) =>
				{
					float3 lineVec = segment.to - segment.from;
					var rot = quaternion.identity;
					var pos = segment.from;
					var scale = new float3{ x=segment.lineWidth , y=1f , z=math.length(lineVec) };
					wrb.Value = AABB.Transform( float4x4.TRS(pos,rot,scale) , bounds.Value );
				}).ScheduleParallel();
		}

	}
}
