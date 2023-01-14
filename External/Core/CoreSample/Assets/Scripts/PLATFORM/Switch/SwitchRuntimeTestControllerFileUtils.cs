#if UNITY_SWITCH
using NBG.Automation.RuntimeTests.Controller;
using System.IO;
using UnityEngine;

class SwitchFileUtils : IFileUtils
{
    public string MountName { get { return "SD"; } }
    public string OutputPath { get { return "SD:/"; } }
    public string PlayerLogPath { get { return null; } }

    public void CreateDirectory(string path)
    {
        Utils.Log($"[Automation] Creating directory '{path}'");
        nn.Result ret;

        var handle = new nn.fs.DirectoryHandle();
        ret = nn.fs.Directory.Open(ref handle, path, nn.fs.OpenDirectoryMode.Directory);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(ret))
        {
            var result = nn.fs.Directory.Create(path);
            //if (!nn.fs.FileSystem.ResultPathAlreadyExists.Includes(result)) // ResultPathAlreadyExists is ok
                result.abortUnlessSuccess();
        }
        else
        {
            nn.fs.Directory.Close(handle);
        }
    }

    public void DeleteDirectory(string path)
    {
        Utils.Log($"[Automation] Deleting directory '{path}'");

        var result = nn.fs.Directory.DeleteRecursively(path);
        if (!nn.fs.FileSystem.ResultPathNotFound.Includes(result)) // ResultPathNotFound is ok
            result.abortUnlessSuccess();
    }

    public void WriteFile(string path, byte[] data)
    {
        Utils.Log($"[Automation] Writing file '{path}'");

        var file = new nn.fs.FileHandle();

        var result = nn.fs.File.Create(path, data.Length);
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref file, path, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        result = nn.fs.File.Write(file, 0, data, data.Length, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(file);
    }

    public void FlushFile(string path)
    {
        Utils.Log($"[Automation] Flushing file '{path}'");

        var file = new nn.fs.FileHandle();

        var result = nn.fs.File.Open(ref file, path, nn.fs.OpenFileMode.Write | nn.fs.OpenFileMode.AllowAppend);
        if (result.IsSuccess())
        {
            result = nn.fs.File.Flush(file);
            result.abortUnlessSuccess();

            nn.fs.File.Close(file);
        }
        else
        {
            Utils.Log($"[Automation] Flushing file failed: could not open '{path}'");
        }
    }

    public void Mount(string mountName)
    {
        Utils.Log($"[Automation] Mounting sdcard as '{mountName}'");
        var result = nn.fs.SdCard.MountForDebug(mountName);
        result.abortUnlessSuccess();
    }

    public void Unmount(string mountName)
    {
        Utils.Log($"[Automation] Unmounting '{mountName}'");
        nn.fs.FileSystem.Unmount(mountName);
    }
}
#endif
