import QtQuick 2.15
import QtQuick.Controls 2.15

ApplicationWindow {
    id: appRoot
    visible: true
    width: 1920
    height: 1080
    title: "吊装作业三维动态安全管控平台"
    color: "#f4f7fc" 

    // ═══ 严谨的色值 ═══
    readonly property color clrThemeNavy: "#01469a"    // 标题栏深蓝色
    readonly property color clrBorder: "#cbd5e1"       // 浅灰边框
    readonly property color clrTxtMain: "#1e293b"      // 主要文字
    readonly property color clrTxtSub: "#64748b"       // 次要文字
    
    readonly property color clrRed: "#ef4444"
    readonly property color clrOrange: "#f97316"
    readonly property color clrYellow: "#f59e0b"
    readonly property color clrBlue: "#3b82f6"
    readonly property color clrGreen: "#10b981"

    // =========================================================================
    // 1. TOP HEADER (顶部状态栏 - 固定高度 60)
    // =========================================================================
    Rectangle {
        id: headerArea
        anchors.top: parent.top
        anchors.left: parent.left
        anchors.right: parent.right
        height: 60
        color: "#ffffff"
        border.color: "#e2e8f0"

        Text {
            text: "吊装作业三维动态安全管控平台"
            font.pixelSize: 22
            font.bold: true
            color: "#0f172a"
            anchors.horizontalCenter: parent.horizontalCenter
            anchors.top: parent.top; anchors.topMargin: 6
        }

        // 状态文字排成一行
        Row {
            anchors.bottom: parent.bottom; anchors.bottomMargin: 8
            anchors.left: parent.left; anchors.right: parent.right
            anchors.leftMargin: 20; anchors.rightMargin: 20
            spacing: 30

            Text { text: "时间：2024-06-18 10:30:45"; font.pixelSize: 11; color: clrTxtSub }
            Text { text: "项目名称：500kV变电站扩建工程"; font.pixelSize: 11; color: clrTxtSub }
            Text { text: "当前作业状态：<font color='" + clrGreen + "'>● <b>吊装作业进行中</b></font>"; font.pixelSize: 11 }

            // 右侧风险等级标识
            Row {
                anchors.right: parent.right
                spacing: 15
                Text { text: "风险等级："; font.pixelSize: 11; color: clrTxtSub }
                Repeater {
                    model: [{n:"蓝色",c:clrBlue},{n:"黄色",c:clrYellow},{n:"橙色",c:clrOrange},{n:"红色",c:clrRed}]
                    Row {
                        spacing: 4; anchors.verticalCenter: parent.verticalCenter
                        Rectangle { width: 8; height: 8; radius: 4; color: modelData.c; anchors.verticalCenter: parent.verticalCenter }
                        Text { text: modelData.n; font.pixelSize: 11; color: clrTxtSub }
                    }
                }
            }
        }
    }

    // =========================================================================
    // 2. LEFT PANEL (左侧：资源与图层管理 - 宽度绝对固定 340)
    // =========================================================================
    Rectangle {
        id: leftPanel
        anchors.top: headerArea.bottom
        anchors.bottom: bottomArea.top
        anchors.left: parent.left
        anchors.margins: 10
        width: 340
        color: "#ffffff"
        border.color: clrBorder
        radius: 4

        Rectangle {
            id: lHead
            width: parent.width; height: 32; color: clrThemeNavy; radius: 3
            Text { text: "资源与图层管理面板"; color: "white"; font.pixelSize: 12; font.bold: true; anchors.left: parent.left; anchors.leftMargin: 12; anchors.verticalCenter: parent.verticalCenter }
        }

        ScrollView {
            anchors.top: lHead.bottom; anchors.bottom: parent.bottom; anchors.fill: parent; anchors.topMargin: 35; anchors.margins: 10
            clip: true
            Column {
                width: 315; spacing: 15
                
                // 图层树
                Text { text: "▼ 模型图层"; font.pixelSize: 12; font.bold: true; color: clrThemeNavy }
                Grid {
                    columns: 2; spacing: 10; width: parent.width
                    Repeater {
                        model: ["变电站构架", "高压导线", "地面设施", "场地边界", "安全区域", "设备模型"]
                        Row {
                            spacing: 6; width: 150
                            Rectangle { width: 13; height: 13; border.color: clrThemeNavy; radius: 2; color: "#ffffff"
                                Rectangle { width: 7; height: 7; color: clrThemeNavy; anchors.centerIn: parent }
                            }
                            Text { text: modelData; font.pixelSize: 11; color: clrTxtMain }
                        }
                    }
                }

                // 传感器
                Text { text: "▼ 传感器状态"; font.pixelSize: 12; font.bold: true; color: clrThemeNavy }
                Text { text: "  ⚓ 吊钩/拉力/倾角传感器 <font color='"+clrGreen+"'>正常 26/26</font>"; font.pixelSize: 11 }
                Text { text: "  🍃 风速传感器 <font color='"+clrGreen+"'>正常 2/2</font>"; font.pixelSize: 11 }
                Text { text: "  📹 视频监控系统 <font color='"+clrGreen+"'>正常 6/6</font>"; font.pixelSize: 11 }

                // 区域
                Text { text: "▼ 作业区域区分"; font.pixelSize: 12; font.bold: true; color: clrThemeNavy }
                Grid {
                    columns: 2; spacing: 6
                    Repeater {
                        model: [{n:"警戒区",c:clrYellow},{n:"禁入区",c:clrRed},{n:"危险区",c:clrOrange},{n:"安全通道",c:clrGreen}]
                        Rectangle {
                            width: 150; height: 24; border.color: modelData.c; radius: 2
                            Text { text: modelData.n; font.pixelSize: 11; color: modelData.c; font.bold: true; anchors.centerIn: parent }
                        }
                    }
                }

                // 人员
                Text { text: "▼ 人员列表"; font.pixelSize: 12; font.bold: true; color: clrThemeNavy }
                Column {
                    width: parent.width; spacing: 5
                    Text { text: "  张三 (指挥员)    现场指挥区    <font color='"+clrGreen+"'><b>在线</b></font>"; font.pixelSize: 11 }
                    Text { text: "  李四 (吊车司机)  吊车驾驶室    <font color='"+clrGreen+"'><b>在线</b></font>"; font.pixelSize: 11 }
                    Text { text: "  王五 (安全员)    警戒区边缘    <font color='"+clrGreen+"'><b>在线</b></font>"; font.pixelSize: 11 }
                }
            }
        }
    }

    // =========================================================================
    // 3. RIGHT PANEL (右侧：风险与监控 - 宽度绝对固定 420)
    // =========================================================================
    Rectangle {
        id: rightPanel
        anchors.top: headerArea.bottom
        anchors.bottom: bottomArea.top
        anchors.right: parent.right
        anchors.margins: 10
        width: 420
        color: "#ffffff"
        border.color: clrBorder
        radius: 4

        Rectangle {
            id: rHead
            width: parent.width; height: 32; color: clrThemeNavy; radius: 3
            Text { text: "风险预警与现场状态"; color: "white"; font.pixelSize: 12; font.bold: true; anchors.left: parent.left; anchors.leftMargin: 12; anchors.verticalCenter: parent.verticalCenter }
        }

        ScrollView {
            anchors.top: rHead.bottom; anchors.bottom: parent.bottom; anchors.fill: parent; anchors.topMargin: 35; anchors.margins: 12
            clip: true
            Column {
                width: 395; spacing: 12

                // (1) 警报红框
                Rectangle {
                    width: parent.width; height: 160; border.color: clrRed; border.width: 1.5; radius: 4
                    Column {
                        anchors.fill: parent; anchors.margins: 10; spacing: 6
                        Text { text: "⚠️ <b>高风险级别警报</b>"; font.pixelSize: 14; color: clrRed; font.bold: true }
                        Text { text: "• <b>风险对象:</b> 吊物 (主变压器本体)"; font.pixelSize: 11 }
                        Text { text: "• <b>告警根因:</b> 边缘净空与高压母线间距突破红线值"; font.pixelSize: 11; color: clrRed }
                        Row {
                            width: parent.width; topPadding: 5
                            Column { width: parent.width*0.5
                                Text { text: "当前实际距离"; font.pixelSize: 10; color: clrTxtSub }
                                Text { text: "4.2 m"; font.pixelSize: 20; font.bold: true; color: clrRed }
                            }
                            Column { width: parent.width*0.5
                                Text { text: "最小安全净距 (dmin)"; font.pixelSize: 10; color: clrTxtSub }
                                Text { text: "6.0 m"; font.pixelSize: 20; font.bold: true; color: clrTxtMain }
                            }
                        }
                    }
                }

                // (2) 四路视频
                Text { text: "▼ 视频监控画面"; font.pixelSize: 12; font.bold: true; color: clrThemeNavy }
                Grid {
                    columns: 2; spacing: 6; width: parent.width
                    Repeater {
                        model: ["监视位 01 [前向]", "监视位 02 [吊钩]", "监视位 03 [浅侧]", "监视位 04 [全景]"]
                        Rectangle {
                            width: 192; height: 75; color: "#1e293b"; radius: 2
                            Text { text: modelData; font.pixelSize: 10; color: "#94a3b8"; anchors.centerIn: parent }
                        }
                    }
                }

                // (3) 吊车工况（放回右侧！）
                Text { text: "▼ 吊车实时工况参数 (1#)"; font.pixelSize: 12; font.bold: true; color: clrThemeNavy }
                Rectangle {
                    width: parent.width; height: 110; border.color: "#e2e8f0"; radius: 4; color: "#f8fafc"
                    Row {
                        anchors.fill: parent; anchors.margins: 8; spacing: 15
                        Column {
                            width: 240; spacing: 3
                            Text { text: "• 工作状态: <b>吊装作业</b>"; font.pixelSize: 11 }
                            Text { text: "• 臂长/幅度: 32.5 m / 12.8 m"; font.pixelSize: 11; color: clrTxtSub }
                            Text { text: "• 提升高度: 25.6 m"; font.pixelSize: 11; color: clrTxtSub }
                            Text { text: "• 环境瞬时风速: <font color='"+clrGreen+"'><b>2.2 m/s</b></font>"; font.pixelSize: 11 }
                        }
                        Column {
                            anchors.verticalCenter: parent.verticalCenter; spacing: 2
                            Text { text: "力矩百分比"; font.pixelSize: 9; color: clrTxtSub; anchors.horizontalCenter: parent.horizontalCenter }
                            Rectangle {
                                width: 54; height: 54; radius: 27; color: "white"; border.color: clrBorder
                                Text { text: "56%"; font.pixelSize: 13; font.bold: true; color: clrOrange; anchors.centerIn: parent }
                            }
                        }
                    }
                }

                // (4) 迷你雷达/小地图
                Rectangle {
                    width: parent.width; height: 90; color: "#f0f7ff"; border.color: clrBorder; radius: 4
                    Text { text: "🎯 [ 厂区小地图全局网格及雷达联动位置 ]"; font.pixelSize: 11; color: clrBlue; anchors.centerIn: parent }
                }
            }
        }
    }

    // =========================================================================
    // 4. CENTER WINDOW (中间核心：三维数字孪生大窗口 - 绝对动态填满剩余空间)
    // =========================================================================
    Rectangle {
        id: centerWindow
        anchors.top: headerArea.bottom
        anchors.bottom: bottomArea.top
        anchors.left: leftPanel.right
        anchors.right: rightPanel.left
        anchors.margins: 10
        color: "#ffffff"
        border.color: clrBorder
        radius: 4
        clip: true

        Rectangle {
            id: cHead
            width: parent.width; height: 32; color: clrThemeNavy
            Text { text: "三维数字孪生场景窗口"; color: "white"; font.pixelSize: 12; font.bold: true; anchors.left: parent.left; anchors.leftMargin: 12; anchors.verticalCenter: parent.verticalCenter }
        }

        // Unity 3D 渲染窗口
        Rectangle {
            id: unityContainer
            anchors.top: cHead.bottom; anchors.bottom: parent.bottom; width: parent.width
            color: "#0d1a2a"

            // Unity 未就绪时的占位提示
            Text {
                id: unityPlaceholder
                text: "三维数字孪生场景窗口\n等待 Unity 启动..."
                font.pixelSize: 14; color: "#4a6a8a"
                horizontalAlignment: Text.AlignHCenter
                anchors.centerIn: parent
            }

            // 嵌入 Unity
            Component.onCompleted: {
                if (typeof unityEmbed !== "undefined") {
                    unityEmbed.embed(this)
                }
            }
        }
    }

    // =========================================================================
    // 5. BOTTOM AREA (下方：过程记录与时间轴控制台 - 固定高度 200)
    // =========================================================================
    Rectangle {
        id: bottomArea
        anchors.bottom: parent.bottom
        anchors.left: parent.left
        anchors.right: parent.right
        height: 200
        color: "#ffffff"
        border.color: clrBorder

        Rectangle {
            id: bHead
            width: parent.width; height: 28; color: clrThemeNavy
            Text { text: "过程记录与时间轴控制台"; color: "white"; font.pixelSize: 12; font.bold: true; anchors.left: parent.left; anchors.leftMargin: 15; anchors.verticalCenter: parent.verticalCenter }
        }

        Row {
            anchors.top: bHead.bottom; anchors.bottom: parent.bottom; anchors.left: parent.left; anchors.right: parent.right; anchors.margins: 12
            spacing: 20

            // 告警序列
            Column {
                width: 550; spacing: 5
                Text { text: "● 实时告警历史序列数据流"; font.pixelSize: 11; font.bold: true; color: clrThemeNavy }
                Rectangle {
                    width: parent.width; height: 110; border.color: "#e2e8f0"; radius: 2
                    Column {
                        anchors.fill: parent; anchors.margins: 5; spacing: 4
                        Text { text: "10:30:38  [高级]  物能(主变压器)  安全隔离高度净距突破红线值阈值"; font.pixelSize: 11; color: clrRed }
                        Text { text: "10:28:15  [中级]  1#吊车力矩     力矩负载短时飙升，防倾覆机制触发警告"; font.pixelSize: 11; color: clrOrange }
                        Text { text: "10:25:01  [低级]  环境风速异常   瞬时风速达 5.4m/s 提示注意操作平稳"; font.pixelSize: 11; color: clrYellow }
                    }
                }
            }

            // 进度跟踪
            Column {
                width: 320; spacing: 6
                Text { text: "● 吊装安全实施阶段跟踪"; font.pixelSize: 11; font.bold: true; color: clrThemeNavy }
                Text { text: "当前阶段：主变吊装就位 (75%)"; font.pixelSize: 11 }
                Rectangle { width: parent.width; height: 6; color: "#e2e8f0"; radius: 3
                    Rectangle { width: parent.width * 0.75; height: parent.height; color: clrBlue; radius: 3 }
                }
                Text { text: " ✓ 施工技术准备与现场交底 (100%)\n ▶ 吊装就位与三维姿态纠偏 (75%)\n ⏰ 设备就位后的精准螺栓校验 (0%)"; font.pixelSize: 10; color: clrTxtSub }
            }

            // 时间轴回放
            Column {
                width: 320; spacing: 8
                Text { text: "● 数字孪生历史时空轨迹回放"; font.pixelSize: 11; font.bold: true; color: clrThemeNavy }
                Row {
                    spacing: 6
                    Button { text: "⏮"; implicitWidth: 35; implicitHeight: 24 }
                    Button { text: "⏸ 2.0X"; implicitWidth: 70; implicitHeight: 24 }
                    Button { text: "⏭"; implicitWidth: 35; implicitHeight: 24 }
                    Button { text: "💾 导出"; implicitWidth: 55; implicitHeight: 24 }
                }
                Slider {
                    id: playbackSlider; value: 0.58; width: parent.width
                    background: Rectangle { height: 4; radius: 2; color: "#e2e8f0"
                        Rectangle { width: playbackSlider.visualPosition * parent.width; height: parent.height; color: clrBlue; radius: 2 }
                    }
                }
            }
        }
    }
}