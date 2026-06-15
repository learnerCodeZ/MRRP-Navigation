using System.Collections.Generic;
using UnityEngine;

namespace MRReP.Path
{
    public class PathData : MonoBehaviour
    {
        [SerializeField] private float sampleDistanceThreshold = 0.05f;

        private List<Vector3> _pathPoints = new List<Vector3>();
        private Transform _anchorTransform;

        public List<Vector3> Points => _pathPoints;
        public int Count => _pathPoints.Count;
        public float SampleDistanceThreshold => sampleDistanceThreshold;

        public void SetAnchor(Transform anchor)
        {
            _anchorTransform = anchor;
        }

        public void AddPoint(Vector3 worldPosition)
        {
            if (_pathPoints.Count == 0 ||
                Vector3.Distance(_pathPoints[_pathPoints.Count - 1], worldPosition) >= sampleDistanceThreshold)
            {
                _pathPoints.Add(worldPosition);
            }
        }

        public void Clear()
        {
            _pathPoints.Clear();
        }

        public List<Vector3> GetRelativePoints()
        {
            if (_anchorTransform == null)
                return new List<Vector3>(_pathPoints);

            var relative = new List<Vector3>(_pathPoints.Count);
            foreach (var point in _pathPoints)
            {
                relative.Add(point - _anchorTransform.position);
            }
            return relative;
        }
    }
}
