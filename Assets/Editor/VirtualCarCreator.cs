using UnityEngine;
using UnityEditor;
using MRReP.Robot;

public class VirtualCarCreator : EditorWindow
{
    private float modelScale = 1f;

    [MenuItem("Tools/Create VirtualCar Prefab")]
    static void ShowWindow()
    {
        var window = GetWindow<VirtualCarCreator>("VirtualCar Creator");
        window.minSize = new Vector2(350, 200);
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("VirtualCar Prefab Creator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        modelScale = EditorGUILayout.FloatField("Model Scale", modelScale);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Prefab", GUILayout.Height(30)))
        {
            CreateVirtualCar(modelScale);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Click Create Prefab to generate a realistic car model.\n" +
            "Drag prefab into scene and wire up OdomSubscriber.virtualCar.",
            MessageType.Info);
    }

    static void CreateVirtualCar(float scale)
    {
        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var bodyMat = CreateMaterial("CarBody", new Color(0.85f, 0.12f, 0.12f), true);
        var roofMat = CreateMaterial("CarRoof", new Color(0.75f, 0.10f, 0.10f), true);
        var windowMat = CreateMaterial("CarWindow", new Color(0.4f, 0.6f, 0.85f, 0.5f), false, true);
        var wheelMat = CreateMaterial("CarWheel", new Color(0.15f, 0.15f, 0.15f), false);
        var lightMat = CreateMaterial("CarLight", new Color(1f, 1f, 0.8f), true);
        var tailLightMat = CreateMaterial("CarTailLight", new Color(1f, 0.1f, 0.1f), true);
        var bumperMat = CreateMaterial("CarBumper", new Color(0.3f, 0.3f, 0.3f), false);
        var grillMat = CreateMaterial("CarGrill", new Color(0.2f, 0.2f, 0.2f), false);

        var root = new GameObject("VirtualCar");
        root.AddComponent<VirtualCarController>();

        var carModel = new GameObject("CarModel");
        carModel.transform.SetParent(root.transform);
        carModel.transform.localPosition = Vector3.zero;
        carModel.transform.localScale = Vector3.one * scale;

        Transform parent = carModel.transform;

        // === Body Lower ===
        var bodyLower = CreateBox("BodyLower", parent,
            new Vector3(0, 0.04f, 0),
            new Vector3(0.22f, 0.06f, 0.38f),
            bodyMat);

        // === Body Upper (Cabin) ===
        var bodyUpper = CreateBox("BodyUpper", parent,
            new Vector3(0, 0.095f, -0.03f),
            new Vector3(0.20f, 0.05f, 0.22f),
            roofMat);

        // === Hood ===
        var hood = CreateBox("Hood", parent,
            new Vector3(0, 0.075f, 0.14f),
            new Vector3(0.20f, 0.02f, 0.10f),
            bodyMat);

        // === Trunk ===
        var trunk = CreateBox("Trunk", parent,
            new Vector3(0, 0.075f, -0.16f),
            new Vector3(0.20f, 0.02f, 0.06f),
            bodyMat);

        // === Front Windshield ===
        CreateWindshield("FrontWindshield", parent,
            new Vector3(0, 0.095f, 0.07f),
            new Vector3(0.19f, 0.05f, 0.03f),
            windowMat, -15f);

        // === Rear Windshield ===
        CreateWindshield("RearWindshield", parent,
            new Vector3(0, 0.095f, -0.12f),
            new Vector3(0.19f, 0.05f, 0.03f),
            windowMat, 20f);

        // === Side Windows Left ===
        CreateBox("WindowLeft", parent,
            new Vector3(-0.101f, 0.095f, -0.03f),
            new Vector3(0.005f, 0.04f, 0.18f),
            windowMat);

        // === Side Windows Right ===
        CreateBox("WindowRight", parent,
            new Vector3(0.101f, 0.095f, -0.03f),
            new Vector3(0.005f, 0.04f, 0.18f),
            windowMat);

        // === Headlights ===
        CreateBox("HeadlightL", parent,
            new Vector3(-0.08f, 0.055f, 0.19f),
            new Vector3(0.04f, 0.025f, 0.005f),
            lightMat);
        CreateBox("HeadlightR", parent,
            new Vector3(0.08f, 0.055f, 0.19f),
            new Vector3(0.04f, 0.025f, 0.005f),
            lightMat);

        // === Tail Lights ===
        CreateBox("TailLightL", parent,
            new Vector3(-0.08f, 0.055f, -0.19f),
            new Vector3(0.04f, 0.02f, 0.005f),
            tailLightMat);
        CreateBox("TailLightR", parent,
            new Vector3(0.08f, 0.055f, -0.19f),
            new Vector3(0.04f, 0.02f, 0.005f),
            tailLightMat);

        // === Front Bumper ===
        CreateBox("FrontBumper", parent,
            new Vector3(0, 0.025f, 0.195f),
            new Vector3(0.24f, 0.03f, 0.01f),
            bumperMat);

        // === Rear Bumper ===
        CreateBox("RearBumper", parent,
            new Vector3(0, 0.025f, -0.195f),
            new Vector3(0.24f, 0.03f, 0.01f),
            bumperMat);

        // === Front Grill ===
        CreateBox("Grill", parent,
            new Vector3(0, 0.055f, 0.195f),
            new Vector3(0.10f, 0.03f, 0.005f),
            grillMat);

        // === Side Mirrors ===
        CreateBox("MirrorL", parent,
            new Vector3(-0.12f, 0.08f, 0.05f),
            new Vector3(0.02f, 0.015f, 0.025f),
            bodyMat);
        CreateBox("MirrorR", parent,
            new Vector3(0.12f, 0.08f, 0.05f),
            new Vector3(0.02f, 0.015f, 0.025f),
            bodyMat);

        // === Wheels ===
        CreateWheel("FrontLeftWheel", parent, new Vector3(-0.12f, 0.02f, 0.12f), wheelMat);
        CreateWheel("FrontRightWheel", parent, new Vector3(0.12f, 0.02f, 0.12f), wheelMat);
        CreateWheel("RearLeftWheel", parent, new Vector3(-0.12f, 0.02f, -0.12f), wheelMat);
        CreateWheel("RearRightWheel", parent, new Vector3(0.12f, 0.02f, -0.12f), wheelMat);

        // Wire up VirtualCarController
        var controller = root.GetComponent<VirtualCarController>();
        var carModelField = typeof(VirtualCarController).GetField("carModel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (carModelField != null)
            carModelField.SetValue(controller, carModel.transform);

        string prefabPath = $"{prefabDir}/VirtualCar.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VirtualCarCreator] Created realistic car prefab at {prefabPath}");
        EditorUtility.DisplayDialog("VirtualCar Created",
            $"Prefab saved to:\n{prefabPath}\n\nDrag it into your scene and wire up OdomSubscriber.virtualCar.",
            "OK");
    }

    static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = size;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }

    static void CreateWindshield(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, float tiltAngle)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = size;
        obj.transform.localRotation = Quaternion.Euler(tiltAngle, 0, 0);
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
    }

    static void CreateWheel(string name, Transform parent, Vector3 localPos, Material mat)
    {
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent);
        wheel.transform.localPosition = localPos;
        wheel.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        wheel.transform.localScale = new Vector3(0.04f, 0.02f, 0.04f);
        wheel.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(wheel.GetComponent<Collider>());

        var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hub.name = name + "Hub";
        hub.transform.SetParent(wheel.transform);
        hub.transform.localPosition = Vector3.zero;
        hub.transform.localRotation = Quaternion.identity;
        hub.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
        var hubMat = CreateMaterial("CarHub", new Color(0.5f, 0.5f, 0.5f), true);
        hub.GetComponent<Renderer>().sharedMaterial = hubMat;
        Object.DestroyImmediate(hub.GetComponent<Collider>());
    }

    static Material CreateMaterial(string name, Color color, bool useEmission = false, bool transparent = false)
    {
        string matDir = "Assets/Materials";
        string matPath = $"{matDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null)
        {
            existing.color = color;
            if (useEmission)
            {
                existing.EnableKeyword("_EMISSION");
                existing.SetColor("_EmissionColor", color * 0.15f);
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
            mat.SetColor("_EmissionColor", color * 0.15f);
        }

        if (transparent)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }
}
