public interface ISaveable
{
    string GetSaveId();
    string CaptureState();
    void RestoreState(string stateData);
    void OnRestoreComplete();
}