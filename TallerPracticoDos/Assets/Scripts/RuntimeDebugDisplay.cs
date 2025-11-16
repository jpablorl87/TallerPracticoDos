using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Muestra en pantalla los mensajes de Debug.Log, Debug.Warning y Debug.Error
/// útil para builds móviles sin acceso al log remoto.
/// </summary>
public class RuntimeDebugDisplay : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private int maxMessages = 15;
    [SerializeField] private float messageDuration = 10f;
    [SerializeField] private float fontSize = 18f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.6f);

    private readonly Queue<(string message, float time)> messages = new();
    private string displayText = "";
    private GUIStyle style;
    private Texture2D backgroundTex;

    private void Awake()
    {
        Application.logMessageReceived += HandleLog;

        backgroundTex = new Texture2D(1, 1);
        backgroundTex.SetPixel(0, 0, backgroundColor);
        backgroundTex.Apply();

        style = new GUIStyle
        {
            fontSize = (int)fontSize,
            normal = new GUIStyleState { textColor = textColor, background = backgroundTex }
        };
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        string prefix = type switch
        {
            LogType.Warning => "<color=yellow>[W]</color> ",
            LogType.Error => "<color=red>[E]</color> ",
            _ => ""
        };

        messages.Enqueue(($"{prefix}{logString}", Time.time));

        if (messages.Count > maxMessages)
            messages.Dequeue();

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        System.Text.StringBuilder sb = new();
        foreach (var msg in messages)
            sb.AppendLine(msg.message);
        displayText = sb.ToString();
    }

    private void Update()
    {
        bool changed = false;
        while (messages.Count > 0 && Time.time - messages.Peek().time > messageDuration)
        {
            messages.Dequeue();
            changed = true;
        }

        if (changed)
            RefreshDisplay();
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(displayText)) return;

        float width = Screen.width * 0.9f;
        float height = Screen.height * 0.4f;
        GUI.Label(new Rect(10, 10, width, height), displayText, style);
    }
}
