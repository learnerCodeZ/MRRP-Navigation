using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class UIResizer : EditorWindow
{
    private float targetWidth = 240f;
    private float targetHeight = 50f;
    private int targetFontSize = 30;

    [MenuItem("Tools/Resize UI Buttons")]
    static void Open()
    {
        GetWindow<UIResizer>("UI Resizer");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("HoloLens 2 UI Button Resizer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetWidth = EditorGUILayout.FloatField("Button Width", targetWidth);
        targetHeight = EditorGUILayout.FloatField("Button Height", targetHeight);
        targetFontSize = EditorGUILayout.IntField("Font Size", targetFontSize);

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply to All Buttons", GUILayout.Height(30)))
        {
            ApplyToAllButtons();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Recommended for HoloLens 2:\nWidth: 240, Height: 50, Font: 30",
            MessageType.Info);
    }

    void ApplyToAllButtons()
    {
        int count = 0;
        Button[] buttons = FindObjectsOfType<Button>(true);

        foreach (var btn in buttons)
        {
            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                Undo.RecordObject(rt, "Resize Button");
                rt.sizeDelta = new Vector2(targetWidth, targetHeight);
                count++;
            }

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                Undo.RecordObject(tmp, "Resize Font");
                tmp.fontSize = targetFontSize;
            }
        }

        EditorUtility.DisplayDialog("Done",
            $"Resized {count} buttons to {targetWidth}x{targetHeight}, font size {targetFontSize}.",
            "OK");
    }
}
