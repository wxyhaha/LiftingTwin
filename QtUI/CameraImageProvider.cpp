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

QImage CameraImageProvider::requestImage(const QString &id, QSize *size, const QSize &requestedSize)
{
    Q_UNUSED(id)

    QImage frame;
    if (m_client) {
        frame = m_client->currentFrame();
    }

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
