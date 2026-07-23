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
        [SerializeField] private bool carRelative = false;

        [Header("坐标对齐偏移（carRelative=false 时生效）")]
        [Tooltip("路径坐标 + 此偏移 = 正确的 map 坐标。每次 HL2 启动后需重新标定（HL2 原点会变）。")]
        [SerializeField] private float mapOffsetX = 0f;
        [SerializeField] private float mapOffsetY = 0f;
        [Tooltip("旋转修正（度），路径整体旋转此角度。")]
        [SerializeField] private float mapOffsetYawDeg = 0f;

        private ROSConnection _rosConnection;

        // 小车当前 map 位姿(来自 /amcl_pose)
        private bool _haveCarPose = false;
        private double _carX, _carY;
        private double _carYaw = 0.0;
        private double _cosYaw = 1.0, _sinYaw = 0.0;

        // QR 对齐变换（由 QRAlignment 脚本设置）
        private bool _useQRAlignment = false;
        private double _alignOffsetX = 0.0;
        private double _alignOffsetY = 0.0;
        private double _alignRotation = 0.0; // 弧度
        private double _alignCosR = 1.0, _alignSinR = 0.0;

        // 公开属性（QRAlignment 用）
        public bool HaveCarPose => _haveCarPose;
        public float CarMapX => (float)_carX;
        public float CarMapY => (float)_carY;
        public float CarMapYaw => (float)_carYaw;

        private void Start()
        {
            _rosConnection = ROSConnection.GetOrCreateInstance();
            _rosConnection.RegisterPublisher<PathMsg>(topicName);
            // 始终订阅 amcl_pose（QR 对齐需要车位姿）
            _rosConnection.Subscribe<PoseWithCovarianceStampedMsg>(carPoseTopic, OnCarPose);
        }

        /// <summary>由 QRAlignment 调用：设置 Unity↔map 对齐变换</summary>
        public void SetAlignment(float offsetX, float offsetY, float rotationRad)
        {
            _useQRAlignment = true;
            _alignOffsetX = offsetX;
            _alignOffsetY = offsetY;
            _alignRotation = rotationRad;
            _alignCosR = System.Math.Cos(rotationRad);
            _alignSinR = System.Math.Sin(rotationRad);
            Debug.Log($"[PathSender] QR 对齐已设置: offset=({offsetX:F2},{offsetY:F2}), rot={rotationRad * 180.0 / System.Math.PI:F1}°");
        }

        private void OnCarPose(PoseWithCovarianceStampedMsg msg)
        {
            _carX = msg.pose.pose.position.x;
            _carY = msg.pose.pose.position.y;
            _carYaw = YawFromQuaternion(msg.pose.pose.orientation);
            _cosYaw = System.Math.Cos(_carYaw);
            _sinYaw = System.Math.Sin(_carYaw);
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
                    // 绝对坐标 + QR 对齐变换（优先）或手动偏移（兜底）
                    double rx = rosPoints[i].x;
                    double ry = rosPoints[i].y;

                    if (_useQRAlignment)
                    {
                        // QR 对齐：先旋转再平移
                        double tx = _alignCosR * rx - _alignSinR * ry;
                        double ty = _alignSinR * rx + _alignCosR * ry;
                        mx = tx + _alignOffsetX;
                        my = ty + _alignOffsetY;
                    }
                    else
                    {
                        // 手动偏移兜底
                        if (mapOffsetYawDeg != 0f)
                        {
                            double yaw = mapOffsetYawDeg * System.Math.PI / 180.0;
                            double c = System.Math.Cos(yaw), s = System.Math.Sin(yaw);
                            double t2x = c * rx - s * ry;
                            double t2y = s * rx + c * ry;
                            rx = t2x; ry = t2y;
                        }
                        mx = rx + mapOffsetX;
                        my = ry + mapOffsetY;
                    }
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

            Debug.Log($"[PathSender] Sent {poses.Length} points to {topicName} (frame={frameId}, carRelative={useCarRelative}, qrAligned={_useQRAlignment})");
        }

        private static double YawFromQuaternion(QuaternionMsg q)
        {
            // 2D yaw (绕 Z) 从四元数提取
            return System.Math.Atan2(2.0 * (q.w * q.z + q.x * q.y),
                                     1.0 - 2.0 * (q.y * q.y + q.z * q.z));
        }
    }
}
