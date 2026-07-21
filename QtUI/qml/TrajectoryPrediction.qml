import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import FluentUI 1.0

// ═══════════════════════════════════════════════════════════════════════════
// 短时轨迹预测视图窗口（FluentUI 风格）
// 依据开题报告 §4.4.3 / §4.5.3：
//   - 在可配置的未来 3 ~ 5 秒时间窗口内滚动预测
//   - 生成"未来轨迹预测安全管道"，标识未来空间占用
//   - 危险级预警展示：预测冲突对象、预计发生时间、预测最小距离、风险位置
// 三维场景中的轨迹线与安全管道由 Unity 渲染；本窗口负责：
//   - 预测窗口参数配置
//   - 各监测对象轨迹/管道显示开关
//   - 预测冲突列表（按预计发生时间排序）
// ═══════════════════════════════════════════════════════════════════════════
ApplicationWindow {
    visible: true
    id: root
    width: 1500
    height: 900
    title: "短时轨迹预测"

    Component.onCompleted: {
        FluTheme.primaryColor = "#01469a"
    }

    // 预测时间窗口（秒）— 开题报告 §4.4.3：可配置的 3 ~ 5 s
    property int predictWindowSec: 5
    property bool showSafetyTube: true

    // 监测对象（开题报告 §4.4.1：吊臂 / 吊钩 / 吊物 / 人员 / 车辆）
    property var trackedObjects: [
        { id: "T-ARM-01",     category: "吊臂",   visible: true,  speed: "0.8 m/s",  trend: "顺时针回转" },
        { id: "T-HOOK-01",    category: "吊钩",   visible: true,  speed: "0.3 m/s",  trend: "随吊臂运动" },
        { id: "T-CARGO-01",   category: "吊物",   visible: true,  speed: "0.5 m/s",  trend: "横向摆动（幅 0.6 m）" },
        { id: "T-WORKER-04",  category: "人员",   visible: true,  speed: "0.9 m/s",  trend: "向警戒区方向" },
        { id: "T-WORKER-07",  category: "人员",   visible: false, speed: "0 m/s",    trend: "静止" },
        { id: "T-VEHICLE-02", category: "车辆",   visible: true,  speed: "1.2 m/s",  trend: "沿安全通道" }
    ]

    // 预测冲突 — 开题报告 §4.5.3：预测冲突对象、预计发生时间、预测最小距离、风险位置
    property var predictedConflicts: [
        {
            level: "危险级",
            type: "近电安全净距不足",
            objectA: "T-CARGO-01",
            objectB: "500kV 高压母线 A 相",
            timeToImpactSec: 3.2,
            predictedMinDist: "5.4 m",
            position: "(12.5, 8.3, 25.6)"
        },
        {
            level: "危险级",
            type: "人员车辆闯入风险区域",
            objectA: "T-WORKER-04",
            objectB: "吊装作业半径（警戒区）",
            timeToImpactSec: 4.5,
            predictedMinDist: "0.8 m",
            position: "(8.1, 15.2, 0.0)"
        },
        {
            level: "预警级",
            type: "吊臂吊物与构架设备碰撞",
            objectA: "T-ARM-01",
            objectB: "北侧构架立柱",
            timeToImpactSec: 5.0,
            predictedMinDist: "1.2 m",
            position: "(15.3, 6.1, 28.0)"
        }
    ]

    function levelColor(level) {
        switch (level) {
        case "提示级": return FluColors.Blue.normal
        case "预警级": return FluColors.Yellow.normal
        case "危险级": return FluColors.Orange.normal
        case "越限级": return FluColors.Red.normal
        default:       return FluColors.Grey120
        }
    }

    component PanelCard: Rectangle {
        default property alias contentData: inner.data
        property string title: ""
        color: FluTheme.dark ? Qt.rgba(32/255,32/255,32/255,1) : "#ffffff"
        radius: 6
        border.color: FluTheme.dividerColor
        border.width: 1

        ColumnLayout {
            anchors.fill: parent
            anchors.margins: 0
            spacing: 0
            Rectangle {
                visible: title !== ""
                Layout.fillWidth: true
                Layout.preferredHeight: 40
                color: "transparent"
                FluText {
                    text: title
                    font: FluTextStyle.Subtitle
                    color: FluTheme.primaryColor
                    anchors.left: parent.left
                    anchors.leftMargin: 14
                    anchors.verticalCenter: parent.verticalCenter
                }
            }
            ColumnLayout {
                id: inner
                Layout.fillWidth: true
                Layout.fillHeight: true
                Layout.margins: 0
                spacing: 0
            }
        }
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        // ═══ 顶部：预测参数配置 ═══
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
                spacing: 24

                FluText {
                    text: "短时轨迹预测"
                    font: FluTextStyle.Title
                    color: FluTheme.primaryColor
                }

                // 预测窗口选择
                Row {
                    spacing: 8
                    Layout.alignment: Qt.AlignVCenter
                    FluText {
                        text: "预测时间窗口："
                        font: FluTextStyle.Body
                        color: FluTheme.fontSecondaryColor
                        anchors.verticalCenter: parent.verticalCenter
                    }
                    Repeater {
                        model: [3, 4, 5]
                        delegate: Rectangle {
                            width: 48; height: 28; radius: 4
                            color: root.predictWindowSec === modelData
                                   ? FluTheme.primaryColor
                                   : (FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f1f5f9")
                            border.color: FluTheme.dividerColor
                            border.width: 1
                            FluText {
                                text: modelData + " s"
                                font: FluTextStyle.Body
                                color: root.predictWindowSec === modelData ? "#ffffff" : FluTheme.fontPrimaryColor
                                anchors.centerIn: parent
                            }
                            MouseArea {
                                anchors.fill: parent
                                onClicked: root.predictWindowSec = modelData
                            }
                        }
                    }
                }

                // 安全管道开关
                FluToggleSwitch {
                    text: "显示未来轨迹预测安全管道"
                    checked: root.showSafetyTube
                    onCheckedChanged: root.showSafetyTube = checked
                }

                Item { Layout.fillWidth: true }

                FluText {
                    text: "滚动更新 · 每帧修正"
                    font: FluTextStyle.Caption
                    color: FluTheme.fontSecondaryColor
                }
            }
        }

        // ═══ 主体：左对象列表 + 中 3D 占位 + 右冲突列表 ═══
        RowLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            Layout.margins: 14
            spacing: 14

            // ─── 左：监测对象 ───
            PanelCard {
                Layout.preferredWidth: 320
                Layout.fillHeight: true
                title: "监测对象（显示控制）"

                ListView {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    Layout.margins: 12
                    clip: true
                    spacing: 8
                    model: root.trackedObjects

                    delegate: Rectangle {
                        width: parent.width
                        height: 78
                        radius: 4
                        color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f8fafc"
                        border.color: FluTheme.dividerColor
                        border.width: 1

                        ColumnLayout {
                            anchors.fill: parent
                            anchors.margins: 12
                            spacing: 6

                            RowLayout {
                                Layout.fillWidth: true
                                Rectangle {
                                    implicitWidth: 10; implicitHeight: 10; radius: 2
                                    color: modelData.visible ? FluColors.Green.normal : FluColors.Grey120
                                }
                                FluText {
                                    text: modelData.id
                                    font: FluTextStyle.BodyStrong
                                }
                                FluText {
                                    text: modelData.category
                                    font: FluTextStyle.Caption
                                    color: FluTheme.fontSecondaryColor
                                }
                                Item { Layout.fillWidth: true }
                                FluToggleSwitch {
                                    checked: modelData.visible
                                    scale: 0.7
                                    onCheckedChanged: {
                                        root.trackedObjects[index].visible = checked
                                    }
                                }
                            }

                            Row {
                                spacing: 14
                                FluText {
                                    text: "速度 " + modelData.speed
                                    font: FluTextStyle.Caption
                                    color: FluTheme.fontSecondaryColor
                                }
                                FluText {
                                    text: modelData.trend
                                    font: FluTextStyle.Caption
                                    color: FluTheme.fontSecondaryColor
                                }
                            }
                        }
                    }
                }
            }

            // ─── 中：三维场景占位（Unity 嵌入） ───
            Rectangle {
                Layout.fillWidth: true
                Layout.fillHeight: true
                radius: 6
                color: "#0d1a2a"
                border.color: FluTheme.dividerColor
                border.width: 1

                Column {
                    anchors.centerIn: parent
                    spacing: 8
                    FluText {
                        text: "三维数字孪生场景"
                        font: FluTextStyle.Title
                        color: "#94a3b8"
                        anchors.horizontalCenter: parent.horizontalCenter
                    }
                    FluText {
                        text: "（由 Unity 渲染：实时轨迹 + 预测轨迹 + 安全管道）"
                        font: FluTextStyle.Caption
                        color: "#64748b"
                        anchors.horizontalCenter: parent.horizontalCenter
                    }
                }
            }

            // ─── 右：预测冲突列表 ───
            PanelCard {
                Layout.preferredWidth: 440
                Layout.fillHeight: true
                title: "预测冲突（按预计发生时间排序）"

                // 空态
                FluText {
                    visible: root.predictedConflicts.length === 0
                    text: "当前预测窗口内无冲突"
                    font: FluTextStyle.BodyStrong
                    color: FluColors.Green.normal
                    Layout.alignment: Qt.AlignHCenter
                    Layout.topMargin: 40
                }

                ListView {
                    visible: root.predictedConflicts.length > 0
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    Layout.margins: 12
                    clip: true
                    spacing: 10
                    model: root.predictedConflicts

                    delegate: Rectangle {
                        width: parent.width
                        height: 136
                        radius: 4
                        color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#ffffff"
                        border.color: root.levelColor(modelData.level)
                        border.width: 2

                        ColumnLayout {
                            anchors.fill: parent
                            anchors.margins: 12
                            spacing: 8

                            // 等级 + 类型 + 倒计时
                            RowLayout {
                                Layout.fillWidth: true
                                Rectangle {
                                    implicitWidth: 64; implicitHeight: 22; radius: 11
                                    color: root.levelColor(modelData.level)
                                    FluText {
                                        text: modelData.level
                                        font: FluTextStyle.Caption
                                        color: "#ffffff"
                                        anchors.centerIn: parent
                                    }
                                }
                                FluText {
                                    text: modelData.type
                                    font: FluTextStyle.BodyStrong
                                    Layout.fillWidth: true
                                }
                                FluText {
                                    text: "T-" + modelData.timeToImpactSec.toFixed(1) + " s"
                                    font: FluTextStyle.Subtitle
                                    color: root.levelColor(modelData.level)
                                }
                            }

                            // 对象
                            FluText {
                                Layout.fillWidth: true
                                text: modelData.objectA + "  ↔  " + modelData.objectB
                                font: FluTextStyle.Body
                                elide: Text.ElideRight
                            }

                            // 距离与位置
                            Row {
                                spacing: 28
                                Column {
                                    FluText { text: "预测最小距离"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                    FluText {
                                        text: modelData.predictedMinDist
                                        font: FluTextStyle.Subtitle
                                    }
                                }
                                Column {
                                    FluText { text: "风险位置"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                    FluText {
                                        text: modelData.position
                                        font: FluTextStyle.Subtitle
                                    }
                                }
                            }
                        }
                    }
                }

                // 底部说明
                Rectangle {
                    Layout.fillWidth: true
                    Layout.preferredHeight: 64
                    color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f8fafc"
                    border.color: FluTheme.dividerColor
                    border.width: 1

                    FluText {
                        anchors.fill: parent
                        anchors.margins: 12
                        text: "提示：危险级表示未来 " + root.predictWindowSec + " s 内预测安全管道将与风险实体发生交叠，请提前减速或停止吊装动作。"
                        font: FluTextStyle.Caption
                        color: FluTheme.fontSecondaryColor
                        wrapMode: Text.WordWrap
                    }
                }
            }
        }

        // ═══ 底部状态栏 ═══
        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 36
            color: FluTheme.dark ? Qt.rgba(28/255,28/255,28/255,1) : "#ffffff"
            border.color: FluTheme.dividerColor
            border.width: 1

            Row {
                anchors.left: parent.left
                anchors.leftMargin: 16
                anchors.verticalCenter: parent.verticalCenter
                spacing: 24
                FluText {
                    text: "监测对象 " + root.trackedObjects.length + " 个"
                    font: FluTextStyle.Caption
                    color: FluTheme.fontSecondaryColor
                }
                FluText {
                    text: "预测冲突 " + root.predictedConflicts.length + " 条"
                    font: FluTextStyle.Caption
                    color: root.predictedConflicts.length > 0 ? FluColors.Orange.normal : FluColors.Green.normal
                }
                FluText {
                    text: "预测窗口 " + root.predictWindowSec + " s"
                    font: FluTextStyle.Caption
                    color: FluTheme.fontSecondaryColor
                }
            }
        }
    }
}
