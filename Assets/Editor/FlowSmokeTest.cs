using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Play-mode smoke test for Phase 9 (scene flow + save robustness).
/// Start with Tools > Fly Life > Flow Smoke Test (or FlowSmokeTest.Start from BridgeScratch).
/// Backs up the real save file, runs scripted scenarios, restores the save,
/// and writes a report to Temp/flow_smoke.txt.
/// </summary>
[InitializeOnLoad]
public static class FlowSmokeTest
{
    const string KeyStep   = "FlowSmoke.Step";
    const string KeyReport = "FlowSmoke.Report";
    const string KeyMode   = "FlowSmoke.Mode";
    public const string ReportPath = "Temp/flow_smoke.txt";

    static double _waitUntil;
    static double _stepStarted;
    static readonly List<string> _errors = new();

    static string SavePath  => Path.Combine(Application.persistentDataPath, "flylife_save.json");
    static string BackupDir => Path.Combine(Application.persistentDataPath, "smoke_backup");

    static FlowSmokeTest()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Tools/Fly Life/Flow Smoke Test")]
    public static void StartFlow() => Begin("flow");

    [MenuItem("Tools/Fly Life/Save Stress Test")]
    public static void StartSave() => Begin("save");

    public static string Begin(string mode)
    {
        if (EditorApplication.isPlaying) return "Already in play mode.";
        if (File.Exists(ReportPath)) File.Delete(ReportPath);
        BackupSave();
        SessionState.SetString(KeyMode, mode);
        SessionState.SetString(KeyReport, $"# Smoke test ({mode}) {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
        SessionState.SetInt(KeyStep, 1);
        SessionState.SetBool("FlowSmoke.SawBattle", false);

        if (mode == "save")
        {
            // Scenario A: corrupt save file must be backed up and replaced with a fresh state
            File.WriteAllText(SavePath, "{garbage");
            Log("Wrote corrupt save file before play.");
        }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity");
        EditorApplication.isPlaying = true;
        return $"Started {mode} smoke test; report will be at {ReportPath}";
    }

    // ---------- plumbing ----------

    static void OnLog(string msg, string stack, LogType type)
    {
        if (SessionState.GetInt(KeyStep, 0) == 0) return;
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            // Expected error from the corrupt-save scenario
            if (msg.Contains("[SaveSystem] Corrupt save")) { Log("Got expected corrupt-save error log."); return; }
            string first = stack?.Split('\n').FirstOrDefault(l => l.Contains("Assets/")) ?? "";
            _errors.Add($"{type}: {msg} {first}".Trim());
        }
    }

    static void Log(string line)
    {
        SessionState.SetString(KeyReport, SessionState.GetString(KeyReport, "") + line + "\n");
    }

    static void Pass(string what) => Log("PASS " + what);
    static void Fail(string what) => Log("FAIL " + what);
    static void Check(bool ok, string what) { if (ok) Pass(what); else Fail(what); }

    static void Next(double delay = 0)
    {
        SessionState.SetInt(KeyStep, SessionState.GetInt(KeyStep, 0) + 1);
        _waitUntil = EditorApplication.timeSinceStartup + delay;
        _stepStarted = EditorApplication.timeSinceStartup;
    }

    static double Elapsed => EditorApplication.timeSinceStartup - _stepStarted;
    static string Active => SceneManager.GetActiveScene().name;

