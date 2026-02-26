using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class BridgeScratch
{
    public static string Run()
    {
        var canvasGo = GameObject.Find("UI");
        if (canvasGo == null) return "ERROR: UI not found";

        // Canvas → Screen Space Overlay
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        EditorUtility.SetDirty(canvas);

        // CanvasScaler → Scale With Screen Size, portrait 1080×1920
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 1f;
            EditorUtility.SetDirty(scaler);
        }

        // DistrictInfoPanel height → 350 canvas-px
        var dipGo = GameObject.Find("DistrictInfoPanel");
        if (dipGo != null)
        {
            var rt = dipGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(0f, 350f);
                EditorUtility.SetDirty(dipGo);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        return $"Canvas={canvas.renderMode} | scaler={scaler?.uiScaleMode} ref={scaler?.referenceResolution} | panel saved";
    }
}
 
