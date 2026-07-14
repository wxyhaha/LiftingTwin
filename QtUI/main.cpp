#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QQmlContext>
#include <QDebug>
#include <QProcess>
#include <QTimer>
#include <QQuickItem>
#include <QQuickWindow>
#include <QWindow>
#include <QDir>
#include <QStandardPaths>
#include "QtRosBridge.h"

#ifdef Q_OS_WIN
#include <windows.h>
#include <tlhelp32.h>
#endif

// Unity 嵌入器：启动 Unity 子进程并将其窗口嵌入 QML 容器
class UnityEmbed : public QObject {
    Q_OBJECT
public:
    explicit UnityEmbed(QObject *parent = nullptr) : QObject(parent) {
        m_resizeTimer = new QTimer(this);
        m_resizeTimer->setInterval(100);
        connect(m_resizeTimer, &QTimer::timeout, this, &UnityEmbed::syncSize);
    }

    ~UnityEmbed() {
        stop();
    }

    Q_INVOKABLE void embed(QQuickItem *container) {
        if (!container) {
            qWarning() << "[UnityEmbed] container is null";
            return;
        }

        m_container = container;
        m_containerWinId = container->window() ? container->window()->winId() : 0;

#ifdef Q_OS_WIN
        // 启动 Unity
        QString unityPath = findUnityPath();
        if (unityPath.isEmpty()) {
            qWarning() << "[UnityEmbed] Unity exe not found";
            return;
        }

        qInfo() << "[UnityEmbed] Starting Unity from:" << unityPath;
        m_process = new QProcess(this);
        m_process->setWorkingDirectory(QFileInfo(unityPath).absolutePath());
        m_process->start(unityPath, QStringList());

        if (!m_process->waitForStarted(5000)) {
            qWarning() << "[UnityEmbed] Failed to start Unity";
            delete m_process;
            m_process = nullptr;
            return;
        }

        qInfo() << "[UnityEmbed] Unity started, PID:" << m_process->processId();

        // 轮询查找 Unity 窗口并嵌入
        m_embedAttempts = 0;
        QTimer *pollTimer = new QTimer(this);
        connect(pollTimer, &QTimer::timeout, this, [this, pollTimer]() {
            m_embedAttempts++;
            if (tryEmbed()) {
                pollTimer->stop();
                pollTimer->deleteLater();
                m_resizeTimer->start();
                syncSize();
                qInfo() << "[UnityEmbed] Unity embedded successfully";
            } else if (m_embedAttempts > 50) {
                pollTimer->stop();
                pollTimer->deleteLater();
                qWarning() << "[UnityEmbed] Failed to find Unity window after 5s";
            }
        });
        pollTimer->start(100);

        // 监听进程退出
        connect(m_process, &QProcess::finished, this, [](int exitCode) {
            qInfo() << "[UnityEmbed] Unity process exited with code:" << exitCode;
        });
#else
        qWarning() << "[UnityEmbed] Windows only";
#endif
    }

    Q_INVOKABLE void stop() {
        m_resizeTimer->stop();
#ifdef Q_OS_WIN
        if (m_process) {
            m_process->kill();
            m_process->waitForFinished(3000);
            delete m_process;
            m_process = nullptr;
        }
#endif
    }

private slots:
    void syncSize() {
#ifdef Q_OS_WIN
        if (!m_container || !m_unityHwnd) return;

        // 将 QML item 的全局坐标转换为父窗口坐标
        QPointF pos = m_container->mapToItem(nullptr, QPointF(0, 0));
        QWindow *win = m_container->window();
        if (!win) return;

        int x = static_cast<int>(pos.x());
        int y = static_cast<int>(pos.y());
        int w = static_cast<int>(m_container->width());
        int h = static_cast<int>(m_container->height());

        if (w > 0 && h > 0) {
            ::SetWindowPos(m_unityHwnd, nullptr, x, y, w, h,
                           SWP_NOZORDER | SWP_NOACTIVATE);
        }
#endif
    }

private:
#ifdef Q_OS_WIN
    QString findUnityPath() {
        // 按优先级查找 Unity 可执行文件
        QString appDir = QCoreApplication::applicationDirPath();

        QStringList searchPaths = {
            // 1. Qt 同级目录的 Build/Windows/
            appDir + "/../../../Build/Windows/LiftingTwin.exe",
            // 2. 项目根目录
            appDir + "/../../Build/Windows/LiftingTwin.exe",
            // 3. 用户目录
            QDir::homePath() + "/Documents/unity/LiftingTwin/Build/Windows/LiftingTwin.exe",
        };

        for (const auto &path : searchPaths) {
            if (QFileInfo::exists(path)) {
                return QFileInfo(path).absoluteFilePath();
            }
        }
        return QString();
    }

