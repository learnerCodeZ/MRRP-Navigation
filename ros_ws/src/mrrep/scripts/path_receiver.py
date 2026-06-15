#!/usr/bin/env python3
import rospy
from geometry_msgs.msg import PoseArray


def path_callback(msg):
    rospy.loginfo("Received {} poses on /hrp_path".format(len(msg.poses)))
    for i, pose in enumerate(msg.poses):
        p = pose.position
        rospy.loginfo("  Pose {}: x={:.3f}, y={:.3f}, z={:.3f}".format(
            i, p.x, p.y, p.z))


def main():
    rospy.init_node("path_receiver")
    rospy.Subscriber("/hrp_path", PoseArray, path_callback, queue_size=10)
    rospy.loginfo("path_receiver: Listening on /hrp_path ...")
    rospy.spin()


if __name__ == "__main__":
    main()
