using UnityEngine;
using MRReP.Anchor;
using MRReP.Path;

namespace MRReP.Robot
{
    public class LocalPathFollower : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private VirtualCarController virtualCar;
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float lookaheadDistance = 0.3f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float reachThreshold = 0.08f;
        [SerializeField] private bool loopPath = false;

        private int _nearestIndex;
        private bool _isFollowing;
        private SpatialAnchorManager _anchorManager;

        public bool IsFollowing => _isFollowing;

        private Transform GetCarModel()
        {
            if (virtualCar == null) return transform;
            var field = typeof(VirtualCarController).GetField("carModel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var carModel = field.GetValue(virtualCar) as Transform;
                if (carModel != null) return carModel;
            }
            return virtualCar.transform;
        }

        private Vector3 GetWorldPoint(Vector3 localPoint)
        {
            if (_anchorManager != null && _anchorManager.AnchorTransform != null)
                return localPoint + _anchorManager.AnchorTransform.position;
            return localPoint;
        }

        public void StartFollowing()
        {
            if (pathData == null || pathData.Count == 0)
            {
                Debug.LogWarning("[LocalPathFollower] No path to follow.");
                return;
            }

            _nearestIndex = 0;
            _isFollowing = true;
            _anchorManager = FindObjectOfType<SpatialAnchorManager>();

            var carModel = GetCarModel();
            Vector3 startPos = GetWorldPoint(pathData.Points[0]);
            startPos.y = carModel.position.y;
            carModel.position = startPos;

            if (pathData.Points.Count > 1)
            {
                Vector3 next = GetWorldPoint(pathData.Points[1]);
                next.y = startPos.y;
                Vector3 dir = (next - startPos).normalized;
                if (dir != Vector3.zero)
                    carModel.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            Debug.Log($"[LocalPathFollower] Start following {pathData.Count} points");
        }

        public void StopFollowing()
        {
            _isFollowing = false;
        }

        private int FindNearestIndex(Vector3 pos)
        {
            var points = pathData.Points;
            float minDist = float.MaxValue;
            int nearest = _nearestIndex;

            int start = Mathf.Max(0, _nearestIndex - 2);
            int end = Mathf.Min(points.Count - 1, _nearestIndex + 10);

            for (int i = start; i <= end; i++)
            {
                Vector3 wp = GetWorldPoint(points[i]);
                wp.y = pos.y;
                float dist = Vector3.Distance(pos, wp);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = i;
                }
            }

            return nearest;
        }

        private int FindLookaheadIndex(Vector3 pos)
        {
            var points = pathData.Points;

            for (int i = _nearestIndex; i < points.Count; i++)
            {
                Vector3 wp = GetWorldPoint(points[i]);
                wp.y = pos.y;
                float dist = Vector3.Distance(pos, wp);
                if (dist >= lookaheadDistance)
                    return i;
            }

            return points.Count - 1;
        }

        private void Update()
        {
            if (!_isFollowing || pathData == null) return;

            var points = pathData.Points;
            if (points.Count == 0) return;

            var carModel = GetCarModel();
            Vector3 currentPos = carModel.position;
            float carY = currentPos.y;

            Vector3 lastPoint = GetWorldPoint(points[points.Count - 1]);
            lastPoint.y = carY;
            float distToEnd = Vector3.Distance(currentPos, lastPoint);

            if (distToEnd < reachThreshold)
            {
                if (loopPath)
                    _nearestIndex = 0;
                else
                {
                    _isFollowing = false;
                    Debug.Log("[LocalPathFollower] Goal reached!");
                    return;
                }
            }

            _nearestIndex = FindNearestIndex(currentPos);
            int targetIndex = FindLookaheadIndex(currentPos);

            Vector3 target = GetWorldPoint(points[targetIndex]);
            target.y = carY;

            Vector3 currentFlat = new Vector3(currentPos.x, carY, currentPos.z);
            Vector3 direction = (target - currentFlat).normalized;

            if (direction == Vector3.zero) return;

            float angleDiff = Vector3.SignedAngle(carModel.forward, direction, Vector3.up);
            float turnFactor = Mathf.Clamp(Mathf.Abs(angleDiff) / 90f, 0f, 1f);
            float currentSpeed = speed * (1f - 0.6f * turnFactor);

            Vector3 newPos = currentFlat + direction * currentSpeed * Time.deltaTime;
            newPos.y = carY;
            carModel.position = newPos;

            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            carModel.rotation = Quaternion.Slerp(carModel.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }
}
