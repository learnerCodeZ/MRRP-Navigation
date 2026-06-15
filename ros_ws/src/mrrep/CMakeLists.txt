cmake_minimum_required(VERSION 3.0.2)
project(mrrep)

find_package(catkin REQUIRED COMPONENTS
  rospy
  std_msgs
  geometry_msgs
  nav_msgs
  ros_tcp_endpoint
)

catkin_package()

install(PROGRAMS
  scripts/tcp_endpoint.py
  scripts/path_receiver.py
  scripts/pure_pursuit.py
  DESTINATION ${CATKIN_PACKAGE_BIN_DESTINATION}
)
