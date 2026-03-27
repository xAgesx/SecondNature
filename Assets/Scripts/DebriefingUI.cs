using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebriefingUI : MonoBehaviour
{
    [Header("Panel root")]
    public GameObject debriefPanel;

    [Header("Stat fields")]
    public TextMeshProUGUI completionTimeText;
    public TextMeshProUGUI errorsText;
    public TextMeshProUGUI finalTempText;
    public TextMeshProUGUI objectivesText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI feedbackText;

    [Header("Performance bar")]
    public Slider performanceBar;
    public Image barFillImage;
    public Gradient barGradient;

    [Header("Score thresholds (0–100)")]
    public int scoreForS = 90;
    public int scoreForA = 75;
    public int scoreForB = 60;
    public int scoreForC = 45;

    [Header("Scoring penalties")]
    public float targetTimeSeconds = 180f;
    public float timePenaltyPerSecond = 0.05f;
    public float errorPenalty = 8f;

    [Header("Buttons")]
    public Button replayButton;
    public Button mainMenuButton;

    [Header("Scene names")]
    [Tooltip("Exact name of your main menu scene in Build Settings.")]
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        debriefPanel.SetActive(false);
        TemperatureSystem.Instance.onGameOver.AddListener(ShowDebriefing);

        replayButton?.onClick.AddListener(OnReplay);
        mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    public void ShowDebriefing()
    {
        ScoreTracker.Instance.StopTracking();
        debriefPanel.SetActive(true);

        var tracker = ScoreTracker.Instance;
        float elapsed = tracker.ElapsedTime;
        int errors = tracker.ErrorCount;
        float finalTemp = TemperatureSystem.Instance.CurrentTemp;
        int objDone = tracker.CompletedObjectives;
        int objTotal = tracker.totalObjectives;

        completionTimeText.text = "Completion Time:  " + FormatTime(elapsed);
        errorsText.text = "Errors:  " + errors;
        finalTempText.text = "Final Temp:  " + finalTemp.ToString("F1") + " °C";
        objectivesText.text = "Objectives:  " + objDone + " / " + objTotal;

        int score = CalculateScore(elapsed, errors, objDone, objTotal);
        gradeText.text = GetGrade(score);

        float normalized = score / 100f;
        performanceBar.value = normalized;
        if (barFillImage != null)
            barFillImage.color = barGradient.Evaluate(normalized);

        feedbackText.text = BuildFeedback(elapsed, errors, objDone, objTotal);
    }

    private int CalculateScore(float time, int errors, int objDone, int objTotal)
    {
        float score = 100f;
        float overtime = Mathf.Max(0f, time - targetTimeSeconds);
        score -= overtime * timePenaltyPerSecond;
        score -= errors * errorPenalty;
        float objRatio = objTotal > 0 ? (float)objDone / objTotal : 1f;
        score *= objRatio;
        return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
    }

    private string GetGrade(int score)
    {
        if (score >= scoreForS) return "S";
        if (score >= scoreForA) return "A";
        if (score >= scoreForB) return "B";
        if (score >= scoreForC) return "C";
        return "D";
    }

    private string BuildFeedback(float time, int errors, int objDone, int objTotal)
    {
        var lines = new List<string>();
        if (time > targetTimeSeconds) lines.Add("⏱  Completion time was a bit long.");
        if (errors > 3) lines.Add("⚠  Several errors to correct!");
        if (objDone < objTotal) lines.Add("✗  Not all objectives completed.");
        if (lines.Count == 0) lines.Add("✓  Perfect run!");
        return string.Join("\n", lines);
    }

    private void OnReplay()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void OnMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return m + ":" + s.ToString("D2");
    }
}