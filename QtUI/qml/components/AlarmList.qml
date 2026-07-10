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
            Rectangle { width: 3; height: 16; radius: 1; color: theme.statusOrange; anchors.verticalCenter: parent.verticalCenter }
            Text { text: "设备状态"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        Repeater {
            model: [
                { name: "风速传感器", value: "2.2 m/s", status: "正常", clr: theme.statusGreen },
                { name: "倾角传感器", value: "0.3°",    status: "正常", clr: theme.statusGreen },
                { name: "拉力传感器", value: "45.0t",   status: "正常", clr: theme.statusGreen },
                { name: "吊钩高度",   value: "25.6m",   status: "正常", clr: theme.statusGreen }
            ]

            Row {
                spacing: 8
                width: parent.width
                Rectangle { width: 8; height: 8; radius: 4; color: modelData.clr; anchors.verticalCenter: parent.verticalCenter }
                Text { text: modelData.name; font.pixelSize: theme.fontSizeSmall; color: theme.textPrimary; width: 80 }
                Text { text: modelData.value; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 50 }
                Text { text: modelData.status; font.pixelSize: theme.fontSizeSmall; color: modelData.clr; width: 40; horizontalAlignment: Text.AlignRight }
            }
        }
    }
}
