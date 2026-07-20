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
#if !UNITY_EDITOR
            // 方案A(设备测试)：开机自动进画线模式（设备上 UI 按钮不可用，靠自动画+自动发）
            OnAddClicked();
#endif
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
#if !UNITY_EDITOR
            // 方案A(设备测试)：画完停笔 1.5s 自动发 /hrp_path（绕开设备上不可用的 UI 按钮）
            DoAutoSendDwell();
#endif
        }

#if !UNITY_EDITOR
        // 方案A：检测"画完停笔"——路径点数停止增长超过 dwell 秒 → 自动发送
        private float _lastChangeTime;
        private int _lastCount = -1;
        private bool _autoSent;
        private void DoAutoSendDwell()
        {
            int c = pathData == null ? 0 : pathData.Count;
            if (c != _lastCount)
            {
                _lastCount = c;
                _lastChangeTime = Time.time;
                _autoSent = false;
            }
            if (_currentState == MenuState.Add && c >= 2 && !_autoSent && (Time.time - _lastChangeTime) > 1.5f)
            {
                _autoSent = true;
                handTracker.StopTracking();
                if (localPathFollower != null) localPathFollower.StartFollowing();
                else pathSender.SendPath(pathData);
                _currentState = MenuState.Send;
                UpdateStatusText();
            }
        }
#endif

        public void OnAddClicked()
        {
            _currentState = MenuState.Add;
            handTracker.StartTracking();
            UpdateStatusText();
        }

        public void OnClearClicked()
        {
            handTracker.StopTracking();
            confirmDialog.Show("Are you sure you want to delete all?", OnClearConfirmed);
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
            confirmDialog.Show("Are you sure you want to SEND PATH to the robot?", OnSendConfirmed);
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
