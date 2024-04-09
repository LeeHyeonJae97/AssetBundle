using MemoryPack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[MemoryPackable]
public partial class AssetBundleSettings
{
    public static string Root { get { return $"{Application.streamingAssetsPath}/AssetBundleSettings"; } }

    [MemoryPackInclude]
    public string Url { get; private set; }

    [MemoryPackInclude]
    public AssetBundleLoadMode LoadMode { get; private set; }

    [MemoryPackConstructor]
    public AssetBundleSettings()
    {

    }

    public AssetBundleSettings(string url, AssetBundleLoadMode loadMode)
    {
        Url = url;
        LoadMode = loadMode;
    }
}
