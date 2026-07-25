using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

namespace MRReP.ROS
{
    /// <summary>
    /// 发布 HL2 头部位姿到 /hololens/pose（10Hz）。
    /// WebRop 订阅后在地图上显示 HL2 标记。
    ///
    /// 关键：位置和朝向都用 CoordinateConverter.UnityToROS —— 和 PathSender 发路径
    /// 用的是同一套 Unity→ROS 变换。这样 HL2 标记的朝向与"画出来的路径方向"同坐标系，
    /// 也和 WebRop 上小车(AMCL)标记的渲染约定一致（见 Scene3D.HololensMarker）。
    /// 不做校准平移——位置仍可能不在正确地点（Step 2 校准），但朝向不再镜像。
    /// </summary>
    public class HololensPosePublisher : MonoBehaviour
    {
        [SerializeField] private string topicName = "/hololens/pose";
        [SerializeField] private string frameId = "map";
        [SerializeField] private float publishInterval = 0.1f; // 10Hz
        // 朝向偏移（度）= Unity 坐标系 ↔ 地图坐标系的固定 yaw 差。
        // 现在用 WebRop 拖拽校准自动吸收这个差（推荐），所以默认 0。
        // 仅当不想用拖拽校准、又需要手动补偿固定偏角时才在这里调。
        [Tooltip("朝向偏移（度）。默认 0——用 WebRop 拖拽校准自动补偿。仅手动兜底时调。")]
        [SerializeField] private float headOffsetDeg = 0f;

        private ROSConnection _ros;
        private float _timer;

        private void Start()
        {
            _ros = ROSConnection.GetOrCreateInstance();
            _ros.RegisterPublisher<PoseStampedMsg>(topicName);
            Debug.Log("[HololensPosePublisher] 已注册发布器: " + topicName);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < publishInterval) return;
            _timer = 0f;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 unityPos = cam.transform.position;

            // 位置：用 (-x, -z) 映射——实测这套"移动方向"才对（向东→地图东，等等）。
            //   注意：位置的正确映射是 (-x,-z)，和朝向用的 UnityToROS 不是同一套，
            //   但位置(平移)和朝向(角度)各自正确即可；平移方向是二维的，校准补不了，必须用这个。
            //   朝向仍走 UnityToROS 四元数 + WebRop 拖拽校准（已验证对）。
            Quaternion rosRot = CoordinateConverter.UnityToROS(cam.transform.rotation);
            float rosYaw = YawOf(rosRot) + headOffsetDeg * Mathf.Deg2Rad;
            float half = rosYaw * 0.5f;

            var msg = new PoseStampedMsg
            {
                header = new HeaderMsg { frame_id = frameId },
                pose = new PoseMsg(
                    new PointMsg(-unityPos.x, -unityPos.z, 0.0),
                    new QuaternionMsg(0, 0, Mathf.Sin(half), Mathf.Cos(half)))
            };

            _ros.Send(topicName, msg);
        }

        /// <summary>从 ROS 四元数（Unity Quaternion 布局）提取绕 Z 的 yaw。</summary>
        private static float YawOf(Quaternion q)
        {
            return Mathf.Atan2(2f * (q.w * q.z + q.x * q.y), 1f - 2f * (q.y * q.y + q.z * q.z));
        }
    }
}
