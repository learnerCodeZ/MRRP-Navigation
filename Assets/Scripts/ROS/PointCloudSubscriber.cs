using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

namespace MRReP.ROS
{
    /// <summary>
    /// HL2 点云接收 + 渲染（HL2 点云透视计划 H1，含对齐 H2、穿墙 H3）。
    /// 订阅 /d435i/cloud_map（PointCloud2，map 帧），逐点用 PathSender.MapPointToUnity
    /// 变到 Unity 世界坐标，用 ParticleSystem 渲染成小色块；材质用 AlwaysOnTop → 穿墙可见。
    ///
    /// 渲染：ParticleSystem（billboard 色块，HL2 上比 Mesh Points 稳、可见）。
    /// 对齐：复用信标那套（QR/WebRop/手动）→ 没校准时退化成 ROSToUnity（点云会出现但位置飘）。
    /// 用法：挂到一个空物体上（自带 ParticleSystem），PathSender 拖进来，pointMaterial 赋 AlwaysOnTop（粉色）。
    /// </summary>
    public class PointCloudSubscriber : MonoBehaviour
    {
        [SerializeField] private string topicName = "/d435i/cloud_map";
        [SerializeField] private PathSender pathSender;          // 对齐 map→Unity（同信标）
        [SerializeField] private Material pointMaterial;         // AlwaysOnTop 材质（粉色）
        [SerializeField] private float pointSize = 0.03f;        // 色块大小（米）
        [SerializeField] private int maxPoints = 8000;
        [SerializeField] private Color pointColor = new Color(1f, 0.27f, 0.70f, 0.9f); // 粉色，和 WebRop 一致

        private ParticleSystem _ps;
        private ParticleSystem.Particle[] _particles;

        private void Start()
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) _ps = gameObject.AddComponent<ParticleSystem>();

            var main = _ps.main;
            main.maxParticles = maxPoints;
            main.startLifetime = 999999f;          // 不让粒子自然消亡，由 SetParticles 整批覆盖
            main.startSize = pointSize;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;
            var em = _ps.emission; em.enabled = false;   // 不自动喷射，全靠 SetParticles

            var ren = GetComponent<ParticleSystemRenderer>();
            if (pointMaterial != null && ren != null) ren.material = pointMaterial;

            _particles = new ParticleSystem.Particle[maxPoints];
            for (int i = 0; i < maxPoints; i++)
            {
                _particles[i].startSize = pointSize;
                _particles[i].startColor = pointColor;
                _particles[i].remainingLifetime = 999999f;
            }

            var ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<PointCloud2Msg>(topicName, OnCloud);
            Debug.Log("[PointCloudSubscriber] 已订阅 " + topicName);
        }

        private void OnCloud(PointCloud2Msg msg)
        {
            if (msg.data == null || msg.data.Length == 0) return;

            // 找 x/y/z 在每个点里的字节偏移（PointCloud2 标准）
            int ox = 0, oy = 4, oz = 8;
            if (msg.fields != null)
            {
                for (int i = 0; i < msg.fields.Length; i++)
                {
                    var f = msg.fields[i];
                    if (f.name == "x") ox = (int)f.offset;
                    else if (f.name == "y") oy = (int)f.offset;
                    else if (f.name == "z") oz = (int)f.offset;
                }
            }
            int pointStep = msg.point_step > 0 ? (int)msg.point_step : 12;
            byte[] data = msg.data;

            long n = (long)msg.width * msg.height;   // uint*uint → long 防溢出
            if (n <= 0) n = data.Length / pointStep;

            int count = 0;
            for (long i = 0; i < n && count < maxPoints; i++)
            {
                int b = (int)(i * pointStep);
                if (b + oz + 4 > data.Length) break;

                float x = System.BitConverter.ToSingle(data, b + ox);
                float y = System.BitConverter.ToSingle(data, b + oy);
                float z = System.BitConverter.ToSingle(data, b + oz);
                if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) ||
                    float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(z)) continue;

                // map 点 → Unity 世界坐标（用对齐；退化时纯轴变换）
                Vector3 u;
                if (pathSender != null && pathSender.MapPointToUnity(x, y, out u))
                    u.y += z;   // ROS z(高度) → Unity y(向上)
                else
                    u = CoordinateConverter.ROSToUnity(new Vector3(x, y, z));

                _particles[count].position = u;
                _particles[count].startSize = pointSize;
                _particles[count].startColor = pointColor;
                _particles[count].remainingLifetime = 999999f;
                count++;
            }

            if (count > 0) _ps.SetParticles(_particles, count);
        }
    }
}
