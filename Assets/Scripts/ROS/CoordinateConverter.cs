using UnityEngine;

namespace MRReP.ROS
{
    public static class CoordinateConverter
    {
        public static Vector3 UnityToROS(Vector3 unityPoint)
        {
            return new Vector3(unityPoint.z, -unityPoint.x, unityPoint.y);
        }

        public static Vector3 ROSToUnity(Vector3 rosPoint)
        {
            return new Vector3(-rosPoint.y, rosPoint.z, rosPoint.x);
        }

        public static Quaternion UnityToROS(Quaternion unityQuat)
        {
            return new Quaternion(unityQuat.z, -unityQuat.x, unityQuat.y, -unityQuat.w);
        }

        public static Quaternion ROSToUnity(Quaternion rosQuat)
        {
            return new Quaternion(-rosQuat.y, rosQuat.z, rosQuat.x, -rosQuat.w);
        }

        public static Vector3[] ConvertPathToROS(System.Collections.Generic.List<Vector3> unityPoints)
        {
            var rosPoints = new Vector3[unityPoints.Count];
            for (int i = 0; i < unityPoints.Count; i++)
            {
                rosPoints[i] = UnityToROS(unityPoints[i]);
            }
            return rosPoints;
        }
    }
}
