using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveDirectoryHandler
{
    private const string SaveFolderName = "GameSaveData";
    private const int MaxSaveFiles = 200;

    public string GetSaveDirectory()
    {
        string path = Path.Combine(Application.persistentDataPath, SaveFolderName);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }

    public void ManageSaves()
    {
        string dir = GetSaveDirectory();
        DirectoryInfo info = new DirectoryInfo(dir);
        FileInfo[] files = info.GetFiles("*.sav");

        if (files.Length >= MaxSaveFiles)
        {
            Array.Sort(files, (a, b) => a.LastWriteTime.CompareTo(b.LastWriteTime));
            int filesToDelete = files.Length - MaxSaveFiles + 1;

            for (int i = 0; i < filesToDelete; i++)
            {
                try
                {
                    files[i].Delete();
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to delete old save: " + e.Message);
                }
            }
        }
    }

    public string GetSavePath(string fileName)
    {
        if (!fileName.EndsWith(".sav"))
        {
            fileName += ".sav";
        }

        return Path.Combine(GetSaveDirectory(), fileName);
    }

    public string GetTempPath(string fileName)
    {
        if (!fileName.EndsWith(".tmp"))
        {
            fileName += ".tmp";
        }

        return Path.Combine(GetSaveDirectory(), fileName);
    }

    public string GetLatestSaveFile()
    {
        string dir = GetSaveDirectory();
        DirectoryInfo info = new DirectoryInfo(dir);
        FileInfo[] files = info.GetFiles("*.sav");

        if (files.Length == 0)
        {
            return string.Empty;
        }

        Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
        return files[0].Name;
    }
}