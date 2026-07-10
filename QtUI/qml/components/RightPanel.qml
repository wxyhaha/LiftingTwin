import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    id: root
    property var theme
    color: theme.bgPrimary
    border.color: theme.borderDefault
    border.width: 1

    Flickable {
        anchors.fill: parent
        anchors.margins: 8
        contentHeight: rightCol.implicitHeight
        clip: true
        ScrollBar.vertical: ScrollBar { policy: ScrollBar.AsNeeded }

        Column {
            id: rightCol
            width: parent.width
            spacing: 12

            RiskPanel { width: parent.width; theme: root.theme }
            AlarmList { width: parent.width; theme: root.theme }

            // Video placeholder
            Rectangle {
                width: parent.width; height: 140
                color: theme.bgCard; radius: theme.radiusMedium; border.color: theme.borderDefault

                Column {
                    anchors.centerIn: parent; spacing: 4
                    Text { text: "📹"; font.pixelSize: 24; anchors.horizontalCenter: parent.horizontalCenter }
                    Text { text: "视频监控画面"; font.pixelSize: theme.fontSizeSmall; color: theme.textMuted; anchors.horizontalCenter: parent.horizontalCenter }

                    Row {
                        anchors.horizontalCenter: parent.horizontalCenter; spacing: 4
                        Repeater {
                            model: 4
                            Rectangle {
                                width: 60; height: 45; radius: 4; color: theme.bgInput; border.color: theme.borderDefault
                                Text { text: "监控" + (index + 1); font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; anchors.centerIn: parent }
                            }
                        }
                    }
                }
            }

            // Crane status right
            Rectangle {
                width: parent.width; height: craneCol.implicitHeight + 24
                color: theme.bgCard; radius: theme.radiusMedium; border.color: theme.borderDefault

                Column {
                    id: craneCol
                    anchors.fill: parent; anchors.margins: theme.cardPadding; spacing: 8

                    Row {
                        spacing: 6
                        Rectangle { width: 3; height: 16; radius: 1; color: theme.accentCyan; anchors.verticalCenter: parent.verticalCenter }
                        Text { text: "吊车工况参数 (1#)"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
                    }

                    Repeater {
                        model: [
                            { label: "臂长", value: "32.5 m", pct: 0.65 },
                            { label: "幅度", value: "12.8 m", pct: 0.45 },
                            { label: "高度", value: "25.6 m", pct: 0.55 },
                            { label: "风速", value: "2.2 m/s", pct: 0.22 }
                        ]

                        Column {
                            spacing: 2; width: parent.width
                            Row {
                                width: parent.width
                                Text { text: modelData.label; font.pixelSize: theme.fontSizeTiny; color: theme.textSecondary; width: 36 }
                                Text { text: modelData.value; font.pixelSize: theme.fontSizeTiny; color: theme.textPrimary; width: 50; horizontalAlignment: Text.AlignRight }
                            }
                            Rectangle {
                                width: parent.width; height: 4; radius: 2; color: theme.bgInput
                                Rectangle { width: parent.width * modelData.pct; height: parent.height; radius: 2; color: theme.accentBlue }
                            }
                        }
                    }
                }
            }

            // Map placeholder
            Rectangle {
                width: parent.width; height: 120
                color: theme.bgCard; radius: theme.radiusMedium; border.color: theme.borderDefault

                Column {
                    anchors.centerIn: parent; spacing: 4
                    Text { text: "🗺"; font.pixelSize: 20; anchors.horizontalCenter: parent.horizontalCenter }
                    Text { text: "电子地图 / 小地图"; font.pixelSize: theme.fontSizeSmall; color: theme.textMuted; anchors.horizontalCenter: parent.horizontalCenter }
                }
            }
        }
    }
}
