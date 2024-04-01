using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public abstract class AssetBundleManager
{
    public static AssetBundleManager Create()
    {
#if UNITY_EDITOR
        if (EditorPrefs.GetBool("SimulateAssetBundle"))
        {
            return new AssetBundleManagerSimulator();
        }
        else
#endif
        {
            return new AssetBundleManagerDefault();
        }
    }

    public abstract Task InitializeAsync(string key);

    public abstract ValueTask<AssetBundlePatch> ReadyPatchAsync();

    public abstract ValueTask<AssetBundlePatch> PatchAsync(AssetBundlePatch assetBundlePatch);

    public abstract ValueTask LoadAssetBundleAsync(string assetBundleName, bool loadDependency = true);

    public abstract ValueTask<T> LoadAssetAsync<T>(string assetBundleName, string assetName) where T : Object;

    public abstract T LoadAsset<T>(string assetBundleName, string assetName) where T : Object;

    public abstract ValueTask LoadSceneAsync(string assetBundleName, string sceneName);

    public abstract void LoadScene(string assetBundleName, string sceneName);

    public abstract ValueTask UnloadAssetBundleAsync(string assetBundleName, bool unloadAllLoadedObjects);

    public abstract void UnloadAssetBundle(string assetBundleName, bool unloadAllLoadedObjects);

    public abstract void UnloadAsset<T>(string assetBundleName, string assetName) where T : Object;

    public abstract void UnloadAsset<T>(string assetBundleName, T asset) where T : Object;

    public abstract Task UnloadSceneAsync(string assetBundleName, string sceneName);

    protected abstract Task LoadCatalogAsync(AssetBundleSettings settings);

    protected abstract ValueTask<LoadedAssetBundle> LoadAssetBundleInternalAsync(string assetBundleName, bool loadDependency = true);

    protected abstract string GetUrl(string assetBundleName);

    protected abstract string GetAssetPath(string assetBundleName, string assetName);

    protected abstract void Log(string message);

    protected class LoadedAssetBundle
    {
        public string Name { get; private set; }

        public AssetBundle AssetBundle { get; private set; }

        public Dictionary<string, LoadedAsset> Assets { get; private set; }

        public LoadedAssetBundle(string name, AssetBundle assetBundle)
        {
            Name = name;
            AssetBundle = assetBundle;
            Assets = new Dictionary<string, LoadedAsset>();
        }
    }

    protected class LoadedAsset
    {
        public Object Object { get; private set; }

        public int ReferenceCount { get; set; }

        public LoadedAsset(Object obj)
        {
            Object = obj;
            ReferenceCount = 0;
        }
    }
}
