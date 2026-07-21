import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import FluentUI 1.0

// ═══════════════════════════════════════════════════════════════════════════
// 移动小车与云台控制窗口（FluentUI 风格）
// 依据开题报告 §4.2.2 / §4.2.4 / §4.3.1：
//   - 小车底盘：行驶 / 转向 / 慢速巡航，内置防碰撞与电子围栏越界保护
//   - 双轴智能云台：水平 360° 连续旋转，仰俯 -60° ~ +60°
//   - 远端监控终端向小车下达行驶/转向指令，小车停靠后由云台调整观测视角
// 控制指令通过 rosBridge 发送至 Unity → ROS2（话题名由后端约定，此处为示例）。
// ═══════════════════════════════════════════════════════════════════════════
ApplicationWindow {
    visible: true
    id: root
    width: 1400
    height: 900
    title: "移动小车与云台控制"

    Component.onCompleted: {
        FluTheme.primaryColor = "#01469a"
    }

    // ─── 小车状态（后续由 ROS 订阅实时更新） ───
    property bool   cartConnected:      true
    property string cartMode:           "manual"
    property real   cartSpeed:          0.0
    property string cartPosition:       "(3.2, 12.5)"
    property bool   collisionGuardOn:   true
    property bool   fenceGuardOn:       true
    property string fenceStatus:        "边界内"

    // ─── 云台状态 ───
    property real gimbalYaw:   0.0
    property real gimbalPitch: 0.0

    // ─── 速度档位 ───
    property string speedLevel: "slow"

    // ─── 指令发送（统一入口，待与后端约定话题名） ───
    function sendCartCommand(cmd) {
        console.log("[TrolleyControl] cart cmd:", cmd)
        // if (typeof rosBridge !== "undefined")
        //     rosBridge.publishString("/trolley/cmd", cmd)
    }
    function sendGimbalCommand(yawDeg, pitchDeg) {
        console.log("[TrolleyControl] gimbal:", yawDeg, pitchDeg)
        // if (typeof rosBridge !== "undefined")
        //     rosBridge.publishString("/gimbal/cmd",
        //         JSON.stringify({yaw: yawDeg, pitch: pitchDeg}))
    }
    function emergencyStop() {
        console.log("[TrolleyControl] EMERGENCY STOP")
        sendCartCommand("ESTOP")
    }

    // 通用卡片组件
    component PanelCard: Rectangle {
        default property alias contentData: inner.data
        property string title: ""
        color: FluTheme.dark ? Qt.rgba(32/255,32/255,32/255,1) : "#ffffff"
        radius: 6
        border.color: FluTheme.dividerColor
        border.width: 1

        ColumnLayout {
            anchors.fill: parent
            anchors.margins: 14
            spacing: 12
            FluText {
                visible: title !== ""
                text: title
                font: FluTextStyle.Subtitle
                color: FluTheme.primaryColor
            }
            ColumnLayout {
                id: inner
                Layout.fillWidth: true
                Layout.fillHeight: true
                spacing: 12
            }
        }
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        // ═══ 顶部：状态栏 + 急停 ═══
        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 64
            color: FluTheme.dark ? Qt.rgba(28/255,28/255,28/255,1) : "#ffffff"
            border.color: FluTheme.dividerColor
            border.width: 1

            RowLayout {
                anchors.fill: parent
                anchors.leftMargin: 20
                anchors.rightMargin: 20

                ColumnLayout {
                    Layout.fillWidth: true
                    spacing: 2
                    FluText {
                        text: "移动小车与云台控制"
                        font: FluTextStyle.Title
                        color: FluTheme.primaryColor
                    }
                    Row {
                        spacing: 6
                        Rectangle {
                            width: 8; height: 8; radius: 4
                            color: root.cartConnected ? FluColors.Green.normal : FluColors.Red.normal
                            anchors.verticalCenter: parent.verticalCenter
                        }
                        FluText {
                            text: root.cartConnected ? "小车链路已连接" : "小车链路未连接"
                            font: FluTextStyle.Caption
                            color: FluTheme.fontSecondaryColor
                        }
                    }
                }

                // 急停按钮
                Rectangle {
                    Layout.preferredWidth: 140
                    Layout.preferredHeight: 46
                    radius: 4
                    color: FluColors.Red.normal
                    border.color: FluColors.Red.dark
                    border.width: 2

                    FluText {
                        text: "急  停"
                        font: FluTextStyle.Subtitle
                        color: "#ffffff"
                        anchors.centerIn: parent
                    }
                    MouseArea {
                        anchors.fill: parent
                        onClicked: root.emergencyStop()
                    }
                }
            }
        }

        // ═══ 主体：左小车 + 右云台 ═══
        RowLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            Layout.margins: 14
            spacing: 14

            // ─── 左：小车底盘控制 ───
            PanelCard {
                Layout.fillWidth: true
                Layout.fillHeight: true
                Layout.preferredWidth: 1
                title: "小车底盘控制"

                // 模式切换
                RowLayout {
                    Layout.fillWidth: true
                    spacing: 12
                    FluText { text: "模式"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                    FluRadioButton {
                        text: "手动控制"
                        checked: root.cartMode === "manual"
                        onCheckedChanged: if (checked) root.cartMode = "manual"
                    }
                    FluRadioButton {
                        text: "自动停靠扫描站位"
                        checked: root.cartMode === "docking"
                        onCheckedChanged: if (checked) root.cartMode = "docking"
                    }
                }

                // 速度档位（开题报告 §4.3.1：现场慢速行驶）
                ColumnLayout {
                    Layout.fillWidth: true
                    spacing: 4
                    FluText { text: "速度档位"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                    Row {
                        spacing: 12
                        FluRadioButton {
                            text: "慢速（推荐）"
                            checked: root.speedLevel === "slow"
                            onCheckedChanged: if (checked) root.speedLevel = "slow"
                        }
                        FluRadioButton {
                            text: "中速"
                            checked: root.speedLevel === "mid"
                            onCheckedChanged: if (checked) root.speedLevel = "mid"
                        }
                        FluRadioButton {
                            text: "快速"
                            checked: root.speedLevel === "fast"
                            onCheckedChanged: if (checked) root.speedLevel = "fast"
                        }
                    }
                }

                // 方向控制盘
                ColumnLayout {
                    Layout.alignment: Qt.AlignHCenter
                    spacing: 8
                    FluText {
                        text: "行驶方向（按住移动，松开停止）"
                        font: FluTextStyle.Caption
                        color: FluTheme.fontSecondaryColor
                        Layout.alignment: Qt.AlignHCenter
                    }

                    Grid {
                        columns: 3
                        spacing: 8
                        Layout.alignment: Qt.AlignHCenter

                        Item { width: 68; height: 68 }
                        FluIconButton {
                            width: 68; height: 68
                            iconSource: FluentIcons.Up
                            iconSize: 22
                            onPressed:  root.sendCartCommand("FORWARD")
                            onReleased: root.sendCartCommand("STOP")
                        }
                        Item { width: 68; height: 68 }

                        FluIconButton {
                            width: 68; height: 68
                            iconSource: FluentIcons.Back
                            iconSize: 22
                            onPressed:  root.sendCartCommand("LEFT")
                            onReleased: root.sendCartCommand("STOP")
                        }
                        Rectangle {
                            width: 68; height: 68
                            radius: 4
                            color: FluColors.Orange.normal
                            FluText {
                                text: "停"
                                font: FluTextStyle.Subtitle
                                color: "#ffffff"
                                anchors.centerIn: parent
                            }
                            MouseArea {
                                anchors.fill: parent
                                onClicked: root.sendCartCommand("STOP")
                            }
                        }
                        FluIconButton {
                            width: 68; height: 68
                            iconSource: FluentIcons.Forward
                            iconSize: 22
                            onPressed:  root.sendCartCommand("RIGHT")
                            onReleased: root.sendCartCommand("STOP")
                        }

                        Item { width: 68; height: 68 }
                        FluIconButton {
                            width: 68; height: 68
                            iconSource: FluentIcons.Down
                            iconSize: 22
                            onPressed:  root.sendCartCommand("BACKWARD")
                            onReleased: root.sendCartCommand("STOP")
                        }
                        Item { width: 68; height: 68 }
                    }
                }

                // 车载防护
                Rectangle {
                    Layout.fillWidth: true
                    Layout.preferredHeight: 130
                    radius: 4
                    color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f8fafc"
                    border.color: FluTheme.dividerColor
                    border.width: 1

                    ColumnLayout {
                        anchors.fill: parent
                        anchors.margins: 12
                        spacing: 6
                        FluText {
                            text: "车载防护"
                            font: FluTextStyle.BodyStrong
                            color: FluTheme.primaryColor
                        }
                        Row {
                            spacing: 8
                            Rectangle {
                                width: 8; height: 8; radius: 4
                                color: root.collisionGuardOn ? FluColors.Green.normal : FluColors.Grey120
                                anchors.verticalCenter: parent.verticalCenter
                            }
                            FluText {
                                text: "地面障碍防碰撞：" + (root.collisionGuardOn ? "已启用" : "已停用")
                                font: FluTextStyle.Body
                            }
                        }
                        Row {
                            spacing: 8
                            Rectangle {
                                width: 8; height: 8; radius: 4
                                color: root.fenceGuardOn ? FluColors.Green.normal : FluColors.Grey120
                                anchors.verticalCenter: parent.verticalCenter
                            }
                            FluText {
                                text: "电子围栏越界保护：" + (root.fenceGuardOn ? "已启用" : "已停用")
                                font: FluTextStyle.Body
                            }
                        }
                        FluText {
                            text: "围栏状态：" + root.fenceStatus
                            font: FluTextStyle.BodyStrong
                            color: root.fenceStatus === "边界内" ? FluColors.Green.normal : FluColors.Red.normal
                        }
                    }
                }

                // 实时状态
                Rectangle {
                    Layout.fillWidth: true
                    Layout.preferredHeight: 92
                    radius: 4
                    color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f8fafc"
                    border.color: FluTheme.dividerColor
                    border.width: 1

                    ColumnLayout {
                        anchors.fill: parent
                        anchors.margins: 12
                        spacing: 4
                        FluText {
                            text: "实时状态"
                            font: FluTextStyle.BodyStrong
                            color: FluTheme.primaryColor
                        }
                        FluText { text: "当前位置（作业局部坐标）：" + root.cartPosition; font: FluTextStyle.Body }
                        FluText { text: "当前速度：" + root.cartSpeed.toFixed(2) + " m/s"; font: FluTextStyle.Body }
                    }
                }

                Item { Layout.fillHeight: true }
            }

            // ─── 右：云台控制 ───
            PanelCard {
                Layout.fillWidth: true
                Layout.fillHeight: true
                Layout.preferredWidth: 1
                title: "双轴智能云台"

                FluText {
                    text: "水平 360° 连续旋转 · 仰俯 -60° ~ +60°"
                    font: FluTextStyle.Caption
                    color: FluTheme.fontSecondaryColor
                }

                // 水平角
                ColumnLayout {
                    Layout.fillWidth: true
                    spacing: 4
                    RowLayout {
                        Layout.fillWidth: true
                        FluText { text: "水平角（Yaw）"; font: FluTextStyle.Body; Layout.fillWidth: true }
                        FluText {
                            text: root.gimbalYaw.toFixed(1) + "°"
                            font: FluTextStyle.BodyStrong
                            color: FluTheme.primaryColor
                        }
                    }
                    FluSlider {
                        Layout.fillWidth: true
                        from: 0; to: 360; stepSize: 0.5
                        value: root.gimbalYaw
                        onValueChanged: {
                            root.gimbalYaw = value
                            root.sendGimbalCommand(root.gimbalYaw, root.gimbalPitch)
                        }
                    }
                }

                // 仰俯角
                ColumnLayout {
                    Layout.fillWidth: true
                    spacing: 4
                    RowLayout {
                        Layout.fillWidth: true
                        FluText { text: "仰俯角（Pitch）"; font: FluTextStyle.Body; Layout.fillWidth: true }
                        FluText {
                            text: root.gimbalPitch.toFixed(1) + "°"
                            font: FluTextStyle.BodyStrong
                            color: FluTheme.primaryColor
                        }
                    }
                    FluSlider {
                        Layout.fillWidth: true
                        from: -60; to: 60; stepSize: 0.5
                        value: root.gimbalPitch
                        onValueChanged: {
                            root.gimbalPitch = value
                            root.sendGimbalCommand(root.gimbalYaw, root.gimbalPitch)
                        }
                    }
                }

                // 预设视角（开题报告 §4.3.1）
                ColumnLayout {
                    Layout.fillWidth: true
                    spacing: 8
                    FluText { text: "预设视角"; font: FluTextStyle.Body }
                    Grid {
                        columns: 2
                        spacing: 8

                        FluButton {
                            text: "回零位"
                            implicitWidth: 170; implicitHeight: 34
                            onClicked: {
                                root.gimbalYaw = 0
                                root.gimbalPitch = 0
                                root.sendGimbalCommand(0, 0)
                            }
                        }
                        FluButton {
                            text: "仰视高位（电线/吊臂）"
                            implicitWidth: 170; implicitHeight: 34
                            onClicked: {
                                root.gimbalPitch = 45
                                root.sendGimbalCommand(root.gimbalYaw, 45)
                            }
                        }
                        FluButton {
                            text: "正前平视"
                            implicitWidth: 170; implicitHeight: 34
                            onClicked: {
                                root.gimbalPitch = 0
                                root.sendGimbalCommand(root.gimbalYaw, 0)
                            }
                        }
                        FluButton {
                            text: "俯视地面（吊物下方）"
                            implicitWidth: 170; implicitHeight: 34
                            onClicked: {
                                root.gimbalPitch = -45
                                root.sendGimbalCommand(root.gimbalYaw, -45)
                            }
                        }
                    }
                }

                // 扫描模式
                ColumnLayout {
                    Layout.fillWidth: true
                    spacing: 6
                    FluText { text: "扫描模式"; font: FluTextStyle.Body }
                    Row {
                        spacing: 14
                        FluRadioButton { text: "单点观测"; checked: true }
                        FluRadioButton { text: "水平巡回扫描" }
                        FluRadioButton { text: "多点位自动扫描" }
                    }
                }

                // 视频反馈占位
                Rectangle {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    Layout.minimumHeight: 200
                    radius: 4
                    color: "#1e293b"

                    FluText {
                        text: "车载相机实时画面\n（接入后由 camStream 提供）"
                        font: FluTextStyle.Body
                        color: "#94a3b8"
                        horizontalAlignment: Text.AlignHCenter
                        anchors.centerIn: parent
                    }
                }
            }
        }

        // ═══ 底部：操作日志 ═══
        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 110
            color: FluTheme.dark ? Qt.rgba(28/255,28/255,28/255,1) : "#ffffff"
            border.color: FluTheme.dividerColor
            border.width: 1

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 12
                spacing: 6
                FluText {
                    text: "控制日志"
                    font: FluTextStyle.BodyStrong
                    color: FluTheme.primaryColor
                }
                Column {
                    spacing: 3
                    FluText { text: "10:32:14  云台调至 (Yaw=128.5°, Pitch=+30.0°)"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                    FluText { text: "10:30:02  小车停靠扫描站位 #3"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                    FluText { text: "10:25:46  小车进入边界预警，已自动减速"; font: FluTextStyle.Caption; color: FluColors.Orange.normal }
                }
            }
        }
    }
}
