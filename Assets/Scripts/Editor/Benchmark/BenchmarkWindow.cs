using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BoatAttack.Benchmark;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public class BenchmarkWindow : EditorWindow
{
    [MenuItem("Tools/Benchmark")]
    static void Init()
    {
        var window = (BenchmarkWindow)GetWindow(typeof(BenchmarkWindow));
        window.Show();
    }

    class Styles
    {
        public static readonly GUIContent[] toolbarOptions = {new GUIContent("Tools"), new GUIContent("Results"), };
    }

    public int currentToolbar;
    public const int ToolbarWidth = 150;
    private List<PerfResults> PerfResults = new List<PerfResults>();
    private int currentResult;

    // TempUI vars
    private bool resultInfoHeader;
    private bool resultDataHeader;

    private void OnGUI()
    {
        EditorGUILayout.Space(5);

        var toolbarRect = EditorGUILayout.GetControlRect();
        toolbarRect.position += new Vector2((toolbarRect.width - ToolbarWidth) * 0.5f, 0f);
        toolbarRect.width = ToolbarWidth;

        currentToolbar = GUI.Toolbar(toolbarRect, currentToolbar,
            Styles.toolbarOptions);

        switch (currentToolbar)
        {
            case 0:
                DrawTools();
                break;
            case 1:
                DrawResults();
                break;
        }
    }

    private void DrawTools()
    {
        GUILayout.Label("Tools Page");
    }

    private void DrawResults()
    {
        if (PerfResults == null || PerfResults.Count == 0)
        {
            PerfResults = Benchmark.LoadAllBenchmarkStats();
        }

        string[] files = new string[PerfResults.Count];
        for (var index = 0; index < PerfResults.Count; index++)
        {
            files[index] = PerfResults[index].fileName;
        }

        currentResult = EditorGUILayout.Popup(new GUIContent("File"), currentResult, files);

        EditorGUILayout.Space(4);

        DrawPerfInfo(PerfResults[currentResult].perfStats[0].info);

        DrawPerf(PerfResults[currentResult].perfStats[0]);
    }

    private void DrawPerfInfo(TestInfo info)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        resultInfoHeader = EditorGUILayout.BeginFoldoutHeaderGroup(resultInfoHeader, "Info");
        if (resultInfoHeader)
        {
            var fields = info.GetType().GetFields();
            var half = fields.Length / 2;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            for (var index = 0; index < fields.Length; index++)
            {
                var prop = fields[index];
                EditorGUILayout.LabelField(prop.Name, prop.GetValue(info).ToString(), EditorStyles.boldLabel);
                if (index == half)
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndVertical();
    }

    private void DrawPerf(PerfBasic data)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        resultDataHeader = EditorGUILayout.BeginFoldoutHeaderGroup(resultDataHeader, "Data");
        if (resultDataHeader)
        {
            EditorGUILayout.BeginHorizontal();
            {
                var lw = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 50f;
                EditorGUILayout.LabelField("Average:", $"{data.AvgMs:F2}ms", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Min:", $"{data.MinMs:F2}ms at frame {data.MinMSFrame}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Max:", $"{data.MaxMs:F2}ms at frame {data.MaxMSFrame}", EditorStyles.boldLabel);
                EditorGUIUtility.labelWidth = lw;
            }
            EditorGUILayout.EndHorizontal();

            var graphRect = EditorGUILayout.GetControlRect(false, 500f);
            DrawGraph(graphRect, data.RawSamples);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndVertical();
    }

    private void DrawGraph(Rect rect, float[] values)
    {
        var padding = 20f;
        rect.max -= Vector2.one * padding;
        rect.min += Vector2.one * padding;

        GUI.DrawTexture(rect, Texture2D.grayTexture, ScaleMode.StretchToFill);
        for (var index = 0; index < values.Length; index++)
        {
            var val = values[index];
            var pos = Mathf.InverseLerp(rect.min.x, rect.max.x, (float)index / values.Length);
            GUI.Box(new Rect(new Vector2(pos, rect.max.y), Vector2.one * 5f), Texture2D.whiteTexture);
        }
    }
}
