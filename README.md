# Plain Craft Launcher UUID 修复版

[![Stars](https://img.shields.io/github/stars/PCL-Community/PCL2-Uuid-Fix?style=flat&logo=data:image/svg%2bxml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZlcnNpb249IjEiIHdpZHRoPSIxNiIgaGVpZ2h0PSIxNiI+PHBhdGggZD0iTTggLjI1YS43NS43NSAwIDAgMSAuNjczLjQxOGwxLjg4MiAzLjgxNSA0LjIxLjYxMmEuNzUuNzUgMCAwIDEgLjQxNiAxLjI3OWwtMy4wNDYgMi45Ny43MTkgNC4xOTJhLjc1MS43NTEgMCAwIDEtMS4wODguNzkxTDggMTIuMzQ3bC0zLjc2NiAxLjk4YS43NS43NSAwIDAgMS0xLjA4OC0uNzlsLjcyLTQuMTk0TC44MTggNi4zNzRhLjc1Ljc1IDAgMCAxIC40MTYtMS4yOGw0LjIxLS42MTFMNy4zMjcuNjY4QS43NS43NSAwIDAgMSA4IC4yNVoiIGZpbGw9IiNlYWM1NGYiLz48L3N2Zz4=&logoSize=auto&label=Stars&labelColor=666666&color=eac54f)](https://github.com/Hex-Dragon/PCL2/)
[![Issues](https://img.shields.io/github/issues/PCL-Community/PCL2-Uuid-Fix?style=flat&label=Issues&labelColor=666666&color=1a7f37)](https://github.com/Hex-Dragon/PCL2/issues)
[![爱发电](https://img.shields.io/badge/赞助-%E7%88%B1%E5%8F%91%E7%94%B5-946ce6?style=flat&labelColor=666666&logoSize=auto)](https://afdian.net/@LTCat)

## 介绍

由于一些历史原因，PCL 的采用了一种和其他启动器非常不同的方式生成离线玩家的 UUID，这导致 PCL 的离线游戏存档和其他启动器的存档可能不能互通，并对局域网联机的一些常见操作造成诸多不便。

本仓库对 PCL 进行了相应修改，使其能够以默认通用的方式生成离线玩家的 UUID，并通过一些选项允许玩家在启动时自定义自己的 UUID。

## 改动
相比原始的仓库版PCL，粗略的修改内容如下：
- 修改了注册表根节点位置为`HKCU\SOFTWARE\PCL-Community\Uuid-Fix`
- 修改了通用占位识别码为`UUID-FIXD-ONTS-HARE`
- 用离线 UUID 选项替代了离线皮肤选项，并提供以下选项：
  - `默认`：使用`OfflinePlayer:玩家名`生成 MD5 然后设置 UUID 版本为 `3`，变体为 `RFC 4122`
  - `正版玩家`：获取某名在线玩家的 UUID 并应用。
  - `启动时询问`：在启动时询问需要使用的 UUID，缺省与`默认`相同。
  - `自定义皮肤`：使用`默认` UUID 的同时加载皮肤纹理包，实现替换皮肤。
 
`默认` UUID 的生成函数大致如下（位于`ModLaunch.vb`）：
```vb
Public Function McLoginLegacyUuid(Name As String)
      Dim NameHash As String = GetStringMD5("OfflinePlayer:" & Name)
      Dim PendingVariant As Integer = Conversion.Val("&H" & NameHash(16))
      PendingVariant = (PendingVariant Mod 4) + 8
      Dim FinalVarient As String = PendingVariant.ToString("x")
      Dim FinalUuid As String = (NameHash.Substring(0, 12) & "3" & NameHash.Substring(13, 3) & FinalVarient & NameHash.Substring(17, 15)).ToLower()
      Return FinalUuid
End Function
```

## 杂项
根据 PCL 的使用许可，本修改版本不提供可用的二进制文件，请自行编译源代码。

[使用效果展示](https://github.com/PCL-Community/PCL2-Uuid-Fix/blob/Silverteal-commits/TRIVIAS.md)
