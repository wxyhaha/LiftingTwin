import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    property var theme
    height: equipColumn.implicitHeight + 24
    color: theme.bgCard
    radius: theme.radiusMedium
    border.color: theme.borderDefault

    Column {
        id: equipColumn
        anchors.fill: parent
        anchors.margins: theme.cardPadding
        spacing: 8

        Row {
            spacing: 6
            Rectangle { width: 3; height: 16; radius: 1; color: theme.accentCyan; anchors.verticalCenter: parent.verticalCenter }
            Text { text: "设备列表"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        Repeater {
            model: [
                { name: "吊车 (1#)",      status: "运行中",  icon: "🏗️", sc: theme.statusGreen },
                { name: "吊车 (2#)",      status: "待机",    icon: "🏗️", sc: theme.textMuted },
                { name: "吊物 (主变压器)", status: "吊装中", icon: "📦", sc: theme.statusYellow },
                { name: "运输车辆 (3)",   status: "在线",    icon: "🚛", sc: theme.statusGreen },
                { name: "高空作业车 (1)",  status: "待机",    icon: "🔧", sc: theme.textMuted }
            ]

            Row {
                spacing: 8
                width: parent.width
                Text { text: modelData.icon; font.pixelSize: theme.fontSizeNormal; width: 24; horizontalAlignment: Text.AlignHCenter }
                Text { text: modelData.name; font.pixelSize: theme.fontSizeSmall; color: theme.textPrimary; Layout.fillWidth: true; width: parent.width - 70 }
                Text { text: modelData.status; font.pixelSize: theme.fontSizeSmall; color: modelData.sc; width: 44; horizontalAlignment: Text.AlignRight }
            }
        }
    }
}
