using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class PlayModeUIFixer : MonoBehaviour
{
    // === Design Guideline Colors ===
    static readonly Color PanelBg = new Color(0.102f, 0.145f, 0.361f, 0.70f);
    static readonly Color ButtonNormal = new Color(0.173f, 0.227f, 0.541f, 0.85f);
    static readonly Color ButtonHighlighted = new Color(0.239f, 0.310f, 0.769f, 0.90f);
    static readonly Color ButtonPressed = new Color(0.353f, 0.435f, 0.910f, 1.0f);

    EventSystem cachedEventSystem;

    void Awake()
    {
        EnsureEventSystem();
        EnsureGraphicRaycasters();
        EnsurePanelBackgrounds();
        EnsureButtonStyles();
        EnsureButtonLabels();
    }

    void EnsureEventSystem()
    {
        cachedEventSystem = FindObjectOfType<EventSystem>();
        if (cachedEventSystem == null)
        {
            var go = new GameObject("PlayModeEventSystem");
            cachedEventSystem = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            Debug.Log("[PlayModeUIFixer] Created new EventSystem + StandaloneInputModule");
        }
    }

    void EnsureGraphicRaycasters()
    {
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    void EnsurePanelBackgrounds()
    {
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            var img = canvas.gameObject.GetComponent<Image>();
            if (img == null)
                img = canvas.gameObject.AddComponent<Image>();

            img.color = PanelBg;
            img.raycastTarget = false;
        }
    }

    void EnsureButtonStyles()
    {
        foreach (var btn in Object.FindObjectsOfType<Button>())
        {
            var img = btn.GetComponent<Image>();
            if (img == null)
            {
                img = btn.gameObject.AddComponent<Image>();
                btn.targetGraphic = img;
            }

            img.color = ButtonNormal;
            img.raycastTarget = true;

            // ColorBlock for hover/press feedback
            var colors = btn.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHighlighted;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonHighlighted;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            // Ensure text is white
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = Color.white;
                tmp.fontStyle = FontStyles.Bold;
            }
        }
    }

    void EnsureButtonLabels()
    {
        var labels = new Dictionary<string, string>
        {
            { "Preferred Path", "PreferredPath" },
            { "ADD", "ADD" },
            { "CLEAR", "CLEAR" },
            { "SEND", "SEND" },
            { "BACK", "Back" },
            { "Yes", "Yes" },
            { "No", "No" },
            { "DialogMessage", "Do you want to\nclear the path?" },
        };

        foreach (var tmp in FindObjectsOfType<TextMeshProUGUI>())
        {
            if (labels.TryGetValue(tmp.gameObject.name, out string desiredText))
            {
                if (tmp.text != desiredText)
                    tmp.text = desiredText;
                tmp.color = Color.white;
            }
        }
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (cachedEventSystem == null) return;

        var pointerData = new PointerEventData(cachedEventSystem)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        cachedEventSystem.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            var button = result.gameObject.GetComponentInParent<Button>();
            if (button != null)
            {
                button.onClick.Invoke();
                Debug.Log($"[PlayModeUIFixer] Clicked: {button.gameObject.name}");
                break;
            }
        }
    }
}
