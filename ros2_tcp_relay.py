#!/usr/bin/env python3
"""
ROS2 → TCP JPEG 中继（事件驱动版）
订阅 /left_camera/image/image (sensor_msgs/Image)
→ JPEG 编码 → TCP 推流 (4字节长度头 + JPEG数据)

每来一帧新图像才发送一次，不空转不重发旧帧。

用法:
  python3 ros2_tcp_relay.py --topic /left_camera/image/image --port 9001 --quality 80
"""
import argparse
import struct
import socket
import threading
import cv2
import numpy as np
import rclpy
from rclpy.node import Node
from sensor_msgs.msg import Image
from cv_bridge import CvBridge


class ImageTcpRelay(Node):
    def __init__(self, topic, listen_port, jpeg_quality):
        super().__init__("image_tcp_relay")
        self.bridge = CvBridge()
        self.jpeg_quality = jpeg_quality
        self.latest_frame = None
        self.frame_lock = threading.Lock()
        # 新帧事件：有新帧时设置，发送线程发送后清除
        self.frame_event = threading.Event()

        # 启动 TCP 服务器（子线程）
        self.tcp_thread = threading.Thread(target=self._tcp_server, args=(listen_port,), daemon=True)
        self.tcp_thread.start()
        self.get_logger().info(f"TCP server started on 0.0.0.0:{listen_port}")

        # 订阅相机话题
        self.sub = self.create_subscription(Image, topic, self._on_image, 10)
        self.get_logger().info(f"Subscribed to {topic}")

    def _on_image(self, msg):
        """有新帧到达时：编码 JPEG + 通知发送线程"""
        try:
            cv_img = self.bridge.imgmsg_to_cv2(msg, desired_encoding="bgr8")
            _, jpeg = cv2.imencode(".jpg", cv_img, [
                cv2.IMWRITE_JPEG_QUALITY, self.jpeg_quality
            ])
            with self.frame_lock:
                self.latest_frame = jpeg.tobytes()
            self.frame_event.set()  # 通知发送线程有新帧
        except Exception as e:
            self.get_logger().warn(f"Frame encode error: {e}")

    def _tcp_server(self, port):
        srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        srv.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        srv.bind(("0.0.0.0", port))
        srv.listen(1)
        while rclpy.ok():
            try:
                client, addr = srv.accept()
                self.get_logger().info(f"Qt client connected: {addr}")
                self._send_loop(client)
            except Exception as e:
                self.get_logger().error(f"TCP error: {e}")

    def _send_loop(self, client):
        """发送循环：等待新帧事件 → 发送最新帧 → 继续等待"""
        while rclpy.ok():
            try:
                # 等待新帧（最长等 1 秒，用于检测断开）
                if not self.frame_event.wait(timeout=1.0):
                    continue
                self.frame_event.clear()

                with self.frame_lock:
                    jpeg = self.latest_frame
                if jpeg is not None:
                    header = struct.pack(">I", len(jpeg))
                    client.sendall(header + jpeg)
            except (BrokenPipeError, ConnectionResetError, OSError):
                break
        client.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--topic", default="/left_camera/image/image")
    parser.add_argument("--port", type=int, default=9001)
    parser.add_argument("--quality", type=int, default=80)
    args = parser.parse_args()

    rclpy.init()
    node = ImageTcpRelay(args.topic, args.port, args.quality)
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    main()
