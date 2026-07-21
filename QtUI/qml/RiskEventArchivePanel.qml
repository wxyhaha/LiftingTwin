import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15
import FluentUI 1.0

// ═══════════════════════════════════════════════════════════════════════════
// 风险事件归档与追溯复盘面板（FluentUI 风格）
// 依据开题报告 §4.6.3：以风险事件为基本单元归档，支持按 时间 / 风险类型 /
// 目标编号 / 风险等级 / 处置状态 查询，联动三维场景回放历史与预测轨迹，
// 支持处置登记与结果导出，形成"识别—校验—预警—记录—复盘"闭环。
// ═══════════════════════════════════════════════════════════════════════════
Item {
    id: root

    // ─── 当前选中查看的事件 ───
    property var selectedEvent: null

    // 风险等级 → FluentUI 主题色（开题报告 §4.5.3 四级预警）
    function levelColor(level) {
        switch (level) {
        case "提示级": return FluColors.Blue.normal
        case "预警级": return FluColors.Yellow.normal
        case "危险级": return FluColors.Orange.normal
        case "越限级": return FluColors.Red.normal
        default:       return FluColors.Grey120
        }
    }
    function handleStatusColor(status) {
        switch (status) {
        case "已闭环": return FluColors.Green.normal
        case "处置中": return FluColors.Orange.normal
        case "未处置": return FluColors.Red.normal
        default:       return FluColors.Grey120
        }
    }

    // ─── 演示数据：字段严格对应开题报告 §4.6.3 风险事件归档字段 ───
    property var eventList: [
        {
            id: "EV-20260721-003",
            startTime: "2026-07-21 10:30:38",
            endTime:   "2026-07-21 10:32:14",
            riskType:  "近电安全净距不足",
            level:     "越限级",
            targetId:  "T-CARGO-01",
            targetCategory: "吊物（主变压器）",
            coord:     "(12.5, 8.3, 25.6)",
            relatedEntity: "500kV 高压母线 A 相",
            currentMinDist:   "4.2 m",
            predictedMinDist: "3.5 m",
            triggerRule: "实时最小距离 < 最小安全净距（6.0 m）",
            levelHistory: [
                { level: "预警级", time: "10:29:52" },
                { level: "危险级", time: "10:30:15" },
                { level: "越限级", time: "10:30:38" }
            ],
            handleStatus: "已闭环",
            handleMethod: "提升吊物高度，调整吊车位置",
            handleTime:   "2026-07-21 10:32:10",
            handleResult: "吊物与母线距离恢复至 7.8 m，告警解除"
        },
        {
            id: "EV-20260721-002",
            startTime: "2026-07-21 10:28:15",
            endTime:   "2026-07-21 10:29:40",
            riskType:  "人员车辆闯入风险区域",
            level:     "危险级",
            targetId:  "T-WORKER-04",
            targetCategory: "作业人员",
            coord:     "(8.1, 15.2, 0.0)",
            relatedEntity: "吊装作业半径（禁入区）",
            currentMinDist:   "0.0 m",
            predictedMinDist: "—",
            triggerRule: "人员三维包络与禁入区边界发生实际交叠",
            levelHistory: [
                { level: "预警级", time: "10:27:50" },
                { level: "危险级", time: "10:28:15" }
            ],
            handleStatus: "已闭环",
            handleMethod: "现场喊话，人员撤离至安全通道",
            handleTime:   "2026-07-21 10:29:32",
            handleResult: "人员退出禁入区，告警解除"
        },
        {
            id: "EV-20260721-001",
            startTime: "2026-07-21 10:22:07",
            endTime:   "",
            riskType:  "防护装备异常",
            level:     "提示级",
            targetId:  "T-WORKER-07",
            targetCategory: "作业人员",
            coord:     "(3.5, 20.4, 0.0)",
            relatedEntity: "吊装作业区",
            currentMinDist:   "—",
            predictedMinDist: "—",
            triggerRule: "同一人员在作业区域内连续 5 s 未检测到安全帽",
            levelHistory: [
                { level: "提示级", time: "10:22:07" }
            ],
            handleStatus: "未处置",
            handleMethod: "",
            handleTime:   "",
            handleResult: ""
        }
    ]

    Component.onCompleted: {
        if (eventList.length > 0) selectedEvent = eventList[0]
    }

    // 内部通用卡片样式
    component RiskCard: Rectangle {
        default property alias contentData: inner.data
        property string title: ""
        color: FluTheme.dark ? Qt.rgba(32/255,32/255,32/255,1) : "#ffffff"
        radius: 6
        border.color: FluTheme.dividerColor
        border.width: 1

        ColumnLayout {
            anchors.fill: parent
            anchors.margins: 14
            spacing: 8
            FluText {
                visible: title !== ""
                text: title
                font: FluTextStyle.BodyStrong
                color: FluTheme.primaryColor
            }
            ColumnLayout {
                id: inner
                Layout.fillWidth: true
                Layout.fillHeight: true
                spacing: 8
            }
        }
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        // ═══ 顶部：查询过滤条 ═══
        Rectangle {
            Layout.fillWidth: true
            Layout.preferredHeight: 64
            color: FluTheme.dark ? Qt.rgba(28/255,28/255,28/255,1) : "#ffffff"
            border.color: FluTheme.dividerColor
            border.width: 1

            RowLayout {
                anchors.fill: parent
                anchors.leftMargin: 16
                anchors.rightMargin: 16
                spacing: 8

                FluText { text: "时间"; font: FluTextStyle.Caption }
                FluTextBox { placeholderText: "开始时间"; implicitWidth: 130 }
                FluText { text: "至"; font: FluTextStyle.Caption }
                FluTextBox { placeholderText: "结束时间"; implicitWidth: 130 }

                FluText { text: "类型"; font: FluTextStyle.Caption; Layout.leftMargin: 8 }
                FluComboBox {
                    implicitWidth: 190
                    model: ["全部","近电安全净距不足","吊臂吊物与构架设备碰撞","人员车辆闯入风险区域","吊物下方站人","防护装备异常"]
                }

                FluText { text: "等级"; font: FluTextStyle.Caption; Layout.leftMargin: 8 }
                FluComboBox {
                    implicitWidth: 100
                    model: ["全部","提示级","预警级","危险级","越限级"]
                }

                FluText { text: "目标"; font: FluTextStyle.Caption; Layout.leftMargin: 8 }
                FluTextBox { placeholderText: "T-CARGO-01"; implicitWidth: 120 }

                FluText { text: "状态"; font: FluTextStyle.Caption; Layout.leftMargin: 8 }
                FluComboBox {
                    implicitWidth: 100
                    model: ["全部","未处置","处置中","已闭环"]
                }

                Item { Layout.fillWidth: true }

                FluFilledButton { text: "查询" }
                FluButton { text: "导出" }
            }
        }

        // ═══ 主体：左事件列表 + 右事件详情 ═══
        SplitView {
            Layout.fillWidth: true
            Layout.fillHeight: true
            orientation: Qt.Horizontal

            // ─── 左：事件列表 ───
            Rectangle {
                SplitView.preferredWidth: 640
                SplitView.minimumWidth: 420
                color: FluTheme.dark ? Qt.rgba(28/255,28/255,28/255,1) : "#ffffff"

                ColumnLayout {
                    anchors.fill: parent
                    spacing: 0

                    // 表头
                    Rectangle {
                        Layout.fillWidth: true
                        Layout.preferredHeight: 38
                        color: FluTheme.dark ? Qt.rgba(40/255,40/255,40/255,1) : "#f8fafc"
                        border.color: FluTheme.dividerColor
                        border.width: 1

                        Row {
                            anchors.fill: parent
                            anchors.leftMargin: 16
                            spacing: 0
                            FluText { width: 150; text: "事件编号"; font: FluTextStyle.Caption; anchors.verticalCenter: parent.verticalCenter }
                            FluText { width: 130; text: "开始时间"; font: FluTextStyle.Caption; anchors.verticalCenter: parent.verticalCenter }
                            FluText { width: 180; text: "风险类型"; font: FluTextStyle.Caption; anchors.verticalCenter: parent.verticalCenter }
                            FluText { width: 70;  text: "等级";     font: FluTextStyle.Caption; anchors.verticalCenter: parent.verticalCenter }
                            FluText { width: 90;  text: "处置状态"; font: FluTextStyle.Caption; anchors.verticalCenter: parent.verticalCenter }
                        }
                    }

                    ListView {
                        id: eventListView
                        Layout.fillWidth: true
                        Layout.fillHeight: true
                        clip: true
                        model: root.eventList

                        delegate: Rectangle {
                            width: eventListView.width
                            height: 56
                            color: root.selectedEvent === modelData
                                   ? FluTools.withOpacity(FluTheme.primaryColor, 0.12)
                                   : (ma.containsMouse
                                      ? (FluTheme.dark ? Qt.rgba(255/255,255/255,255/255,0.04) : Qt.rgba(0/255,0/255,0/255,0.03))
                                      : "transparent")
                            border.color: root.selectedEvent === modelData ? FluTheme.primaryColor : "transparent"
                            border.width: root.selectedEvent === modelData ? 1 : 0
                            radius: 4

                            MouseArea {
                                id: ma
                                anchors.fill: parent
                                hoverEnabled: true
                                onClicked: root.selectedEvent = modelData
                            }

                            Row {
                                anchors.fill: parent
                                anchors.leftMargin: 16
                                spacing: 0

                                FluText {
                                    width: 150
                                    text: modelData.id
                                    font: FluTextStyle.Body
                                    elide: Text.ElideRight
                                    anchors.verticalCenter: parent.verticalCenter
                                }
                                FluText {
                                    width: 130
                                    text: modelData.startTime.substring(5)
                                    font: FluTextStyle.Caption
                                    color: FluTheme.fontSecondaryColor
                                    anchors.verticalCenter: parent.verticalCenter
                                }
                                FluText {
                                    width: 180
                                    text: modelData.riskType
                                    font: FluTextStyle.Body
                                    elide: Text.ElideRight
                                    anchors.verticalCenter: parent.verticalCenter
                                }
                                Item {
                                    width: 70
                                    height: parent.height
                                    Rectangle {
                                        width: 56; height: 22; radius: 11
                                        anchors.centerIn: parent
                                        color: root.levelColor(modelData.level)
                                        FluText {
                                            text: modelData.level
                                            font: FluTextStyle.Caption
                                            color: "#ffffff"
                                            anchors.centerIn: parent
                                        }
                                    }
                                }
                                FluText {
                                    width: 90
                                    text: modelData.handleStatus
                                    font: FluTextStyle.Body
                                    color: root.handleStatusColor(modelData.handleStatus)
                                    anchors.verticalCenter: parent.verticalCenter
                                }
                            }
                        }
                    }
                }
            }

            // ─── 右：事件详情（可滚动） ───
            Rectangle {
                SplitView.fillWidth: true
                color: FluTheme.dark ? Qt.rgba(24/255,24/255,24/255,1) : "#f5f7fa"

                ScrollView {
                    id: detailScroll
                    anchors.fill: parent
                    clip: true
                    contentWidth: availableWidth

                    ColumnLayout {
                        width: detailScroll.availableWidth
                        spacing: 12

                        FluText {
                            visible: root.selectedEvent === null
                            text: "从左侧列表选择事件以查看归档详情"
                            font: FluTextStyle.Body
                            color: FluTheme.fontSecondaryColor
                            Layout.alignment: Qt.AlignHCenter
                            Layout.topMargin: 60
                        }

                        ColumnLayout {
                            visible: root.selectedEvent !== null
                            Layout.fillWidth: true
                            Layout.margins: 14
                            spacing: 12

                            // (1) 标题卡：事件编号 + 当前等级
                            Rectangle {
                                Layout.fillWidth: true
                                Layout.preferredHeight: 76
                                radius: 6
                                color: FluTheme.dark ? Qt.rgba(32/255,32/255,32/255,1) : "#ffffff"
                                border.color: root.selectedEvent ? root.levelColor(root.selectedEvent.level) : FluTheme.dividerColor
                                border.width: 2

                                RowLayout {
                                    anchors.fill: parent
                                    anchors.margins: 16
                                    ColumnLayout {
                                        Layout.fillWidth: true
                                        spacing: 4
                                        FluText {
                                            text: root.selectedEvent ? root.selectedEvent.id : ""
                                            font: FluTextStyle.Subtitle
                                        }
                                        FluText {
                                            text: root.selectedEvent ? (root.selectedEvent.riskType + " · " + root.selectedEvent.targetCategory) : ""
                                            font: FluTextStyle.Caption
                                            color: FluTheme.fontSecondaryColor
                                        }
                                    }
                                    Rectangle {
                                        Layout.preferredWidth: 96
                                        Layout.preferredHeight: 34
                                        radius: 4
                                        color: root.selectedEvent ? root.levelColor(root.selectedEvent.level) : FluColors.Grey120
                                        FluText {
                                            text: root.selectedEvent ? root.selectedEvent.level : ""
                                            font: FluTextStyle.BodyStrong
                                            color: "#ffffff"
                                            anchors.centerIn: parent
                                        }
                                    }
                                }
                            }

                            // (2) 等级发展过程（开题报告 §4.6.3：等级变化避免拆分独立告警）
                            RiskCard {
                                Layout.fillWidth: true
                                Layout.preferredHeight: 116
                                title: "风险等级发展过程"

                                Row {
                                    spacing: 6
                                    Layout.alignment: Qt.AlignVCenter
                                    Repeater {
                                        model: root.selectedEvent ? root.selectedEvent.levelHistory : []
                                        delegate: Row {
                                            spacing: 6
                                            Rectangle {
                                                width: 116; height: 48; radius: 4
                                                color: root.levelColor(modelData.level)
                                                Column {
                                                    anchors.centerIn: parent
                                                    FluText { text: modelData.level; font: FluTextStyle.BodyStrong; color: "#ffffff"; anchors.horizontalCenter: parent.horizontalCenter }
                                                    FluText { text: modelData.time;  font: FluTextStyle.Caption; color: "#ffffff"; anchors.horizontalCenter: parent.horizontalCenter }
                                                }
                                            }
                                            FluText {
                                                visible: index < (root.selectedEvent ? root.selectedEvent.levelHistory.length - 1 : 0)
                                                text: "→"
                                                font: FluTextStyle.Subtitle
                                                color: FluTheme.fontSecondaryColor
                                                anchors.verticalCenter: parent.verticalCenter
                                            }
                                        }
                                    }
                                }
                            }

                            // (3) 基础信息
                            RiskCard {
                                Layout.fillWidth: true
                                Layout.preferredHeight: 152
                                title: "基础信息"

                                Grid {
                                    columns: 2
                                    columnSpacing: 24
                                    rowSpacing: 6
                                    Layout.fillWidth: true
                                    FluText { text: "开始时间：  " + (root.selectedEvent ? root.selectedEvent.startTime : ""); font: FluTextStyle.Body }
                                    FluText { text: "结束时间：  " + (root.selectedEvent && root.selectedEvent.endTime !== "" ? root.selectedEvent.endTime : "（未解除）"); font: FluTextStyle.Body }
                                    FluText { text: "目标编号：  " + (root.selectedEvent ? root.selectedEvent.targetId : ""); font: FluTextStyle.Body }
                                    FluText { text: "目标类别：  " + (root.selectedEvent ? root.selectedEvent.targetCategory : ""); font: FluTextStyle.Body }
                                    FluText { text: "三维坐标：  " + (root.selectedEvent ? root.selectedEvent.coord : ""); font: FluTextStyle.Body }
                                    FluText { text: "关联实体：  " + (root.selectedEvent ? root.selectedEvent.relatedEntity : ""); font: FluTextStyle.Body }
                                }
                            }

                            // (4) 距离与触发规则
                            RiskCard {
                                Layout.fillWidth: true
                                Layout.preferredHeight: 140
                                title: "距离与触发规则"

                                Row {
                                    spacing: 32
                                    Column {
                                        FluText { text: "实时最小距离"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                        FluText {
                                            text: root.selectedEvent ? root.selectedEvent.currentMinDist : "—"
                                            font: FluTextStyle.Title
                                            color: root.selectedEvent ? root.levelColor(root.selectedEvent.level) : FluTheme.fontPrimaryColor
                                        }
                                    }
                                    Column {
                                        FluText { text: "预测最小距离"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                        FluText {
                                            text: root.selectedEvent ? root.selectedEvent.predictedMinDist : "—"
                                            font: FluTextStyle.Title
                                        }
                                    }
                                }
                                FluText {
                                    Layout.fillWidth: true
                                    text: "触发规则：" + (root.selectedEvent ? root.selectedEvent.triggerRule : "")
                                    font: FluTextStyle.Caption
                                    color: FluTheme.fontSecondaryColor
                                    wrapMode: Text.WordWrap
                                }
                            }

                            // (5) 现场证据
                            RiskCard {
                                Layout.fillWidth: true
                                Layout.preferredHeight: 190
                                title: "现场证据"

                                Row {
                                    spacing: 12
                                    Rectangle {
                                        width: 220; height: 130
                                        radius: 4
                                        color: FluTheme.dark ? Qt.rgba(20/255,20/255,20/255,1) : "#1e293b"
                                        FluText { text: "现场图像"; color: "#94a3b8"; font: FluTextStyle.Caption; anchors.centerIn: parent }
                                    }
                                    Rectangle {
                                        width: 220; height: 130
                                        radius: 4
                                        color: FluTheme.dark ? Qt.rgba(20/255,20/255,20/255,1) : "#1e293b"
                                        FluText { text: "视频片段回放"; color: "#94a3b8"; font: FluTextStyle.Caption; anchors.centerIn: parent }
                                    }
                                }
                            }

                            // (6) 三维轨迹回放
                            RiskCard {
                                Layout.fillWidth: true
                                Layout.preferredHeight: 140
                                title: "三维轨迹回放（历史轨迹 + 预测轨迹）"

                                Row {
                                    spacing: 8
                                    FluIconButton { iconSource: FluentIcons.Previous; iconSize: 14 }
                                    FluIconButton { iconSource: FluentIcons.Play;     iconSize: 14 }
                                    FluIconButton { iconSource: FluentIcons.Pause;    iconSize: 14 }
                                    FluComboBox {
                                        implicitWidth: 90
                                        model: ["0.5×","1×","2×","4×"]
                                        currentIndex: 1
                                    }
                                    FluCheckBox { text: "显示预测轨迹"; checked: true }
                                    FluCheckBox { text: "显示安全管道"; checked: true }
                                }

                                FluSlider {
                                    Layout.fillWidth: true
                                    from: 0; to: 1; value: 0.6
                                }
                            }

                            // (7) 处置信息
                            RiskCard {
                                Layout.fillWidth: true
                                Layout.preferredHeight: 200
                                title: "处置信息"

                                Row {
                                    spacing: 8
                                    FluText { text: "当前状态"; font: FluTextStyle.Caption; color: FluTheme.fontSecondaryColor }
                                    Rectangle {
                                        width: 70; height: 20; radius: 10
                                        color: root.selectedEvent ? root.handleStatusColor(root.selectedEvent.handleStatus) : FluColors.Grey120
                                        FluText {
                                            text: root.selectedEvent ? root.selectedEvent.handleStatus : ""
                                            font: FluTextStyle.Caption; color: "#ffffff"
                                            anchors.centerIn: parent
                                        }
                                    }
                                }
                                FluText {
                                    Layout.fillWidth: true
                                    text: "处置方式：" + (root.selectedEvent && root.selectedEvent.handleMethod !== "" ? root.selectedEvent.handleMethod : "（待登记）")
                                    font: FluTextStyle.Body
                                    wrapMode: Text.WordWrap
                                }
                                FluText {
                                    text: "处置时间：" + (root.selectedEvent && root.selectedEvent.handleTime !== "" ? root.selectedEvent.handleTime : "—")
                                    font: FluTextStyle.Body
                                }
                                FluText {
                                    Layout.fillWidth: true
                                    text: "处理结果：" + (root.selectedEvent && root.selectedEvent.handleResult !== "" ? root.selectedEvent.handleResult : "—")
                                    font: FluTextStyle.Body
                                    wrapMode: Text.WordWrap
                                }

                                Row {
                                    spacing: 8
                                    FluFilledButton {
                                        text: "登记处置"
                                        disabled: !(root.selectedEvent && root.selectedEvent.handleStatus !== "已闭环")
                                    }
                                    FluButton { text: "导出该事件" }
                                }
                            }
                        }
                    }
                }
            }
        }

        // ═══ 底部：统计栏 ═══
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
                    text: "共 " + root.eventList.length + " 条事件"
                    font: FluTextStyle.Caption
                    color: FluTheme.fontSecondaryColor
                }
                FluText {
                    text: "未闭环 " + root.eventList.filter(function(e){ return e.handleStatus !== "已闭环" }).length
                    font: FluTextStyle.Caption
                    color: FluColors.Red.normal
                }
                FluText {
                    text: "已闭环 " + root.eventList.filter(function(e){ return e.handleStatus === "已闭环" }).length
                    font: FluTextStyle.Caption
                    color: FluColors.Green.normal
                }
            }
        }
    }
}
