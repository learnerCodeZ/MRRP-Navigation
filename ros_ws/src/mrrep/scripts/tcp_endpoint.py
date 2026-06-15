#!/usr/bin/env python3
import rospy
from std_msgs.msg import String
from ros_tcp_endpoint import TcpServer


def main():
    rospy.init_node("mrrep_endpoint")
    tcp_server = TcpServer(DEFAULT_TCP_SERVER_NAME)
    tcp_server.start()
    rospy.spin()


if __name__ == "__main__":
    main()
