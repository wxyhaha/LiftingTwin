#ifndef CAMERA_IMAGE_PROVIDER_H
#define CAMERA_IMAGE_PROVIDER_H

#include <QQuickImageProvider>
#include <QImage>
#include <QMutex>
#include <functional>

class CameraStreamClient;

/// 相机图像提供器 — QML 通过 image://camerafeed/live 获取帧
class CameraImageProvider : public QQuickImageProvider
{
public:
    CameraImageProvider();
    void setSource(CameraStreamClient *client);
    /// 备选帧源（如 ROS2 rosbridge WebSocket 订阅），主源返回空帧时使用
    using AltFrameFn = std::function<QImage()>;
    void setAltSource(AltFrameFn fn);

    QImage requestImage(const QString &id, QSize *size, const QSize &requestedSize) override;

private:
    CameraStreamClient *m_client = nullptr;
    AltFrameFn m_altFrameFn;
};

#endif // CAMERA_IMAGE_PROVIDER_H
