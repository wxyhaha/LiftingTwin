import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    property var theme
    height: evCol.implicitHeight + 24
    color: theme.bgCard
    radius: theme.radiusMedium
    border.color: theme.borderDefault

    Column {
        id: evCol
        anchors.fill: parent
        anchors.margins: theme.cardPadding
        spacing: 8

        Row {
            spacing: 6
            Rectangle { width: 3; height: 16; radius: 1; color: theme.statusYellow; anchors.verticalCenter: parent.verticalCenter }
            Text { text: "实时告警列表"; font.pixelSize: theme.fontSizeSmall; font.bold: true; color: theme.textPrimary; anchors.verticalCenter: parent.verticalCenter }
        }

        Row {
            width: parent.width; spacing: 4
            Text { text: "时间";     font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 56 }
            Text { text: "风险等级"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 50 }
            Text { text: "风险对象"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 68 }
            Text { text: "告警原因"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 68 }
            Text { text: "状态";     font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; width: 36; horizontalAlignment: Text.AlignRight }
        }

        Repeater {
            model: [
                { time: "10:30:38", level: "高", lclr: theme.riskRed,    obj: "吊物（主变压器）", reason: "与高压母线安全距离不足", status: "触发" },
                { time: "10:28:15", level: "中", lclr: theme.riskYellow, obj: "吊车（1#）",       reason: "力矩百分比过高",       status: "已恢复" },
                { time: "10:25:42", level: "中", lclr: theme.riskYellow, obj: "吊钩",             reason: "挂载槽内异常人员",     status: "已恢复" },
                { time: "10:22:07", level: "低", lclr: theme.riskBlue,   obj: "风速传感器",       reason: "风速超过预警值",       status: "已恢复" }
            ]

            Row {
                width: parent.width; spacing: 4
                Text { text: modelData.time;   font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 56 }
                Text { text: modelData.level;   font.pixelSize: theme.fontSizeSmall; font.bold: true; color: modelData.lclr; width: 50 }
                Text { text: modelData.obj;     font.pixelSize: theme.fontSizeSmall; color: theme.textPrimary; width: 68 }
                Text { text: modelData.reason;  font.pixelSize: theme.fontSizeSmall; color: theme.statusYellow; width: 68; elide: Text.ElideRight }
                Text { text: modelData.status;  font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary; width: 36; horizontalAlignment: Text.AlignRight }
            }
        }
    }
}
