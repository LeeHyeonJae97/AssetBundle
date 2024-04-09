#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling.Memory;
using UnityEditor;
using UnityEngine;

public class AssetBundleLogWindow : EditorWindow
{
    // Log like unity console about
    // 1. import setting (load mode, url, ...)
    // 2. patch list and result
    // 3. function calls and result
    // 
    // Inspect current loaded asset bundle

    [MenuItem("Window/AssetBundle/Log")]
    static void Init()
    {
        GetWindow<AssetBundleLogWindow>("AssetBundle Log").Show();
    }

    void OnGUI()
    {

    }
}
#endif
