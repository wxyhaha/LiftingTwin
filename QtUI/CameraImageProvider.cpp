#include "CameraImageProvider.h"
#include "CameraStreamClient.h"

CameraImageProvider::CameraImageProvider()
    : QQuickImageProvider(QQuickImageProvider::Image)
{
}

void CameraImageProvider::setSource(CameraStreamClient *client)
{
    m_client = client;
}

void CameraImageProvider::setAltSource(AltFrameFn fn)
{
    m_altFrameFn = std::move(fn);
}

QImage CameraImageProvider::requestImage(const QString &id, QSize *size, const QSize &requestedSize)
{
    Q_UNUSED(id)

    QImage frame;

    // 1) 优先从 CameraStreamClient (TCP JPEG 流) 获取
    if (frame.isNull() && m_client) {
        frame = m_client->currentFrame();
    }

    // 2) 回退到备选源 (ROS2 rosbridge)
    if (frame.isNull() && m_altFrameFn) {
        frame = m_altFrameFn();
    }

    // 3) 都没有 → 深色占位图
    if (frame.isNull()) {
        frame = QImage(192, 75, QImage::Format_RGB32);
        frame.fill(QColor("#1e293b"));
    }

    if (size)
        *size = frame.size();

    if (requestedSize.isValid() && requestedSize != frame.size())
        frame = frame.scaled(requestedSize, Qt::KeepAspectRatio, Qt::SmoothTransformation);

    return frame;
}
