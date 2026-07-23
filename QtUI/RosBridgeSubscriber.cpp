#include "RosBridgeSubscriber.h"
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonValue>
#include <QDebug>
#include <QNetworkProxy>
#include <QtEndian>
#include <QCryptographicHash>
#include <QRandomGenerator>

// ─── WebSocket 辅助 ───────────────────────────────────────────

/// WebSocket 握手 key（RFC 6455）
static QString makeWsKey()
{
    // 16 字节随机数 → base64
    QByteArray rand(16, 0);
    for (int i = 0; i < rand.size(); i++)
        rand[i] = static_cast<char>(QRandomGenerator::global()->bounded(256));
    return QString::fromLatin1(rand.toBase64());
}

/// 解析 WebSocket 帧。返回 true 表示收到完整文本帧，payload 放在 text 中
/// 只处理 text 帧 (opcode 0x1)，不支持分片
static bool parseWsFrame(const QByteArray &buf, int &used, QString &text)
{
    used = 0;
    if (buf.size() < 2) return false;

    uchar b0 = static_cast<uchar>(buf[0]);
    uchar b1 = static_cast<uchar>(buf[1]);
    bool fin = (b0 & 0x80) != 0;
    int opcode = b0 & 0x0F;
    bool masked = (b1 & 0x80) != 0;
    quint64 payloadLen = b1 & 0x7F;

    int headerLen = 2;
    if (payloadLen == 126) {
        if (buf.size() < 4) return false;
        payloadLen = qFromBigEndian<quint16>(reinterpret_cast<const uchar*>(buf.constData() + 2));
        headerLen = 4;
    } else if (payloadLen == 127) {
        if (buf.size() < 10) return false;
        payloadLen = qFromBigEndian<quint64>(reinterpret_cast<const uchar*>(buf.constData() + 2));
        headerLen = 10;
    }

    int maskLen = masked ? 4 : 0;
    int totalHeader = headerLen + maskLen;
    if (buf.size() < totalHeader + static_cast<int>(payloadLen))
        return false;

    // 只处理 text 帧
    if (opcode != 1) {
        used = totalHeader + static_cast<int>(payloadLen);
        return false;
    }

    const char *payload = buf.constData() + totalHeader;
    int len = static_cast<int>(payloadLen);

    if (masked) {
        const char *mask = buf.constData() + headerLen;
        QByteArray unmasked(len, '\0');
        for (int i = 0; i < len; i++)
            unmasked[i] = payload[i] ^ mask[i % 4];
        text = QString::fromUtf8(unmasked);
    } else {
        text = QString::fromUtf8(payload, len);
    }

    used = totalHeader + len;
    return fin && opcode == 1;
}

// ─── RosBridgeSubscriber ──────────────────────────────────────

RosBridgeSubscriber::RosBridgeSubscriber(QObject *parent)
    : QObject(parent)
    , m_socket(new QTcpSocket(this))
    , m_reconnectTimer(new QTimer(this))
{
    m_socket->setProxy(QNetworkProxy::NoProxy);

    connect(m_socket, &QTcpSocket::connected, this, &RosBridgeSubscriber::onConnected);
    connect(m_socket, &QTcpSocket::disconnected, this, &RosBridgeSubscriber::onDisconnected);
    connect(m_socket, &QTcpSocket::readyRead, this, &RosBridgeSubscriber::onReadyRead);
    connect(m_socket, &QTcpSocket::errorOccurred, this, &RosBridgeSubscriber::onError);

    m_reconnectTimer->setInterval(3000);
    connect(m_reconnectTimer, &QTimer::timeout, this, [this]() {
        if (!m_connected)
            connectToRosBridge(m_host, m_port, m_topic);
    });
}

void RosBridgeSubscriber::connectToRosBridge(const QString &host, int port, const QString &topic)
{
    m_host = host;
    m_port = port;
    m_topic = topic;
    m_wsUpgraded = false;
    m_readBuf.clear();
    m_socket->connectToHost(host, port);
}

void RosBridgeSubscriber::disconnect()
{
    m_reconnectTimer->stop();
    m_socket->disconnectFromHost();
    m_connected = false;
    emit connectedChanged();
}

