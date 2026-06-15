using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

namespace MRReP.ROS
{
    public class OdomSubscriber : MonoBehaviour
    {
        [SerializeField] private string topicName = "/odom";
        [SerializeField] private Robot.VirtualCarController virtualCar;
        [SerializeField] private Anchor.SpatialAnchorManager anchorManager;

        private void Start()
        {
            var ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<OdometryMsg>(topicName, OnOdomReceived);
        }

        private void OnOdomReceived(OdometryMsg message)
        {
            var rosPosition = new Vector3(
                (float)message.pose.pose.position.x,
                (float)message.pose.pose.position.y,
                (float)message.pose.pose.position.z
            );

            var rosRotation = new Quaternion(
                (float)message.pose.pose.orientation.x,
                (float)message.pose.pose.orientation.y,
                (float)message.pose.pose.orientation.z,
                (float)message.pose.pose.orientation.w
            );

            var unityPosition = CoordinateConverter.ROSToUnity(rosPosition);
            var unityRotation = CoordinateConverter.ROSToUnity(rosRotation);

            if (anchorManager != null && anchorManager.AnchorTransform != null)
            {
                unityPosition += anchorManager.AnchorTransform.position;
            }

            if (virtualCar != null)
            {
                virtualCar.UpdatePose(unityPosition, unityRotation);
            }
        }
    }
}
