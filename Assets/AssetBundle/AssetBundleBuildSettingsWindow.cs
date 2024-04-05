#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundleBuildSettingsWindow : EditorWindow
{
    [SerializeReference]
    AssetBundleBuildSettings _buildSettings;

    SerializedObject _serializedObject;
    Vector2 _scroll;
    bool _isDirty;
    bool _expandHierarchy;

    [MenuItem("Window/AssetBundle Build Settings")]
    static void Init()
    {
        GetWindow<AssetBundleBuildSettingsWindow>("AssetBundle Build Settings");
    }

    void OnEnable()
    {
        _serializedObject = new SerializedObject(this);
    }

    void OnDisable()
    {

    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(150));
        DrawAssetBundleBuildSettingsList();
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        DrawAssetBundleBuildSettings();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        if (_serializedObject.hasModifiedProperties)
        {
            _isDirty = true;
            _serializedObject.ApplyModifiedProperties();
        }
        _serializedObject.Update();
    }

    void DrawAssetBundleBuildSettingsList()
    {
        var exist = Directory.Exists(AssetBundleBuildSettings.Root);

        if (GUILayout.Button("+"))
        {
            var key = "NewAssetBundleBuildSettings";

            if (!exist)
            {
                Directory.CreateDirectory(AssetBundleBuildSettings.Root);
            }
            File.WriteAllText($"{AssetBundleBuildSettings.Root}/{key}.json", JsonUtility.ToJson(new AssetBundleBuildSettings(key)));
        }

        if (exist)
        {
            foreach (var filePath in Directory.GetFiles(AssetBundleBuildSettings.Root))
            {
                if (filePath.EndsWith(".meta")) continue;

                AssetBundleBuildSettings buildSettings = JsonUtility.FromJson<AssetBundleBuildSettings>(File.ReadAllText(filePath));

                if (GUILayout.Button(buildSettings.Key))
                {
                    var buildSettingsProp = _serializedObject.FindProperty("_buildSettings");

                    buildSettingsProp.managedReferenceValue = buildSettings;
                }
            }
        }
    }

    void DrawAssetBundleBuildSettings()
    {
        var buildSettingsProp = _serializedObject.FindProperty("_buildSettings");

        if (buildSettingsProp.managedReferenceValue == null) return;

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSettings(buildSettingsProp);
        DrawAssetBundles(buildSettingsProp.FindPropertyRelative("AssetBundles", true));
        DrawIgnoreList(buildSettingsProp.FindPropertyRelative("IgnoreList", true));
        DrawAssetBundleHierarchy(buildSettingsProp.FindPropertyRelative("AssetBundles", true));

        EditorGUILayout.EndScrollView();

        DrawBtns(buildSettingsProp);
    }

    void DrawSettings(SerializedProperty settingsProp)
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        DrawKeyProp();
        EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("Version", true));
        EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("PlatformType", true));
        EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("BuildPath", true));
        EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("LoadMode", true));
        EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("LoadCatalogPath", true));
        EditorGUILayout.PropertyField(settingsProp.FindPropertyRelative("LoadAssetBundlePath", true));

        EditorPrefs.SetBool("SimulateAssetBundle", EditorGUILayout.Toggle("Simulation (Editor Only)", EditorPrefs.GetBool("SimulateAssetBundle")));

        void DrawKeyProp()
        {
            var keyProp = settingsProp.FindPropertyRelative("Key", true);

            var key = EditorGUILayout.TextField("Key", keyProp.stringValue);

            if (!keyProp.stringValue.Equals(keyProp))
            {
                File.Delete($"{AssetBundleBuildSettings.Root}/{keyProp.stringValue}.json");

                keyProp.stringValue = key;
                File.WriteAllText($"{AssetBundleBuildSettings.Root}/{keyProp.stringValue}.json", JsonUtility.ToJson(settingsProp.managedReferenceValue));
            }
        }
    }

    void DrawAssetBundles(SerializedProperty assetBundlesProp)
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        var content = new GUIContent($"AssetBundles ({assetBundlesProp.arraySize})");
        EditorGUILayout.LabelField(content, EditorStyles.boldLabel, GUILayout.Width(EditorStyles.boldLabel.CalcSize(content).x));
        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("icon dropdown")), EditorStyles.miniButton, GUILayout.Width(30)))
        {
            assetBundlesProp.isExpanded = !assetBundlesProp.isExpanded;
        }
        if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
        {
            assetBundlesProp.InsertArrayElementAtIndex(assetBundlesProp.arraySize);
        }
        if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)) && assetBundlesProp.arraySize > 0)
        {
            assetBundlesProp.DeleteArrayElementAtIndex(assetBundlesProp.arraySize - 1);
        }
        if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)) && assetBundlesProp.arraySize > 0)
        {
            assetBundlesProp.ClearArray();
        }
        EditorGUILayout.EndHorizontal();

        if (assetBundlesProp.arraySize == 0)
        {
            EditorGUILayout.LabelField("No elements");
        }
        else if (assetBundlesProp.isExpanded)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.miniButton, GUILayout.Width(170));
            EditorGUILayout.LabelField("Path", EditorStyles.miniButton);
            EditorGUILayout.LabelField("PackType", EditorStyles.miniButton, GUILayout.Width(130));
            EditorGUILayout.LabelField("PackRange", EditorStyles.miniButton, GUILayout.Width(130));
            EditorGUILayout.LabelField("CompressionType", EditorStyles.miniButton, GUILayout.Width(130));
            GUILayout.Space(20 + 2 + 20 + 2);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < assetBundlesProp.arraySize; i++)
            {
                var assetBundleProp = assetBundlesProp.GetArrayElementAtIndex(i);
                var deleted = false;

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(assetBundleProp.FindPropertyRelative("Name", true), GUIContent.none, GUILayout.Width(170));
                EditorGUILayout.PropertyField(assetBundleProp.FindPropertyRelative("Path", true), GUIContent.none);
                EditorGUILayout.PropertyField(assetBundleProp.FindPropertyRelative("PackType", true), GUIContent.none, GUILayout.Width(130));
                EditorGUILayout.PropertyField(assetBundleProp.FindPropertyRelative("PackRange", true), GUIContent.none, GUILayout.Width(130));
                EditorGUILayout.PropertyField(assetBundleProp.FindPropertyRelative("CompressionType", true), GUIContent.none, GUILayout.Width(130));

                //if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("icon dropdown")), EditorStyles.miniButton, GUILayout.Width(30)))
                //{
                //    assetBundleProp.isExpanded = !assetBundleProp.isExpanded;
                //}

                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    assetBundlesProp.InsertArrayElementAtIndex(i);
                }

                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)) && assetBundlesProp.arraySize > 0)
                {
                    deleted = true;
                }

                EditorGUILayout.EndHorizontal();

                //if (assetBundleProp.isExpanded)
                //{
                //    var generatedsProp = assetBundleProp.FindPropertyRelative("Generateds", true);

                //    for (int j = 0; j < generatedsProp.arraySize; j++)
                //    {
                //        var generatedProp = generatedsProp.GetArrayElementAtIndex(j);

                //        EditorGUILayout.BeginHorizontal();
                //        GUI.enabled = false;
                //        EditorGUILayout.PropertyField(generatedProp.FindPropertyRelative("Name", true), GUIContent.none, GUILayout.Width(170));
                //        GUI.enabled = true;
                //        GUILayout.FlexibleSpace();
                //        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("icon dropdown")), EditorStyles.miniButton, GUILayout.Width(30)))
                //        {
                //            generatedProp.isExpanded = !generatedProp.isExpanded;
                //        }
                //        GUILayout.Space(20 + 2 + 20 + 2);
                //        EditorGUILayout.EndHorizontal();

                //        if (generatedProp.isExpanded)
                //        {
                //            var assetsProp = generatedProp.FindPropertyRelative("Assets", true);

                //            for (int k = 0; k < assetsProp.arraySize; k++)
                //            {
                //                var assetProp = assetsProp.GetArrayElementAtIndex(k);
                //                var nameProp = assetProp.FindPropertyRelative("Name", true);
                //                var pathProp = assetProp.FindPropertyRelative("Path", true);

                //                EditorGUILayout.BeginHorizontal();
                //                GUI.enabled = false;
                //                EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.Width(170));
                //                EditorGUILayout.PropertyField(pathProp, GUIContent.none);
                //                GUI.enabled = true;
                //                GUILayout.Space(3 + 130 + 2 + 130 + 2 + 130 + 2);
                //                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_scenepicking_pickable_hover")), EditorStyles.miniButton, GUILayout.Width(30)))
                //                {
                //                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(pathProp.stringValue);
                //                    EditorGUIUtility.PingObject(Selection.activeObject);
                //                }
                //                GUILayout.Space(20 + 2 + 20 + 2);
                //                EditorGUILayout.EndHorizontal();
                //            }
                //        }
                //    }
                //}

                if (deleted)
                {
                    assetBundlesProp.DeleteArrayElementAtIndex(i);
                }
            }
        }
    }

    void DrawIgnoreList(SerializedProperty ignoreListProp)
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        var content = new GUIContent($"Ignores ({ignoreListProp.arraySize})");
        EditorGUILayout.LabelField(content, EditorStyles.boldLabel, GUILayout.Width(EditorStyles.boldLabel.CalcSize(content).x));
        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("icon dropdown")), EditorStyles.miniButton, GUILayout.Width(30)))
        {
            ignoreListProp.isExpanded = !ignoreListProp.isExpanded;
        }
        if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
        {
            ignoreListProp.InsertArrayElementAtIndex(ignoreListProp.arraySize);
        }
        if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)) && ignoreListProp.arraySize > 0)
        {
            ignoreListProp.DeleteArrayElementAtIndex(ignoreListProp.arraySize - 1);
        }
        if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20)) && ignoreListProp.arraySize > 0)
        {
            ignoreListProp.ClearArray();
        }
        EditorGUILayout.EndHorizontal();

        if (ignoreListProp.arraySize == 0)
        {
            EditorGUILayout.LabelField("No elements");
        }
        else if (ignoreListProp.isExpanded)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path", EditorStyles.miniButton);
            GUILayout.Space(44);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < ignoreListProp.arraySize; i++)
            {
                var ignoreProp = ignoreListProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(ignoreProp, GUIContent.none);
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    ignoreListProp.InsertArrayElementAtIndex(i);
                }
                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(20)) && ignoreListProp.arraySize > 0)
                {
                    ignoreListProp.DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    void DrawAssetBundleHierarchy(SerializedProperty assetBundlesProp)
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        var content = new GUIContent($"AssetBundles Hierarchy ({assetBundlesProp.arraySize})");
        EditorGUILayout.LabelField(content, EditorStyles.boldLabel, GUILayout.Width(EditorStyles.boldLabel.CalcSize(content).x));
        if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("icon dropdown")), EditorStyles.miniButton, GUILayout.Width(30)))
        {
            _expandHierarchy = !_expandHierarchy;
        }
        EditorGUILayout.EndHorizontal();

        if (assetBundlesProp.arraySize == 0)
        {
            EditorGUILayout.LabelField("No elements");
        }
        else if (_expandHierarchy)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AssetBundleName", EditorStyles.miniButton, GUILayout.Width(200));
            EditorGUILayout.LabelField("GeneratedAssetBundleName", EditorStyles.miniButton, GUILayout.Width(200));
            EditorGUILayout.LabelField("AssetName", EditorStyles.miniButton, GUILayout.Width(200));
            EditorGUILayout.LabelField("AssetPath", EditorStyles.miniButton);
            GUILayout.Space(32);
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < assetBundlesProp.arraySize; i++)
            {
                var assetBundleProp = assetBundlesProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(assetBundleProp.FindPropertyRelative("Name", true).stringValue, EditorStyles.textField, GUILayout.Width(177));
                if (GUILayout.Button(new GUIContent("¢º"), EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    assetBundleProp.isExpanded = !assetBundleProp.isExpanded;
                }

                if (assetBundleProp.isExpanded)
                {
                    var generatedsProp = assetBundleProp.FindPropertyRelative("Generateds", true);

                    EditorGUILayout.BeginVertical();

                    for (int j = 0; j < generatedsProp.arraySize; j++)
                    {
                        var generatedProp = generatedsProp.GetArrayElementAtIndex(j);

                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(generatedProp.FindPropertyRelative("Name", true).stringValue, EditorStyles.textField, GUILayout.Width(177));
                        if (GUILayout.Button(new GUIContent("¢º"), EditorStyles.miniButton, GUILayout.Width(20)))
                        {
                            generatedProp.isExpanded = !generatedProp.isExpanded;
                        }

                        if (generatedProp.isExpanded)
                        {
                            var assetsProp = generatedProp.FindPropertyRelative("Assets", true);

                            EditorGUILayout.BeginVertical();
                            for (int k = 0; k < assetsProp.arraySize; k++)
                            {
                                var nameProp = assetsProp.GetArrayElementAtIndex(k).FindPropertyRelative("Name", true);
                                var pathProp = assetsProp.GetArrayElementAtIndex(k).FindPropertyRelative("Path", true);

                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField(nameProp.stringValue, EditorStyles.textField, GUILayout.Width(200));
                                EditorGUILayout.LabelField(pathProp.stringValue, EditorStyles.textField);
                                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_scenepicking_pickable_hover")), EditorStyles.miniButton, GUILayout.Width(30)))
                                {
                                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(pathProp.stringValue);
                                    EditorGUIUtility.PingObject(Selection.activeObject);
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUILayout.EndVertical();
                        }
                        ;
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    void DrawBtns(SerializedProperty buildSettingsProp)
    {
        var buildSettings = buildSettingsProp.managedReferenceValue as AssetBundleBuildSettings;

        GUILayout.FlexibleSpace();
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(new GUIContent("Update", "Update asset list to build and save this asset bundle build settings")))
        {
            _isDirty = false;

            buildSettings.Save();
            AssetBundleBuilder.Update(buildSettings);
        }

        GUI.enabled = !_isDirty;
        if (GUILayout.Button(new GUIContent("Build", _isDirty ? "Need to update first" : "Build asset bundle content and asset bundle settings and catalog")))
        {
            buildSettings.Save();
            AssetBundleBuilder.Update(buildSettings);
            AssetBundleBuilder.Build(buildSettings);
        }
        GUI.enabled = true;

        var buildPath = buildSettings.Format(buildSettings.BuildPath);
        var exists = Directory.Exists(buildPath);

        GUI.enabled = exists;
        if (GUILayout.Button(new GUIContent("Open", "Open folder at build path")))
        {
            EditorUtility.RevealInFinder(buildPath);
        }
        GUI.enabled = true;

        GUI.enabled = exists;
        if (GUILayout.Button(new GUIContent("Clear", "Delete folder at build path")))
        {
            Directory.Delete(buildPath, true);
        }
        GUI.enabled = true;

        if (GUILayout.Button(new GUIContent("Clear Cache", "Clear cache for asset bundles in this asset bundle build settings")))
        {
            for (int i = 0; i < buildSettings.AssetBundles.Length; i++)
            {
                foreach (var generated in buildSettings.AssetBundles[i].Generateds)
                {
                    Caching.ClearAllCachedVersions(generated.Name);
                }
            }
        }

        if (GUILayout.Button(new GUIContent("Copy", "Copy this asset bundle build settings")))
        {
            var key = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath($"{AssetBundleBuildSettings.Root}/{buildSettings.Key}.json"));

            buildSettings.Copy(key).Save();
        }

        if (GUILayout.Button(new GUIContent("Remove", "Remove this asset bundle build settings")))
        {
            buildSettingsProp.managedReferenceValue = null;

            File.Delete($"{AssetBundleBuildSettings.Root}/{buildSettings.Key}.json");
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif
