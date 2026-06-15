using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
public class PlayModeUIFixer : MonoBehaviour
{
    EventSystem cachedEventSystem;

    void Awake()
    {
        EnsureEventSystem();
        EnsureGraphicRaycasters();
        EnsureButtonImages();
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
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"[PlayModeUIFixer] Added GraphicRaycaster to: {canvas.gameObject.name}");
            }
        }
    }

    void EnsureButtonImages()
    {
        foreach (var btn in FindObjectsOfType<Button>())
        {
            if (btn.GetComponent<Image>() == null)
            {
                var img = btn.gameObject.AddComponent<Image>();
                img.color = new Color(0.25f, 0.25f, 0.25f, 0.9f);
                img.raycastTarget = true;
                btn.targetGraphic = img;
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
