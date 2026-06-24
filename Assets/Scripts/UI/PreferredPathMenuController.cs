using UnityEngine;
using TMPro;
using MRReP.Path;
using MRReP.ROS;
using MRReP.Robot;

namespace MRReP.UI
{
    public enum MenuState { Off, Add, Send }

    public class PreferredPathMenuController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject preferredPathMenu;
        [SerializeField] private MainMenuController mainMenuController;

        [SerializeField] private HandTracker handTracker;
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private PathData pathData;
        [SerializeField] private PathSender pathSender;
        [SerializeField] private LocalPathFollower localPathFollower;
        [SerializeField] private ConfirmDialog confirmDialog;

        private MenuState _currentState = MenuState.Off;
        private bool _resolved;

        private void ResolveAll()
        {
            if (_resolved) return;
            _resolved = true;

            if (pathData == null) pathData = FindObjectOfType<PathData>();
            if (handTracker == null) handTracker = FindObjectOfType<HandTracker>();
            if (pathRenderer == null) pathRenderer = FindObjectOfType<PathRenderer>();
            if (localPathFollower == null) localPathFollower = FindObjectOfType<LocalPathFollower>();
            if (pathSender == null) pathSender = FindObjectOfType<PathSender>();
            if (confirmDialog == null) confirmDialog = FindObjectOfType<ConfirmDialog>();
            if (mainMenuController == null) mainMenuController = FindObjectOfType<MainMenuController>();

            Debug.Log($"[MenuCtrl] Resolve: pathData={pathData != null} hand={handTracker != null} renderer={pathRenderer != null} follower={localPathFollower != null} sender={pathSender != null} dialog={confirmDialog != null}");
        }

        private void Awake() => ResolveAll();
        private void Start() => UpdateStatusText();

        public void OnAddClicked()
        {
            ResolveAll();
            _currentState = MenuState.Add;
            if (handTracker != null) handTracker.StartTracking();
            UpdateStatusText();
        }

        public void OnClearClicked()
        {
            ResolveAll();
            if (confirmDialog != null)
                confirmDialog.Show("Are you sure you want to delete all?", confirmed => { if (confirmed) DoClear(); });
            else
                DoClear();
        }

        public void OnSendClicked()
        {
            ResolveAll();
            if (pathData == null || pathData.Count == 0) return;
            if (handTracker != null) handTracker.StopTracking();

            if (confirmDialog != null)
                confirmDialog.Show("Are you sure you want to SEND PATH to the robot?", confirmed => { if (confirmed) DoSend(); });
            else
                DoSend();
        }

        public void OnBackClicked()
        {
            if (handTracker != null) handTracker.StopTracking();
            _currentState = MenuState.Off;
            if (preferredPathMenu != null) preferredPathMenu.SetActive(false);
            if (mainMenuController != null) mainMenuController.ShowMainMenu();
        }

        private void DoClear()
        {
            if (handTracker != null) handTracker.StopTracking();
            if (localPathFollower != null) localPathFollower.StopFollowing();
            if (pathRenderer != null) pathRenderer.ClearRenderers();
            if (pathData != null) pathData.Clear();
            _currentState = MenuState.Off;
            UpdateStatusText();
        }

        private void DoSend()
        {
            if (localPathFollower != null)
            {
                localPathFollower.StartFollowing();
                Debug.Log($"[MenuCtrl] Follower started, isFollowing={localPathFollower.IsFollowing}");
            }
            else
            {
                Debug.LogError("[MenuCtrl] localPathFollower is NULL! Cannot follow path.");
                if (pathSender != null) pathSender.SendPath(pathData);
            }
            _currentState = MenuState.Send;
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;
            switch (_currentState)
            {
                case MenuState.Off: statusText.text = "OFF MODE"; break;
                case MenuState.Add: statusText.text = "ADD MODE"; break;
                case MenuState.Send: statusText.text = "SEND PATH"; break;
            }
        }
    }
}
