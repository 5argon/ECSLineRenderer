using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// Line will rotate to face the first camera found with this proxy.
    /// </summary>
    [RequireComponent(typeof(CopyTransformFromGameObjectProxy))]
    [RequireComponent(typeof(LocalToWorldProxy))]
    public class BillboardCameraProxy : ComponentDataProxy<BillboardCamera> { }
}
