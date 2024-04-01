#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AssetBundleManagerSimulator : AssetBundleManager
{
    Dictionary<string, LoadedAssetBundle> _loaded;
    AssetBundleBuildSettings _buildSettings;

    public override async Task InitializeAsync(string key)
    {
        _loaded = new Dictionary<string, LoadedAssetBundle>();
        _buildSettings = JsonUtility.FromJson<AssetBundleBuildSettings>(File.ReadAllText($"{AssetBundleBuildSettings.Root}/{key}.json"));
    }

    public override async ValueTask<AssetBundlePatch> ReadyPatchAsync()
    {
        return new AssetBundlePatch(AssetBundlePatchState.Ready, new List<string>(), 0);
    }

    public override async ValueTask<AssetBundlePatch> PatchAsync(AssetBundlePatch assetBundlePatch)
    {
        return new AssetBundlePatch(AssetBundlePatchState.Success, assetBundlePatch.AssetBundleNames, assetBundlePatch.PatchSize);
    }

    public override async ValueTask LoadAssetBundleAsync(string assetBundleName, bool loadDependency = true)
    {
        await LoadAssetBundleInternalAsync(assetBundleName, loadDependency);
    }

    public override async ValueTask<T> LoadAssetAsync<T>(string assetBundleName, string assetName)
    {
        // load asset bundle if not loaded yet
        var loadedAssetBundle = await LoadAssetBundleInternalAsync(assetBundleName);

        var assetPath = GetAssetPath(assetBundleName, assetName);

        if (!loadedAssetBundle.Assets.ContainsKey(assetPath))
        {
            loadedAssetBundle.Assets[assetPath] = new LoadedAsset(AssetDatabase.LoadAssetAtPath<T>(assetPath));
        }
        loadedAssetBundle.Assets[assetPath].ReferenceCount++;

        return loadedAssetBundle.Assets[assetPath].Object as T;
    }

    public override T LoadAsset<T>(string assetBundleName, string assetName)
    {
        // asset bundle should be loaded already
        var loadedAssetBundle = LoadAssetBundleInternalAsync(assetBundleName).Result;

        var assetPath = GetAssetPath(assetBundleName, assetName);

        if (!loadedAssetBundle.Assets.ContainsKey(assetPath))
        {
            loadedAssetBundle.Assets[assetPath] = new LoadedAsset(AssetDatabase.LoadAssetAtPath<T>(assetPath));
        }
        loadedAssetBundle.Assets[assetPath].ReferenceCount++;

        return loadedAssetBundle.Assets[assetPath].Object as T;
    }

    public override async ValueTask LoadSceneAsync(string assetBundleName, string sceneName)
    {
        // load asset bundle if not loaded yet
        var loadedAssetBundle = await LoadAssetBundleInternalAsync(assetBundleName);

        if (!loadedAssetBundle.Assets.ContainsKey(sceneName))
        {
            loadedAssetBundle.Assets[sceneName] = new LoadedAsset(null);
        }
        loadedAssetBundle.Assets[sceneName].ReferenceCount++;

        var path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"{sceneName} t:scene")[0]);

        await EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive)).ToAsync();
    }

    public override void LoadScene(string assetBundleName, string sceneName)
    {
        // asset bundle should be loaded already
        var loadedAssetBundle = LoadAssetBundleInternalAsync(assetBundleName).Result;

        if (!loadedAssetBundle.Assets.ContainsKey(sceneName))
        {
            loadedAssetBundle.Assets[sceneName] = new LoadedAsset(null);
        }
        loadedAssetBundle.Assets[sceneName].ReferenceCount++;

        var path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"{sceneName} t:scene")[0]);

        EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
    }

    public override async ValueTask UnloadAssetBundleAsync(string assetBundleName, bool unloadAllLoadedObjects)
    {
        _loaded.Remove(assetBundleName);
    }

    public override void UnloadAssetBundle(string assetBundleName, bool unloadAllLoadedObjects)
    {
        _loaded.Remove(assetBundleName);
    }

    public override void UnloadAsset<T>(string assetBundleName, string assetName)
    {
        if (!_loaded.TryGetValue(assetBundleName, out var loaded)) return;

        if (loaded.Assets.TryGetValue(assetName, out var asset))
        {
            asset.ReferenceCount--;

            if (asset.ReferenceCount == 0)
            {
                Resources.UnloadAsset(asset.Object);
                loaded.Assets.Remove(assetName);
            }
        }
    }

    public override void UnloadAsset<T>(string assetBundleName, T asset)
    {
        if (!_loaded.TryGetValue(assetBundleName, out var loaded)) return;

        foreach (var kv in loaded.Assets)
        {
            var key = kv.Key;
            var value = kv.Value;

            if (value.Object == asset)
            {
                if (--value.ReferenceCount == 0)
                {
                    Resources.UnloadAsset(value.Object);
                    loaded.Assets.Remove(key);
                }
                return;
            }
        }
    }

    public override async Task UnloadSceneAsync(string assetBundleName, string sceneName)
    {
        if (!_loaded.TryGetValue(assetBundleName, out var loaded)) return;

        if (loaded.Assets.TryGetValue(sceneName, out var asset))
        {
            asset.ReferenceCount--;

            if (asset.ReferenceCount == 0)
            {
                await EditorSceneManager.UnloadSceneAsync(sceneName).ToAsync();
                loaded.Assets.Remove(sceneName);
            }
        }
    }

    protected override async Task LoadCatalogAsync(AssetBundleSettings settings)
    {

    }

    protected override async ValueTask<LoadedAssetBundle> LoadAssetBundleInternalAsync(string assetBundleName, bool loadDependency = true)
    {
        if (!_loaded.ContainsKey(assetBundleName))
        {
            _loaded[assetBundleName] = new LoadedAssetBundle(assetBundleName, null);
        }
        return _loaded[assetBundleName];
    }

    protected override string GetUrl(string assetBundleName)
    {
        return assetBundleName;
    }

    protected override string GetAssetPath(string assetBundleName, string assetName)
    {
        var assetBundleAsset = _buildSettings.Assets.Find((assetBundleAsset) =>
        {
            var comparison = System.StringComparison.CurrentCultureIgnoreCase;
            var isSameAssetBundle = assetBundleAsset.AssetBundleName.Equals(assetBundleName, comparison);
            var isSameAsset = assetBundleAsset.Name.Equals(assetName, comparison);

            return isSameAssetBundle && isSameAsset;
        });

        return assetBundleAsset == null ? assetName : assetBundleAsset.Path;
    }

    protected override void Log(string message)
    {
        Debug.Log(message);
    }
}
#endif
