using UnityEngine;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Input;

namespace MRReP.Path
{
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
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                pathData.AddPoint(ray.GetPoint(0.5f));
#else
                AddHoloLensPinchPoint();
#endif
                _lastSampleTime = Time.time;
            }
        }

#if !UNITY_EDITOR
        private bool CheckHoloLensPinch()
        {
            var handJointService = CoreServices.InputSystem?.GetDataProviders<IMixedRealityHandJointService>()
                .FirstOrDefault() as IMixedRealityHandJointService;
            if (handJointService == null) return false;

            bool hasThumb = handJointService.TryGetJoint(TrackedHandJoint.ThumbTip, out var thumbPose);
            bool hasIndex = handJointService.TryGetJoint(TrackedHandJoint.IndexTip, out var indexPose);

            if (hasThumb && hasIndex)
            {
                float pinchDist = Vector3.Distance(thumbPose.Position, indexPose.Position);
                return pinchDist < pinchThreshold;
            }
            return false;
        }

        private void AddHoloLensPinchPoint()
        {
            var handJointService = CoreServices.InputSystem?.GetDataProviders<IMixedRealityHandJointService>()
                .FirstOrDefault() as IMixedRealityHandJointService;
            if (handJointService == null) return;

            if (handJointService.TryGetJoint(TrackedHandJoint.ThumbTip, out var thumbPose) &&
                handJointService.TryGetJoint(TrackedHandJoint.IndexTip, out var indexPose))
            {
                Vector3 pinchPoint = (thumbPose.Position + indexPose.Position) / 2f;
                pathData.AddPoint(pinchPoint);
            }
        }
#endif
    }
}
