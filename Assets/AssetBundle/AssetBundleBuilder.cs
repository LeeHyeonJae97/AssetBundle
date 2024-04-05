#if UNITY_EDITOR
using MemoryPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.Build.Pipeline;

public class AssetBundleBuilder
{
    public static void Build(AssetBundleBuildSettings buildSettings)
    {
        if (BuildContent(out var results))
        {
            BuildSettings();
            BuildCatalog(results.BundleInfos);
        }

        bool BuildContent(out IBundleBuildResults results)
        {
            BuildTarget target = buildSettings.PlatformType switch
            {
                PlatformType.Android => BuildTarget.Android,
                PlatformType.iOS => BuildTarget.iOS,
                PlatformType.Standalone => EditorUserBuildSettings.activeBuildTarget,
            };

            BuildTargetGroup group = buildSettings.PlatformType switch
            {
                PlatformType.Android => BuildTargetGroup.Android,
                PlatformType.iOS => BuildTargetGroup.iOS,
                PlatformType.Standalone => BuildTargetGroup.Standalone,
            };

            var path = buildSettings.Format(buildSettings.BuildPath);

            var buildParams = new AssetBundleBuildParameters(buildSettings.AssetBundles, target, group, path);
            var buildContent = new BundleBuildContent(GetAssetBundleBuilds(buildSettings.Assets));

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var returnCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out results);

            return returnCode == ReturnCode.Success || returnCode == ReturnCode.SuccessCached || returnCode == ReturnCode.SuccessNotRun;
        }

        void BuildSettings()
        {
            if (!Directory.Exists(AssetBundleSettings.Root))
            {
                Directory.CreateDirectory(AssetBundleSettings.Root);
            }

            var settings = new AssetBundleSettings($"{buildSettings.Format(buildSettings.LoadCatalogPath)}/catalog.bin", buildSettings.LoadMode);

            File.WriteAllBytes($"{AssetBundleSettings.Root}/{buildSettings.Key}.bin", MemoryPackSerializer.Serialize(settings));
        }

