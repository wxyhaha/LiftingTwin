#ifndef CAMERA_STREAM_CLIENT_H
#define CAMERA_STREAM_CLIENT_H

#include <QObject>
#include <QTcpSocket>
#include <QTimer>
#include <QImage>
#include <QPixmap>
#include <QMutex>
#include <QByteArray>

/// 相机 TCP 流客户端
/// 连接 WSL 中 camera_stream_server 的 TCP 端口 9001
/// 接收 JPEG 帧，供 QML 通过 QQuickImageProvider 显示
class CameraStreamClient : public QObject
{
    Q_OBJECT
    Q_PROPERTY(bool connected READ connected NOTIFY connectedChanged)
    Q_PROPERTY(QString cameraName READ cameraName WRITE setCameraName NOTIFY cameraNameChanged)

public:
    explicit CameraStreamClient(QObject *parent = nullptr);

    /// 连接到流服务器
    Q_INVOKABLE void connectToCamera(const QString &host = "127.0.0.1", int port = 9001);
    /// 断开连接
    Q_INVOKABLE void disconnect();
    /// 是否已连接
    bool connected() const { return m_connected; }

    QString cameraName() const { return m_cameraName; }
    void setCameraName(const QString &name);

    /// 获取当前帧 (线程安全)
    QImage currentFrame() const;

signals:
    void connectedChanged();
    void cameraNameChanged();
    void frameUpdated();

private slots:
    void onConnected();
    void onDisconnected();
    void onReadyRead();
    void onError(QAbstractSocket::SocketError error);

private:
    void processFrame(const QByteArray &jpegData);

    QTcpSocket *m_socket;
    QTimer *m_reconnectTimer;

    QByteArray m_recvBuf;
    QImage m_currentFrame;
    mutable QMutex m_frameMutex;

    bool m_connected = false;
    int m_frameCount = 0;

    QString m_cameraName = "Hikvision Camera";
    QString m_host = "127.0.0.1";
    int m_port = 9001;

    enum State { ReadingHeader, ReadingData };
    State m_state = ReadingHeader;
    quint32 m_expectedLength = 0;
};

#endif // CAMERA_STREAM_CLIENT_H