QImage RosBridgeSubscriber::currentFrame() const
{
    QMutexLocker locker(&m_frameMutex);
    return m_currentFrame;
}

// ── Socket 事件 ───────────────────────────────────────────────

void RosBridgeSubscriber::onConnected()
{
    qInfo() << "[RosBridgeSubscriber] TCP connected to" << m_host << ":" << m_port;
    m_reconnectTimer->stop();
    sendHttpUpgrade();
}

void RosBridgeSubscriber::onDisconnected()
{
    qWarning() << "[RosBridgeSubscriber] Disconnected, reconnecting...";
    m_connected = false;
    m_wsUpgraded = false;
    emit connectedChanged();
    m_reconnectTimer->start();
}

void RosBridgeSubscriber::onReadyRead()
{
    m_readBuf.append(m_socket->readAll());

    if (!m_wsUpgraded) {
        // 检查 HTTP 101 响应
        int idx = m_readBuf.indexOf("\r\n\r\n");
        if (idx >= 0) {
            QString header = QString::fromUtf8(m_readBuf.left(idx));
            if (header.contains("101 Switching Protocols")) {
                m_wsUpgraded = true;
                m_connected = true;
                emit connectedChanged();
                m_readBuf.remove(0, idx + 4);
                qInfo() << "[RosBridgeSubscriber] WebSocket upgraded, subscribing...";
                subscribe(m_topic);
            } else {
                qWarning() << "[RosBridgeSubscriber] Upgrade rejected:" << header;
                m_socket->close();
                return;
            }
        }
        // 还没收到完整 HTTP 响应头，等待更多数据
        if (!m_wsUpgraded) return;
    }

    // 解析 WebSocket 帧
    while (m_readBuf.size() > 0) {
        int used = 0;
        QString text;
        if (parseWsFrame(m_readBuf, used, text)) {
            m_readBuf.remove(0, used);
            if (!text.isEmpty()) {
                QJsonDocument doc = QJsonDocument::fromJson(text.toUtf8());
                if (doc.isObject()) {
                    QJsonObject obj = doc.object();
                    if (obj["op"].toString() == "publish" &&
                        obj["topic"].toString() == m_topic) {
                        processImage(obj["msg"].toObject());
                    }
                }
            }
        } else if (used > 0) {
            // 非 text 帧，跳过
            m_readBuf.remove(0, used);
        } else {
            break; // 需要更多数据
        }
    }
}

void RosBridgeSubscriber::onError(QAbstractSocket::SocketError error)
{
    Q_UNUSED(error)
    qWarning() << "[RosBridgeSubscriber] Error:" << m_socket->errorString();
}

// ── WebSocket 握手 ───────────────────────────────────────────

void RosBridgeSubscriber::sendHttpUpgrade()
{
    QString key = makeWsKey();
    QString upgrade =
        QString("GET / HTTP/1.1\r\n"
                "Host: %1:%2\r\n"
                "Upgrade: websocket\r\n"
                "Connection: Upgrade\r\n"
                "Sec-WebSocket-Key: %3\r\n"
                "Sec-WebSocket-Version: 13\r\n"
                "\r\n")
            .arg(m_host).arg(m_port).arg(key);
    m_socket->write(upgrade.toUtf8());
}

// ── WebSocket 帧发送（文本帧，不掩码） ────────────────────────

void RosBridgeSubscriber::sendWsFrame(const QByteArray &payload)
{
    QByteArray frame;
    // FIN + opcode text (0x81)
    frame.append(static_cast<char>(0x81));

    int len = payload.size();
    if (len < 126) {
        frame.append(static_cast<char>(len));
    } else if (len < 65536) {
        frame.append(static_cast<char>(126));
        quint16 beLen = qToBigEndian<quint16>(static_cast<quint16>(len));
        frame.append(reinterpret_cast<const char*>(&beLen), 2);
    } else {
        frame.append(static_cast<char>(127));
        quint64 beLen = qToBigEndian<quint64>(static_cast<quint64>(len));
        frame.append(reinterpret_cast<const char*>(&beLen), 8);
    }

    frame.append(payload);
    m_socket->write(frame);
}

void RosBridgeSubscriber::sendJson(const QJsonObject &obj)
{
    if (m_socket->state() != QAbstractSocket::ConnectedState || !m_wsUpgraded) return;
    QByteArray data = QJsonDocument(obj).toJson(QJsonDocument::Compact);
    sendWsFrame(data);
}

