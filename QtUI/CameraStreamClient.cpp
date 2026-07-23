#include "CameraStreamClient.h"
#include <QDebug>
#include <QNetworkProxy>

CameraStreamClient::CameraStreamClient(QObject *parent)
    : QObject(parent)
    , m_socket(new QTcpSocket(this))
    , m_reconnectTimer(new QTimer(this))
{
    // 禁用系统代理（Clash 等会干扰直连）
    m_socket->setProxy(QNetworkProxy::NoProxy);
    m_socket->setSocketOption(QAbstractSocket::KeepAliveOption, 1);

    connect(m_socket, &QTcpSocket::connected, this, &CameraStreamClient::onConnected);
    connect(m_socket, &QTcpSocket::disconnected, this, &CameraStreamClient::onDisconnected);
    connect(m_socket, &QTcpSocket::readyRead, this, &CameraStreamClient::onReadyRead);
    connect(m_socket, &QTcpSocket::errorOccurred, this, &CameraStreamClient::onError);

    // 重连: 每 3 秒尝试
    m_reconnectTimer->setInterval(3000);
    connect(m_reconnectTimer, &QTimer::timeout, this, [this]() {
        if (!m_connected)
            connectToCamera(m_host, m_port);
    });
}

void CameraStreamClient::connectToCamera(const QString &host, int port)
{
    m_host = host;
    m_port = port;
    m_recvBuf.clear();
    m_state = ReadingHeader;
    m_expectedLength = 0;
    m_socket->connectToHost(host, port);
}

void CameraStreamClient::disconnect()
{
    m_reconnectTimer->stop();
    m_socket->disconnectFromHost();
    m_socket->abort();
    m_connected = false;
    emit connectedChanged();
}

void CameraStreamClient::setCameraName(const QString &name)
{
    if (m_cameraName != name) {
        m_cameraName = name;
        emit cameraNameChanged();
    }
}

QImage CameraStreamClient::currentFrame() const
{
    QMutexLocker locker(&m_frameMutex);
    return m_currentFrame;
}

void CameraStreamClient::onConnected()
{
    qInfo() << "[CameraStream] Connected to" << m_host << ":" << m_port;
    m_connected = true;
    m_recvBuf.clear();
    m_state = ReadingHeader;
    emit connectedChanged();
    m_reconnectTimer->stop();
}

void CameraStreamClient::onDisconnected()
{
    qWarning() << "[CameraStream] Disconnected, reconnecting...";
    m_connected = false;
    emit connectedChanged();
    m_reconnectTimer->start();
}

void CameraStreamClient::onReadyRead()
{
    m_recvBuf.append(m_socket->readAll());

    // 缓冲区积压时（>200KB≈多帧旧数据），丢弃旧的，只保留最新完整帧
    if (m_recvBuf.size() > 200 * 1024) {
        int tail = m_recvBuf.size();
        int lastStart = -1;
        int pos = 0;
        while (pos + 4 <= tail) {
            const auto *d = reinterpret_cast<const quint8*>(m_recvBuf.constData() + pos);
            quint32 flen = (static_cast<quint32>(d[0]) << 24)
                         | (static_cast<quint32>(d[1]) << 16)
                         | (static_cast<quint32>(d[2]) << 8)
                         | static_cast<quint32>(d[3]);
            if (pos + 4 + static_cast<int>(flen) > tail) break;
            lastStart = pos;
            pos += 4 + static_cast<int>(flen);
        }
        if (lastStart > 0) {
            m_recvBuf.remove(0, lastStart);
            m_state = ReadingHeader;
            m_expectedLength = 0;
            qInfo() << "[CameraStream] Skip" << lastStart << "bytes, buffer:" << m_recvBuf.size();
        }
    }

    while (true) {
        if (m_state == ReadingHeader) {
            if (m_recvBuf.size() < 4) break;

            const auto *data = reinterpret_cast<const quint8*>(m_recvBuf.constData());
            m_expectedLength = (static_cast<quint32>(data[0]) << 24)
                             | (static_cast<quint32>(data[1]) << 16)
                             | (static_cast<quint32>(data[2]) << 8)
                             | static_cast<quint32>(data[3]);

            m_recvBuf.remove(0, 4);
            m_state = ReadingData;
        }

        if (m_state == ReadingData) {
            if (static_cast<quint32>(m_recvBuf.size()) < m_expectedLength) break;

            QByteArray jpegData = m_recvBuf.left(static_cast<int>(m_expectedLength));
            m_recvBuf.remove(0, static_cast<int>(m_expectedLength));

            processFrame(jpegData);
            m_state = ReadingHeader;
            m_expectedLength = 0;
        }
    }
}

void CameraStreamClient::onError(QAbstractSocket::SocketError error)
{
    Q_UNUSED(error)
    qWarning() << "[CameraStream] Error:" << m_socket->errorString();
}

void CameraStreamClient::processFrame(const QByteArray &jpegData)
{
    QImage img;
    if (!img.loadFromData(jpegData, "JPEG")) {
        qWarning() << "[CameraStream] JPEG decode failed, size:" << jpegData.size();
        return;
    }

    {
        QMutexLocker locker(&m_frameMutex);
        m_currentFrame = img;
    }

    m_frameCount++;
    emit frameUpdated();
}
