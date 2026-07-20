using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;

namespace MRReP.UI
{
    /// <summary>
    /// 代码构建 MRTK 原生菜单：运行时实例化 PressableButton 预制体 ×4（Add/Clear/Send/Back），
    /// 设文字、把 ButtonPressed 事件接到 PreferredPathMenuController 的对应方法。
    ///
    /// 用法：挂到【激活】物体上（面板或 Managers），Inspector 赋三个引用：
    ///   - buttonPrefab：PressableButton 预制体（如 PressableButton_32x32mm_IconAndText）
    ///   - panelParent：按钮放在哪个面板下（该面板的 Transform）
    ///   - controller：挂 PreferredPathMenuController 的物体
    /// 运行时自动生成 4 个按钮并接线，AirTap 即触发对应方法。
    /// </summary>
    public class MRTKMenuBuilder : MonoBehaviour
    {
        [Header("Prefabs & References")]
        [SerializeField] private GameObject buttonPrefab;                 // 拖入 PressableButton 预制体
        [SerializeField] private Transform panelParent;                   // 按钮放在哪个面板下
        [SerializeField] private PreferredPathMenuController controller;  // 事件目标（OnAddClicked 等）

        [Header("Layout")]
        [SerializeField] private float spacing = 0.08f;                   // 按钮垂直间距(米)
        [SerializeField] private Vector3 buttonScale = new Vector3(1f, 1f, 1f);

        private void Start()
        {
            if (buttonPrefab == null || panelParent == null || controller == null)
            {
                Debug.LogError("[MRTKMenuBuilder] Inspector 没赋值：buttonPrefab / panelParent / controller。请在 Inspector 拖好这三个引用。");
                return;
            }
            BuildButton("Add", 0, controller.OnAddClicked);
            BuildButton("Clear", 1, controller.OnClearClicked);
            BuildButton("Send", 2, controller.OnSendClicked);
            BuildButton("Back", 3, controller.OnBackClicked);
            Debug.Log("[MRTKMenuBuilder] 已构建 4 个 MRTK 按钮（Add/Clear/Send/Back）并接好事件。");
        }

        private void BuildButton(string label, int index, UnityAction action)
        {
            var go = Instantiate(buttonPrefab, panelParent);
            go.name = label;
            go.transform.localPosition = new Vector3(0, -index * spacing, 0);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = buttonScale;

            // 设按钮文字（兼容 TextMeshPro 与 TextMeshProUGUI 两种）
            var tmp = go.GetComponentInChildren<TextMeshPro>();
            if (tmp != null) tmp.text = label;
            else
            {
                var tmpU = go.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpU != null) tmpU.text = label;
            }

            // 接线：Interactable.OnClick → controller 对应方法
            // Interactable.OnClick 在 Editor 鼠标和 HL2 AirTap 上都能响应
            var ia = go.GetComponent<Interactable>();
            if (ia != null)
            {
                ia.OnClick.AddListener(action);
            }
            else
            {
                // 兜底找 PressableButton.ButtonPressed
                var pb = go.GetComponent<PressableButton>();
                if (pb != null)
                {
                    pb.ButtonPressed.AddListener(action);
                }
                else
                {
                    Debug.LogWarning($"[MRTKMenuBuilder] '{label}' 预制体既没 Interactable 也没 PressableButton，事件没接上。");
                }
            }
        }
    }
}
