using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TEMP: Auto-creates Canvas + DistrictInfoPanel UI at runtime if not
/// already present in the scene.  Wires all TMP and Button references.
/// Attach to any GameObject in MainMap scene, or let MapTestHelper spawn it.
/// Remove before shipping.
/// </summary>
public class MapSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MapSceneBootstrap] No GameManager — redirecting to Bootstrap.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(Constants.SCENE_BOOTSTRAP);
            return;
        }
        if (MapManager.Instance == null) return;
        if (MapManager.Instance.InfoPanel != null) return; // already wired

        var panel = BuildDistrictInfoPanel();
        MapManager.Instance.InfoPanel = panel;

        BuildClanButton(panel.transform.parent); // add to same canvas
        Debug.Log("[MapSceneBootstrap] DistrictInfoPanel + ClanButton created and wired.");
    }

    void BuildClanButton(Transform canvas)
    {
        // Top-right "Clan" button on the main UI canvas — navigates to ClanScreen
        var btnGo = new GameObject("ClanBtn");
        btnGo.transform.SetParent(canvas, false);
        var btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.75f, 0.93f);
        btnRect.anchorMax = new Vector2(0.97f, 0.98f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.5f, 0.3f, 0.7f);
        var btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(() => GameManager.Instance?.LoadScene(Constants.SCENE_CLAN));

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Clan";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    DistrictInfoPanel BuildDistrictInfoPanel()
    {
        // Canvas
        var canvasGo = new GameObject("UICanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel background — anchored to bottom
        var panelGo = new GameObject("DistrictInfoPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.sizeDelta = new Vector2(0, 320);
        var panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        // Vertical layout
        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 16, 16);
        vlg.spacing = 8;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        // Text elements
        var nameText  = CreateTMP(panelGo.transform, "DistrictName", 28, FontStyles.Bold);
        var ownerText = CreateTMP(panelGo.transform, "OwnerText", 22, FontStyles.Normal);
        var pooText   = CreateTMP(panelGo.transform, "PooCountText", 20, FontStyles.Normal);
        var bonusText = CreateTMP(panelGo.transform, "BonusText", 18, FontStyles.Italic);

        // Button row
        var btnRow = new GameObject("Buttons");
        btnRow.transform.SetParent(panelGo.transform, false);
        var btnRect = btnRow.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(0, 50);
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;

        var sendBtn  = CreateButton(btnRow.transform, "SendFlies", "Send Flies",
            new Color(0.2f, 0.6f, 0.9f));
        var attackBtn = CreateButton(btnRow.transform, "Attack", "Attack",
            new Color(0.9f, 0.3f, 0.2f));
        var claimBtn = CreateButton(btnRow.transform, "Claim", "Claim",
            new Color(0.3f, 0.8f, 0.4f));
        var spotPooBtn = CreateButton(btnRow.transform, "SpotPoo", "Spot Poo",
            new Color(0.6f, 0.4f, 0.1f));

        // FlyDeployPanel — lives on its own GameObject, builds its own canvas
        var deployGo = new GameObject("FlyDeployPanel");
        var deployPanel = deployGo.AddComponent<FlyDeployPanel>();

        // Wire up DistrictInfoPanel component
        var panel = panelGo.AddComponent<DistrictInfoPanel>();
        panel.DistrictNameText = nameText;
        panel.OwnerText        = ownerText;
        panel.PooCountText     = pooText;
        panel.BonusText        = bonusText;
        panel.SendFliesButton  = sendBtn;
        panel.AttackButton     = attackBtn;
        panel.ClaimButton      = claimBtn;
        panel.SpotPooButton    = spotPooBtn;
        panel.DeployPanel      = deployPanel;

        return panel;
    }

    TextMeshProUGUI CreateTMP(Transform parent, string name, int fontSize, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, fontSize + 12);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.text = name;
        return tmp;
    }

    Button CreateButton(Transform parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 50);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        var txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }
}