    bool tryEmbed() {
        if (!m_process || m_process->processId() == 0) return false;

        // 查找 Unity 进程的所有窗口
        DWORD unityPid = static_cast<DWORD>(m_process->processId());
        struct EnumData {
            DWORD pid;
            HWND found;
        } data = { unityPid, nullptr };

        ::EnumWindows([](HWND hwnd, LPARAM lParam) -> BOOL {
            auto *d = reinterpret_cast<EnumData *>(lParam);
            DWORD windowPid;
            ::GetWindowThreadProcessId(hwnd, &windowPid);
            if (windowPid == d->pid && ::IsWindowVisible(hwnd)) {
                // Unity 启动时主窗口可能不可见，查找其子窗口
                d->found = hwnd;
                // 继续查找子窗口（Unity 的实际渲染窗口）
                HWND child = ::GetWindow(hwnd, GW_CHILD);
                if (child) {
                    d->found = child;
                }
                return FALSE;
            }
            return TRUE;
        }, reinterpret_cast<LPARAM>(&data));

        if (!data.found) return false;

        m_unityHwnd = data.found;

        // 嵌入到 Qt 窗口
        ::SetParent(m_unityHwnd, reinterpret_cast<HWND>(m_containerWinId));

        // 去掉标题栏和边框，改为子窗口样式
        LONG style = ::GetWindowLong(m_unityHwnd, GWL_STYLE);
        style = (style & ~WS_CAPTION & ~WS_THICKFRAME & ~WS_BORDER & ~WS_POPUP) | WS_CHILD;
        ::SetWindowLong(m_unityHwnd, GWL_STYLE, style);
        return true;
    }

    HWND m_unityHwnd = nullptr;
    qint64 m_containerWinId = 0;
    int m_embedAttempts = 0;
#endif

    QProcess *m_process = nullptr;
    QQuickItem *m_container = nullptr;
    QTimer *m_resizeTimer = nullptr;
};

int main(int argc, char *argv[])
{
    QGuiApplication app(argc, argv);

    app.setApplicationName("LiftingTwin");
    app.setOrganizationName("LiftingTwin Team");

    QQmlApplicationEngine engine;

    // 注册 UnityEmbed 到 QML
    UnityEmbed unityEmbed;
    engine.rootContext()->setContextProperty("unityEmbed", &unityEmbed);

    // 注册 QtRosBridge 到 QML
    QtRosBridge rosBridge;
    engine.rootContext()->setContextProperty("rosBridge", &rosBridge);

    // 直接从文件系统加载 QML
    QUrl url = QUrl::fromLocalFile("c:/Users/Administrator/Documents/unity/LiftingTwin/QtUI/qml/main.qml");
    qInfo() << "Loading QML from:" << url.toString();

    QObject::connect(&engine, &QQmlApplicationEngine::warnings,
        [](const QList<QQmlError> &warnings) {
            for (const auto &warning : warnings) {
                qWarning() << "QML Warning:" << warning.toString();
            }
        });

    engine.load(url);

    if (engine.rootObjects().isEmpty()) {
        qCritical() << "Failed to load QML - no root objects";
        return -1;
    }

    qInfo() << "Application started successfully";
    return app.exec();
}

#include "main.moc"
