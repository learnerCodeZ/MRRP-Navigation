using UnityEngine;
#if WINDOWS_UWP
using Microsoft.MixedReality.Toolkit.Utilities;
#endif

namespace MRReP.Anchor
{
    public class SpatialAnchorManager : MonoBehaviour
    {
        [SerializeField] private Path.PathData pathData;

        private GameObject _anchorObject;

        public Transform AnchorTransform => _anchorObject?.transform;

        private void Start()
        {
            if (pathData == null)
                pathData = FindObjectOfType<Path.PathData>();
            CreateAnchor(transform.position);
            Debug.Log($"[SpatialAnchorManager] Anchor created at {transform.position}, pathData={(pathData != null ? "OK" : "NULL")}");
        }

        public void CreateAnchor(Vector3 worldPosition)
        {
            if (_anchorObject != null)
                Destroy(_anchorObject);

            _anchorObject = new GameObject("SpatialAnchor");
            _anchorObject.transform.position = worldPosition;

            if (pathData != null)
                pathData.SetAnchor(_anchorObject.transform);
        }

        public void CreateAnchorAtCurrentPosition()
        {
            CreateAnchor(transform.position);
        }

        public void ClearAnchor()
        {
            if (_anchorObject != null)
            {
                Destroy(_anchorObject);
                _anchorObject = null;
            }
        }
    }
}
