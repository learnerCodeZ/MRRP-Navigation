using UnityEngine;
using MRReP.Path;

namespace MRReP.UI
{
    public class KeyboardTest : MonoBehaviour
    {
        [SerializeField] private HandTracker handTracker;
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private PathData pathData;
        [SerializeField] private MainMenuController mainMenuController;

        private bool _isDrawing;

        private void Update()
        {
            // M 键：切换主菜单
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (mainMenuController != null)
                {
                    var panel = mainMenuController.transform.gameObject;
                    if (panel.activeSelf)
                        mainMenuController.HideMainMenu();
                    else
                        mainMenuController.ShowMainMenu();
                }
            }

            // A 键：开始/停止画线
            if (Input.GetKeyDown(KeyCode.A))
            {
                _isDrawing = !_isDrawing;
                if (_isDrawing)
                {
                    handTracker.StartTracking();
                    Debug.Log("[Test] 开始画线");
                }
                else
                {
                    handTracker.StopTracking();
                    Debug.Log("[Test] 停止画线");
                }
            }

            // C 键：清空路径
            if (Input.GetKeyDown(KeyCode.C))
            {
                handTracker.StopTracking();
                pathRenderer.ClearRenderers();
                pathData.Clear();
                _isDrawing = false;
                Debug.Log("[Test] 清空路径");
            }

            // S 键：打印路径点数
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log($"[Test] 当前路径点数: {pathData.Count}");
            }
        }
    }
}
