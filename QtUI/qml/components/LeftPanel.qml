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
        contentHeight: contentColumn.implicitHeight
        clip: true
        ScrollBar.vertical: ScrollBar { policy: ScrollBar.AsNeeded }

        Column {
            id: contentColumn
            width: parent.width
            spacing: 12

            EquipmentList { width: parent.width; theme: root.theme }
            ModelLayers { width: parent.width; theme: root.theme }
            SensorStatus { width: parent.width; theme: root.theme }

            // ── 作业区域 ──
            Rectangle {
                width: parent.width
                height: zoneCol.implicitHeight + 24
                color: theme.bgCard
                radius: theme.radiusMedium
                border.color: theme.borderDefault

                Column {
                    id: zoneCol
                    anchors.fill: parent
                    anchors.margins: theme.cardPadding
                    spacing: 8

                    Row {
                        spacing: 6
                        Rectangle { width: 3; height: 16; radius: 1; color: theme.accentCyan; anchors.verticalCenter: parent.verticalCenter }
                        Text { text: "作业区域"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
                    }

                    Flow {
                        width: parent.width
                        spacing: 6

                        Repeater {
                            model: [
                                { label: "警戒区",      clr: "#e65100" },
                                { label: "禁入区",      clr: "#c62828" },
                                { label: "吊物危险区",  clr: "#c62828" },
                                { label: "吊臂摆动范围", clr: "#1565c0" },
                                { label: "安全通道",     clr: "#2e7d32" }
                            ]

                            Rectangle {
                                width: zoneLabel.implicitWidth + 16
                                height: 26
                                radius: 4
                                color: Qt.rgba(
                                    Qt.lighter(modelData.clr).r * 0.15 + 0.02,
                                    Qt.lighter(modelData.clr).g * 0.15 + 0.02,
                                    Qt.lighter(modelData.clr).b * 0.15 + 0.02,
                                    0.15)
                                border.color: modelData.clr

                                Text {
                                    id: zoneLabel
                                    text: modelData.label
                                    font.pixelSize: theme.fontSizeSmall
                                    color: modelData.clr
                                    anchors.centerIn: parent
                                }
                            }
                        }
                    }
                }
            }

            PersonnelList { width: parent.width; theme: root.theme }
            CraneStatus { width: parent.width; theme: root.theme }
        }
    }
}
