#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine;

public class AssetBundleBuildParameters : BundleBuildParameters
{
    Dictionary<string, BuildCompression> _compressions;

    public AssetBundleBuildParameters(AssetBundleBuildSettingsAssetBundle[] assetBundles, BuildTarget target, BuildTargetGroup group, string outpuFolder) : base(target, group, outpuFolder)
    {
        _compressions = new Dictionary<string, BuildCompression>();

        foreach (var assetBundle in assetBundles)
        {
            foreach (var generated in assetBundle.Generateds)
            {
                _compressions[generated.Name] = assetBundle.CompressionType switch
                {
                    CompressionType.None => BuildCompression.Uncompressed,
                    CompressionType.Lzma => BuildCompression.LZMA,
                    CompressionType.Lz4 => BuildCompression.LZ4Runtime,
                    CompressionType.Lz4HC => BuildCompression.LZ4,
                };
            }
        }
    }

    public override BuildCompression GetCompressionForIdentifier(string identifier)
    {
        return _compressions.TryGetValue(identifier, out var value) ? value : BuildCompression.Uncompressed;
    }
}
#endif
