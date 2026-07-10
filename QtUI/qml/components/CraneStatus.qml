import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    property var theme
    height: col.implicitHeight + 24
    color: theme.bgCard
    radius: theme.radiusMedium
    border.color: theme.borderDefault

    Column {
        id: col
        anchors.fill: parent
        anchors.margins: theme.cardPadding
        spacing: 8

        Row {
            spacing: 6
            Rectangle { width: 3; height: 16; radius: 1; color: theme.accentCyan; anchors.verticalCenter: parent.verticalCenter }
            Text { text: "吊车状态 (1#)"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        GridLayout {
            columns: 2
            columnSpacing: 12
            rowSpacing: 4
            width: parent.width

            Repeater {
                model: [
                    { label: "工作状态", value: "吊装作业", clr: theme.statusYellow },
                    { label: "臂长",     value: "32.5 m", clr: theme.textPrimary },
                    { label: "幅度",     value: "12.8 m", clr: theme.textPrimary },
                    { label: "高度",     value: "25.6 m", clr: theme.textPrimary },
                    { label: "吊重",     value: "45.0 t", clr: theme.textPrimary },
                    { label: "额定载荷", value: "80.0 t", clr: theme.textSecondary },
                    { label: "力矩百分比", value: "56%",   clr: theme.statusYellow }
                ]

                Row {
                    spacing: 8
                    Layout.fillWidth: true
                    Text { text: modelData.label; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 70 }
                    Text { text: modelData.value; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: modelData.clr }
                }
            }
        }
    }
}