        void BuildCatalog(Dictionary<string, BundleDetails> bundleInfos)
        {
            Dictionary<string, AssetBundleCatalogAssetBundle> assetBundles = new Dictionary<string, AssetBundleCatalogAssetBundle>();

            for (int i = 0; i < buildSettings.Assets.Count; i++)
            {
                var asset = buildSettings.Assets[i];

                if (bundleInfos.TryGetValue(asset.AssetBundleName, out var info))
                {
                    if (!assetBundles.ContainsKey(asset.AssetBundleName))
                    {
                        // if there's no change in asset bundle, the hash will not be updated too
                        assetBundles[asset.AssetBundleName] = new AssetBundleCatalogAssetBundle(asset.AssetBundleName, info.Hash, info.Dependencies);
                    }
                    assetBundles[asset.AssetBundleName].Assets[asset.Name] = new AssetBundleCatalogAssetBundleAsset(asset.Name, asset.Path);
                }
            }

            var path = buildSettings.Format(buildSettings.BuildPath);
            var catalog = new AssetBundleCatalog(buildSettings.Format(buildSettings.LoadAssetBundlePath), buildSettings.LoadMode, assetBundles);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            File.WriteAllBytes($"{path}/catalog.bin", MemoryPackSerializer.Serialize(catalog));
        }
    }

    public static void Update(AssetBundleBuildSettings buildSettings)
    {
        var assetBundles = buildSettings.AssetBundles.OrderByDescending((assetBundle) => assetBundle.Path.Length);
        var ignoreList = buildSettings.IgnoreList == null || buildSettings.IgnoreList.Length == 0 ? new HashSet<string>() : new HashSet<string>(buildSettings.IgnoreList);
        var assets = buildSettings.Assets;

        if (assets.Count > 0)
        {
            assets.Clear();
        }

        foreach (var assetBundle in assetBundles)
        {
            if (assetBundle.Generateds.Count > 0)
            {
                assetBundle.Generateds.Clear();
            }

            var path = assetBundle.Path;

            if (!AssetDatabase.IsValidFolder(path) || ignoreList.Contains(path)) continue;

            var assetBundleName = assetBundle.Name;
            var packType = assetBundle.PackType;
            var packRange = assetBundle.PackRange;

            if (packType == AssetBundlePackType.PackTogether)
            {
                UpdatePackTogether(assetBundle, path, assetBundleName, packRange);
            }
            else if (packType == AssetBundlePackType.PackByFolder)
            {
                UpdatePackByFolder(assetBundle, path, assetBundleName, packRange);
            }
            else if (packType == AssetBundlePackType.Separately)
            {
                UpdatePackSeparately(assetBundle, path, assetBundleName, packRange);
            }
        }

        void UpdatePackTogether(AssetBundleBuildSettingsAssetBundle assetBundle, string path, string assetBundleName, AssetBundlePackRange packRange)
        {
            var files = Directory.GetFiles(path, "*", packRange == AssetBundlePackRange.Directly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
            var assetsInAssetBundle = new List<AssetBundleBuildSettingsAssetBundleAsset>();

            if (string.IsNullOrEmpty(assetBundleName))
            {
                Debug.LogError($"AssetBundleName can not be empty : {path}");
                return;
            }

            foreach (var file in files)
            {
                if (IsAsset(file))
                {
                    var importer = AssetImporter.GetAtPath(file);

                    var assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                    var assetPath = importer.assetPath;
                    var assetState = GetState(assetBundleName, assetPath);

                    if (!ignoreList.Contains(assetPath) && !assets.Any((asset) => asset.Path.Equals(assetPath)))
                    {
                        var asset = new AssetBundleBuildSettingsAssetBundleAsset(assetName, assetPath, assetBundleName, assetState);

                        assets.Add(asset);
                        assetsInAssetBundle.Add(asset);
                    }
                }
            }

            if (assetsInAssetBundle.Count > 0)
            {
                assetBundle.Generateds.Add(new AssetBundleBuildSettingsGeneratedAssetBundle(assetBundleName, assetsInAssetBundle));
            }
        }

        void UpdatePackByFolder(AssetBundleBuildSettingsAssetBundle assetBundle, string path, string assetBundleName, AssetBundlePackRange packRange)
        {
            var directories = Directory.GetDirectories(path, "*", packRange == AssetBundlePackRange.Directly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories).Append(path).OrderBy((directory) => directory);

            foreach (var directory in directories)
            {
                var assetsInAssetBundle = new List<AssetBundleBuildSettingsAssetBundleAsset>();
                var importer = AssetImporter.GetAtPath(directory);

                var directoryName = Path.GetFileNameWithoutExtension(importer.assetPath);
                var directoryPath = importer.assetPath;

                if (!ignoreList.Contains(directoryPath) && (directory.Equals(path) || !assetBundles.Any((assetBundle) => assetBundle.Path.Equals(directoryPath))))
                {
                    var files = Directory.GetFiles(directoryPath);

                    foreach (var file in files)
                    {
                        if (IsAsset(file))
                        {
                            importer = AssetImporter.GetAtPath(file);

                            var assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                            var assetPath = importer.assetPath;
                            var assetState = GetState($"{assetBundleName}{directoryName}", assetPath);

                            if (!ignoreList.Contains(assetPath))
                            {
                                var asset = new AssetBundleBuildSettingsAssetBundleAsset(assetName, assetPath, $"{assetBundleName}{directoryName}", assetState);

                                assets.Add(asset);
                                assetsInAssetBundle.Add(asset);
                            }
                        }
                    }

                    if (assetsInAssetBundle.Count > 0)
                    {
                        assetBundle.Generateds.Add(new AssetBundleBuildSettingsGeneratedAssetBundle($"{assetBundleName}{directoryName}", assetsInAssetBundle));
                    }
                }
            }
        }

        void UpdatePackSeparately(AssetBundleBuildSettingsAssetBundle assetBundle, string path, string assetBundleName, AssetBundlePackRange packRange)
        {
            var files = Directory.GetFiles(path, "*", packRange == AssetBundlePackRange.Directly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (IsAsset(file))
                {
                    var importer = AssetImporter.GetAtPath(file);

                    var assetName = Path.GetFileNameWithoutExtension(importer.assetPath);
                    var assetPath = importer.assetPath;
                    var assetState = GetState($"{assetBundleName}{assetName}", assetPath);

                    if (!ignoreList.Contains(assetPath) && !assets.Any((asset) => asset.Path.Equals(assetPath)))
                    {
                        var asset = new AssetBundleBuildSettingsAssetBundleAsset(assetName, assetPath, $"{assetBundleName}{assetName}", assetState);

                        assets.Add(asset);
                        assetBundle.Generateds.Add(new AssetBundleBuildSettingsGeneratedAssetBundle($"{assetBundleName}{assetName}", new List<AssetBundleBuildSettingsAssetBundleAsset>() { asset }));
                    }
                }
            }
        }

        bool IsAsset(string path)
        {
            var extension = Path.GetExtension(path);

            if (extension.Equals(".meta") || extension.Equals(".cs")) return false;

            return true;
        }

        AssetBundleAssetState GetState(string assetBundleName, string assetPath)
        {
            var builtAssetBundle = new FileInfo($"{buildSettings.Format(buildSettings.BuildPath)}/{assetBundleName}");

            if (!builtAssetBundle.Exists)
            {
                return AssetBundleAssetState.New;
            }
            else if (builtAssetBundle.LastWriteTimeUtc < new FileInfo(assetPath).LastWriteTimeUtc)
            {
                return AssetBundleAssetState.Updated;
            }
            else
            {
                return AssetBundleAssetState.None;
            }
        }
    }

    static AssetBundleBuild[] GetAssetBundleBuilds(List<AssetBundleBuildSettingsAssetBundleAsset> Assets)
    {
        var groups = Assets.GroupBy((asset) => asset.AssetBundleName);
        var builds = groups.Select((group) => new AssetBundleBuild()
        {
            assetBundleName = group.Key,
            assetNames = group.Select((asset) => asset.Path).ToArray(),
        });

        return builds.ToArray();
    }
}
#endif
