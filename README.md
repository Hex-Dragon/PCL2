<div align="center">
  
![Title](1.jpg)
  
# Plain Craft Launcher 2 源代码库
[![](https://img.shields.io/badge/%E7%88%B1%E5%8F%91%E7%94%B5-%40%E9%BE%99%E8%85%BE%E7%8C%AB%E8%B7%83-blueviolet)](https://afdian.net/@LTCatt)
[![](https://img.shields.io/badge/Bilibili-%40%E9%BE%99%E8%85%BE%E7%8C%AB%E8%B7%83-ff69b4?logo=bilibili)](https://b23.tv/rMUeYME)
[![](https://img.shields.io/badge/Github-@LTCatt-green?logo=github)](https://github.com/LTCatt)

[![](https://img.shields.io/github/issues/Hex-Dragon/PCL2?style=flat,logo=github)](https://github.com/Hex-Dragon/PCL2/issues)
[![](https://img.shields.io/github/forks/Hex-Dragon/PCL2?style=flat,logo=github)](https://github.com/Hex-Dragon/PCL2/network/members)
![](https://img.shields.io/github/stars/Hex-Dragon/PCL2?style=flat,logo=github)
[![](https://img.shields.io/badge/License-Custom-A31F34?logo=.NET&logoColor=ffffff&style=flat,logo=github)](https://github.com/Hex-Dragon/PCL2/blob/main/LICENSE.txt)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/Hex-Dragon/PCL2)](https://afdian.net/p/0164034c016c11ebafcb52540025c377)
  
`经历了 DMCA Takedown 等重重波折，这个存储库终于开放了。`

</div>

---

### 作者的话
这里提供了 PCL2 的大多数源代码，包括 UI 库、动画模块、下载模块、Minecraft 启动模块等！

PCL2 的代码绝大多数其实都是几年前学生时代的产物了……那时候英语还不过关……所以经常出现奇葩命名，还有令人高血压的高耦合啊，没做单例啊，瞎勾八乱糊啊之类的问题……额，我也不可能把这一堆玩意儿再从头写一次，各位就基于能跑就行的原则凑合凑合着看吧，求求别喷了（run

你也可以丢 [Pull Request](https://github.com/Hex-Dragon/PCL2/pull)，虽然这个源代码库并不能直接编译，但一些简单的修改应该还是没问题的……

---

### 相关内容：
- [PCL2 议题提交](https://github.com/Hex-Dragon/PCL2/issues/new/choose)
  使用`PCL2`时遇到了问题？亦或者有新的想法，希望作者开发出新的功能？你可以在[`Issues`](https://github.com/Hex-Dragon/PCL2/issues/new/choose)提出！
- [PCL2 功能投票](https://github.com/Hex-Dragon/PCL2/discussions/2)<br>
  > 如何参加？
  请见 [@LTCatt: 关于PCL2 功能投票·Discussion #2](https://github.com/Hex-Dragon/PCL2/discussions/2)。

- [PCL2 正式版下载](https://afdian.net/p/0164034c016c11ebafcb52540025c377)：下载免费的正式版 PCL2。
- [PCL2 内测版下载](https://afdian.net/@LTCat)<br>
  *不过，你需要先赞助……*
- [帮助文档库](https://github.com/LTCatt/PCL2Help)：PCL2 帮助文档在 GitHub 上的存储库（是的，帮助库在另一个 Repo……）

---

### 这个存储库包括什么？

#### 存储库中公开了 PCL2 的`绝大多数源代码`，包括：
- 自制的 UI 控件库（Controls 文件夹）
- 所使用的图片、资源文件，包含 PCL2 的 Logo 等（Images、Resources 文件夹）
- 自制动画引擎（Modules/Base/ModAnimation.vb）
- 自制多线程多文件下载模块（Modules/Base/ModNet.vb）
- 基础函数库、输入验证函数库、图片处理函数库、多任务系统逻辑（Modules/Base）
- MC 崩溃分析与日志监测模块（Modules/Minecraft/ModCrash.vb、ModWatcher.vb）
- MC 与整合包的下载和安装（Modules/Minecraft/ModDownload.vb、ModModpack.vb）
- 登录与 MC 启动核心（Modules/Minecraft/ModLaunch.vb）
- Java、MC 版本、资源文件处理函数库（Modules/Minecraft/ModMinecraft.vb）
- 音乐播放函数库（Modules/ModMusic.vb）
- PCL2 的绝大多数界面（Pages 文件夹、FormMain.xaml）

还有一堆杂七杂八的文件，感兴趣的话自己去看吧.jpg

#### 未公开的部分有：
- 有潜在风险的：联机模块、自动更新模块、设置项的存取模块
- 主题与彩蛋：愚人节彩蛋、解密游戏、回声洞、主题切换与解锁模块
- 敏感内容：加密解密函数、识别码与解锁码校验函数、API 鉴权函数等
- 开发者工具：PCL2 更新打包器之类的公开了也鬼用没有的东西

PCL2 的源代码仅以帮助公共研究为目的而公开，依然保留所有权利，因此没有附带开源协议（根据 GitHub 相关条款，不附带协议即默认保留所有权利）。

所以，虽然你不能直接复制粘贴代码到自己的项目，但可以参考其中的逻辑，然后自己车一套更好的……总之，可以借鉴，不要复制啦。

---

**一些无趣的小细节：**
- 别想在这里面找到解密游戏的线索，那部分的代码被我抠掉了（笑）
- 哦，对，解锁隐藏主题的相关代码也都被抠掉了，所以别想着用翻源代码这种歪门邪道来绕过解密了.jpg
- 还有，不要想着在 Releases 里面找到内测版的下载链接了，你大概率是找不到的.psd
- 源代码库**并不是**即时更新的，而是在每次 PCL2 发布更新时（手动）同步一次
- 这个 PCL2 被部分内测成员戏称为“PCLDownloader”，因为其内置的多线程下载实在太厉害了……甚至有时都强过了IDM……

- 友情链接：
  - [PCL1](https://github.com/LTCatt/PCL1) 这是个古老的项目
  - [这个 Readme 的作者](https://github.com/liubanlaobanzhang)
---
