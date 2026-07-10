import QtQuick 2.15
import QtQuick.Controls 2.15
import QtQuick.Layouts 1.15

Rectangle {
    property var theme
    property string title: ""
    property string value: ""
    property color valueColor: theme.textPrimary

    height: 56
    color: theme.bgCard
    radius: theme.radiusMedium
    border.color: theme.borderDefault

    RowLayout {
        anchors.fill: parent
        anchors.margins: theme.cardPadding
        spacing: 8

        Text {
            text: title
            font.pixelSize: theme.fontSizeSmall
            color: theme.textSecondary
            Layout.fillWidth: true
        }

        Text {
            text: value
            font.pixelSize: theme.fontSizeLarge
            font.bold: true
            color: valueColor
        }
    }
}
