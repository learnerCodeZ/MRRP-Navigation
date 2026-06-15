using UnityEngine;
using TMPro;
using MRReP.Path;
using MRReP.ROS;

namespace MRReP.UI
{
    public enum MenuState
    {
        Off,
        Add,
        Send
    }

    public class PreferredPathMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject preferredPathMenu;
        [SerializeField] private MainMenuController mainMenuController;

        [Header("References")]
        [SerializeField] private HandTracker handTracker;
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private PathData pathData;
        [SerializeField] private PathSender pathSender;

        [Header("Confirm Dialog")]
        [SerializeField] private ConfirmDialog confirmDialog;

        private MenuState _currentState = MenuState.Off;

        private void Start()
        {
            UpdateStatusText();
        }

        private void Update()
        {
            // 键盘快捷键测试（Play 模式下使用）
            if (Input.GetKeyDown(KeyCode.A))
                OnAddClicked();
            if (Input.GetKeyDown(KeyCode.C))
                OnClearClicked();
            if (Input.GetKeyDown(KeyCode.S))
                OnSendClicked();
            if (Input.GetKeyDown(KeyCode.B))
                OnBackClicked();
        }

        public void OnAddClicked()
        {
            _currentState = MenuState.Add;
            handTracker.StartTracking();
            UpdateStatusText();
        }

        public void OnClearClicked()
        {
            confirmDialog.Show("Are you sure you want to delete all?", OnClearConfirmed);
        }

        private void OnClearConfirmed(bool confirmed)
        {
            if (!confirmed) return;

            handTracker.StopTracking();
            pathRenderer.ClearRenderers();
            pathData.Clear();
            _currentState = MenuState.Off;
            UpdateStatusText();
        }

        public void OnSendClicked()
        {
            if (pathData.Count == 0) return;

            confirmDialog.Show("Are you sure you want to SEND PATH to the robot?", OnSendConfirmed);
        }

        private void OnSendConfirmed(bool confirmed)
        {
            if (!confirmed) return;

            handTracker.StopTracking();
            pathSender.SendPath(pathData);
            _currentState = MenuState.Send;
            UpdateStatusText();
        }

        public void OnBackClicked()
        {
            handTracker.StopTracking();
            _currentState = MenuState.Off;
            preferredPathMenu.SetActive(false);
            mainMenuController.ShowMainMenu();
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;

            switch (_currentState)
            {
                case MenuState.Off:
                    statusText.text = "Stage 0: OFF MODE";
                    break;
                case MenuState.Add:
                    statusText.text = "Stage 0: ADD MODE";
                    break;
                case MenuState.Send:
                    statusText.text = "SEND PATH";
                    break;
            }
        }
    }
}
