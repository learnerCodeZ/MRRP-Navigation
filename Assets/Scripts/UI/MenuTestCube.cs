using UnityEngine;

namespace MRReP.UI
{
    /// <summary>
    /// 测试脚本：在MRTKMenu位置生成4个红色立方体（纯3D物体，不依赖Canvas）。
    /// 测试完后删掉这个脚本。
    /// </summary>
    public class MenuTestCube : MonoBehaviour
    {
        private void Start()
        {
            string[] labels = { "Add", "Clear", "Send", "Back" };
            float spacing = 0.04f;

            for (int i = 0; i < labels.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = labels[i];
                // 直接放在世界空间，不挂Canvas下面
                cube.transform.position = transform.position + new Vector3(0, -i * spacing, 0);
                cube.transform.localScale = new Vector3(0.03f, 0.025f, 0.005f);

                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = Color.red;

                var collider = cube.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }

            Debug.Log("[MenuTestCube] 已在世界空间生成4个红色测试方块");
        }
    }
}
