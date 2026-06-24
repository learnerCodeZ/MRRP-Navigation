using UnityEngine;
using MRReP.Path;

namespace MRReP.Robot
{
    public class LocalPathFollower : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private float speed = 0.5f;
        [SerializeField] private float lookaheadDistance = 0.3f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float reachThreshold = 0.08f;
        [SerializeField] private bool loopPath = false;

        private int _nearestIndex;
        private bool _isFollowing;

        public bool IsFollowing => _isFollowing;

        private void Awake()
        {
            if (pathData == null) pathData = FindObjectOfType<PathData>();
        }

        private static float XZDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
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

            Vector3 p0 = pathData.Points[0];
            transform.position = new Vector3(p0.x, transform.position.y, p0.z);

            if (pathData.Points.Count > 1)
            {
                Vector3 p1 = pathData.Points[1];
                Vector3 dir = new Vector3(p1.x - p0.x, 0, p1.z - p0.z).normalized;
                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            Debug.Log($"[LocalPathFollower] START: {pathData.Count}pts, car={transform.position}, p0={pathData.Points[0]}, pLast={pathData.Points[pathData.Points.Count - 1]}");
        }

        public void StopFollowing()
        {
            _isFollowing = false;
        }

        private void Update()
        {
            if (!_isFollowing || pathData == null || pathData.Count == 0) return;

            Vector3 currentPos = transform.position;

            Vector3 lastPt = pathData.Points[pathData.Points.Count - 1];
            float distToEnd = XZDist(currentPos, lastPt);

            if (distToEnd < reachThreshold)
            {
                if (loopPath) { _nearestIndex = 0; }
                else { _isFollowing = false; Debug.Log("[LocalPathFollower] Goal reached!"); return; }
            }

            _nearestIndex = FindNearestIndex(currentPos);
            int targetIdx = FindLookaheadIndex(currentPos);

            Vector3 target = new Vector3(pathData.Points[targetIdx].x, currentPos.y, pathData.Points[targetIdx].z);

            Vector3 direction = (target - currentPos).normalized;
            if (direction == Vector3.zero) return;

            float angleDiff = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            float turnFactor = Mathf.Clamp(Mathf.Abs(angleDiff) / 90f, 0f, 1f);
            float currentSpeed = speed * (1f - 0.6f * turnFactor);

            transform.position = currentPos + direction * currentSpeed * Time.deltaTime;

            Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
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
                float dist = XZDist(pos, points[i]);
                if (dist < minDist) { minDist = dist; nearest = i; }
            }
            return nearest;
        }

        private int FindLookaheadIndex(Vector3 pos)
        {
            var points = pathData.Points;
            for (int i = _nearestIndex; i < points.Count; i++)
            {
                if (XZDist(pos, points[i]) >= lookaheadDistance)
                    return i;
            }
            return points.Count - 1;
        }
    }
}
