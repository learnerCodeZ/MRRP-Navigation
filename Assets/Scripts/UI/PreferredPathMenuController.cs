using UnityEngine;
using TMPro;
using MRReP.Path;
using MRReP.ROS;
using MRReP.Robot;

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
        [SerializeField] private LocalPathFollower localPathFollower;

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
            // 【仅 PlayMode 测试用】按 P 直接发 /hrp_path，跳过确认弹窗
            // （弹窗的 Yes 按钮在 PlayMode 下点不到；HL2 上用 AirTap 点按钮，无此问题）
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (pathData.Count > 0)
                {
                    handTracker.StopTracking();
                    pathSender.SendPath(pathData);
                    _currentState = MenuState.Send;
                    UpdateStatusText();
                }
            }
            // 【仅 PlayMode 测试用】按 X 直接清空路径，跳过确认弹窗
            if (Input.GetKeyDown(KeyCode.X))
            {
                handTracker.StopTracking();
                if (localPathFollower != null)
                    localPathFollower.StopFollowing();
                pathRenderer.ClearRenderers();
                pathData.Clear();
                _currentState = MenuState.Off;
                UpdateStatusText();
            }
        }

        public void OnAddClicked()
        {
            _currentState = MenuState.Add;
            handTracker.StartTracking();
            UpdateStatusText();
        }

        public void OnClearClicked()
        {
            // 直接清空（跳过确认弹窗——HL2 上 Unity UI 弹窗点不了）
            handTracker.StopTracking();
            if (localPathFollower != null)
                localPathFollower.StopFollowing();
            pathRenderer.ClearRenderers();
            pathData.Clear();
            _currentState = MenuState.Off;
            UpdateStatusText();
        }

        private void OnClearConfirmed(bool confirmed)
        {
            if (!confirmed) return;

            handTracker.StopTracking();
            if (localPathFollower != null)
                localPathFollower.StopFollowing();
            pathRenderer.ClearRenderers();
            pathData.Clear();
            _currentState = MenuState.Off;
            UpdateStatusText();
        }

        public void OnSendClicked()
        {
            if (pathData.Count == 0) return;

            handTracker.StopTracking();
            // 直接发送（跳过确认弹窗——HL2 上 Unity UI 弹窗点不了）
            if (localPathFollower != null)
                localPathFollower.StartFollowing();
            else
                pathSender.SendPath(pathData);
            _currentState = MenuState.Send;
            UpdateStatusText();
        }

        private void OnSendConfirmed(bool confirmed)
        {
            if (!confirmed) return;

            handTracker.StopTracking();

            if (localPathFollower != null)
            {
                localPathFollower.StartFollowing();
            }
            else
            {
                pathSender.SendPath(pathData);
            }

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
