using UnityEngine;
using UnityEngine.UI;
#if !UNITY_EDITOR
using Microsoft.MixedReality.Toolkit.Input;
#endif

namespace MRReP.UI
{
    /// <summary>
    /// 设备(HL2)上把"鼠标式 Unity UI"改成可交互：
    /// 菜单原本按鼠标/Screen Space 设计，设备上 World Space Canvas 缺事件相机
    /// → 手射线打不到按钮、AirTap 不灵、位置偏低。设备 Awake 时自动：
    ///   1) Canvas 设事件相机 = 主相机 + 确保 GraphicRaycaster（手射线远场 AirTap）
    ///   2) 每个 Button 加 NearInteractionTouchableUnityUI（近场直接捏）
    ///   3) 菜单根调到眼高、前方
    /// 挂到菜单根(如 MainMenu)或场景常驻物体上；Editor 里此类存在但 Awake 空跑(#if !UNITY_EDITOR)。
    /// 注：若 AirTap 仍不灵，需在 Unity 里把 Button 换成 MRTK PressableButton 预制体(UI 重构)。
    /// </summary>
    public class HL2UICanvasSetup : MonoBehaviour
    {
        [SerializeField] private Transform menuRoot;                                   // 菜单根(调位置用；空=用自身)
        [SerializeField] private Vector3 position = new Vector3(0f, 1.4f, 1.6f);       // 眼高、前方 1.6m
        [SerializeField] private Vector3 scale = new Vector3(0.001f, 0.001f, 0.001f);  // World Space Canvas 像素→米

        private void Awake()
        {
#if !UNITY_EDITOR
            // 1) Canvas：事件相机 + GraphicRaycaster（World Space）
            var cam = Camera.main;
            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                if (canvas.renderMode != RenderMode.WorldSpace)
                    canvas.renderMode = RenderMode.WorldSpace;
                if (cam != null)
                    canvas.worldCamera = cam;
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // 2) Button：加近场可捏（MRTK NearInteractionTouchableUnityUI）
            foreach (var btn in FindObjectsOfType<Button>())
            {
                if (btn.GetComponent<NearInteractionTouchableUnityUI>() == null)
                    btn.gameObject.AddComponent<NearInteractionTouchableUnityUI>();
            }

            // 3) 菜单根：抬到眼高、前方
            var t = menuRoot != null ? menuRoot : transform;
            t.position = position;
            t.localScale = scale;
#endif
        }
    }
}
