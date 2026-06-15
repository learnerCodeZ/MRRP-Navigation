using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

namespace MRReP.ROS
{
    public class PathSender : MonoBehaviour
    {
        [SerializeField] private string topicName = "/hrp_path";

        private ROSConnection _rosConnection;

        private void Start()
        {
            _rosConnection = ROSConnection.GetOrCreateInstance();
            _rosConnection.RegisterPublisher<PoseArrayMsg>(topicName);
        }

        public void SendPath(Path.PathData pathData)
        {
            if (pathData == null || pathData.Count == 0)
            {
                Debug.LogWarning("[PathSender] No path points to send.");
                return;
            }

            var relativePoints = pathData.GetRelativePoints();
            var rosPoints = CoordinateConverter.ConvertPathToROS(relativePoints);

            var poses = new PoseMsg[rosPoints.Length];
            for (int i = 0; i < rosPoints.Length; i++)
            {
                poses[i] = new PoseMsg(
                    new PointMsg(rosPoints[i].x, rosPoints[i].y, rosPoints[i].z),
                    new QuaternionMsg(0, 0, 0, 1)
                );
            }

            var message = new PoseArrayMsg { poses = poses };
            _rosConnection.Publish(topicName, message);

            Debug.Log($"[PathSender] Sent {poses.Length} points to {topicName}");
        }
    }
}
