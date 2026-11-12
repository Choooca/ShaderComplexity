using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ComplexityDrawMode
{
    const string MODE_NAME = "Shader Complexity";
    const string SECTION_NAME = "Shader Complexity";

    static readonly Color[] RAMP =
    {
        new Color(0.000f, 1.000f, 0.127f),
        new Color(0.000f, 1.000f, 0.000f),
        new Color(0.046f, 0.520f, 0.000f),
        new Color(0.215f, 0.215f, 0.000f),
        new Color(0.520f, 0.046f, 0.000f),
        new Color(0.700f, 0.000f, 0.000f),
        new Color(1.000f, 0.000f, 0.000f),
        new Color(1.000f, 0.000f, 0.500f),
        new Color(1.000f, 0.900f, 0.900f)
    };

    const float LEGEND_WIDTH = 200f;
    const float RAMP_HEIGHT = 12f;
    const float LABEL_HEIGHT = 16f;
    const float PADDING = 6f;
    const float MARGIN = 10f;

    static Texture2D _ramp_texture;
    static GUIStyle _right_label_style;

    static ComplexityDrawMode()
    {
        SceneView.AddCameraMode(MODE_NAME, SECTION_NAME);
        SceneView.duringSceneGui += UpdateSceneView;
    }

    static void UpdateSceneView(SceneView scene_view)
    {
        var mode = scene_view.cameraMode;
        bool enabled = mode.drawMode == DrawCameraMode.UserDefined && mode.name == MODE_NAME;

        UpdateEnabledCamera(scene_view, enabled);

        if (enabled)
            DrawLegend(scene_view);
    }

    static void UpdateEnabledCamera(SceneView scene_view, bool enabled)
    {
        var cameras = ShaderComplexityFeature._enabled_cameras;

        cameras.RemoveWhere(camera => camera == null);

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

    static void DrawLegend(SceneView scene_view)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        if (_ramp_texture == null)
            _ramp_texture = CreateRampTexture();

        if (_right_label_style == null)
            _right_label_style = new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.MiddleRight };

        float view_height = scene_view.camera.pixelRect.height / EditorGUIUtility.pixelsPerPoint;

        float legend_height = PADDING + RAMP_HEIGHT + LABEL_HEIGHT + PADDING;
        Rect background_rect = new Rect(MARGIN, view_height - legend_height - MARGIN, LEGEND_WIDTH + PADDING * 2f, legend_height);
        Rect ramp_rect = new Rect(background_rect.x + PADDING, background_rect.y + PADDING, LEGEND_WIDTH, RAMP_HEIGHT);
        Rect label_rect = new Rect(ramp_rect.x, ramp_rect.yMax, LEGEND_WIDTH, LABEL_HEIGHT);

        Handles.BeginGUI();

        EditorGUI.DrawRect(background_rect, new Color(0f, 0f, 0f, 0.6f));
        GUI.DrawTexture(ramp_rect, _ramp_texture, ScaleMode.StretchToFill);
        GUI.Label(label_rect, "0", EditorStyles.whiteMiniLabel);
        GUI.Label(label_rect, ShaderComplexityFeature._displayed_max_complexity.ToString("0"), _right_label_style);

        Handles.EndGUI();
    }

    static Texture2D CreateRampTexture()
    {
        const int WIDTH = 256;

        var texture = new Texture2D(WIDTH, 1, TextureFormat.RGBA32, false, true)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int x = 0; x < WIDTH; ++x)
            texture.SetPixel(x, 0, SampleRamp(x / (float)(WIDTH - 1)));

        texture.Apply();
        return texture;
    }
    
    static Color SampleRamp(float t)
    {
        t = Mathf.Clamp01(t);

        float scaled = t * (RAMP.Length - 1);
        int index = Mathf.Min(Mathf.FloorToInt(scaled), RAMP.Length - 2);
        float f = scaled - index;
        return Color.Lerp(RAMP[index], RAMP[index + 1], f);
    }
}
