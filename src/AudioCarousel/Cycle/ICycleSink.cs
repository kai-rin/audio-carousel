namespace AudioCarousel.Cycle;

public interface ICycleSink
{
    void ShowToast(string text);
    void ShowErrorToast(string text);
    void NotifyCurrentDeviceChanged();
}
