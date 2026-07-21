import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import FluentUI 1.0

// ═══════════════════════════════════════════════════════════════════════════
// 系统运行监控窗口（FluentUI 风格）
// 依据开题报告 §4.6.2 / §4.6.3：
//   Tab1「预警链路状态」—— 设备在线、数据频率、时间同步、通信延迟、
//                          处理队列、计算资源、目标跟踪、算法模块状态
//   Tab2「风险事件归档与追溯复盘」—— 本窗口重点，内嵌 RiskEventArchivePanel
// ═══════════════════════════════════════════════════════════════════════════
ApplicationWindow {
    visible: true
    id: root
    width: 1600
    height: 960
    title: "系统运行监控"

    // 主题：浅色 + 深蓝主色（贴合参考设计图）
    Component.onCompleted: {
        FluTheme.primaryColor = "#01469a"
    }

    // 设备状态 → 颜色
    function statusColor(status) {
        switch (status) {
        case "在线":   return FluColors.Green.normal
        case "异常":   return FluColors.Red.normal
        case "降级":   return FluColors.Orange.normal
        case "离线":   return FluColors.Grey120
        default:       return FluColors.Grey120
        }
    }

    // 开题报告 §4.2 / §4.6.1 列出的现场设备清单
    property var deviceList: [
        { name: "可见光相机",       category: "现场感知端", status: "在线", metric: "30 fps" },
        { name: "三维激光雷达",     category: "现场感知端", status: "在线", metric: "10 Hz · 内置 IMU" },
        { name: "双轴智能云台",     category: "现场感知端", status: "在线", metric: "水平 360° / 仰俯 ±60°" },
        { name: "移动小车底盘",     category: "现场感知端", status: "在线", metric: "待命" },
        { name: "声光报警终端",     category: "现场感知端", status: "在线", metric: "待命" },
        { name: "边缘计算设备",     category: "边缘计算端", status: "在线", metric: "GPU 42% · CPU 38%" },
        { name: "工业无线局域网",   category: "通信链路",   status: "在线", metric: "延迟 8 ms" },
        { name: "远端监控终端",     category: "远端监控端", status: "在线", metric: "本机" }
    ]

    // 开题报告 §4.6.1 列出的算法模块
    property var algorithmModules: [
        { name: "静态场景建模",     status: "在线", metric: "模型 v20260721-A" },
        { name: "动态对象监测",     status: "在线", metric: "6 个目标 · 10 Hz" },
        { name: "连续状态估计",     status: "在线", metric: "跟踪稳定" },
        { name: "趋势预判",         status: "在线", metric: "预测窗口 5 s" },
        { name: "风险研判",         status: "在线", metric: "规则库 v1.2" },
        { name: "预警与记录",       status: "在线", metric: "归档正常" }
    ]

    // 通用卡片组件
    component StatusCard: Rectangle {
        default property alias contentData: inner.data
        property string title: ""
        color: FluTheme.dark ? Qt.rgba(32/255,32/255,32/255,1) : "#ffffff"
        radius: 6
        border.color: FluTheme.dividerColor
        border.width: 1

        ColumnLayout {
            anchors.fill: parent
            anchors.margins: 14
            spacing: 10
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
                spacing: 10
            }
        }
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        // ═══ 顶部：Tab 栏（默认聚焦「风险事件归档与追溯复盘」） ═══
        FluPivot {
            id: pivot
            Layout.fillWidth: true
            Layout.preferredHeight: 44
            currentIndex: 1

            FluPivotItem {
                title: "预警链路状态"
            }
            FluPivotItem {
                title: "风险事件归档与追溯复盘"
            }
        }

        FluDivider { Layout.fillWidth: true }

        // ═══ Tab 内容 ═══
        StackLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            currentIndex: pivot.currentIndex

            // ─────────── Tab 1：预警链路状态 ───────────
            Rectangle {
                color: FluTheme.dark ? Qt.rgba(24/255,24/255,24/255,1) : "#f5f7fa"

                ScrollView {
                    anchors.fill: parent
                    anchors.margins: 14
                    clip: true
                    contentWidth: availableWidth

                    ColumnLayout {
                        width: parent.parent.width - 28
                        spacing: 14

                        // (1) 设备在线状态
                        StatusCard {
                            Layout.fillWidth: true
                            Layout.preferredHeight: 260
                            title: "设备在线状态（现场感知端 / 边缘计算端 / 通信链路 / 远端监控端）"

                            GridLayout {
                                Layout.fillWidth: true
                                Layout.fillHeight: true
                                columns: 4
                                rowSpacing: 10
                                columnSpacing: 10

                                Repeater {
                                    model: root.deviceList
                                    delegate: Rectangle {
                                        Layout.fillWidth: true
                                        Layout.preferredHeight: 88
                                        radius: 4
                                        color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f8fafc"
                                        border.color: FluTheme.dividerColor
                                        border.width: 1

                                        ColumnLayout {
                                            anchors.fill: parent
                                            anchors.margins: 12
                                            spacing: 4
                                            RowLayout {
                                                Layout.fillWidth: true
                                                FluText {
                                                    text: modelData.name
                                                    font: FluTextStyle.BodyStrong
                                                    Layout.fillWidth: true
                                                }
                                                Rectangle {
                                                    implicitWidth: 8; implicitHeight: 8; radius: 4
                                                    color: root.statusColor(modelData.status)
                                                }
                                                FluText {
                                                    text: modelData.status
                                                    font: FluTextStyle.Caption
                                                    color: root.statusColor(modelData.status)
                                                }
                                            }
                                            FluText {
                                                text: modelData.category
                                                font: FluTextStyle.Caption
                                                color: FluTheme.fontSecondaryColor
                                            }
                                            FluText {
                                                text: modelData.metric
                                                font: FluTextStyle.Caption
                                                color: FluTheme.fontSecondaryColor
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // (2) 链路质量
                        StatusCard {
                            Layout.fillWidth: true
                            Layout.preferredHeight: 140
                            title: "链路质量"

                            GridLayout {
                                Layout.fillWidth: true
                                columns: 4
                                columnSpacing: 24
                                rowSpacing: 6

                                Column {
                                    FluText { text: "通信延迟"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                    FluText { text: "8 ms"; font: FluTextStyle.Title; color: FluColors.Green.normal }
                                }
                                Column {
                                    FluText { text: "点云接收频率"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                    FluText { text: "10 Hz"; font: FluTextStyle.Title; color: FluColors.Green.normal }
                                }
                                Column {
                                    FluText { text: "时间同步状态"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                    FluText { text: "已同步"; font: FluTextStyle.Title; color: FluColors.Green.normal }
                                }
                                Column {
                                    FluText { text: "处理队列长度"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                    FluText { text: "3 帧"; font: FluTextStyle.Title; color: FluColors.Green.normal }
                                }
                            }
                        }

                        // (3) 算法模块运行状态
                        StatusCard {
                            Layout.fillWidth: true
                            Layout.preferredHeight: 220
                            title: "算法模块运行状态"

                            GridLayout {
                                Layout.fillWidth: true
                                columns: 3
                                columnSpacing: 10
                                rowSpacing: 10

                                Repeater {
                                    model: root.algorithmModules
                                    delegate: Rectangle {
                                        Layout.fillWidth: true
                                        Layout.preferredHeight: 66
                                        radius: 4
                                        color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f8fafc"
                                        border.color: FluTheme.dividerColor
                                        border.width: 1

                                        ColumnLayout {
                                            anchors.fill: parent
                                            anchors.margins: 12
                                            spacing: 4
                                            RowLayout {
                                                Layout.fillWidth: true
                                                FluText {
                                                    text: modelData.name
                                                    font: FluTextStyle.BodyStrong
                                                    Layout.fillWidth: true
                                                }
                                                Rectangle {
                                                    implicitWidth: 8; implicitHeight: 8; radius: 4
                                                    color: root.statusColor(modelData.status)
                                                }
                                            }
                                            FluText {
                                                text: modelData.metric
                                                font: FluTextStyle.Caption
                                                color: FluTheme.fontSecondaryColor
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // (4) 最近异常日志
                        StatusCard {
                            Layout.fillWidth: true
                            Layout.preferredHeight: 170
                            title: "最近异常与处理"

                            Column {
                                spacing: 6
                                Layout.fillWidth: true
                                FluText { text: "10:14:22  相机帧短暂丢失 200 ms，已自动重连"; font: FluTextStyle.Body; color: FluTheme.fontSecondaryColor }
                                FluText { text: "09:58:10  点云队列积压超过阈值，已自动清理缓存"; font: FluTextStyle.Body; color: FluTheme.fontSecondaryColor }
                                FluText { text: "09:30:45  无线链路抖动，边缘侧本地缓存待同步"; font: FluTextStyle.Body; color: FluTheme.fontSecondaryColor }
                            }
                        }
                    }
                }
            }

            // ─────────── Tab 2：风险事件归档与追溯复盘（本窗口重点） ───────────
            RiskEventArchivePanel {
                // 直接内嵌，填满
            }
        }
    }
}
