using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace MRReP.ROS
{
    public class ROSConnectionManager : MonoBehaviour
    {
        [SerializeField] private string rosIPAddress = "192.168.1.100";
        [SerializeField] private int rosPort = 10000;

        private void Awake()
        {
            var ros = ROSConnection.GetOrCreateInstance();
            ros.RosIPAddress = rosIPAddress;
            ros.RosPort = rosPort;
        }
    }
}
