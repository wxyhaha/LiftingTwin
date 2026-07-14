cmake_minimum_required(VERSION 3.16)
project(LiftingTwinUI VERSION 1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)
set(CMAKE_AUTOUIC ON)

find_package(Qt6 REQUIRED COMPONENTS Quick Network)

qt_standard_project_setup(REQUIRES 6.5)

qt_add_executable(appLiftingTwinUI
    main.cpp
    QtRosBridge.h
)

qt_add_qml_module(appLiftingTwinUI
    URI LiftingTwin
    VERSION 1.0
    QML_FILES
        qml/main.qml
        qml/components/TopBar.qml
        qml/components/LeftPanel.qml
        qml/components/CenterView.qml
        qml/components/RightPanel.qml
        qml/components/BottomPanel.qml
        qml/components/StatusCard.qml
        qml/components/AlarmList.qml
        qml/components/EventTable.qml
        qml/components/CraneStatus.qml
        qml/components/RiskPanel.qml
        qml/components/SensorStatus.qml
        qml/components/PersonnelList.qml
        qml/components/ModelLayers.qml
        qml/components/EquipmentList.qml
)

target_link_libraries(appLiftingTwinUI PRIVATE Qt6::Quick Qt6::Network)
