using UnityEngine;
using UnityEditor;
using MRReP.Robot;

public class VirtualCarCreator : EditorWindow
{
    private GameObject selectedModel;
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

        selectedModel = (GameObject)EditorGUILayout.ObjectField(
            "Car Model (FBX/OBJ/GLTF)", selectedModel, typeof(GameObject), false);

        modelScale = EditorGUILayout.FloatField("Model Scale", modelScale);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Prefab", GUILayout.Height(30)))
        {
            CreateVirtualCar(selectedModel, modelScale);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. Drop your car model into Assets/Models/\n" +
            "2. Select it above\n" +
            "3. Click Create Prefab\n" +
            "4. Drag prefab into scene",
            MessageType.Info);
    }

    static void CreateVirtualCar(GameObject modelPrefab, float scale)
    {
        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Root
        var root = new GameObject("VirtualCar");
        root.AddComponent<VirtualCarController>();

        // CarModel (controlled by VirtualCarController)
        var carModel = new GameObject("CarModel");
        carModel.transform.SetParent(root.transform);
        carModel.transform.localPosition = Vector3.zero;

        if (modelPrefab != null)
        {
            // Use external model
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            if (modelInstance == null)
                modelInstance = (GameObject)AssetDatabase.LoadAssetAtPath(
                    AssetDatabase.GetAssetPath(modelPrefab), typeof(GameObject));

            if (modelInstance != null)
            {
                modelInstance.transform.SetParent(carModel.transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one * scale;

                // Remove colliders from model (VirtualCarController handles movement)
                foreach (var col in modelInstance.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);

                Debug.Log($"[VirtualCarCreator] Using model: {modelPrefab.name}, scale: {scale}");
            }
            else
            {
                Debug.LogWarning("[VirtualCarCreator] Could not instantiate model, falling back to primitives");
                CreatePrimitiveCar(carModel.transform);
            }
        }
        else
        {
            // Fallback: primitive car
            CreatePrimitiveCar(carModel.transform);
        }

        // Wire up VirtualCarController
        var controller = root.GetComponent<VirtualCarController>();
        var carModelField = typeof(VirtualCarController).GetField("carModel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (carModelField != null)
            carModelField.SetValue(controller, carModel.transform);

        // Save as prefab
        string prefabPath = $"{prefabDir}/VirtualCar.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VirtualCarCreator] Created prefab at {prefabPath}");
        EditorUtility.DisplayDialog("VirtualCar Created",
            $"Prefab saved to:\n{prefabPath}\n\nDrag it into your scene and wire up OdomSubscriber.virtualCar.",
            "OK");
    }

    static void CreatePrimitiveCar(Transform parent)
    {
        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        var bodyMat = CreateMaterial("CarBody", new Color(0.85f, 0.85f, 0.85f));
        var wheelMat = CreateMaterial("CarWheel", new Color(0.15f, 0.15f, 0.15f));

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(parent);
        body.transform.localPosition = new Vector3(0, 0.06f, 0);
        body.transform.localScale = new Vector3(0.2f, 0.08f, 0.12f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Wheels
        Vector3 wheelScale = new Vector3(0.03f, 0.05f, 0.03f);
        Vector3 frontOffset = new Vector3(0, 0.03f, 0.08f);
        Vector3 rearOffset = new Vector3(0, 0.03f, -0.08f);
        Vector3 leftOffset = new Vector3(-0.10f, 0, 0);
        Vector3 rightOffset = new Vector3(0.10f, 0, 0);

        CreateWheel("FrontLeftWheel", parent, frontOffset + leftOffset, wheelScale, wheelMat);
        CreateWheel("FrontRightWheel", parent, frontOffset + rightOffset, wheelScale, wheelMat);
        CreateWheel("RearLeftWheel", parent, rearOffset + leftOffset, wheelScale, wheelMat);
        CreateWheel("RearRightWheel", parent, rearOffset + rightOffset, wheelScale, wheelMat);
    }

    static void CreateWheel(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
    {
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(parent);
        wheel.transform.localPosition = localPos;
        wheel.transform.localScale = scale;
        wheel.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(wheel.GetComponent<Collider>());
    }

    static Material CreateMaterial(string name, Color color)
    {
        string matPath = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        AssetDatabase.CreateAsset(mat, matPath);
        return mat;
    }
}
