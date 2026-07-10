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
            Text { text: "传感器状态"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        Repeater {
            model: [
                { name: "吊钩传感器", count: "12/12" },
                { name: "拉力传感器", count: "8/8" },
                { name: "倾角传感器", count: "6/6" },
                { name: "风速传感器", count: "2/2" },
                { name: "视频监控",   count: "6/6" }
            ]

            Row {
                spacing: 8
                width: parent.width
                Rectangle { width: 8; height: 8; radius: 4; color: theme.statusGreen; anchors.verticalCenter: parent.verticalCenter }
                Text { text: "正常"; font.pixelSize: theme.fontSizeSmall; color: theme.statusGreen; width: 36 }
                Text { text: modelData.name; font.pixelSize: theme.fontSizeSmall; color: theme.textPrimary; Layout.fillWidth: true; width: parent.width - 90 }
                Text { text: modelData.count; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 40; horizontalAlignment: Text.AlignRight }
            }
        }
    }
}
