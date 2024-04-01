using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AssetBundlePatch
{
    public AssetBundlePatchState State { get; private set; }

    public List<string> AssetBundleNames { get; private set; }

    public double PatchSize { get; private set; }

    public AssetBundlePatch(AssetBundlePatchState state, List<string> assetBundleNames, double patchSize)
    {
        State = state;
        AssetBundleNames = assetBundleNames;
        PatchSize = patchSize;
    }
}
