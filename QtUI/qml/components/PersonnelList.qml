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
            Text { text: "人员列表"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        Row {
            width: parent.width
            spacing: 4
            Text { text: "姓名"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 36 }
            Text { text: "角色"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 52 }
            Text { text: "位置"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 70 }
            Text { text: "状态"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 36; horizontalAlignment: Text.AlignRight }
        }

        Repeater {
            model: [
                { name: "张三", role: "指挥员",   loc: "现场指挥区" },
                { name: "李四", role: "吊车司机", loc: "吊车驾驶室" },
                { name: "王五", role: "现场监护", loc: "警戒区北侧" },
                { name: "赵六", role: "作业人员", loc: "吊物区域" }
            ]

            Row {
                width: parent.width
                spacing: 4
                Text { text: modelData.name; font.pixelSize: theme.fontSizeSmall; color: theme.textPrimary; width: 36 }
                Text { text: modelData.role; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 52 }
                Text { text: modelData.loc;  font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 70 }
                Text { text: "在线"; font.pixelSize: theme.fontSizeSmall; color: theme.statusGreen; width: 36; horizontalAlignment: Text.AlignRight }
            }
        }
    }
}
