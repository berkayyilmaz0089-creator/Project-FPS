using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("References")]
    public SaveAnimations saveAnimations;
    
    private SaveDirectoryHandler _directoryHandler;
    private GameInput _gameInput;
    private bool _isBusy = false;

    private const string SecretKey = "project_fps_1234"; // NOT A REAL KEY, ONLY FOR TESTONG

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _directoryHandler = new SaveDirectoryHandler();
        
        if (saveAnimations != null)
        {
            saveAnimations.transform.SetParent(transform);
        }
    }

    private void Start()
    {
        _gameInput = new GameInput();
        _gameInput.Enable();
    }

    private void OnDestroy()
    {
        if (_gameInput != null)
        {
            _gameInput.Disable();
            _gameInput.Dispose();
        }
    }

    private void Update()
    {
        if (_isBusy)
        {
            return;
        }

        if (Time.timeScale == 0.0f)
        {
            return;
        }

        if (_gameInput.Player.QuickSave.WasPressedThisFrame())
        {
            QuickSave();
        }
        else if (_gameInput.Player.QuickLoad.WasPressedThisFrame())
        {
            QuickLoad();
        }
    }

    public void QuickSave()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        StartCoroutine(SaveGameRoutine("QuickSave_" + timestamp));
    }

    public void QuickLoad()
    {
        string latestFile = _directoryHandler.GetLatestSaveFile();

        if (string.IsNullOrEmpty(latestFile))
        {
            Debug.LogWarning("No save files found to load.");
            return;
        }

        StartCoroutine(LoadGameRoutine(latestFile));
    }

    private IEnumerator SaveGameRoutine(string saveName)
    {
        _isBusy = true;
        _directoryHandler.ManageSaves();
        
        string finalPath = _directoryHandler.GetSavePath(saveName);
        string tempPath = _directoryHandler.GetTempPath(saveName);

        saveAnimations.ShowSaveIcon();

        yield return new WaitForSecondsRealtime(0.2f);

        MasterSaveData masterData = new MasterSaveData();
        masterData.saveVersion = 1;
        masterData.timestamp = DateTime.Now.ToString();

        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (ISaveable saveable in allScripts.OfType<ISaveable>())
        {
            SaveableEntry entry = new SaveableEntry();
            entry.saveId = saveable.GetSaveId();
            entry.stateData = saveable.CaptureState();
            masterData.savedObjects.Add(entry);
        }

        string jsonString = JsonUtility.ToJson(masterData, true);
        string encryptedJson = EncryptDecrypt(jsonString);

        try
        {
            File.WriteAllText(tempPath, encryptedJson);

            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            File.Move(tempPath, finalPath);
            Debug.Log("Game Saved: " + saveName);
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e.Message);
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        saveAnimations.HideSaveIcon();
        _isBusy = false;
    }

    private IEnumerator LoadGameRoutine(string fileName)
    {
        _isBusy = true;
        string filePath = _directoryHandler.GetSavePath(fileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError("Load failed: File not found.");
            _isBusy = false;
            yield break;
        }

        Time.timeScale = 0.0f;

        saveAnimations.ShowLoadingUI(true);
        yield return new WaitForSecondsRealtime(0.2f);

        try
        {
            string encryptedJson = File.ReadAllText(filePath);
            string jsonString = EncryptDecrypt(encryptedJson);

            MasterSaveData masterData = JsonUtility.FromJson<MasterSaveData>(jsonString);

            MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (SaveableEntry entry in masterData.savedObjects)
            {
                foreach (ISaveable saveable in allScripts.OfType<ISaveable>())
                {
                    if (saveable.GetSaveId() == entry.saveId)
                    {
                        saveable.RestoreState(entry.stateData);
                        break;
                    }
                }
            }

            foreach (ISaveable saveable in allScripts.OfType<ISaveable>())
            {
                saveable.OnRestoreComplete();
            }

            Debug.Log("Game Loaded: " + fileName);
        }
        catch (Exception e)
        {
            Debug.LogError("Load failed: " + e.Message);
        }

        yield return new WaitForSecondsRealtime(0.2f);
        
        saveAnimations.ShowLoadingUI(false);
        
        Time.timeScale = 1.0f;
        
        _isBusy = false;
    }

    private string EncryptDecrypt(string text)
    {
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            result.Append((char)(text[i] ^ SecretKey[i % SecretKey.Length]));
        }

        return result.ToString();
    }
}

[Serializable]
public class SaveableEntry
{
    public string saveId;
    public string stateData;
}

[Serializable]
public class MasterSaveData
{
    public int saveVersion;
    public string timestamp;
    public List<SaveableEntry> savedObjects = new List<SaveableEntry>();
}