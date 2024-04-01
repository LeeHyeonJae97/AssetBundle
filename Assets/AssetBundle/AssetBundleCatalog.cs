using MemoryPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[MemoryPackable]
public partial class AssetBundleCatalog
{
    [MemoryPackInclude]
    public string Url { get; private set; }

    [MemoryPackInclude]
    public AssetBundleLoadMode LoadMode { get; private set; }

    [MemoryPackInclude]
    public Dictionary<string, AssetBundleCatalogAssetBundle> AssetBundles { get; private set; }

    public AssetBundleCatalog(string url, AssetBundleLoadMode loadMode, Dictionary<string, AssetBundleCatalogAssetBundle> assetBundles)
    {
        Url = url;
        LoadMode = loadMode;
        AssetBundles = assetBundles;
    }
}

[MemoryPackable]
public partial class AssetBundleCatalogAssetBundle
{
    [MemoryPackInclude]
    public string Name { get; private set; }

    [MemoryPackInclude]
    public Hash128 Hash { get; private set; }

    [MemoryPackInclude]
    public string[] Dependencies { get; private set; }

    [MemoryPackInclude]
    public Dictionary<string, AssetBundleCatalogAssetBundleAsset> Assets { get; private set; }

    public AssetBundleCatalogAssetBundle(string name, Hash128 hash, string[] dependencies)
    {
        Name = name;
        Hash = hash;
        Dependencies = dependencies;
        Assets = new Dictionary<string, AssetBundleCatalogAssetBundleAsset>();
    }
}

[MemoryPackable]
public partial class AssetBundleCatalogAssetBundleAsset
{
    [MemoryPackInclude]
    public string Name { get; private set; }

    [MemoryPackInclude]
    public string Path { get; private set; }

    public AssetBundleCatalogAssetBundleAsset(string name, string path)
    {
        Name = name;
        Path = path;
    }
}
