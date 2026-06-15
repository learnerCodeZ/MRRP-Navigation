#!/usr/bin/env python3
import math
import numpy as np
import rospy
from geometry_msgs.msg import PoseArray, Quaternion
from nav_msgs.msg import Odometry
from tf.transformations import quaternion_from_euler


def normalize_angle(angle):
    while angle > math.pi:
        angle -= 2 * math.pi
    while angle < -math.pi:
        angle += 2 * math.pi
    return angle


class PurePursuitController:
    def __init__(self):
        rospy.init_node("pure_pursuit")

        self.path_points = []
        self.path_received = False

        self.x = 0.0
        self.y = 0.0
        self.theta = 0.0

        self.v = 0.3
        self.ld = 0.5
        self.dt = 0.05

        self.odom_pub = rospy.Publisher("/odom", Odometry, queue_size=10)
        rospy.Subscriber("/hrp_path", PoseArray, self.path_callback, queue_size=10)

        rospy.loginfo("PurePursuit: Waiting for path on /hrp_path ...")

    def path_callback(self, msg):
        for pose in msg.poses:
            self.path_points.append((pose.position.x, pose.position.y))
        if not self.path_received and len(self.path_points) > 0:
            self.path_received = True
            self.x = self.path_points[0][0]
            self.y = self.path_points[0][1]
            self.theta = 0.0
            rospy.loginfo("PurePursuit: Path received, {} points".format(
                len(self.path_points)))

    def find_target_point(self):
        if not self.path_points:
            return None

        min_dist = float("inf")
        target = None

        for px, py in self.path_points:
            dx = px - self.x
            dy = py - self.y
            dist = math.sqrt(dx * dx + dy * dy)

            if abs(dist - self.ld) < min_dist:
                min_dist = abs(dist - self.ld)
                target = (px, py)

        return target

    def compute_control(self, target):
        dx = target[0] - self.x
        dy = target[1] - self.y

        local_x = math.cos(self.theta) * dx + math.sin(self.theta) * dy
        local_y = -math.sin(self.theta) * dx + math.cos(self.theta) * dy

        curvature = 2.0 * local_y / (self.ld * self.ld)
        omega = self.v * curvature

        return self.v, omega

    def publish_odom(self):
        odom = Odometry()
        odom.header.stamp = rospy.Time.now()
        odom.header.frame_id = "odom"
        odom.child_frame_id = "base_link"

        odom.pose.pose.position.x = self.x
        odom.pose.pose.position.y = self.y
        odom.pose.pose.position.z = 0.0

        q = quaternion_from_euler(0, 0, self.theta)
        odom.pose.pose.orientation = Quaternion(*q)

        self.odom_pub.publish(odom)

    def run(self):
        rate = rospy.Rate(1.0 / self.dt)

        while not rospy.is_shutdown():
            if not self.path_received:
                rate.sleep()
                continue

            target = self.find_target_point()
            if target is None:
                rate.sleep()
                continue

            dist_to_end = math.sqrt(
                (self.path_points[-1][0] - self.x) ** 2
                + (self.path_points[-1][1] - self.y) ** 2
            )

            if dist_to_end < 0.1:
                rospy.loginfo("PurePursuit: Goal reached!")
                self.publish_odom()
                break

            v, omega = self.compute_control(target)
            self.x += v * math.cos(self.theta) * self.dt
            self.y += v * math.sin(self.theta) * self.dt
            self.theta += omega * self.dt
            self.theta = normalize_angle(self.theta)

            self.publish_odom()
            rate.sleep()


if __name__ == "__main__":
    controller = PurePursuitController()
    controller.run()
