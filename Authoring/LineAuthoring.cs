using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// For use with Game Object Conversion Workflow / Subscene.
    /// 
    /// Generates an Entity with <see cref="LineSegment"/> and <see cref="LineStyle"/>.
    /// Then <see cref="LineSegmentRegisterSystem"/> will pick that up. 
    /// </summary>
    public class LineAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public LineSegment lineSegment;
        public LineStyle lineStyle;

        void OnValidate()
        {
            lineSegment.lineWidth = math.max(0, lineSegment.lineWidth);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, lineSegment);
            dstManager.AddSharedComponentData(entity, lineStyle);
        }
    }
}