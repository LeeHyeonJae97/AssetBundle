#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

[Serializable]
public class AssetBundleBuildReport
{
    public static string Root { get { return "Build/AssetBundle/Report"; } }

    [field: SerializeField]
    public string Key { get; private set; }

    [field: SerializeField]
    public string Date { get; private set; }

    [field: SerializeField]
    public string Elapsed { get; private set; }

    [field: SerializeField]
    public double TotalSize { get; private set; }

    [field: SerializeField]
    public double TotalCount { get; private set; }

    [field: SerializeField]
    public List<string> NewlyBuilt { get; private set; }

    [field: SerializeField]
    public List<string> Updated { get; private set; }

    public AssetBundleBuildReport(string key, DateTime date, double elapsed, IBundleBuildResults bundleBuildResults)
    {
        // **** Asset Bundle ****
        // Key         : Test
        // Date        : 2024-03-27 15:23
        // Time        : 03m 13s
        // Total Size  : 1.5GB
        // Total Count : 1,653
        //
        // Newly Built :
        //   - Assets/Test/Test1/TestNested8
        //   - Assets/Test/Test1/TestNested8
        //
        // Updated     :
        //   - Assets/Test/TestNested10
        //   - Assets/Test/TestNested10
        //

        Key = key;
        Date = date.ToString("yyyy-MM-dd HH:mm:ss");
        Elapsed = $"{(int)(elapsed / 1000 / 60)}m {(int)(elapsed / 1000)}s";
        TotalSize = (int)(bundleBuildResults.BundleInfos.Values.Select((bundleDetail) => new FileInfo(bundleDetail.FileName).Length).Sum() / 1000);
        TotalCount = bundleBuildResults.BundleInfos.Count;
        NewlyBuilt = new List<string>();
        Updated = new List<string>();

        foreach (var result in bundleBuildResults.BundleInfos.Values)
        {
            var assetPath = result.FileName;

            if (string.IsNullOrEmpty(assetPath))
            {
                // TODO :
                // handle error
            }
            else
            {
                var fileInfo = new FileInfo(assetPath);

                if (fileInfo.LastWriteTime >= date)
                {
                    if ((fileInfo.LastWriteTime - fileInfo.CreationTime).TotalSeconds < 1)
                    {
                        NewlyBuilt.Add(assetPath);
                    }
                    else
                    {
                        Updated.Add(assetPath);
                    }
                }
            }
        }
    }
}
#endif
