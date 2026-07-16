#ifndef CAMERA_IMAGE_PROVIDER_H
#define CAMERA_IMAGE_PROVIDER_H

#include <QQuickImageProvider>
#include <QImage>
#include <QMutex>

class CameraStreamClient;

/// 相机图像提供器 — QML 通过 image://camerafeed/live 获取帧
class CameraImageProvider : public QQuickImageProvider
{
public:
    CameraImageProvider();
    void setSource(CameraStreamClient *client);

    QImage requestImage(const QString &id, QSize *size, const QSize &requestedSize) override;

private:
    CameraStreamClient *m_client = nullptr;
};

#endif // CAMERA_IMAGE_PROVIDER_H
