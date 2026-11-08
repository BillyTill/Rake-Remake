/// <summary>
/// Interface for objects that can be paused.
/// </summary>
public interface IPausable
{
    void OnPause();
    void OnResume();
}