using System.Collections.Generic;
using UnityEngine;

namespace MRReP.Path
{
    [RequireComponent(typeof(LineRenderer))]
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private PathData pathData;
        [SerializeField] private Material pathMaterial;
        [SerializeField] private Color pathColor = new Color(0.2f, 0.6f, 1f, 0.7f);
        [SerializeField] private float lineWidth = 0.01f;
        [SerializeField] private float sphereRadius = 0.015f;

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
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
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
                renderer.material = pathMaterial != null
                    ? new Material(pathMaterial)
                    : new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = pathColor;

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
                    _sphereMarkers[i].SetActive(false);
                }
            }
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
