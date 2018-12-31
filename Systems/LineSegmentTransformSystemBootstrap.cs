using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace E7.ECS.LineRenderer
{
    /// <summary>
    /// Shameless copy from Unity ones..
    /// </summary>
    [ExecuteInEditMode]
    public class LineSegmentTransformSystemBootstrap : ComponentSystem
    {
        protected override void OnCreateManager()
        {
            RenderPipeline.beginCameraRendering += OnBeforeCull;
            Camera.onPreCull += OnBeforeCull;

            this.Enabled = false;
        }

        protected override void OnUpdate() { }

        [Inject]
#pragma warning disable 649
        LineSegmentTransformSystem LSTS;
#pragma warning restore 649      
        public void OnBeforeCull(Camera camera)
        {
            //There are 2 more with hide flags : `SceneCamera` and `Preview Camera`
            if(camera != Camera.main) return;

#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER
            var prefabEditMode = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle() !=
                                 UnityEditor.SceneManagement.StageUtility.GetMainStageHandle();
            var gameCamera = (camera.hideFlags & HideFlags.DontSave) == 0;
            if (prefabEditMode && !gameCamera)
                return;
#endif
            LSTS.RememberCamera(camera);
            //Without this force update it would not render for the first frame in edit mode until we touch something?
            LSTS.Update(); 
        }
    }
}
