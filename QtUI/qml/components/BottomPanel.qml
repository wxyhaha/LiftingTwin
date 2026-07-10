import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    id: root
    property var theme
    color: theme.bgPrimary
    border.color: theme.borderDefault
    border.width: 1

    RowLayout {
        anchors.fill: parent
        anchors.margins: 8
        spacing: 12

        // Left: Event table
        EventTable {
            Layout.fillWidth: true
            Layout.fillHeight: true
            theme: root.theme
        }

        // Right: Process info + Timeline
        RowLayout {
            Layout.preferredWidth: 600
            Layout.fillHeight: true
            spacing: 12

            // Process records
            Rectangle {
                Layout.fillWidth: true
                Layout.fillHeight: true
                color: theme.bgCard
                radius: theme.radiusMedium
                border.color: theme.borderDefault

                Column {
                    anchors.fill: parent
                    anchors.margins: theme.cardPadding
                    spacing: 8

                    Row {
                        spacing: 6
                        Rectangle { width: 3; height: 16; radius: 1; color: theme.accentTeal; anchors.verticalCenter: parent.verticalCenter }
                        Text { text: "处置记录"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
                    }

                    Row {
                        width: parent.width; spacing: 4
                        Text { text: "时间";     font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 56 }
                        Text { text: "处置内容"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 68 }
                        Text { text: "处置人";   font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 50 }
                        Text { text: "结果";     font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 36; horizontalAlignment: Text.AlignRight }
                    }

                    Repeater {
                        model: [
                            { time: "10:32:10", content: "升高 (1#) 降低吊物，减小幅度", person: "李四", result: "已解除" },
                            { time: "10:28:48", content: "升高 吊车，人员撤离到指挥区", person: "王五", result: "已解除" },
                            { time: "10:22:15", content: "风速感应，预警围栏内，等待继续吊装", person: "张三", result: "已解除" }
                        ]

                        Row {
                            width: parent.width; spacing: 4
                            Text { text: modelData.time;    font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 56 }
                            Text { text: modelData.content; font.pixelSize: theme.fontSizeSmall; color: theme.textPrimary; width: 68; elide: Text.ElideRight; maximumLineCount: 1 }
                            Text { text: modelData.person;  font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 50 }
                            Text { text: modelData.result;  font.pixelSize: theme.fontSizeSmall; color: theme.statusGreen; width: 36; horizontalAlignment: Text.AlignRight }
                        }
                    }
                }
            }

            // Timeline + playback
            Rectangle {
                Layout.preferredWidth: 200
                Layout.fillHeight: true
                color: theme.bgCard
                radius: theme.radiusMedium
                border.color: theme.borderDefault

                Column {
                    anchors.fill: parent
                    anchors.margins: theme.cardPadding
                    spacing: 8

                    Row {
                        spacing: 6
                        Rectangle { width: 3; height: 16; radius: 1; color: theme.accentBlue; anchors.verticalCenter: parent.verticalCenter }
                        Text { text: "回放控制"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
                    }

                    // Playback controls
                    Row {
                        anchors.horizontalCenter: parent.horizontalCenter
                        spacing: 8
                        Repeater {
                            model: ["⏮", "▶", "⏭", "⏩"]
                            Rectangle {
                                width: 28; height: 28; radius: 4
                                color: index === 1 ? theme.accentBlue : theme.bgInput
                                border.color: theme.borderDefault
                                Text { text: modelData; font.pixelSize: 12; color: theme.textPrimary; anchors.centerIn: parent }
                            }
                        }
                    }

                    // Speed buttons
                    Row {
                        anchors.horizontalCenter: parent.horizontalCenter
                        spacing: 4
                        Repeater {
                            model: ["0.5x", "1x", "2x", "4x"]
                            Rectangle {
                                width: 32; height: 22; radius: 4
                                color: index === 1 ? theme.accentBlue : theme.bgInput
                                border.color: theme.borderDefault
                                Text { text: modelData; font.pixelSize: theme.fontSizeTiny; color: theme.textPrimary; anchors.centerIn: parent }
                            }
                        }
                    }

                    // Progress bar
                    Column {
                        width: parent.width; spacing: 2
                        Rectangle {
                            width: parent.width; height: 6; radius: 3; color: theme.bgInput
                            Rectangle { width: parent.width * 0.75; height: parent.height; radius: 3; color: theme.accentBlue }
                        }
                        Row {
                            width: parent.width
                            Text { text: "08:00"; font.pixelSize: 8; color: theme.textMuted }
                            Item { Layout.fillWidth: true }
                            Text { text: "10:30:45"; font.pixelSize: 8; color: theme.accentCyan; font.bold: true }
                            Item { Layout.fillWidth: true }
                            Text { text: "12:00"; font.pixelSize: 8; color: theme.textMuted }
                        }
                    }

                    // Process stage
                    Column {
                        width: parent.width; spacing: 4
                        Repeater {
                            model: [
                                { label: "施工准备", pct: "100%", clr: theme.statusGreen, active: true },
                                { label: "吊装就位", pct: "75%",  clr: theme.accentBlue, active: true },
                                { label: "设备就位", pct: "0%",   clr: theme.textMuted, active: false },
                                { label: "验收检查", pct: "0%",   clr: theme.textMuted, active: false }
                            ]
                            Row {
                                spacing: 6; width: parent.width
                                Rectangle { width: 8; height: 8; radius: 4; color: modelData.clr }
                                Text { text: modelData.label; font.pixelSize: theme.fontSizeTiny; color: modelData.clr }
                                Item { Layout.fillWidth: true }
                                Text { text: modelData.pct; font.pixelSize: theme.fontSizeTiny; color: modelData.active ? theme.textSecondary : theme.textMuted }
                            }
                        }
                    }
                }
            }
        }
    }
}
