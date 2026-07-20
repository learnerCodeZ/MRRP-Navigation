using UnityEngine;

namespace MRReP.UI
{
    /// <summary>
    /// 手掌菜单：运行时生成4个简单3D方块作为菜单按钮。
    /// 挂到HandMenu上即可，不依赖Canvas和MRTK按钮。
    /// </summary>
    public class SimpleHandMenu : MonoBehaviour
    {
        [Header("菜单设置")]
        [SerializeField] private float spacing = 0.04f;
        [SerializeField] private Vector3 buttonSize = new Vector3(0.04f, 0.03f, 0.01f);
        [SerializeField] private float menuOffsetZ = 0.15f;

        private GameObject menuPanel;

        private void Start()
        {
            CreateMenu();
        }

        private void CreateMenu()
        {
            // 创建菜单面板
            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(transform, false);
            menuPanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

            // 4个按钮标签和颜色
            string[] labels = { "Add", "Clear", "Send", "Back" };
            Color[] colors = { Color.green, Color.yellow, Color.blue, Color.red };

            for (int i = 0; i < labels.Length; i++)
            {
                // 创建按钮方块
                var button = GameObject.CreatePrimitive(PrimitiveType.Cube);
                button.name = labels[i];
                button.transform.SetParent(menuPanel.transform, false);
                button.transform.localPosition = new Vector3(0, -i * spacing, 0);
                button.transform.localScale = buttonSize;

                // 设置颜色
                var renderer = button.GetComponent<Renderer>();
                renderer.material.color = colors[i];

                // 删除Collider
                var collider = button.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                // 添加文字（用3D TextMeshPro或简单TextMesh）
                var textObj = new GameObject("Text_" + labels[i]);
                textObj.transform.SetParent(button.transform, false);
                textObj.transform.localPosition = new Vector3(0, 0, -0.006f);
                textObj.transform.localScale = new Vector3(1, 1, 1);

                var textMesh = textObj.AddComponent<TextMesh>();
                textMesh.text = labels[i];
                textMesh.characterSize = 0.02f;
                textMesh.fontSize = 48;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.color = Color.white;
            }

            Debug.Log("[SimpleHandMenu] 已生成菜单: Add/Clear/Send/Back");
        }
    }
}
