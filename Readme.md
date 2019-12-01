# EDCHost21
## 软件使用方法
- **在此页面上方的releases中有最新版的可执行程序。**

- v3.4之后的上位机，首先会弹出“设置”窗口，**选择正确的串口端口号Port和摄像头编号Capture**（Capture0有可能是笔记本电脑的自带摄像头）。

- **角点标定**：“开始”之前要先点一下场地的四个角校正坐标，从**左上-右上-左下-右下**。场地上已画出四个十字标定点；为修正小车高度所引入的误差，可使用标定架（大致与小车同高）进行标定。

- **颜色参数设置**：在“设置”中调节识别参数。使用颜色进行小车及小球位置的识别；使用的是HSV(Hue, Sat, Val)颜色空间：0是小球颜色，1和2分别是A、B车颜色；Hue1L和Hue1H是色调的上下限；Sat1L是饱和度的下限，没有上限；ValueL是明度的下限，没有上限；AreaL是有效面积的下限，没有上限。点击“保存”，颜色参数将会保存至程序同一文件夹下；点击“读取”，可读取程序同一文件夹下所保存的颜色参数。
  
  > 参考调节技巧：先把SatxL调至最小，再调Hue确定一个范围，并放松至适当宽；最后调高SatxL到没有误识别点就基本可以了。新版本上位机增加蒙版显示功能，可查看识别的中间效果。
  
- 在“设置”中，可以切换串口端口号Port和摄像头编号Capture。Port为(None)表示该串口关闭。

- 单击“开始”以开始比赛进程。切换小节时先单击“下一节”，再单击“开始”进入计时。

## 通信模块使用方法
使用ZigBee进行通信。首先将模块按照“ZigBee”文件夹中的《DL-30按键配置使用说明书》进行配置，再连接至电脑后打开上位机（或进入上位机“设置”中手动选择端口）即可。

通信模块设置参数：

- 波特率：115200

- 频道：设置为只有右下角的灯闪烁

- 工作模式：广播模式


## 通信协议

通信协议见“EDC21通信协议v x.x”。

通信协议在数据包第28、29字节增加了CRC16校验码，多项式0x8005，初始值0xFFFF。可在网站[https://crccalc.com/](https://crccalc.com/)上进行在线计算（其中的CRC-16/MODBUS）。注意，这里所计算的是数据包中除去最后四行（**第0~27字节**，包含了保留位，不包含末尾的0x0D、0x0A）的校验码。关于校验码，提供一个示例程序crc16.c，位于“ZigBee”文件夹中，包含了一个计算CRC16校验码的函数`unsigned short crc16(unsigned char *data_p, unsigned char length)`。

## 从源码编译

- 测试环境：Microsoft Visual Studio 2017, .NET Framework 4.6。打开EDCHost21.sln后直接生成解决方案即可。
- 运行前将ForDebug文件夹下的dll.zip解压为文件夹后放至"<项目路径>\EDCHost21\bin\Debug"路径下。“data.txt”是颜色设置参考数值，也可放在"<项目路径>\EDCHost21\bin\Debug"路径下，程序将自动读取。