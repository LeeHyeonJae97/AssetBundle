using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    async void Start()
    {
        await ResourceManager.InitializeAsync();
        await ResourceManager.Test.LoadAssetBundleAsync("TestPrefabs");
        await ResourceManager.Test.LoadAssetBundleAsync("TestMaterials");
        await ResourceManager.Test.LoadAssetBundleAsync("TestScenes");
    }
}
