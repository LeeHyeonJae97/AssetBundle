#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundleBuildReportWindow : EditorWindow
{
    List<AssetBundleBuildReport> _reports;
    AssetBundleBuildReport _selected;

    public static void Init(Type type)
    {
        GetWindow<AssetBundleBuildReportWindow>("AssetBundle Build Report", type).Show();
    }

    [MenuItem("Window/AssetBundle/Report")]
    static void Init()
    {
        GetWindow<AssetBundleBuildReportWindow>("AssetBundle Build Report").Show();
    }

    void OnEnable()
    {
        if (!Directory.Exists(AssetBundleBuildReport.Root)) return;

        _reports = new List<AssetBundleBuildReport>();

        foreach (var file in Directory.GetFiles(AssetBundleBuildReport.Root))
        {
            _reports.Add(JsonUtility.FromJson<AssetBundleBuildReport>(File.ReadAllText(file)));
        }

        _reports.Sort((l, r) => DateTime.Parse(r.Date).CompareTo(DateTime.Parse(l.Date)));

        _selected = _reports[0];
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(150));
        DrawList();
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        DrawInspector();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    void DrawList()
    {
        for (int i = 0; i < _reports.Count; i++)
        {
            if (GUILayout.Button(_reports[i].Date))
            {
                _selected = _reports[i];
            }
        }
    }

    void DrawInspector()
    {
        if (_selected != null)
        {
            EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Key", GUILayout.Width(200));
            EditorGUILayout.LabelField(_selected.Key, EditorStyles.textField);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Date", GUILayout.Width(200));
            EditorGUILayout.LabelField(_selected.Date, EditorStyles.textField);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Elapsed", GUILayout.Width(200));
            EditorGUILayout.LabelField(_selected.Elapsed, EditorStyles.textField);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Size", GUILayout.Width(200));
            EditorGUILayout.LabelField($"{_selected.TotalSize}KB", EditorStyles.textField);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Count", GUILayout.Width(200));
            EditorGUILayout.LabelField($"{_selected.TotalCount}", EditorStyles.textField);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Newly Built ({_selected.NewlyBuilt.Count})", GUILayout.Width(200));
            EditorGUI.indentLevel++;
            for (int i = 0; i < _selected.NewlyBuilt.Count; i++)
            {
                EditorGUILayout.LabelField(_selected.NewlyBuilt[i], EditorStyles.textField);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField($"Updated ({_selected.Updated.Count})", GUILayout.Width(200));
            EditorGUI.indentLevel++;
            for (int i = 0; i < _selected.Updated.Count; i++)
            {
                EditorGUILayout.LabelField(_selected.Updated[i], EditorStyles.textField);
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif