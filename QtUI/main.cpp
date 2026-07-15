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
        if (!container) return;
        m_container = container;

        auto doStart = [this]() {
            if (m_started) return;
            QWindow *win = m_container ? m_container->window() : nullptr;
            if (!win) return;
            m_containerWinId = win->winId();
            if (m_containerWinId == 0) return;
            m_started = true;
            startUnityAndEmbed();
        };

        if (container->window() && container->window()->isVisible())
            doStart();
        else
            QObject::connect(container, &QQuickItem::windowChanged, this, [this, doStart]() {
                if (m_container->window() && m_container->window()->isVisible()) doStart();
            });
    }

    Q_INVOKABLE void stop() {
        m_resizeTimer->stop();
        if (m_pollTimer) m_pollTimer->stop();
#ifdef Q_OS_WIN
        if (m_process) {
            // 先优雅关闭，给 Unity 保存机会
            m_process->terminate();
            if (!m_process->waitForFinished(5000)) {
                m_process->kill();
                m_process->waitForFinished(3000);
            }
            delete m_process;
            m_process = nullptr;
        }
#endif
    }

private slots:
    void syncSize() {
#ifdef Q_OS_WIN
        if (!m_container || !m_unityWindow) return;

        QPointF pos = m_container->mapToItem(nullptr, QPointF(0, 0));
        int x = static_cast<int>(pos.x());
        int y = static_cast<int>(pos.y());
        int w = static_cast<int>(m_container->width());
        int h = static_cast<int>(m_container->height());

        if (w > 0 && h > 0 && (x != m_lastX || y != m_lastY || w != m_lastW || h != m_lastH)) {
            m_unityWindow->setPosition(x, y);
            m_unityWindow->resize(w, h);
            m_lastX = x; m_lastY = y; m_lastW = w; m_lastH = h;
        }
#endif
    }

private:
#ifdef Q_OS_WIN
    void startUnityAndEmbed() {
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
        m_pollTimer = new QTimer(this);
        connect(m_pollTimer, &QTimer::timeout, this, [this]() {
            m_embedAttempts++;
            if (tryEmbed()) {
                m_pollTimer->stop();
                m_resizeTimer->start();
                syncSize();
                qInfo() << "[UnityEmbed] Unity embedded successfully";
            } else if (m_embedAttempts > 50) {
                m_pollTimer->stop();
                qWarning() << "[UnityEmbed] Failed to find Unity window after 5s";
            }
        });
        m_pollTimer->start(100);

        // 监听进程退出
        connect(m_process, &QProcess::finished, this, [](int exitCode) {
            qInfo() << "[UnityEmbed] Unity process exited with code:" << exitCode;
        });
    }

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

        m_unityWindow = QWindow::fromWinId(reinterpret_cast<WId>(data.found));
        if (!m_unityWindow) return false;

        QWindow *parentWin = m_container->window();
        if (!parentWin) return false;
        m_unityWindow->setParent(parentWin);

        m_unityWindow->setPosition(m_lastX >= 0 ? m_lastX : 0, m_lastY >= 0 ? m_lastY : 0);

        return true;
    }

    HWND m_unityHwnd = nullptr;
    qint64 m_containerWinId = 0;
    int m_embedAttempts = 0;
    int m_lastX = -1, m_lastY = -1, m_lastW = -1, m_lastH = -1;
    bool m_started = false;
    QTimer *m_pollTimer = nullptr;
    QWindow *m_unityWindow = nullptr;
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

    // 通过 CMake 资源系统或文件系统加载 QML
    const QString localQml = QCoreApplication::applicationDirPath() + "/LiftingTwin/qml/main.qml";
    if (QFileInfo::exists(localQml)) {
        engine.load(QUrl::fromLocalFile(localQml));
    } else {
        engine.loadFromModule("LiftingTwin", "main");
    }

    QObject::connect(&engine, &QQmlApplicationEngine::warnings,
        [](const QList<QQmlError> &warnings) {
            for (const auto &warning : warnings) {
                qWarning() << "QML Warning:" << warning.toString();
            }
        });

    if (engine.rootObjects().isEmpty()) {
        qCritical() << "Failed to load QML - no root objects";
        return -1;
    }

    qInfo() << "Application started successfully";
    return app.exec();
}

#include "main.moc"
