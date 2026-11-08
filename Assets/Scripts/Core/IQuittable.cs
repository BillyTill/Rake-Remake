/// <summary>
/// Interface for objects that need cleanup on quit.
/// </summary>
public interface IQuittable
{
    void OnApplicationQuit();
}