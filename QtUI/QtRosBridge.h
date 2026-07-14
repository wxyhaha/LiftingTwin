#include <QObject>
#include <QTcpSocket>
#include <QJsonDocument>
#include <QJsonObject>
#include <QTimer>

/// Qt ↔ Unity/ROS IPC 桥接客户端
/// 连接 Unity QtBridge 的 TCP 服务器 (localhost:9000)
class QtRosBridge : public QObject
{
    Q_OBJECT
    Q_PROPERTY(bool connected READ connected NOTIFY connectedChanged)
public:
    explicit QtRosBridge(QObject *parent = nullptr)
        : QObject(parent), m_socket(new QTcpSocket(this)), m_reconnectTimer(new QTimer(this))
    {
        connect(m_socket, &QTcpSocket::readyRead, this, &QtRosBridge::onReadyRead);
        connect(m_socket, &QTcpSocket::connected, this, [this]() {
            qInfo() << "[RosBridge] 已连接到 Unity";
            emit connectedChanged();
            m_reconnectTimer->stop();
        });
        connect(m_socket, &QTcpSocket::disconnected, this, [this]() {
            qInfo() << "[RosBridge] 与 Unity 断开，5秒后重连";
            emit connectedChanged();
            m_reconnectTimer->start();
        });
        // 使用旧式语法连接 error 信号（避免 QOverload 歧义）
        connect(m_socket, SIGNAL(errorOccurred(QAbstractSocket::SocketError)),
                this, SLOT(onError(QAbstractSocket::SocketError)));

        m_reconnectTimer->setInterval(5000);
        connect(m_reconnectTimer, &QTimer::timeout, this, [this]() { connectToUnity(); });
    }

    bool connected() const { return m_socket->state() == QAbstractSocket::ConnectedState; }

    /// 连接到 Unity (localhost:port)
    Q_INVOKABLE void connectToUnity(int port = 9000) {
        m_socket->connectToHost("127.0.0.1", port);
    }

    /// 发布字符串消息到 ROS2
    Q_INVOKABLE void publishString(const QString &topic, const QString &data) {
        QJsonObject cmd;
        cmd["cmd"] = "publish_string";
        cmd["topic"] = topic;
        cmd["data"] = data;
        sendJson(cmd);
    }

    /// 订阅 ROS2 话题
    Q_INVOKABLE void subscribe(const QString &topic) {
        QJsonObject cmd;
        cmd["cmd"] = "subscribe";
        cmd["topic"] = topic;
        sendJson(cmd);
    }

    /// 取消订阅
    Q_INVOKABLE void unsubscribe(const QString &topic) {
        QJsonObject cmd;
        cmd["cmd"] = "unsubscribe";
        cmd["topic"] = topic;
        sendJson(cmd);
    }

signals:
    void connectedChanged();
    void messageReceived(const QString &topic, const QString &data);
    void logReceived(const QString &level, const QString &text);

private slots:
    void onError(QAbstractSocket::SocketError) {
        qWarning() << "[RosBridge] 连接错误:" << m_socket->errorString();
    }

private:
    void sendJson(const QJsonObject &obj) {
        if (m_socket->state() != QAbstractSocket::ConnectedState) return;
        QByteArray data = QJsonDocument(obj).toJson(QJsonDocument::Compact) + "\n";
        m_socket->write(data);
        m_socket->flush();
    }

    void onReadyRead() {
        m_recvBuf.append(m_socket->readAll());
        int nl;
        while ((nl = m_recvBuf.indexOf('\n')) >= 0) {
            QByteArray line = m_recvBuf.left(nl).trimmed();
            m_recvBuf.remove(0, nl + 1);
            if (!line.isEmpty()) handleMessage(line);
        }
    }

    void handleMessage(const QByteArray &json) {
        QJsonDocument doc = QJsonDocument::fromJson(json);
        if (!doc.isObject()) return;
        QJsonObject obj = doc.object();

        QString event = obj["event"].toString();
        if (event == "message") {
            emit messageReceived(obj["topic"].toString(), obj["data"].toString());
        } else if (event == "log") {
            emit logReceived(obj["level"].toString(), obj["text"].toString());
        }
    }

    QTcpSocket *m_socket;
    QTimer *m_reconnectTimer;
    QByteArray m_recvBuf;
};
