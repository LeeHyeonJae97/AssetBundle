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

        //var go = ResourceManager.Test.LoadAsset<GameObject>("TestPrefabs", "New Prefab 1");

        //ResourceManager.Test.UnloadAsset<GameObject>("TestPrefabs", go);

        //ResourceManager.Test.UnloadAssetBundle("TestPrefabs", true);
    }
}
