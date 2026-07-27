using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using MRReP.Anchor;

namespace MRReP.ROS
{
    /// <summary>
    /// 小车透视信标（HL2 点云透视计划 H0）。
    /// 订阅 /amcl_pose（map 帧，小车真实位姿），把信标物体锚定到小车在 HL2 世界里的真实位置。
    /// 信标的材质用 AlwaysOnTop shader（ZTest Always）→ 小车走到障碍物/墙后、肉眼看不见时，
    /// 信标"穿墙"继续显示，随时知道小车在哪。
    ///
    /// 坐标（H2 对齐）：优先用 PathSender.MapPointToUnity —— 即 QR 扫码 或 WebRop 两点校准
    /// 算出的 map→Unity 逆变换，信标准确贴车；无对齐时退化为 ROSToUnity + 未校准 Anchor（粗略）。
    ///
    /// 用法：把本脚本挂到一个"信标"物体上（信标 = 浮动图标 + 垂直光柱，在 Unity 里搭），
    /// 给信标的材质赋 AlwaysOnTop shader；把 PathSender 拖进来（对齐用），AnchorManager 可选（退化兜底）。
    /// </summary>
    public class CarBeacon : MonoBehaviour
    {
        [SerializeField] private string topicName = "/amcl_pose";
        [SerializeField] private PathSender pathSender;        // ★拿对齐(QR/web)用；没设则退化为未校准
        [SerializeField] private SpatialAnchorManager anchorManager;  // 退化兜底（未校准）
        [Tooltip("信标悬浮在地面上方的高度（米），便于从远处 / 穿墙看到。")]
        [SerializeField] private float hoverHeight = 0.3f;
        [Tooltip("平滑跟随最新位姿；关掉则瞬移。")]
        [SerializeField] private bool smoothFollow = true;
        [SerializeField] private float positionLerpSpeed = 6f;

        private ROSConnection _ros;
        private Vector3 _targetPos;
        private bool _hasPose;

        private void Start()
        {
            _ros = ROSConnection.GetOrCreateInstance();
            _ros.Subscribe<PoseWithCovarianceStampedMsg>(topicName, OnCarPose);
            Debug.Log("[CarBeacon] 已订阅 " + topicName + "（map 帧，信标将锚定到小车真实位置）");
        }

        private void OnCarPose(PoseWithCovarianceStampedMsg msg)
        {
            var p = msg.pose.pose.position;
            Vector3 unityPos;

            // ★优先用对齐（QR 扫码 / WebRop 两点）：map→Unity 逆变换，准确贴车
            bool aligned = pathSender != null && pathSender.MapPointToUnity(p.x, p.y, out unityPos);
            if (!aligned)
            {
                // 退化：无对齐时纯轴变换 + 未校准 Anchor（粗略，仅保证能看到信标）
                var rosPos = new Vector3((float)p.x, (float)p.y, (float)p.z);
                unityPos = CoordinateConverter.ROSToUnity(rosPos);
                if (anchorManager != null && anchorManager.AnchorTransform != null)
                    unityPos += anchorManager.AnchorTransform.position;
            }

            unityPos.y += hoverHeight; // 悬浮一点，远处/穿墙更容易看到
            _targetPos = unityPos;
            _hasPose = true;
        }

        private void Update()
        {
            if (!_hasPose) return;
            if (smoothFollow)
                transform.position = Vector3.Lerp(transform.position, _targetPos, Time.deltaTime * positionLerpSpeed);
            else
                transform.position = _targetPos;
        }
    }
}
