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

        // 实时草稿轮询：边画边发 /hrp_draft（WebRop 实时镜像），clear/send 时清空
        private float _draftTimer;
        private int _lastDraftCount = -1;

        private void Start()
        {
            UpdateStatusText();
        }

        private void Update()
        {
            // 实时草稿：点数变化时发 /hrp_draft（add 边画边发；clear 发空），10Hz 节流
            _draftTimer += Time.deltaTime;
            if (_draftTimer >= 0.1f)
            {
                _draftTimer = 0f;
                int n = pathData != null ? pathData.Count : 0;
                if (n != _lastDraftCount)
                {
                    _lastDraftCount = n;
                    if (n > 0) pathSender.SendDraft(pathData);
                    else pathSender.ClearDraft();
                }
            }

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
                    pathSender.ClearDraft(); // 路径已发，清掉 WebRop 上的草稿
                    _lastDraftCount = pathData.Count; // pathData 还有 N 点，避免轮询立刻重发草稿
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
            pathSender.ClearDraft(); // 路径已发，清掉 WebRop 上的草稿
            _lastDraftCount = pathData.Count;
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
            pathSender.ClearDraft();
            _lastDraftCount = pathData.Count;

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
