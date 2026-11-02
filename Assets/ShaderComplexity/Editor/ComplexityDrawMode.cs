using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ComplexityDrawMode
{
    const string MODE_NAME = "Shader Complexity";
    const string SECTION_NAME = "Shader Complexity";

    static ComplexityDrawMode()
    {
        SceneView.AddCameraMode(MODE_NAME, SECTION_NAME);
        SceneView.duringSceneGui += UpdateSceneView;
    }

    static void UpdateSceneView(SceneView scene_view)
    {
        var cameras = ShaderComplexityFeature._enabled_cameras;
        
        cameras.RemoveWhere(camera => camera == null);

        var mode = scene_view.cameraMode;
        bool enabled = mode.drawMode == DrawCameraMode.UserDefined && mode.name == MODE_NAME;

        Camera scene_camera = scene_view.camera;
        if (enabled == cameras.Contains(scene_camera))
            return;

        if (enabled)
        {
            cameras.Add(scene_camera);

            if (ShaderComplexityFeature._complexities.Count == 0)
                ComplexityCounter.CalculateFromMenu();
        }
        else
        {
            cameras.Remove(scene_camera);
        }

        scene_view.Repaint();
    }
}
