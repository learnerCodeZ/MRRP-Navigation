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
            mainMenuPanel.SetActive(false);
            preferredPathMenu.SetActive(false);
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
