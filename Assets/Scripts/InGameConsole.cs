using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InGameConsole : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI logText;

    [Header("Settings")]
    [Tooltip("Maximum number of log lines to show on screen at once.")]
    [SerializeField] private int maxLogLines = 8;

    private Queue<string> logQueue = new Queue<string>();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Color-code warnings and errors for easy reading
        string formattedLog = logString;
        if (type == LogType.Error || type == LogType.Exception)
        {
            formattedLog = $"<color=red>{logString}</color>";
        }
        else if (type == LogType.Warning)
        {
            formattedLog = $"<color=yellow>{logString}</color>";
        }

        logQueue.Enqueue(formattedLog);

        // Keep only the most recent lines so the text doesn't overflow
        while (logQueue.Count > maxLogLines)
        {
            logQueue.Dequeue();
        }

        if (logText != null)
        {
            logText.text = string.Join("\n", logQueue.ToArray());
        }
    }
}