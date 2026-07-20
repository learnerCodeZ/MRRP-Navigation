using UnityEngine;
using TMPro;

namespace MRReP.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject preferredPathMenu;

        private void Start()
        {
            // 方案A 测试模式：隐藏菜单（设备上 UI 按钮不可用）。
            // 设备上由 PreferredPathMenuController 开机自动进画线 + 停笔自动发送，无需菜单。
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (preferredPathMenu != null) preferredPathMenu.SetActive(false);
        }

        public void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            preferredPathMenu.SetActive(false);
        }

        public void HideMainMenu()
        {
            mainMenuPanel.SetActive(false);
        }

        public void OnPreferredPathClicked()
        {
            mainMenuPanel.SetActive(false);
            preferredPathMenu.SetActive(true);
        }
    }
}
