using UnityEngine;

namespace MRReP.Robot
{
    public class VirtualCarController : MonoBehaviour
    {
        [SerializeField] private Transform carModel;
        [SerializeField] private bool smoothMovement = true;
        [SerializeField] private float positionLerpSpeed = 5f;
        [SerializeField] private float rotationLerpSpeed = 5f;

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private bool _hasNewPose;

        public Transform CarModel
        {
            get
            {
                if (carModel != null) return carModel;
                var child = transform.Find("CarModel");
                if (child != null) return child;
                return transform;
            }
        }

        public void UpdatePose(Vector3 position, Quaternion rotation)
        {
            _targetPosition = position;
            _targetRotation = rotation;
            _hasNewPose = true;
        }

        public void StopExternalControl()
        {
            _hasNewPose = false;
        }

        private void Update()
        {
            if (!_hasNewPose) return;

            Transform target = CarModel;

            if (smoothMovement)
            {
                target.position = Vector3.Lerp(target.position, _targetPosition, Time.deltaTime * positionLerpSpeed);
                target.rotation = Quaternion.Slerp(target.rotation, _targetRotation, Time.deltaTime * rotationLerpSpeed);
            }
            else
            {
                target.position = _targetPosition;
                target.rotation = _targetRotation;
            }
        }
    }
}
