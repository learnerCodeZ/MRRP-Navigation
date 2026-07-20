using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Std;

namespace MRReP.ROS
{
    public class PathSender : MonoBehaviour
    {
        [SerializeField] private string topicName = "/hrp_path";
        [SerializeField] private string frameId = "map";
        [SerializeField] private string carPoseTopic = "/amcl_pose";
        // 方案A(测试用)：路径以"车当前位姿"为基准——画的形状盖到车位姿上
        //   起点=车，方向随车头。画"朝前"的形状 → 路径从车头方向延伸。
        //   关闭则退回绝对坐标(落 map 原点)。真机用 QR 对齐(Phase 10)时关掉此项。
        [SerializeField] private bool carRelative = true;

        private ROSConnection _rosConnection;

        // 小车当前 map 位姿(来自 /amcl_pose)
        private bool _haveCarPose = false;
        private double _carX, _carY;
        private double _cosYaw = 1.0, _sinYaw = 0.0;

        private void Start()
        {
            _rosConnection = ROSConnection.GetOrCreateInstance();
            // nav_msgs/Path：机器人侧 hrp_follower_node 订阅的是 Path
            _rosConnection.RegisterPublisher<PathMsg>(topicName);
            if (carRelative)
                _rosConnection.Subscribe<PoseWithCovarianceStampedMsg>(carPoseTopic, OnCarPose);
        }

        private void OnCarPose(PoseWithCovarianceStampedMsg msg)
        {
            _carX = msg.pose.pose.position.x;
            _carY = msg.pose.pose.position.y;
            double yaw = YawFromQuaternion(msg.pose.pose.orientation);
            _cosYaw = System.Math.Cos(yaw);
            _sinYaw = System.Math.Sin(yaw);
            _haveCarPose = true;
        }

        public void SendPath(Path.PathData pathData)
        {
            if (pathData == null || pathData.Count == 0)
            {
                Debug.LogWarning("[PathSender] No path points to send.");
                return;
            }

            // 原始手绘点(Unity) → ROS 轴向
            var rosPoints = CoordinateConverter.ConvertPathToROS(pathData.Points);

            // 以第一个点为形状原点(路径起点)：整体相对起点，再旋转到车头方向 + 平移到车位姿
            double o0x = rosPoints[0].x, o0y = rosPoints[0].y;
            bool useCarRelative = carRelative && _haveCarPose;

            var poses = new PoseStampedMsg[rosPoints.Length];
            for (int i = 0; i < rosPoints.Length; i++)
            {
                double mx, my;
                if (useCarRelative)
                {
                    double ox = rosPoints[i].x - o0x;  // 相对起点的形状
                    double oy = rosPoints[i].y - o0y;
                    mx = _carX + _cosYaw * ox - _sinYaw * oy;  // 旋转(随车头) + 平移(到车位姿)
                    my = _carY + _sinYaw * ox + _cosYaw * oy;
                }
                else
                {
                    mx = rosPoints[i].x;  // fallback：绝对(落 map 原点)
                    my = rosPoints[i].y;
                }

                poses[i] = new PoseStampedMsg
                {
                    header = new HeaderMsg { frame_id = frameId },
                    pose = new PoseMsg(
                        new PointMsg(mx, my, 0.0),
                        new QuaternionMsg(0, 0, 0, 1))
                };
            }

            var message = new PathMsg
            {
                header = new HeaderMsg { frame_id = frameId },
                poses = poses
            };
            _rosConnection.Publish(topicName, message);

            Debug.Log($"[PathSender] Sent {poses.Length} points to {topicName} (frame={frameId}, carRelative={useCarRelative})");
        }

        private static double YawFromQuaternion(QuaternionMsg q)
        {
            // 2D yaw (绕 Z) 从四元数提取
            return System.Math.Atan2(2.0 * (q.w * q.z + q.x * q.y),
                                     1.0 - 2.0 * (q.y * q.y + q.z * q.z));
        }
    }
}