void RosBridgeSubscriber::subscribe(const QString &topic)
{
    m_msgId++;
    QJsonObject sub;
    sub["op"] = "subscribe";
    sub["id"] = QString("sub_%1").arg(m_msgId);
    sub["topic"] = topic;
    sub["type"] = "sensor_msgs/Image";
    sub["throttle_rate"] = 0;
    sub["queue_length"] = 1;
    sendJson(sub);
    qInfo() << "[RosBridgeSubscriber] Subscribed to" << topic;
}

// ── 图像解析 ─────────────────────────────────────────────────

void RosBridgeSubscriber::processImage(const QJsonObject &msg)
{
    int width = msg["width"].toInt();
    int height = msg["height"].toInt();
    QString encoding = msg["encoding"].toString();
    QString dataB64 = msg["data"].toString();

    if (width <= 0 || height <= 0 || dataB64.isEmpty()) {
        qWarning() << "[RosBridgeSubscriber] Invalid image msg:" << width << height << dataB64.size();
        return;
    }

    QByteArray raw = QByteArray::fromBase64(dataB64.toUtf8());
    qInfo() << "[RosBridgeSubscriber] Got image" << width << "x" << height << "enc:" << encoding << "raw:" << raw.size() << "bytes";

    // JPEG/PNG 直通（CompressedImage 或 image_transport 压缩流）
    if (encoding == "jpeg" || encoding == "jpg") {
        QImage img;
        if (img.loadFromData(raw, "JPEG")) {
            QMutexLocker locker(&m_frameMutex);
            m_currentFrame = img;
            emit frameUpdated();
            return;
        }
    }
    if (encoding == "png") {
        QImage img;
        if (img.loadFromData(raw, "PNG")) {
            QMutexLocker locker(&m_frameMutex);
            m_currentFrame = img;
            emit frameUpdated();
            return;
        }
    }

    // 原始像素数据，需要按编码转换
    QImage img(width, height, QImage::Format_RGB888);
    if (img.isNull()) {
        qWarning() << "[RosBridgeSubscriber] Failed to create image" << width << height;
        return;
    }

    const uchar *src = reinterpret_cast<const uchar*>(raw.constData());
    int step = msg["step"].toInt(width * 3);

    if (encoding == "bgr8" || encoding == "bgr") {
        for (int y = 0; y < height && y < img.height(); y++) {
            uchar *dst = img.scanLine(y);
            const uchar *row = src + y * step;
            for (int x = 0; x < width; x++) {
                dst[x * 3 + 0] = row[x * 3 + 2];
                dst[x * 3 + 1] = row[x * 3 + 1];
                dst[x * 3 + 2] = row[x * 3 + 0];
            }
        }
    } else if (encoding == "rgb8" || encoding == "rgb") {
        for (int y = 0; y < height && y < img.height(); y++) {
            memcpy(img.scanLine(y), src + y * step, static_cast<size_t>(width) * 3);
        }
    } else if (encoding == "mono8" || encoding == "8UC1" || encoding == "gray") {
        for (int y = 0; y < height && y < img.height(); y++) {
            uchar *dst = img.scanLine(y);
            const uchar *row = src + y * step;
            for (int x = 0; x < width; x++) {
                dst[x * 3 + 0] = dst[x * 3 + 1] = dst[x * 3 + 2] = row[x];
            }
        }
    } else {
        qWarning() << "[RosBridgeSubscriber] Unknown encoding:" << encoding << "- trying raw copy";
        for (int y = 0; y < height && y < img.height(); y++) {
            memcpy(img.scanLine(y), src + y * step,
                   qMin(static_cast<size_t>(step), static_cast<size_t>(width * 3)));
        }
    }

    // 缩放到适合显示的大小（原始 1280x1024 → ~426x320，显示区域仅 240px 高）
    if (width > 640 || height > 480) {
        img = img.scaled(640, 480, Qt::KeepAspectRatio, Qt::SmoothTransformation);
    }

    {
        QMutexLocker locker(&m_frameMutex);
        m_currentFrame = img;
    }

    emit frameUpdated();
}
