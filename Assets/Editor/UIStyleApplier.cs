using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public class UIStyleApplier : EditorWindow
{
    // === Color Palette (from Design Guidelines) ===
    static readonly Color PanelBg = new Color(0.102f, 0.145f, 0.361f, 0.70f);   // #1A255C alpha 70%
    static readonly Color ButtonNormal = new Color(0.173f, 0.227f, 0.541f, 0.85f); // #2C3A8A alpha 85%
    static readonly Color ButtonHighlighted = new Color(0.239f, 0.310f, 0.769f, 0.90f); // #3D4FC4
    static readonly Color ButtonPressed = new Color(0.353f, 0.435f, 0.910f, 1.0f);     // #5A6FE8
    static readonly Color TextWhite = Color.white;
    static readonly Color PathColor = new Color(0.298f, 0.933f, 0.918f, 0.9f);  // #4DEEEA
    static readonly Color SphereColor = new Color(0.298f, 0.933f, 0.918f, 0.6f); // #4DEEEA alpha 60%

    [MenuItem("Tools/Apply UI Style (Design Guidelines)")]
    static void ApplyStyle()
    {
        int fixes = 0;

        // 1. Create materials
        var panelMat = CreateMaterial("MAT_PanelBackground", PanelBg, true);
        var btnMat = CreateMaterial("MAT_ButtonNormal", ButtonNormal, true);
        var pathMat = CreateMaterial("MAT_PathLine", PathColor, true);
        var sphereMat = CreateMaterial("MAT_PathSphere", SphereColor, true);

        // 2. Apply panel backgrounds to all Canvases
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
        {
            ApplyPanelBackground(canvas.gameObject, panelMat);
            fixes++;
        }

        // 3. Style all buttons
        foreach (var btn in Object.FindObjectsOfType<Button>())
        {
            ApplyButtonStyle(btn, btnMat);
            fixes++;
        }

        // 4. Reposition buttons in each Canvas
        RepositionButtons();
        fixes++;

        // 5. Apply status text styling
        ApplyStatusTextStyle();
        fixes++;

        Debug.Log($"[UIStyleApplied] Done! {fixes} style blocks applied.");
        EditorUtility.DisplayDialog("UI Style Applied",
            "All styles applied.\n\n" +
            "- Panel backgrounds: deep blue glassmorphism\n" +
            "- Buttons: blue with hover/press effects\n" +
            "- Layout: 2x2 grid for PreferredPathMenu\n" +
            "- Colors: #4DEEEA path, #FFFFFF text",
            "OK");
    }

    [MenuItem("Tools/Apply Path Renderer Style")]
    static void ApplyPathStyle()
    {
        var pathRenderers = Object.FindObjectsOfType<MRReP.Path.PathRenderer>();
        foreach (var pr in pathRenderers)
        {
            var field = typeof(MRReP.Path.PathRenderer).GetField("pathColor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(pr, PathColor);

            var matField = typeof(MRReP.Path.PathRenderer).GetField("pathMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (matField != null && matField.GetValue(pr) == null)
            {
                var pathMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_PathLine.mat");
                if (pathMat != null)
                    matField.SetValue(pr, pathMat);
            }

            EditorUtility.SetDirty(pr);
        }

        Debug.Log($"[UIStyleApplied] Path renderer style applied to {pathRenderers.Length} objects.");
        EditorUtility.DisplayDialog("Path Style Applied",
            $"Updated {pathRenderers.Length} PathRenderer(s) with color #4DEEEA.", "OK");
    }

    // === Panel Background ===
    static void ApplyPanelBackground(GameObject go, Material mat)
    {
        var img = go.GetComponent<Image>();
        if (img == null)
            img = go.AddComponent<Image>();

        img.material = mat;
        img.color = Color.white; // let material control color
        img.type = Image.Type.Sliced;
        img.raycastTarget = false; // panels don't need raycast
    }

    // === Button Style ===
    static void ApplyButtonStyle(Button btn, Material mat)
    {
        var img = btn.GetComponent<Image>();
        if (img == null)
        {
            img = btn.gameObject.AddComponent<Image>();
            btn.targetGraphic = img;
        }

        img.material = mat;
        img.color = Color.white;
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

        // Ensure text is white and bold
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.color = TextWhite;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }

    // === Button Repositioning ===
    static void RepositionButtons()
    {
        // PreferredPathMenu: 2x2 grid
        var preferredPathButtons = new Dictionary<string, Vector2>
        {
            { "ADD", new Vector2(-80, 40) },
            { "CLEAR", new Vector2(80, 40) },
            { "SEND", new Vector2(-80, -40) },
            { "BACK", new Vector2(80, -40) },
        };

        foreach (var btn in Object.FindObjectsOfType<Button>())
        {
            string name = btn.gameObject.name;

            if (preferredPathButtons.TryGetValue(name, out Vector2 pos))
            {
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = pos;
                    rt.sizeDelta = new Vector2(140, 50);
                }
            }
            else if (name == "Preferred Path")
            {
                // MainMenu: centered
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(200, 60);
                }
            }
            else if (name == "Yes")
            {
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(60, -20);
                    rt.sizeDelta = new Vector2(120, 45);
                }
            }
            else if (name == "No")
            {
                var rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-60, -20);
                    rt.sizeDelta = new Vector2(120, 45);
                }
            }
        }

        // StatusText: top center
        foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>())
        {
            if (tmp.gameObject.name == "StatusText")
            {
                var rt = tmp.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, 80);
                    rt.sizeDelta = new Vector2(300, 60);
                }
                tmp.color = TextWhite;
                tmp.fontStyle = FontStyles.Bold;
                tmp.fontSize = 18;
                tmp.alignment = TextAlignmentOptions.Center;
            }
            else if (tmp.gameObject.name == "DialogMessage")
            {
                var rt = tmp.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, 60);
                    rt.sizeDelta = new Vector2(350, 80);
                }
                tmp.color = TextWhite;
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    // === Status Text Styling ===
    static void ApplyStatusTextStyle()
    {
        foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>())
        {
            // All text should be white
            if (tmp.color != TextWhite)
            {
                tmp.color = TextWhite;
                EditorUtility.SetDirty(tmp);
            }
        }
    }

    // === Helper: Create Material ===
    static Material CreateMaterial(string name, Color color, bool useEmission = false)
    {
        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string matPath = $"{matDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null)
        {
            existing.color = color;
            if (useEmission)
            {
                existing.EnableKeyword("_EMISSION");
                existing.SetColor("_EmissionColor", color * 0.3f);
            }
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var mat = new Material(Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;

        if (useEmission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.3f);
        }

        // Set rendering mode for transparency
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }
}
