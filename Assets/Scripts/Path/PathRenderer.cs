using System.Collections.Generic;
using UnityEngine;

namespace MRReP.Path
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private Material pathMaterial;
        [SerializeField] private Color pathColor = new Color(0.298f, 0.933f, 0.918f, 1.0f); // #4DEEEA（加亮：alpha 1.0）
        [SerializeField] private float lineWidth = 0.015f;
        [SerializeField] private float sphereRadius = 0.015f;   // 小球：1.5cm，密集
        [SerializeField] private float emissionIntensity = 2.0f; // 加亮：0.3→2.0，强发光

        private LineRenderer _lineRenderer;
        private List<GameObject> _sphereMarkers = new List<GameObject>();
        private int _lastPointCount = -1;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.useWorldSpace = true;

            if (pathMaterial != null)
            {
                _lineRenderer.material = pathMaterial;
            }
            else
            {
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = pathColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", pathColor * emissionIntensity);
                _lineRenderer.material = mat;
            }
            _lineRenderer.startColor = pathColor;
            _lineRenderer.endColor = pathColor;
        }

        private void Update()
        {
            if (pathData.Count != _lastPointCount)
            {
                UpdateLineRenderer();
                _lastPointCount = pathData.Count;
            }
        }

        private void UpdateLineRenderer()
        {
            var points = pathData.Points;
            int currentCount = points.Count;

            _lineRenderer.positionCount = currentCount;
            for (int i = 0; i < currentCount; i++)
            {
                _lineRenderer.SetPosition(i, points[i]);
            }

            while (_sphereMarkers.Count < currentCount)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = Vector3.one * sphereRadius * 2f;
                sphere.transform.parent = transform;

                var renderer = sphere.GetComponent<Renderer>();
                var mat = pathMaterial != null
                    ? new Material(pathMaterial)
                    : new Material(Shader.Find("Sprites/Default"));
                mat.color = pathColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", pathColor * emissionIntensity);
                renderer.material = mat;

                Destroy(sphere.GetComponent<Collider>());
                _sphereMarkers.Add(sphere);
            }

            for (int i = 0; i < _sphereMarkers.Count; i++)
            {
                if (i < currentCount)
                {
                    _sphereMarkers[i].SetActive(true);
                    _sphereMarkers[i].transform.position = points[i];
                }
                else
                {
                    Destroy(_sphereMarkers[i]);
                }
            }

            if (_sphereMarkers.Count > currentCount)
                _sphereMarkers.RemoveRange(currentCount, _sphereMarkers.Count - currentCount);
        }

        public void ClearRenderers()
        {
            _lineRenderer.positionCount = 0;

            foreach (var sphere in _sphereMarkers)
            {
                Destroy(sphere);
            }
            _sphereMarkers.Clear();
        }
    }
}
