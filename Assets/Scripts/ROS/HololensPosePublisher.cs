using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

namespace MRReP.ROS
{
    /// <summary>
    /// 发布 HL2 头部位姿到 /hololens/pose（10Hz）。
    /// WebRop 订阅后在地图上显示 HL2 标记。
    /// 不做校准变换——发的是 raw Unity→ROS 坐标（位置可能不对，Step 2 校准）。
    /// </summary>
    public class HololensPosePublisher : MonoBehaviour
    {
        [SerializeField] private string topicName = "/hololens/pose";
        [SerializeField] private string frameId = "map";
        [SerializeField] private float publishInterval = 0.1f; // 10Hz

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
            // 直接发 Unity 坐标，取反让 WebRop 方向一致
            //   Scene_x = ROS_x = -Unity_x → 东反修正
            //   Scene_z = -ROS_y = -Unity_z → 南反修正
            float rosYaw = -cam.transform.eulerAngles.y * Mathf.Deg2Rad;
            float halfYaw = rosYaw * 0.5f;

            var msg = new PoseStampedMsg
            {
                header = new HeaderMsg { frame_id = frameId },
                pose = new PoseMsg(
                    new PointMsg(-unityPos.x, -unityPos.z, 0.0),
                    new QuaternionMsg(0, 0, Mathf.Sin(halfYaw), Mathf.Cos(halfYaw)))
            };

            _ros.Send(topicName, msg);
        }
    }
}
