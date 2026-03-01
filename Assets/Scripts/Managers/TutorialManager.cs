using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Guided tutorial for first-time players. Runs once, then skipped via SaveSystem.TutorialComplete.
/// Builds its own UI at runtime.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // Runtime UI
    Canvas _canvas;
    GameObject _overlay;
    TextMeshProUGUI _dialogueText;
    GameObject _tapPrompt;
    bool _waitingForTap;
    bool _active;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        // The GameManager.Start() coroutine calls StartTutorial after Load.
        // This just ensures singleton availability.
    }

    public bool IsActive => _active;

    public void StartTutorial()
    {
        if (_active) return;
        _active = true;
        BuildUI();
        StartCoroutine(TutorialSequence());
    }

    void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("TutorialCanvas");
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGo);

        // Semi-transparent overlay
        _overlay = new GameObject("Overlay");
        _overlay.transform.SetParent(canvasGo.transform, false);
        var overlayRect = _overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImg = _overlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        overlayImg.raycastTarget = true;

        // Dialogue box at bottom
        var dialogueBox = new GameObject("DialogueBox");
        dialogueBox.transform.SetParent(_overlay.transform, false);
        var dbRect = dialogueBox.AddComponent<RectTransform>();
        dbRect.anchorMin = new Vector2(0.05f, 0.05f);
        dbRect.anchorMax = new Vector2(0.95f, 0.3f);
        dbRect.offsetMin = Vector2.zero;
        dbRect.offsetMax = Vector2.zero;
        var dbImg = dialogueBox.AddComponent<Image>();
        dbImg.color = new Color(0.15f, 0.1f, 0.05f, 0.95f);

        // Dialogue text
        var textGo = new GameObject("DialogueText");
        textGo.transform.SetParent(dialogueBox.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.15f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        _dialogueText = textGo.AddComponent<TextMeshProUGUI>();
        _dialogueText.fontSize = 22;
        _dialogueText.color = Color.white;
        _dialogueText.alignment = TextAlignmentOptions.TopLeft;

        // Tap to continue prompt
        var tapGo = new GameObject("TapPrompt");
        tapGo.transform.SetParent(dialogueBox.transform, false);
        var tapRect = tapGo.AddComponent<RectTransform>();
        tapRect.anchorMin = new Vector2(0.5f, 0.02f);
        tapRect.anchorMax = new Vector2(0.95f, 0.15f);
        tapRect.offsetMin = Vector2.zero;
        tapRect.offsetMax = Vector2.zero;
        _tapPrompt = tapGo;
        var tapText = tapGo.AddComponent<TextMeshProUGUI>();
        tapText.text = "[ Tap to continue ]";
        tapText.fontSize = 16;
        tapText.color = new Color(1f, 0.9f, 0.5f, 0.8f);
        tapText.alignment = TextAlignmentOptions.BottomRight;

        _overlay.SetActive(false);
    }

    IEnumerator TutorialSequence()
    {
        // Wait for MainMap to load
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "MainMap");
        yield return new WaitForSeconds(0.5f); // let map tiles spawn

        // Step 1: Welcome
        yield return ShowDialogue("Welcome, Commander! Your fly clan needs territory.\nLet's take your first district.");

        // Step 2: Point at center tile
        yield return ShowDialogue("Tap the center district to inspect it.\n(It's the easiest one!)");

        // Wait for player to tap center district
        _overlay.SetActive(false);
        yield return new WaitUntil(() => DistrictInfoPanel.ActiveDistrict?.Id == "district_2_2");
        yield return new WaitForSeconds(0.3f);

        // Step 3: Attack
        yield return ShowDialogue("Now tap 'Attack' or 'Claim' to fight for this district!");

        // Wait for battle scene
        _overlay.SetActive(false);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Battle");

        // Step 4: Wait for battle to end and return to map
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "MainMap");
        yield return new WaitForSeconds(0.5f);

        // Step 5: Congrats
        yield return ShowDialogue("Victory! This district is now yours.\nIt generates PooCoins every minute.\n\nKeep conquering the city!");

        // Done
        _active = false;
        _overlay.SetActive(false);
        SaveSystem.MarkTutorialComplete();
        AnalyticsManager.Instance?.TrackTutorialComplete();
        Debug.Log("[TutorialManager] Tutorial complete!");

        // Clean up
        if (_canvas != null) Destroy(_canvas.gameObject);
    }

    IEnumerator ShowDialogue(string text)
    {
        _overlay.SetActive(true);
        _dialogueText.text = text;
        _tapPrompt.SetActive(true);
        _waitingForTap = true;
        yield return new WaitUntil(() => !_waitingForTap);
        _tapPrompt.SetActive(false);
    }

    void Update()
    {
        if (_waitingForTap && (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
            _waitingForTap = false;
    }
}
