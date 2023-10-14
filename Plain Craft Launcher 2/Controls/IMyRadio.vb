' 这是一个公共接口用于描述一个自定义的广播器（机翻，我只负责混pr
Public Interface IMyRadio
    ' 当选中状态发生变化时触发的事件
    Event Check(sender As Object, e As RouteEventArgs)

    ' 当广播器状态发生变化时触发的事件
    Event Changed(sender As Object, e As RouteEventArgs)
End Interface