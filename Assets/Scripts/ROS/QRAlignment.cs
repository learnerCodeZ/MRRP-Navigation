using UnityEngine;
using Microsoft.MixedReality.OpenXR;
using MRReP.ROS;

namespace MRReP.UI
{
    /// <summary>
    /// QR 码坐标对齐：检测车上 QR 码 → 结合 amcl_pose → 自动建 Unity↔map 变换。
    ///
    /// 流程：
    /// 1. HL2 看向车上 QR 码 → ARMarkerManager 检测到 → 拿 QR 在 Unity 的位姿
    /// 2. 从 ROS amcl_pose 拿小车在 map 的位姿
    /// 3. 两者一减 → Unity↔map 的平移 + 旋转变换
    /// 4. PathSender 发送路径时，每个点 × 变换 → 正确的 map 坐标
    ///
    /// 用法：挂到场景激活物体上，配好 PathSender + QR偏移。
    /// 戴头显看一眼车上的 QR → Console 打印"已对齐" → 之后画路径自动对齐。
    /// </summary>
    public class QRAlignment : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private PathSender pathSender;

        [Header("QR 到 base_link 的偏移（用尺子量，固定值）")]
        [Tooltip("QR 中心相对 base_link 的位置（ROS 坐标系：x=前, y=左, z=上）")]
        [SerializeField] private float qrOffsetForward = 0.0f;   // QR 在 base_link 前方多少米
        [SerializeField] private float qrOffsetLeft = 0.0f;      // QR 在 base_link 左侧多少米
        [SerializeField] private float qrOffsetUp = 0.15f;       // QR 在 base_link 上方多少米（车顶）

        [Header("调试")]
        [SerializeField] private bool debugLog = true;

        private bool _aligned = false;

        private void Update()
        {
            if (_aligned) return;
            if (pathSender == null || !pathSender.HaveCarPose) return;

            // 从 ARMarkerManager 查找 QR 码
            var markerManager = ARMarkerManager.Instance;
            if (markerManager == null) return;

            foreach (var marker in markerManager.trackables)
            {
                if (marker == null) continue;
                if (marker.markerType != ARMarkerType.QRCode) continue;

                // 拿到 QR 在 Unity 的位姿
                Vector3 qrUnityPos = marker.transform.position;
                Quaternion qrUnityRot = marker.transform.rotation;

                if (debugLog)
                    Debug.Log($"[QRAlignment] QR 检测到！Unity 位姿: pos={qrUnityPos}, rot={qrUnityRot.eulerAngles}");

                ComputeAlignment(qrUnityPos, qrUnityRot);
                _aligned = true;
                return; // 对齐一次就够
            }
        }

        /// <summary>
        /// 计算 Unity↔map 变换并存入 PathSender。
        ///
        /// 原理：
        ///   QR 在 Unity 空间的位姿 → CoordinateConverter → ROS 轴的位姿 (qrRosPos)
        ///   QR 在 map 空间的位姿 = 车位姿(amcl_pose) + QR 相对 base_link 的偏移(旋转到车头方向)
        ///   变换 = map 位姿 - ROS 轴位姿
        /// </summary>
        private void ComputeAlignment(Vector3 qrUnityPos, Quaternion qrUnityRot)
        {
            // 1. QR 在 Unity→ROS 轴变换后的坐标
            Vector3 qrRosPos = CoordinateConverter.UnityToROS(qrUnityPos);

            // 2. QR 在 map 中的位置 = 车位姿 + QR 偏移（偏移要旋转到车头方向）
            //    车头方向从 amcl_pose 的 orientation 来
            float carMapX = pathSender.CarMapX;
            float carMapY = pathSender.CarMapY;
            float carYaw = pathSender.CarMapYaw; // 弧度

            // QR 相对 base_link 的偏移，旋转到车头方向后加到车位姿
            float cosY = Mathf.Cos(carYaw), sinY = Mathf.Sin(carYaw);
            float qrMapX = carMapX + cosY * qrOffsetForward - sinY * qrOffsetLeft;
            float qrMapY = carMapY + sinY * qrOffsetForward + cosY * qrOffsetLeft;

            // 3. 平移偏移 = QR 在 map 的位置 - QR 在 ROS 轴的位置
            float offsetX = qrMapX - qrRosPos.x;
            float offsetY = qrMapY - qrRosPos.y;

            // 4. 旋转偏移 = 车头朝向(map) - QR 法线朝向(ROS 轴)
            //    QR 的"朝前"方向 = QR 平面法线 = qrUnityRot * Vector3.forward
            //    转 ROS 轴后算 yaw
            Vector3 qrForwardUnity = qrUnityRot * Vector3.forward;
            Vector3 qrForwardRos = CoordinateConverter.UnityToROS(qrForwardUnity);
            float qrAngleRos = Mathf.Atan2(qrForwardRos.y, qrForwardRos.x); // QR 法线在 ROS 轴的 yaw
            float rotationOffset = carYaw - qrAngleRos; // 旋转修正

            // 5. 存入 PathSender
            pathSender.SetAlignment(offsetX, offsetY, rotationOffset);

            if (debugLog)
            {
                Debug.Log($"[QRAlignment] ✅ 已对齐！");
                Debug.Log($"  车位姿 map: ({carMapX:F2}, {carMapY:F2}), yaw={carYaw * Mathf.Rad2Deg:F1}°");
                Debug.Log($"  QR Unity: ({qrUnityPos.x:F2}, {qrUnityPos.y:F2}, {qrUnityPos.z:F2})");
                Debug.Log($"  QR ROS轴: ({qrRosPos.x:F2}, {qrRosPos.y:F2})");
                Debug.Log($"  QR map: ({qrMapX:F2}, {qrMapY:F2})");
                Debug.Log($"  平移偏移: ({offsetX:F2}, {offsetY:F2})");
                Debug.Log($"  旋转偏移: {rotationOffset * Mathf.Rad2Deg:F1}°");
            }
        }

        /// <summary>重置对齐（下次可重新扫 QR）</summary>
        public void ResetAlignment()
        {
            _aligned = false;
            if (pathSender != null) pathSender.SetAlignment(0, 0, 0);
            Debug.Log("[QRAlignment] 已重置，重新扫 QR 即可对齐");
        }
    }
}
