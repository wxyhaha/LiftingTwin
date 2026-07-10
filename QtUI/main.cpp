#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QDebug>

int main(int argc, char *argv[])
{
    QGuiApplication app(argc, argv);

    app.setApplicationName("LiftingTwin");
    app.setOrganizationName("LiftingTwin Team");

    QQmlApplicationEngine engine;

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
