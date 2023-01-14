using System;
using System.IO;
using UnityEngine;

public struct DataSerializationStatus
{
    public SerializationStatus serializationStatus;
    public string msg;
    public DataSerializationStatus(SerializationStatus serializationStatus)
    {
        this.serializationStatus = serializationStatus;
        msg = "";
    }
    public DataSerializationStatus(SerializationStatus serializationStatus, string msg)
    {
        this.serializationStatus = serializationStatus;
        this.msg = msg;
    }
}
public enum SerializationStatus
{
    Ok,
    MinorError,
    CriticalError,
}
public static class DataSerializationUtility
{
    public static void Load<T>(string fileName, string fileExtension, Action<T, DataSerializationStatus> callback, int depth = 0)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}{fileExtension}");

        if (File.Exists(filePath))
        {
            try
            {
                string data = File.ReadAllText(filePath);
                var converted = JsonUtility.FromJson<T>(data);

                callback?.Invoke(converted, new DataSerializationStatus(SerializationStatus.Ok));
            }
            catch (Exception e)
            {
                if (depth != 0)
                {
                    callback?.Invoke(CreateInstaceOfType<T>(), new DataSerializationStatus(SerializationStatus.CriticalError, e.Message));
                    return;
                }

                var outcome = BackupSaveFile(fileName, fileExtension);

                if (outcome.serializationStatus == SerializationStatus.Ok)
                {
                    Load(fileName, fileExtension, callback, depth++);
                    return;
                }
                else
                {
                    callback?.Invoke(CreateInstaceOfType<T>(), outcome);
                }

            }
        }
        else
        {
            callback?.Invoke(CreateInstaceOfType<T>(), new DataSerializationStatus(SerializationStatus.Ok));
        }
    }

    public static void Save<T>(T toSave, string fileName, string fileExtension, Action<DataSerializationStatus> callback, int depth = 0)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}{fileExtension}");
        string json = JsonUtility.ToJson(toSave, true);

        var outcome = EnsureFileDirectoryExists<T>(filePath);

        if (outcome.serializationStatus == SerializationStatus.Ok)
        {
            try
            {
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                if (depth != 0)
                {
                    callback?.Invoke(new DataSerializationStatus(SerializationStatus.CriticalError, e.Message));
                    return;
                }

                outcome = BackupSaveFile(fileName, fileExtension);

                if (outcome.serializationStatus == SerializationStatus.Ok)
                {
                    Save(toSave, fileName, fileExtension, callback, depth++);
                    return;
                }
                else
                {
                    callback?.Invoke(outcome);

                }
            }
        }
        else
        {
            callback?.Invoke(outcome);
        }

    }

    private static DataSerializationStatus EnsureFileDirectoryExists<T>(string filePath)
    {
        if (!Directory.Exists(Application.persistentDataPath))
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
            }
            catch (Exception e)
            {
                return new DataSerializationStatus(SerializationStatus.CriticalError, e.Message);
            }
        }
       

        return new DataSerializationStatus(SerializationStatus.Ok);

    }

    private static DataSerializationStatus BackupSaveFile(string fileName, string fileExtension)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}{fileExtension}");

        if (Directory.Exists(Application.persistentDataPath))
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var newfileName = $"{fileName}-Backup-{DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss")}{fileExtension}";
                    var newfilePath = Path.Combine(Application.persistentDataPath, newfileName);
                    File.Move(filePath, newfilePath);
                }
                catch (Exception e)
                {
                    return new DataSerializationStatus(SerializationStatus.MinorError, e.Message);
                }
            }
        }

        return new DataSerializationStatus(SerializationStatus.Ok);
    }


    private static T CreateInstaceOfType<T>()
    {
        return (T)Activator.CreateInstance(typeof(T));
    }
}
