using UnityEngine;
using MRReP.Path;
using MRReP.ROS;
using MRReP.Robot;

namespace MRReP.UI
{
    [DefaultExecutionOrder(100)]
    public class PlayModeInputController : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private HandTracker handTracker;
        [SerializeField] private PathRenderer pathRenderer;
        [SerializeField] private LocalPathFollower localPathFollower;
        [SerializeField] private PathSender pathSender;

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
        }

        private void Awake() => ResolveAll();

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
            {
                ResolveAll();
                handTracker?.StartTracking();
            }

            if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                ResolveAll();
                if (pathData == null || pathData.Count == 0) return;
                handTracker?.StopTracking();
                localPathFollower?.StartFollowing();
            }

            if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                ResolveAll();
                handTracker?.StopTracking();
                localPathFollower?.StopFollowing();
                pathRenderer?.ClearRenderers();
                pathData?.Clear();
            }

            if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Alpha0))
            {
                ResolveAll();
                handTracker?.StopTracking();
            }
#endif
        }
    }
}
