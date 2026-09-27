namespace AudioCarousel.Cycle;

/// <summary>Which kind of device a successful switch changed.</summary>
public enum ToastKind
{
    Output,
    Input,
}

public interface ICycleSink
{
    void ShowToast(string deviceName, ToastKind kind);
    void ShowErrorToast(string text);
    void NotifyCurrentDeviceChanged();
}
