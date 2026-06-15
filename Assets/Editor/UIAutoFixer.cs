using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class UIAutoFixer : EditorWindow
{
    static readonly Dictionary<string, string> ButtonLabels = new Dictionary<string, string>
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

    [MenuItem("Tools/Fix UI Components")]
    static void FixUI()
    {
        int fixes = 0;

        // 1. Add StandaloneInputModule for mouse clicks in Play Mode
        // MRTK's MixedRealityInputModule handles hand tracking, but mouse clicks need this
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            if (mainCamera.GetComponent<StandaloneInputModule>() == null)
            {
                mainCamera.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[UIAutoFixer] Added StandaloneInputModule to Main Camera for Play Mode mouse clicks");
                fixes++;
            }
        }
        else
        {
            Debug.LogWarning("[UIAutoFixer] No Main Camera found! Cannot add StandaloneInputModule.");
        }

        // 2. Add GraphicRaycaster to all Canvases
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
        {
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"[UIAutoFixer] Added GraphicRaycaster to: {canvas.gameObject.name}");
                fixes++;
            }
        }

        // 3. Add Image to all Buttons that have none
        foreach (var btn in Object.FindObjectsOfType<Button>())
        {
            if (btn.GetComponent<Image>() == null)
            {
                var img = btn.gameObject.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                img.raycastTarget = true;
                btn.targetGraphic = img;
                Debug.Log($"[UIAutoFixer] Added Image to: {btn.gameObject.name}");
                fixes++;
            }
        }

        // 4. Fix TMP fonts
        TMP_FontAsset fontAsset = null;
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fontAsset != null) break;
        }

        if (fontAsset != null)
        {
            foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>())
            {
                if (tmp.font == null)
                {
                    tmp.font = fontAsset;
                    EditorUtility.SetDirty(tmp);
                    fixes++;
                }
            }
        }
        else
        {
            Debug.LogWarning("[UIAutoFixer] No TMP_FontAsset found! Import TMP Essential Resources first.");
        }

        // 5. Fix button labels based on GameObject names
        foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>())
        {
            string goName = tmp.gameObject.name;
            if (ButtonLabels.ContainsKey(goName))
            {
                string desiredText = ButtonLabels[goName];
                if (tmp.text != desiredText)
                {
                    tmp.text = desiredText;
                    EditorUtility.SetDirty(tmp);
                    Debug.Log($"[UIAutoFixer] Set text '{desiredText}' on {goName}");
                    fixes++;
                }
            }
        }

        Debug.Log($"[UIAutoFixer] Done! {fixes} fixes applied.");
        EditorUtility.DisplayDialog("UI Auto Fixer", $"{fixes} fixes applied.\n\nCheck Console for details.", "OK");
    }
}
