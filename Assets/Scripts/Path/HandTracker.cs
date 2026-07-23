using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

namespace MRReP.Path
{
    /// <summary>
    /// 右手画线：用 MRTK 自带指针系统（白色射线+圈圈）。
    /// 右手捏合 → 在指针圈圈位置（射线命中点）放一个路径点。
    /// 连续：捏住拖动持续放点。MRTK 自带平滑 + 视觉反馈。
    /// </summary>
    public class HandTracker : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private float trackingInterval = 0.05f;
        [SerializeField] private float pinchThreshold = 0.02f;

        private bool _isTracking;
        private bool _waitingForRelease;
        private float _lastSampleTime;

        public bool IsTracking => _isTracking;

        public void StartTracking()
        {
            _isTracking = true;
            _waitingForRelease = true;
            _lastSampleTime = 0f;
        }

        public void StopTracking()
        {
            _isTracking = false;
            _waitingForRelease = false;
        }

        private void Update()
        {
            if (!_isTracking) return;
            if (Time.time - _lastSampleTime < trackingInterval) return;

            // 只检测右手捏合
            bool isPinching = CheckRightHandPinch();
#if UNITY_EDITOR
            if (!isPinching) isPinching = Input.GetMouseButton(0);
#endif

            if (_waitingForRelease)
            {
                if (!isPinching) _waitingForRelease = false;
                return;
            }

            if (isPinching)
            {
                AddPointAtPointer();
                _lastSampleTime = Time.time;
            }
        }

        /// <summary>右手捏合检测（拇指+食指尖距离 < 阈值）</summary>
        private bool CheckRightHandPinch()
        {
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, Handedness.Right, out var thumb) &&
                HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out var index))
            {
                return Vector3.Distance(thumb.Position, index.Position) < pinchThreshold;
            }
            return false;
        }

        /// <summary>
        /// 用 MRTK 自带指针系统获取右手射线命中点 → 路径点。
        /// MRTK 的白色射线+圈圈就是指针视觉，捏合时圈圈缩小=选中反馈。
        /// 不用自己画瞄准线。
        /// </summary>
        private void AddPointAtPointer()
        {
            var focusProvider = CoreServices.InputSystem?.FocusProvider;
            if (focusProvider == null) return;

            foreach (var pointer in focusProvider.GetPointers<IMixedRealityPointer>())
            {
                if (pointer.Controller == null) continue;
                if (pointer.Controller.ControllerHandedness != Handedness.Right) continue;

                if (focusProvider.TryGetFocusDetails(pointer, out var focus) && focus.Object != null)
                {
                    pathData.AddPoint(focus.Point + Vector3.up * 0.03f);
                    return;
                }
            }
        }
    }
}
