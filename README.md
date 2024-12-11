# SolarNG

SolarNG是一个浏览器界面风格的远程管理客户端，还支持标签页化的本地应用管理以及本地应用启动控制。

![](Doc/SolarNGv2-overview.png)

## 特色功能

- 支持多协议(SSH/TELNET/FTP/SFTP/SCP/RDP/VNC)客户端和会话管理
- 支持多级SOCK4/SOCK5/HTTP/SSH代理
- 支持多级标签分组功能
- 支持批量修改会话等配置信息
- 支持本地应用(包括UWP程序)、进程、窗口的标签页化管理(可切换主窗口标题栏、可跳出标签页)
- 应用启动控制包括支持URI格式、支持多显示器位置指定、支持关闭输入法
- 支持通过命名管道来传递远程客户端的配置文件和口令
- 自有模块总大小不超过2MB

## 未来计划

- [ ] 支持自定义会话类型和应用

- [ ] 支持基于RDP协议的代理通道

- [ ] 支持在设置界面修改SolarNG.cfg配置

- [ ] 按照标签来批量打开会话或者执行脚本

- [ ] 完善说明手册


## 已知问题
- 如果不启用PuTTY的hook功能，需要导入ScrollBarFix.reg来解决滚动条不刷新的问题。pterm也受这个配置的影响，因此必须要导入ScrollBarFix.reg。
- "keepparent"模式只是把目标窗口与SolarNG的窗口贴合在一起。因此在移动SolarNG窗口时，目标窗口不会自动移动，移动结束后，SolarNG才会把目标窗口移动到正确的位置。当用鼠标弹出主菜单或者标签页上的右键菜单时，目标窗口也有可能消失(被SolarNG窗口遮挡了)。
- 任务栏无法隐藏ApplicationFrameWindow窗口。
- 如果直接关闭ApplicationFrameWindow窗口，所属标签页不会联动关闭。
- 由于某些UWP程序并不是SolarNG创建的子进程，因此在捕获窗口时，可能会把之前已经打开的进程名相同的主窗口放置到标签页中。
- Xmind是单进程管理，但由于其再启用一次主进程并不会创建新窗口。因此无法支持标签页管理。
- 仅C#代码开源，native代码(除PlinkX外)不开源。


## 安装需求

| 组件                               | 版本                                                   |
| ---------------------------------- | ------------------------------------------------------ |
| Windows                            | Windows 7+ (x86/x64)                                   |
| Microsoft **.NET**                 | .NET Framework 4.5+                                    |
| PuTTY.exe                          | PuTTY 0.71+/0.77+(通过命名管道传递口令)                |
| WinSCP.exe(可选：SFTP/SCP/FTP支持) | WinSCP 5.9+/5.14+(代理支持)/6.0+(通过命名管道传递口令) |
| tvnviewer.exe(可选：VNC支持)       | TightVNC  2.0+                                         |
| PlinkX.exe(可选：代理支持)         | PlinkX 0.79+                                           |

更多介绍请看[说明手册](Doc/SolarNG.md)。