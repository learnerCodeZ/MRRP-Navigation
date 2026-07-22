using UnityEngine;
using UnityEngine.EventSystems;
using MRReP.ROS;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

namespace MRReP.Path
{
    public class HandTracker : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private float trackingInterval = 0.05f;
        [SerializeField] private float pinchThreshold = 0.02f;
#if UNITY_EDITOR
        [SerializeField] private float editorDrawPlaneY = 0.5f;
        [SerializeField] private float editorMinPointDistance = 0.05f;
        private Vector3 _lastEditorPoint;
#endif

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

            // 手势捏合检测（Remoting + 设备都用），Editor 鼠标兜底
            bool isPinching = CheckHoloLensPinch();
#if UNITY_EDITOR
            if (!isPinching) isPinching = Input.GetMouseButton(0);
#endif

            if (_waitingForRelease)
            {
                if (!isPinching)
                    _waitingForRelease = false;
                return;
            }

            if (isPinching)
            {
                AddHoloLensPinchPoint();
                _waitingForRelease = true;   // 一次捏合放一个点，松手后才能捏下一个
                _lastSampleTime = Time.time;
            }
        }

        private bool CheckHoloLensPinch()
        {
            // MRTK 2.8.3 标准：HandJointUtils.TryGetJointPose(关节, 左右手, out pose)
            foreach (var hand in new[] { Handedness.Right, Handedness.Left })
            {
                if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, hand, out var thumb) &&
                    HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, hand, out var index))
                {
                    if (Vector3.Distance(thumb.Position, index.Position) < pinchThreshold)
                        return true;
                }
            }
            return false;
        }

        private void AddHoloLensPinchPoint()
        {
            // AirTap 模式：从头部正下方打地板(Vector3.down)，路径点落在头部下方的地板上。
            // 头移到想放点的地板位置上方 → AirTap（捏一下）→ 那里出现一个点。
            var cam = Camera.main;
            if (cam == null) return;
            if (Physics.Raycast(cam.transform.position, Vector3.down, out var hit, 5f))
            {
                pathData.AddPoint(hit.point + Vector3.up * 0.05f);  // 抬高 5cm 防陷地
            }
        }
    }
}
