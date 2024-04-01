#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

public class AssetBundleBuildReport
{
    public AssetBundleBuildReport(AssetBundleBuildSettings buildSettings, IBundleBuildResults bundleBuildResults)
    {
        // ******
        // Key  : Test
        // Date : 2024-03-27 15:23
        // Time : 03m 13s
        //
        // ******
        // Newly Built AssetBundles
        // 1. TestNested8 / LZ4 / 20.3KB        
        //  
        // ******
        // Updated AssetBundles
        // 1. TestNested10 / LZ4 / 18.29KB
        //
        // ******
        // Not Built AssetBundles
        //

        var assetResults = bundleBuildResults.AssetResults;

        foreach (var result in assetResults)
        {
            Debug.Log($"AssetResults.AssetPath : {AssetDatabase.GUIDToAssetPath(result.Value.Guid)}");
            Debug.Log($"AssetResults.FileInfo : {new FileInfo(AssetDatabase.GUIDToAssetPath(result.Value.Guid)).LastWriteTimeUtc}");
        }

        var files = Directory.GetFiles(buildSettings.Format(buildSettings.BuildPath));

        foreach(var file in files)
        {
            var info = new FileInfo(file);

            Debug.Log($"BuiltAssetBundles.FileInfo : {file} : {info.LastWriteTimeUtc}");
        }
    }
}
#endif
