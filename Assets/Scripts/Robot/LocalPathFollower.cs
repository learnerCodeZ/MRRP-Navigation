using UnityEngine;
using MRReP.Path;

namespace MRReP.Robot
{
    public class LocalPathFollower : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private VirtualCarController virtualCar;
        [SerializeField] private float speed = 0.3f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float reachThreshold = 0.1f;
        [SerializeField] private bool loopPath = false;

        private int _currentIndex;
        private bool _isFollowing;
        private Anchor.SpatialAnchorManager _anchorManager;

        public bool IsFollowing => _isFollowing;

        private Transform GetCarTransform()
        {
            if (virtualCar == null) return transform;
            var carModelField = typeof(VirtualCarController).GetField("carModel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (carModelField != null)
            {
                var carModel = carModelField.GetValue(virtualCar) as Transform;
                if (carModel != null) return carModel;
            }
            return virtualCar.transform;
        }

        public void StartFollowing()
        {
            if (pathData == null || pathData.Count == 0) return;

            _currentIndex = 0;
            _isFollowing = true;

            _anchorManager = FindObjectOfType<Anchor.SpatialAnchorManager>();

            var points = pathData.Points;
            var startPos = points[0];
            if (_anchorManager != null && _anchorManager.AnchorTransform != null)
                startPos += _anchorManager.AnchorTransform.position;

            startPos.y = GetCarTransform().position.y;

            if (virtualCar != null)
            {
                virtualCar.UpdatePose(startPos, Quaternion.identity);
                var car = GetCarTransform();
                car.position = startPos;
                car.rotation = Quaternion.identity;
            }
        }

        public void StopFollowing()
        {
            _isFollowing = false;
        }

        private void Update()
        {
            if (!_isFollowing || pathData == null) return;

            var points = pathData.Points;
            if (points.Count == 0) return;

            if (_currentIndex >= points.Count)
            {
                if (loopPath)
                    _currentIndex = 0;
                else
                {
                    _isFollowing = false;
                    Debug.Log("[LocalPathFollower] Reached end of path.");
                    return;
                }
            }

            Vector3 target = points[_currentIndex];
            if (_anchorManager != null && _anchorManager.AnchorTransform != null)
                target += _anchorManager.AnchorTransform.position;

            Transform carTransform = GetCarTransform();
            Vector3 currentPos = carTransform.position;
            float carY = currentPos.y;

            Vector3 targetFlat = new Vector3(target.x, carY, target.z);
            Vector3 currentFlat = new Vector3(currentPos.x, carY, currentPos.z);

            float dist = Vector3.Distance(currentFlat, targetFlat);
            if (dist < reachThreshold)
            {
                _currentIndex++;
                return;
            }

            Vector3 direction = (targetFlat - currentFlat).normalized;
            Vector3 newPos = currentFlat + direction * speed * Time.deltaTime;
            newPos.y = carY;

            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                virtualCar.UpdatePose(newPos, targetRot);
            }
        }
    }
}
