import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    id: root
    property var theme
    height: 56
    color: theme.bgSecondary
    border.color: theme.borderDefault
    border.width: 1

    RowLayout {
        anchors.fill: parent
        anchors.leftMargin: 16
        anchors.rightMargin: 16
        spacing: 0

        // ── Logo + Title ──
        Item {
            Layout.preferredWidth: 360
            Layout.fillHeight: true

            Row {
                anchors.verticalCenter: parent.verticalCenter
                spacing: 12

                Rectangle {
                    width: 32; height: 32
                    radius: 6
                    color: theme.accentBlue

                    Text {
                        anchors.centerIn: parent
                        text: "⚡"
                        font.pixelSize: 18
                        color: "white"
                    }
                }

                Column {
                    anchors.verticalCenter: parent.verticalCenter
                    spacing: 0

                    Text {
                        text: "吊装作业三维动态安全管控平台"
                        font.pixelSize: theme.fontSizeMedium
                        font.bold: true
                        color: theme.textPrimary
                    }
                }
            }
        }

        // ── Center Status ──
        Row {
            Layout.fillWidth: true
            Layout.fillHeight: true
            spacing: 24
            layoutDirection: Qt.RightToLeft
            rightPadding: 40

            Row {
                anchors.verticalCenter: parent.verticalCenter
                spacing: 8

                Rectangle {
                    width: 8; height: 8; radius: 4
                    color: theme.statusGreen
                }

                Text {
                    text: "吊装作业进行中"
                    font.pixelSize: theme.fontSizeSmall
                    color: theme.textSecondary
                }
            }

            Row {
                anchors.verticalCenter: parent.verticalCenter
                spacing: 16

                Repeater {
                    model: [
                        { label: "蓝色", clr: theme.riskBlue },
                        { label: "黄色", clr: theme.riskYellow },
                        { label: "橙色", clr: theme.riskOrange },
                        { label: "红色", clr: theme.riskRed }
                    ]

                    Row {
                        spacing: 4
                        anchors.verticalCenter: parent.verticalCenter
                        Rectangle { width: 8; height: 8; radius: 4; color: modelData.clr; anchors.verticalCenter: parent.verticalCenter }
                        Text { text: modelData.label; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary }
                    }
                }

                Text {
                    text: "风险等级："
                    font.pixelSize: theme.fontSizeSmall
                    color: theme.textSecondary
                    anchors.verticalCenter: parent.verticalCenter
                }
            }

            Text {
                text: "当前作业状态："
                font.pixelSize: theme.fontSizeSmall
                color: theme.textMuted
                anchors.verticalCenter: parent.verticalCenter
            }
        }

        // ── Right: Time + Project ──
        Column {
            anchors.verticalCenter: parent.verticalCenter
            spacing: 2

            Text {
                text: "时间：" + Qt.formatDateTime(new Date(), "yyyy-MM-dd HH:mm:ss")
                font.pixelSize: theme.fontSizeSmall
                color: theme.textSecondary
                anchors.right: parent.right
            }

            Text {
                text: "项目名称：500kV变电站扩建工程"
                font.pixelSize: theme.fontSizeSmall
                color: theme.textSecondary
                anchors.right: parent.right
            }
        }
    }

    Timer {
        interval: 1000
        running: true
        repeat: true
        onTriggered: timeLabel.text = Qt.formatDateTime(new Date(), "yyyy-MM-dd HH:mm:ss")
    }

    property alias timeDisplay: timeLabel.text

    Text {
        id: timeLabel
        visible: false
    }
}
