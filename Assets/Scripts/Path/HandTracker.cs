using UnityEngine;
using UnityEngine.EventSystems;
using MRReP.ROS;

#if !UNITY_EDITOR
using System.Linq;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;   // TrackedHandJoint, Handedness, MixedRealityPose
#endif

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

#if UNITY_EDITOR
            bool isPinching = Input.GetMouseButton(0);
#else
            bool isPinching = CheckHoloLensPinch();
#endif

            if (_waitingForRelease)
            {
                if (!isPinching)
                    _waitingForRelease = false;
                return;
            }

            if (isPinching)
            {
#if UNITY_EDITOR
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane drawPlane = new Plane(Vector3.up, new Vector3(0, editorDrawPlaneY, 0));
                if (drawPlane.Raycast(ray, out float distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    if (pathData.Count == 0 || Vector3.Distance(hitPoint, _lastEditorPoint) >= editorMinPointDistance)
                    {
                        pathData.AddPoint(hitPoint);
                        _lastEditorPoint = hitPoint;
                    }
                }
#else
                AddHoloLensPinchPoint();
                _waitingForRelease = true;   // AirTap：一次捏合放一个点，松手后才能捏下一个
#endif
                _lastSampleTime = Time.time;
            }
        }

#if !UNITY_EDITOR
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
            // AirTap 模式：从头部视线（你看向哪）打到地板，路径点落在地板上。
            // 看向地板一个位置 → AirTap（捏一下）→ 那里出现一个点；重复放点连成路径。
            var cam = Camera.main;
            if (cam == null) return;
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 10f))
            {
                pathData.AddPoint(hit.point + Vector3.up * 0.05f);  // 抬高 5cm 防陷地
            }
        }
#endif
    }
}
