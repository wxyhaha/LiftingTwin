import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    id: root
    property var theme
    color: theme.bgPrimary
    border.color: theme.borderDefault
    border.width: 1

    Rectangle {
        anchors.fill: parent
        anchors.margins: 2
        color: "#0d1a2a"

        Column {
            anchors.centerIn: parent
            spacing: 12

            Text { text: "🎮"; font.pixelSize: 48; anchors.horizontalCenter: parent.horizontalCenter }
            Text { text: "Unity 3D View"; font.pixelSize: theme.fontSizeLarge; color: theme.accentCyan; font.bold: true; anchors.horizontalCenter: parent.horizontalCenter }
            Text { text: "三维数字孪生场景窗口"; font.pixelSize: theme.fontSizeSmall; color: theme.textMuted; anchors.horizontalCenter: parent.horizontalCenter }
            Text { text: "WASD 移动 | Q/E 升降 | 右键旋转 | 滚轮缩放"; font.pixelSize: theme.fontSizeTiny; color: theme.textMuted; anchors.horizontalCenter: parent.horizontalCenter }
        }
    }

    // Bottom toolbar
    Rectangle {
        anchors.bottom: parent.bottom
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.margins: 4
        height: 40
        color: Qt.rgba(0.05, 0.08, 0.15, 0.85)
        radius: theme.radiusSmall

        Row {
            anchors.centerIn: parent
            spacing: 32

            Repeater {
                model: [
                    { icon: "🔄", label: "场景复位" },
                    { icon: "👁", label: "视角切换" },
                    { icon: "📏", label: "测量工具" },
                    { icon: "✂",  label: "剖切分析" },
                    { icon: "🗺", label: "图层控制" },
                    { icon: "⛶", label: "全屏显示" }
                ]

                Row {
                    spacing: 4
                    anchors.verticalCenter: parent.verticalCenter
                    Text { text: modelData.icon; font.pixelSize: 14 }
                    Text { text: modelData.label; font.pixelSize: theme.fontSizeSmall; color: theme.textSecondary }
                }
            }
        }
    }
}
