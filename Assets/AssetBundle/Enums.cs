public enum AssetBundleLoadMode
{
    Local,
    Remote,
}

public enum AssetBundlePackRange
{
    Directly,
    Recursively,
}

public enum AssetBundlePackType
{
    PackTogether,
    PackByFolder,
    Separately,
}

public enum AssetBundleAssetState
{
    None,
    Updated,
    New,
}

public enum AssetBundlePatchState
{
    NotReady,
    Ready,
    Success,
    Fail,
}

public enum PlatformType
{
    Android,
    iOS,
    Standalone,
}