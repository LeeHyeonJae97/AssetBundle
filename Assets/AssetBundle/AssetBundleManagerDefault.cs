using MemoryPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AssetBundleManagerDefault : AssetBundleManager
{
    Dictionary<string, LoadedAssetBundle> _loaded;
    AssetBundleCatalog _catalog;

    public override async Task InitializeAsync(string key)
    {
        _loaded = new Dictionary<string, LoadedAssetBundle>();

        var settings = AssetBundleSettings.Load(key);

        await LoadCatalogAsync(settings);
    }

    public override async ValueTask<AssetBundlePatch> ReadyPatchAsync()
    {
        if (_catalog == null)
        {
            return new AssetBundlePatch(AssetBundlePatchState.NotReady, null, 0);
        }

        if (_catalog.LoadMode == AssetBundleLoadMode.Local)
        {
            return new AssetBundlePatch(AssetBundlePatchState.Ready, new List<string>(), 0);
        }

        List<string> patches = new List<string>();
        List<Hash128> cached = new List<Hash128>();

        foreach (var assetBundle in _catalog.AssetBundles.Values)
        {
            if (cached.Count > 0)
            {
                cached.Clear();
            }

            Caching.GetCachedVersions(assetBundle.Name, cached);

            if (cached.Count == 0 || !cached.Contains(assetBundle.Hash))
            {
                patches.Add(assetBundle.Name);
            }
        }

        if (patches.Count == 0)
        {
            return new AssetBundlePatch(AssetBundlePatchState.Ready, patches, 0);
        }
        else
        {
            bool success = true;
            double patchSize = 0;

            var results = await Task.WhenAll(patches.Select((patch) => UnityWebRequest.Head(GetUrl(patch)).SendWebRequest().ToAsync()));

            foreach (var result in results)
            {
                var request = result.webRequest;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    patchSize += double.Parse(request.GetResponseHeader("Content-Length"));
                }
                else
                {
                    success = false;
                }
            }

            return new AssetBundlePatch(success ? AssetBundlePatchState.Ready : AssetBundlePatchState.NotReady, patches, patchSize);
        }
    }

    public override async ValueTask<AssetBundlePatch> PatchAsync(AssetBundlePatch assetBundlePatch)
    {
        if (assetBundlePatch.State == AssetBundlePatchState.Ready)
        {
            bool success = true;

            if (assetBundlePatch.AssetBundleNames.Count > 0)
            {
                var tasks = assetBundlePatch.AssetBundleNames.Select((assetBundleName) => UnityWebRequestAssetBundle.GetAssetBundle(GetUrl(assetBundleName), _catalog.AssetBundles[assetBundleName].Hash).SendWebRequest().ToAsync());
                var results = await Task.WhenAll(tasks);

                foreach (var result in results)
                {
                    var request = result.webRequest;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // after caching, unload asset bundle again.
                        DownloadHandlerAssetBundle.GetContent(request).UnloadAsync(true);
                    }
                    else
                    {
                        success = false;
                    }
                }
            }

            assetBundlePatch = new AssetBundlePatch(success ? AssetBundlePatchState.Success : AssetBundlePatchState.Fail, assetBundlePatch.AssetBundleNames, assetBundlePatch.PatchSize);
        }

        return assetBundlePatch;
    }

    public override async ValueTask LoadAssetBundleAsync(string assetBundleName, bool loadDependency = true)
    {
        await LoadAssetBundleInternalAsync(assetBundleName, loadDependency);
    }

    public override async ValueTask<T> LoadAssetAsync<T>(string assetBundleName, string assetName)
    {
        // load asset bundle if not loaded yet.
        var loadedAssetBundle = await LoadAssetBundleInternalAsync(assetBundleName);

        var assetPath = GetAssetPath(assetBundleName, assetName);

        if (!loadedAssetBundle.Assets.ContainsKey(assetPath))
        {
            var reqeust = await loadedAssetBundle.AssetBundle.LoadAssetAsync<T>(assetPath).ToAsync();

            loadedAssetBundle.Assets[assetPath] = new LoadedAsset(reqeust.asset);
        }
        loadedAssetBundle.Assets[assetPath].ReferenceCount++;

        return loadedAssetBundle.Assets[assetPath].Object as T;
    }

    public override T LoadAsset<T>(string assetBundleName, string assetName)
    {
        // asset bundle should be loaded already.
        var loadedAssetBundle = LoadAssetBundleInternalAsync(assetBundleName).Result;

        var assetPath = GetAssetPath(assetBundleName, assetName);

        if (!loadedAssetBundle.Assets.ContainsKey(assetPath))
        {
            loadedAssetBundle.Assets[assetPath] = new LoadedAsset(loadedAssetBundle.AssetBundle.LoadAsset<T>(assetPath));
        }
        loadedAssetBundle.Assets[assetPath].ReferenceCount++;

        return loadedAssetBundle.Assets[assetPath].Object as T;
    }

    public override async ValueTask LoadSceneAsync(string assetBundleName, string sceneName)
    {
        // load asset bundle if not loaded yet.
        var loadedAssetBundle = await LoadAssetBundleInternalAsync(assetBundleName);

        var scenePath = GetAssetPath(assetBundleName, sceneName);

        if (!loadedAssetBundle.Assets.ContainsKey(scenePath))
        {
            loadedAssetBundle.Assets[scenePath] = new LoadedAsset(null);
        }
        loadedAssetBundle.Assets[scenePath].ReferenceCount++;

        await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive).ToAsync();
    }

    public override void LoadScene(string assetBundleName, string sceneName)
    {
        // asset bundle should be loaded already.
        var loadedAssetBundle = LoadAssetBundleInternalAsync(assetBundleName).Result;

        var scenePath = GetAssetPath(assetBundleName, sceneName);

        if (!loadedAssetBundle.Assets.ContainsKey(scenePath))
        {
            loadedAssetBundle.Assets[scenePath] = new LoadedAsset(null);
        }
        loadedAssetBundle.Assets[scenePath].ReferenceCount++;

        SceneManager.LoadScene(scenePath, LoadSceneMode.Additive);
    }

    public override async ValueTask UnloadAssetBundleAsync(string assetBundleName, bool unloadAllLoadedObjects)
    {
        if (_loaded.Remove(assetBundleName, out var loaded))
        {
            await loaded.AssetBundle.UnloadAsync(unloadAllLoadedObjects).ToAsync();
        }
    }

    public override void UnloadAssetBundle(string assetBundleName, bool unloadAllLoadedObjects)
    {
        if (_loaded.Remove(assetBundleName, out var loaded))
        {
            loaded.AssetBundle.Unload(unloadAllLoadedObjects);
        }
    }

    public override void UnloadAsset<T>(string assetBundleName, string assetName)
    {
        if (!_loaded.TryGetValue(assetBundleName, out var loaded)) return;

        if (loaded.Assets.TryGetValue(assetName, out var asset))
        {
            if (--asset.ReferenceCount == 0)
            {
                loaded.Assets.Remove(assetName);
                Resources.UnloadUnusedAssets();
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
                    loaded.Assets.Remove(key);
                    Resources.UnloadUnusedAssets();
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
            if (--asset.ReferenceCount == 0)
            {
                loaded.Assets.Remove(sceneName);
                await SceneManager.UnloadSceneAsync(sceneName).ToAsync();
            }
        }
    }

    protected override async Task LoadCatalogAsync(AssetBundleSettings settings)
    {
        if (settings.LoadMode == AssetBundleLoadMode.Local)
        {
            var path = settings.Url.Replace("{StreamingAssets}", Application.streamingAssetsPath);

            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                _catalog = await MemoryPackSerializer.DeserializeAsync<AssetBundleCatalog>(stream);
            }
        }
        else
        {
            var request = await UnityWebRequest.Get(settings.Url).SendWebRequest().ToAsync();

            if (request.webRequest.result == UnityWebRequest.Result.Success)
            {
                using (MemoryStream stream = new MemoryStream(request.webRequest.downloadHandler.data))
                {
                    _catalog = await MemoryPackSerializer.DeserializeAsync<AssetBundleCatalog>(stream);
                }
            }
        }
    }

    protected override async ValueTask<LoadedAssetBundle> LoadAssetBundleInternalAsync(string assetBundleName, bool loadDependency = true)
    {
        if (loadDependency)
        {
            var dependencies = _catalog.AssetBundles[assetBundleName].Dependencies;

            var tasks = dependencies
                .Append(assetBundleName)
                .Where((name) => !_loaded.ContainsKey(name))
                .Select((name) => LoadAsync(name));

            await Task.WhenAll(tasks);
        }
        else
        {
            if (!_loaded.ContainsKey(assetBundleName))
            {
                await LoadAsync(assetBundleName);
            }
        }        
        return _loaded[assetBundleName];

        async Task LoadAsync(string assetBundleName)
        {
            if (_catalog.LoadMode == AssetBundleLoadMode.Local)
            {
                var request = await AssetBundle.LoadFromFileAsync(GetUrl(assetBundleName)).ToAsync();

                _loaded[assetBundleName] = new LoadedAssetBundle(assetBundleName, request.assetBundle);
            }
            else
            {
                var request = UnityWebRequestAssetBundle.GetAssetBundle(GetUrl(assetBundleName));

                await request.SendWebRequest().ToAsync();

                var assetBundle = request.result == UnityWebRequest.Result.Success ? DownloadHandlerAssetBundle.GetContent(request) : null;

                _loaded[assetBundleName] = new LoadedAssetBundle(assetBundleName, assetBundle);
            }
        }
    }

    protected override string GetUrl(string assetBundleName)
    {
        return $"{_catalog.Url}/{assetBundleName}";
    }

    protected override string GetAssetPath(string assetBundleName, string assetName)
    {
        var assetBundleAsset = _catalog.AssetBundles[assetBundleName].Assets[assetName];

        return assetBundleAsset == null ? assetName : assetBundleAsset.Path;
    }

    protected override void Log(string message)
    {
        Debug.Log(message);
    }
}
