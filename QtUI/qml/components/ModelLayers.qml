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
            Text { text: "模型图层"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        Repeater {
            model: ["变电站构架", "高压导线", "地面设施", "场地边界", "安全区域", "设备模型"]

            Row {
                spacing: 8
                width: parent.width
                Rectangle {
                    width: 16; height: 16; radius: 3
                    color: theme.accentBlue
                    anchors.verticalCenter: parent.verticalCenter
                    Text { text: "✓"; font.pixelSize: 11; color: "white"; anchors.centerIn: parent }
                }
                Text { text: modelData; font.pixelSize: theme.fontSizeSmall; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
            }
        }
    }
}
