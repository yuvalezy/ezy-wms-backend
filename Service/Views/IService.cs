namespace Service.Views;

public interface IService {
    void LogInfo(string    message);
    void LogWarning(string message);
    void LogError(string   message);
    void StopService();
}