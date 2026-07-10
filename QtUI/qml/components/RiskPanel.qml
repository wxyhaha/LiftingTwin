import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    property var theme
    height: riskCol.implicitHeight + 24
    color: theme.bgCard
    radius: theme.radiusMedium
    border.color: theme.borderDefault

    Column {
        id: riskCol
        anchors.fill: parent
        anchors.margins: theme.cardPadding
        spacing: 10

        Row {
            spacing: 6
            Rectangle { width: 3; height: 16; radius: 1; color: theme.accentCyan; anchors.verticalCenter: parent.verticalCenter }
            Text { text: "风险预警面板"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        // Risk level badge
        Rectangle {
            width: parent.width
            height: 48
            radius: theme.radiusMedium
            color: Qt.rgba(1, 0.1, 0.1, 0.12)
            border.color: theme.riskRed

            Row {
                anchors.centerIn: parent
                spacing: 8
                Text { text: "⚠"; font.pixelSize: 20; color: theme.riskRed }
                Text { text: "高风险"; font.pixelSize: theme.fontSizeMedium; font.bold: true; color: theme.riskRed }
            }
        }

        Column {
            width: parent.width
            spacing: 8

            Repeater {
                model: [
                    { label: "当前风险等级", value: "高风险", vclr: theme.riskRed },
                    { label: "风险对象",     value: "吊物（主变压器）", vclr: theme.textPrimary },
                    { label: "告警原因",     value: "吊物与高压母线\n安全距离不足", vclr: theme.statusYellow },
                    { label: "告警时间",     value: "2024-06-18 10:30:38", vclr: theme.textSecondary }
                ]

                Row {
                    spacing: 8
                    Text { text: modelData.label; font.pixelSize: theme.fontSizeSmall; color: theme.textMuted; width: 80 }
                    Text { text: modelData.value; font.pixelSize: theme.fontSizeSmall; color: modelData.vclr; wrapMode: Text.WordWrap; width: parent.width - 90 }
                }
            }

            Rectangle { width: parent.width; height: 1; color: theme.borderDefault }

            // Safety distance
            Column {
                spacing: 4
                width: parent.width

                Row {
                    spacing: 8
                    Text { text: "安全距离"; font.pixelSize: theme.fontSizeSmall; color: theme.textMuted; width: 80 }
                    Column {
                        spacing: 2
                        Row { spacing: 4
                            Text { text: "实际距离："; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary }
                            Text { text: "4.2 m"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.riskRed }
                        }
                        Row { spacing: 4
                            Text { text: "最小距离 dmin："; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary }
                            Text { text: "6.0 m"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textSecondary }
                        }
                    }
                }
            }

            Rectangle { width: parent.width; height: 1; color: theme.borderDefault }

            Row {
                spacing: 8
                Text { text: "处置建议"; font.pixelSize: theme.fontSizeSmall; color: theme.textMuted; width: 80 }
                Text {
                    text: "请提升吊物高度或调整吊车位置，确保吊物与高压母线安全距离大于dmin。"
                    font.pixelSize: theme.fontSizeSmall
                    color: theme.textSecondary
                    wrapMode: Text.WordWrap
                    width: parent.width - 90
                }
            }

            Rectangle { width: parent.width; height: 1; color: theme.borderDefault }

            Text { text: "风险等级说明"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary }

            Repeater {
                model: [
                    { label: "蓝色 — 低风险",    clr: theme.riskBlue },
                    { label: "黄色 — 一般风险",  clr: theme.riskYellow },
                    { label: "橙色 — 较高风险",  clr: theme.riskOrange },
                    { label: "红色 — 高风险",    clr: theme.riskRed }
                ]

                Row {
                    spacing: 6
                    Rectangle { width: 12; height: 12; radius: 2; color: modelData.clr; anchors.verticalCenter: parent.verticalCenter }
                    Text { text: modelData.label; font.pixelSize: theme.fontSizeTiny; color: theme.textSecondary; anchors.verticalCenter: parent.verticalCenter }
                }
            }
        }
    }
}
