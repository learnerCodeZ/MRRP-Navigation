using UnityEngine;
using UnityEditor;
using MRReP.Robot;

public class VirtualCarCreator : EditorWindow
{
    [MenuItem("Tools/Create VirtualCar Prefab")]
    static void CreateVirtualCar()
    {
        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // Create materials
        var bodyMat = CreateMaterial("CarBody", new Color(0.85f, 0.85f, 0.85f));
        var wheelMat = CreateMaterial("CarWheel", new Color(0.15f, 0.15f, 0.15f));

        // Root
        var root = new GameObject("VirtualCar");
        root.AddComponent<VirtualCarController>();

        // CarModel (controlled by VirtualCarController)
        var carModel = new GameObject("CarModel");
        carModel.transform.SetParent(root.transform);
        carModel.transform.localPosition = Vector3.zero;

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(carModel.transform);
        body.transform.localPosition = new Vector3(0, 0.1f, 0);
        body.transform.localScale = new Vector3(0.4f, 0.15f, 0.2f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Wheels
        Vector3 wheelScale = new Vector3(0.05f, 0.08f, 0.05f);
        Vector3 frontOffset = new Vector3(0, 0.05f, 0.12f);
        Vector3 rearOffset = new Vector3(0, 0.05f, -0.12f);
        Vector3 leftOffset = new Vector3(-0.18f, 0, 0);
        Vector3 rightOffset = new Vector3(0.18f, 0, 0);

        CreateWheel("FrontLeftWheel", carModel.transform, frontOffset + leftOffset, wheelScale, wheelMat);
        CreateWheel("FrontRightWheel", carModel.transform, frontOffset + rightOffset, wheelScale, wheelMat);
        CreateWheel("RearLeftWheel", carModel.transform, rearOffset + leftOffset, wheelScale, wheelMat);
        CreateWheel("RearRightWheel", carModel.transform, rearOffset + rightOffset, wheelScale, wheelMat);

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
