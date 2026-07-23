#ifndef ROS_BRIDGE_SUBSCRIBER_H
#define ROS_BRIDGE_SUBSCRIBER_H

#include <QObject>
#include <QTcpSocket>
#include <QTimer>
#include <QImage>
#include <QMutex>
#include <QJsonObject>
#include <QByteArray>

/// ROS2 rosbridge WebSocket 客户端（QTcpSocket 手写，无需 Qt6::WebSockets）
/// 直接订阅 ROS2 话题（如 /left_camera/image/image），无需 Ubuntu 端额外部署
class RosBridgeSubscriber : public QObject
{
    Q_OBJECT
    Q_PROPERTY(bool connected READ connected NOTIFY connectedChanged)

public:
    explicit RosBridgeSubscriber(QObject *parent = nullptr);

    /// 连接 rosbridge_server (ws://host:port)
    Q_INVOKABLE void connectToRosBridge(const QString &host = "10.36.37.93",
                                        int port = 9090,
                                        const QString &topic = "/left_camera/image/image");
    Q_INVOKABLE void disconnect();
    bool connected() const { return m_connected; }

    /// 获取当前帧 (线程安全)
    QImage currentFrame() const;

signals:
    void connectedChanged();
    void frameUpdated();

private slots:
    void onConnected();
    void onDisconnected();
    void onReadyRead();
    void onError(QAbstractSocket::SocketError error);

private:
    void sendHttpUpgrade();
    void sendWsFrame(const QByteArray &payload);
    void readWsFrame();
    void sendJson(const QJsonObject &obj);
    void subscribe(const QString &topic);
    void processImage(const QJsonObject &msg);

    QTcpSocket *m_socket;
    QTimer *m_reconnectTimer;

    QString m_host;
    int m_port = 9090;
    QString m_topic;

    // WebSocket 解析状态
    QByteArray m_readBuf;
    bool m_wsUpgraded = false;

    // 当前帧
    QImage m_currentFrame;
    mutable QMutex m_frameMutex;

    bool m_connected = false;
    int m_msgId = 0;
    QString m_pendingWsPayload;
};

#endif // ROS_BRIDGE_SUBSCRIBER_H
