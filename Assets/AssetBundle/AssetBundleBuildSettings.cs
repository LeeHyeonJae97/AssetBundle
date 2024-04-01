#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class AssetBundleBuildSettings
{
    public static string Root { get { return $"Assets/AssetBundle/BuildSettings"; } }

    [field: SerializeField]
    public string Key { get; private set; }

    [field: SerializeField]
    public string Version { get; private set; }

    [field: SerializeField]
    public PlatformType PlatformType { get; private set; }

    [field: SerializeField]
    public string BuildPath { get; private set; }

    [field: SerializeField]
    public AssetBundleLoadMode LoadMode { get; private set; }

    [field: SerializeField]
    public string LoadCatalogPath { get; private set; }

    [field: SerializeField]
    public string LoadAssetBundlePath { get; private set; }

    [field: SerializeField]
    public AssetBundleBuildSettingsAssetBundle[] AssetBundles { get; private set; }

    [field: SerializeField]
    public string[] IgnoreList { get; private set; }

    [field: SerializeField]
    public List<AssetBundleBuildSettingsGeneratedAssetBundle> Generateds { get; private set; }

    [field: SerializeField]
    public List<AssetBundleBuildSettingsAssetBundleAsset> Assets { get; private set; }

    public AssetBundleBuildSettings(string key)
    {
        Key = key;
    }

    public AssetBundleBuildSettings Copy(string key)
    {
        var buildSettings = new AssetBundleBuildSettings(key);
        buildSettings.Version = Version;
        buildSettings.PlatformType = PlatformType;
        buildSettings.BuildPath = BuildPath;
        buildSettings.LoadMode = LoadMode;
        buildSettings.LoadCatalogPath = LoadCatalogPath;
        buildSettings.LoadAssetBundlePath = LoadAssetBundlePath;
        buildSettings.AssetBundles = AssetBundles;
        buildSettings.IgnoreList = IgnoreList;
        buildSettings.Generateds = Generateds;
        buildSettings.Assets = Assets;

        return buildSettings;
    }

    public string Format(string path)
    {
        return path
            .Replace("{Key}", $"{Key}")
            .Replace("{PlatformType}", $"{PlatformType}")
            .Replace("{Version}", $"{Version}");
    }

    public void Save()
    {
        File.WriteAllText($"{Root}/{Key}.json", JsonUtility.ToJson(this));                
    }
}

[Serializable]
public class AssetBundleBuildSettingsAssetBundle
{
    [field: SerializeField]
    public string Name { get; private set; }

    [field: SerializeField]
    public string Path { get; private set; }

    [field: SerializeField]
    public AssetBundlePackType PackType { get; private set; }

    [field: SerializeField]
    public AssetBundlePackRange PackRange { get; private set; }

    [field: SerializeField]
    public CompressionType CompressionType { get; private set; }

    [field: SerializeField]
    public List<AssetBundleBuildSettingsGeneratedAssetBundle> Generateds { get; private set; }
}

[Serializable]
public class AssetBundleBuildSettingsGeneratedAssetBundle
{
    [field: SerializeField]
    public string Name { get; private set; }

    [field: SerializeField]
    public List<AssetBundleBuildSettingsAssetBundleAsset> Assets { get; private set; }

    public AssetBundleBuildSettingsGeneratedAssetBundle(string name, List<AssetBundleBuildSettingsAssetBundleAsset> assets)
    {
        Name = name;
        Assets = assets;
    }
}

[Serializable]
public class AssetBundleBuildSettingsAssetBundleAsset
{
    [field: SerializeField]
    public string Name { get; private set; }

    [field: SerializeField]
    public string Path { get; private set; }

    [field: SerializeField]
    public string AssetBundleName { get; private set; }

    [field: SerializeField]
    public AssetBundleAssetState State { get; private set; }

    public AssetBundleBuildSettingsAssetBundleAsset(string name, string path, string assetBundleName, AssetBundleAssetState state)
    {
        Name = name;
        Path = path;
        AssetBundleName = assetBundleName;
        State = state;
    }
}
#endif
