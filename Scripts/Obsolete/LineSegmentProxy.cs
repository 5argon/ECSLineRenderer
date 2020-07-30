using Unity.Entities;

namespace E7.ECS.LineRenderer
{
    [System.Obsolete]
    public class LineSegmentProxy : ComponentDataProxy<LineSegment> { 

        protected override void ValidateSerializedData(ref LineSegment serializedData)
        {
            if(serializedData.lineWidth < 0) serializedData.lineWidth = 0;
        }
    }
}
