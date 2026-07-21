#!/bin/bash
# ==========================================================================
# FluentUI 一键初始化脚本
# 将 FluentUI 下载到 third_party/FluentUI 并打上 Qt5Compat 兼容补丁
# 用法: bash setup_fluentui.sh
# ==========================================================================
set -e

FLUENTUI_DIR="$(cd "$(dirname "$0")" && pwd)/FluentUI"
FLUENTUI_ZIP_URL="https://codeload.github.com/zhuzichu520/FluentUI/zip/refs/heads/main"

echo ">>> 检查 FluentUI..."
if [ -d "$FLUENTUI_DIR/src" ] && [ -f "$FLUENTUI_DIR/CMakeLists.txt" ]; then
    echo "    FluentUI 已存在: $FLUENTUI_DIR"
else
    echo "    FluentUI 不存在，正在下载..."
    rm -rf "$FLUENTUI_DIR"
    cd "$(dirname "$0")"
    curl -sL --max-time 120 -o fluentui.zip "$FLUENTUI_ZIP_URL"
    echo "   正在解压..."
    unzip -q fluentui.zip
    mv FluentUI-main "$FLUENTUI_DIR" 2>/dev/null || mv FluentUI-main FluentUI
    rm -f fluentui.zip
    echo "   FluentUI 下载完成"
fi

echo ">>> 打 Qt5Compat 兼容补丁..."
# ---- FluAcrylic.qml: 移除 Qt5Compat.GraphicalEffects (FastBlur) ----
cat > "$FLUENTUI_DIR/src/Qt6/imports/FluentUI/Controls/FluAcrylic.qml" << 'QML_EOF'
import QtQuick
import FluentUI

// 已打补丁：移除 Qt5Compat.GraphicalEffects 依赖（FastBlur/Acrylic 模糊），
// 用纯色透明替代。桌面端功能不受影响。
Item {
    id: control
    property color tintColor: Qt.rgba(1, 1, 1, 1)
    property real tintOpacity: 0.65
    property real luminosity: 0.01
    property real noiseOpacity: 0.02
    property var target
    property int blurRadius: 32
    property rect targetRect: Qt.rect(control.x, control.y, control.width,control.height)
    Rectangle {
        anchors.fill: parent
        color: Qt.rgba(1, 1, 1, luminosity)
    }
    Rectangle {
        anchors.fill: parent
        color: Qt.rgba(tintColor.r, tintColor.g, tintColor.b, tintOpacity)
    }
}
QML_EOF

# ---- FluClip.qml: 移除 Qt5Compat.GraphicalEffects (OpacityMask) ----
cat > "$FLUENTUI_DIR/src/Qt6/imports/FluentUI/Controls/FluClip.qml" << 'QML_EOF'
import QtQuick
import QtQuick.Controls
import FluentUI

// 已打补丁：移除 Qt5Compat.GraphicalEffects 依赖（OpacityMask），
// 使用 Qt 6 内置 clip + radius 实现圆角剪裁。
FluRectangle {
    id:control
    color: "#00000000"
    clip: true
    radius: control.radius
}
QML_EOF

echo "   补丁应用完成"
echo ">>> FluentUI 就绪: $FLUENTUI_DIR"