    static void OnPlayModeChanged(PlayModeStateChange s)
    {
        if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetInt(KeyStep, 0) > 0)
        {
            _errors.Clear();
            _stepStarted = EditorApplication.timeSinceStartup;
            _waitUntil = 0;
        }
        if (s == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(KeyStep, 0) < 0)
        {
            SessionState.SetInt(KeyStep, 0);
            RestoreSave();
            Directory.CreateDirectory("Temp");
            File.WriteAllText(ReportPath, SessionState.GetString(KeyReport, ""));
            Debug.Log($"[FlowSmokeTest] Report written to {ReportPath}");
        }
    }

    static void Finish()
    {
        Log(_errors.Count == 0 ? "PASS no errors/exceptions logged" : $"FAIL {_errors.Count} errors/exceptions:");
        foreach (var e in _errors.Distinct().Take(30)) Log("  " + e);
        SessionState.SetInt(KeyStep, -1);
        EditorApplication.isPlaying = false;
    }

    static void BackupSave()
    {
        Directory.CreateDirectory(BackupDir);
        foreach (var f in Directory.GetFiles(BackupDir)) File.Delete(f);
        if (File.Exists(SavePath)) File.Copy(SavePath, Path.Combine(BackupDir, "flylife_save.json"));
    }

    static void RestoreSave()
    {
        // Remove anything the test produced, including .corrupt.* backups
        foreach (var f in Directory.GetFiles(Application.persistentDataPath, "flylife_save.json*"))
            File.Delete(f);
        string b = Path.Combine(BackupDir, "flylife_save.json");
        if (File.Exists(b)) File.Copy(b, SavePath);
    }

    static bool ClickButton(string name)
    {
        var btn = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                        .FirstOrDefault(b => b.name == name && b.interactable);
        if (btn == null) return false;
        btn.onClick.Invoke();
        return true;
    }

    static bool UnderSafeArea(string name) =>
        Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
              .Any(t => t.name == name && t.parent != null && t.parent.name == "SafeArea"
                        && t.parent.GetComponent<SafeAreaHandler>() != null);

    static int CountActive(string name) =>
        Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Count(c => c.name == name);

    // ---------- scenarios ----------

    static void Tick()
    {
        int step = SessionState.GetInt(KeyStep, 0);
        if (step <= 0 || !EditorApplication.isPlaying) return;
        if (EditorApplication.timeSinceStartup < _waitUntil) return;

        if (SessionState.GetString(KeyMode, "flow") == "save") TickSave(step);
        else TickFlow(step);
    }

    static void TickFlow(int step)
    {
        var gm = GameManager.Instance;
        switch (step)
        {
            case 1: // cold start reaches MainMap
                if (Active == "MainMap") { Pass("Bootstrap -> MainMap"); Next(2); }
                else if (Elapsed > 15) { Fail($"Bootstrap -> MainMap (stuck in {Active})"); Finish(); }
                break;
            case 2:
                Check(gm != null, "GameManager present on MainMap");
                Check(UnderSafeArea("HUDBar"), "MainMap HUD bar inside SafeArea");
                Check(UnderSafeArea("ToastContainer"), "Toasts inside SafeArea");
                gm?.LoadScene("ClanScreen");
                Next(1.5);
                break;
            case 3:
                Check(Active == "ClanScreen", "MainMap -> ClanScreen");
                Check(CountActive("ClanCanvas") == 1, $"ClanScreen has exactly one ClanCanvas (found {CountActive("ClanCanvas")})");
                Check(ClickButton("BackButton"), "ClanScreen Back button found and clicked");
                Next(1.5);
                break;
            case 4:
                Check(Active == "MainMap", "ClanScreen -> Back -> MainMap");
                var settings = SettingsScreenController.Instance;
                Check(settings != null, "SettingsScreenController present");
                settings?.Show();
                Next(0.5);
                break;
            case 5:
                Check(ClickButton("BackButton"), "Settings Back button found and clicked");
                Next(0.5);
                break;
            case 6:
                Check(Active == "MainMap", "Settings -> Back stays on MainMap");
                gm?.LoadScene("Battle");
                Next(1);
                break;
            case 7:
                if (Active == "Battle" && !SessionState.GetBool("FlowSmoke.SawBattle", false))
                {
                    SessionState.SetBool("FlowSmoke.SawBattle", true);
                    Pass("MainMap -> Battle");
                    Check(UnderSafeArea("RoundText"), "Battle UI inside SafeArea");
                }
                if (Active == "MainMap" && SessionState.GetBool("FlowSmoke.SawBattle", false))
                {
                    SessionState.SetBool("FlowSmoke.SawBattle", false);
                    Pass($"Battle -> MainMap (auto return after {Elapsed:F1}s)");
                    Next(1.5);
                }
                else if (Elapsed > 90) { Fail($"Battle never returned to MainMap (in {Active})"); Next(); }
                break;
            case 8:
                Check(File.Exists(SavePath), "save file exists after scene changes");
                if (File.Exists(SavePath))
                {
                    long size = new FileInfo(SavePath).Length;
                    Check(size < 100 * 1024, $"save file size {size / 1024f:F1} KB < 100 KB");
                }
                Finish();
                break;
        }
    }

    static void TickSave(int step)
    {
        switch (step)
        {
            case 1:
                if (Active == "MainMap") { Pass("corrupt save: game still reaches MainMap"); Next(1.5); }
                else if (Elapsed > 15) { Fail($"corrupt save: stuck in {Active}"); Finish(); }
                break;
            case 2:
            {
                var backups = Directory.GetFiles(Application.persistentDataPath, "flylife_save.json.corrupt.*");
                Check(backups.Length > 0, $"corrupt save backed up ({backups.Length} backup file)");
                Check(GameManager.Instance != null && ResourceManager.Instance != null, "managers alive after corrupt load");
                SaveSystem.Save();
                string json = File.Exists(SavePath) ? File.ReadAllText(SavePath) : "";
                Check(json.StartsWith("{") && json.Contains("SaveVersion"), "fresh valid save written after recovery");

                // Scenario B: no save at all (fresh install) -> reload via Load()
                File.Delete(SavePath);
                SaveSystem.Load();
                Pass("Load() with no save file did not throw");

                // Scenario C: empty file
                File.WriteAllText(SavePath, "");
                SaveSystem.Load();
                Pass("Load() with empty save file did not throw");

                // Scenario D: round trip — gems persist
                int before = ResourceManager.Instance?.Gems ?? -1;
                ResourceManager.Instance?.AddGems(7);
                SaveSystem.Save();
                ResourceManager.Instance?.SpendGems(7);
                SaveSystem.Load();
                int after = ResourceManager.Instance?.Gems ?? -2;
                Check(after == before + 7, $"Gems persist through save/load ({before}+7 -> {after})");
                Finish();
                break;
            }
        }
    }
}
