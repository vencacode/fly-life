using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a brief "LEVEL UP!" popup when a fly levels up.
/// Builds its own UI at runtime — no prefab dependencies.
/// Subscribes to UIEventBus.LevelUp.
/// </summary>
public class LevelUpPopup : MonoBehaviour
{
    Canvas _canvas;
    CanvasGroup _group;
    TextMeshProUGUI _label;
    GameObject _panel;

    void Awake()
    {
        BuildUI();
        _panel.SetActive(false);
        UIEventBus.On(UIEvent.LevelUp, OnLevelUp);
    }

    void OnDestroy() => UIEventBus.Off(UIEvent.LevelUp, OnLevelUp);

    void BuildUI()
    {
        var canvasGo = new GameObject("LevelUpCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100; // on top of everything
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        _panel = new GameObject("LevelUpPanel");
        _panel.transform.SetParent(canvasGo.transform, false);
        var img = _panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        _group = _panel.AddComponent<CanvasGroup>();

        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.4f);
        panelRect.anchorMax = new Vector2(0.85f, 0.6f);
        panelRect.sizeDelta = Vector2.zero;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(_panel.transform, false);
        textGo.AddComponent<RectTransform>();
        _label = textGo.AddComponent<TextMeshProUGUI>();
        _label.fontSize = 24;
        _label.color = new Color(1f, 0.85f, 0.2f);
        _label.alignment = TextAlignmentOptions.Center;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
    }

    void OnLevelUp(object data)
    {
        if (data is not FlyInstance fly) return;
        _label.text = $"LEVEL UP!\n{fly.Role} -> Lv {fly.Level}";
        StopAllCoroutines();
        StartCoroutine(ShowPopup());
    }

    IEnumerator ShowPopup()
    {
        _panel.SetActive(true);
        _group.alpha = 1f;

        // Hold for 1 second
        yield return new WaitForSeconds(1f);

        // Fade out over 0.5 seconds
        float t = 0.5f;
        while (t > 0)
        {
            t -= Time.deltaTime;
            _group.alpha = t / 0.5f;
            yield return null;
        }

        _panel.SetActive(false);
    }
}
