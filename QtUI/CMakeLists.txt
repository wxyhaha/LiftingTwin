cmake_minimum_required(VERSION 3.20)
project(LiftingTwinUI VERSION 1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)
set(CMAKE_AUTOUIC ON)

# FluentUI 依赖 Widgets / PrintSupport，主程序自身用 Quick / Network
find_package(Qt6 REQUIRED COMPONENTS Quick Qml Network Widgets PrintSupport Svg)

qt_standard_project_setup(REQUIRES 6.5)

# ─── FluentUI 第三方库（不构建其示例） ───
set(FLUENTUI_BUILD_EXAMPLES OFF CACHE BOOL "Disable FluentUI examples" FORCE)
add_subdirectory(third_party/FluentUI)

qt_add_executable(appLiftingTwinUI
    main.cpp
    QtRosBridge.h
    CameraStreamClient.h
    CameraStreamClient.cpp
    CameraImageProvider.h
    CameraImageProvider.cpp
    RosBridgeSubscriber.h
    RosBridgeSubscriber.cpp
)

qt_add_qml_module(appLiftingTwinUI
    URI LiftingTwin
    VERSION 1.0
    QML_FILES
        qml/main.qml
        # ─── 新增界面（独立窗口/组件，不改变现有 main.qml 行为） ───
        qml/SystemMonitor.qml
        qml/RiskEventArchivePanel.qml
        qml/TrolleyControl.qml
        qml/TrajectoryPrediction.qml
)

target_link_libraries(appLiftingTwinUI PRIVATE
    Qt6::Quick
    Qt6::Qml
    Qt6::Network
    Qt6::Widgets
    Qt6::PrintSupport
    Qt6::Svg
    fluentuiplugin
)
