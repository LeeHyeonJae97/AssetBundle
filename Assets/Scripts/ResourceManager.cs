using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public static class ResourceManager
{
    public static AssetBundleManager Test { get; private set; }

    public static async Task InitializeAsync()
    {
        var taskInitTest = InitializeAsync("Test");

        await Task.WhenAll(taskInitTest);

        Test = taskInitTest.Result;
    }

    public static async Task<AssetBundleManager> InitializeAsync(string key)
    {
        var manager = AssetBundleManager.Create();
        await manager.InitializeAsync(key);

        return manager;
    }
}
