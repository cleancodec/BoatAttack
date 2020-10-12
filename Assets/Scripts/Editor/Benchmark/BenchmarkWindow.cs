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
    private string[] resultFiles;
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
            UpdateFiles();
        }
        
        if (PerfResults != null && PerfResults.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            currentResult = EditorGUILayout.Popup(new GUIContent("File"), currentResult, resultFiles);
            if (GUILayout.Button("reload", GUILayout.Width(100)))
            {
                UpdateFiles();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            DrawPerfInfo(PerfResults[currentResult].perfStats[0].info);

            DrawPerf(PerfResults[currentResult].perfStats[0], 0);
        }
        else
        {
            GUILayout.Label("No Stats found, please run a benchmark.");
        }
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

    private void DrawPerf(PerfBasic data, int run)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        resultDataHeader = EditorGUILayout.BeginFoldoutHeaderGroup(resultDataHeader, "Data");
        if (resultDataHeader)
        {
            EditorGUILayout.Space(4);
            
            EditorGUILayout.BeginHorizontal();
            {
                var lw = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 50f;
                EditorGUILayout.LabelField("Run:", $"{run}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Average:", $"{data.AvgMs:F2}ms", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Min:", $"{data.MinMs:F2}ms at frame {data.MinMSFrame}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Max:", $"{data.MaxMs:F2}ms at frame {data.MaxMSFrame}", EditorStyles.boldLabel);
                EditorGUIUtility.labelWidth = lw;
            }
            EditorGUILayout.EndHorizontal();

            var graphRect = EditorGUILayout.GetControlRect(false, 500f);
            DrawGraph(graphRect, data.RunData, 0, data.AvgMs * 2f);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndVertical();
    }

    private void DrawGraph(Rect rect, FrameTimes[] values, float minMS, float maxMS)
    {
        var padding = 20f;
        rect.max -= Vector2.one * padding;
        rect.xMax -= 40f;
        rect.min += Vector2.one * padding;

        
        //draw value markers
        GUI.DrawTexture(rect, Texture2D.grayTexture, ScaleMode.StretchToFill);
        
        DrawGraphMarkers(rect, minMS, maxMS, 5);

        var H = 1f;
        foreach (var frames in values)
        {
            var graphPoints = new Vector3[frames.rawSamples.Length];
            for (var j = 0; j < frames.rawSamples.Length; j++)
            {
                var valA = rect.yMax - rect.height * GetGraphLerpValue(frames.rawSamples[j], minMS, maxMS);

                var xLerp = new Vector2(j, j + 1) / frames.rawSamples.Length;
                var xA = Mathf.Lerp(rect.xMin, rect.xMax, xLerp.x);
                var posA = new Vector2(xA, valA);
                graphPoints[j] = posA;
            }
            var c = Color.HSVToRGB(H, 0.75f, 1f);
            c.a = 0.75f;
            Handles.color = c;
            Handles.DrawAAPolyLine(graphPoints);

            H -= 0.1f;
        }
    }

    private void DrawGraphMarkers(Rect rect, float min, float max, int count)
    {
        count--;
        for (int i = 0; i <= count; i++)
        {
            var y = Mathf.Lerp(rect.yMax, rect.yMin, (float)i / count);
            Handles.color = new Color(0f, 0f, 0f, 0.25f);
            Handles.DrawDottedLine(new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 4);
            y -= EditorGUIUtility.singleLineHeight * 0.5f;
            var val = Mathf.Lerp(min, max, (float) i / count);
            GUI.Label(new Rect(new Vector2(rect.xMax, y), new Vector2(80, EditorGUIUtility.singleLineHeight)), $"{val:F1}ms");
        }
    }

    private float GetGraphLerpValue(float ms)
    {
        return GetGraphLerpValue(ms, 0f, 33.33f);
    }

    private float GetGraphLerpValue(float ms, float msMin, float msMax)
    {
        var msA = ms;
        return Mathf.InverseLerp(msMin, msMax, msA);
    }

    private void UpdateFiles()
    {
        PerfResults = Benchmark.LoadAllBenchmarkStats();
        resultFiles = new string[PerfResults.Count];
        for (var index = 0; index < PerfResults.Count; index++)
        {
            resultFiles[index] = PerfResults[index].fileName;
        }
    }
}
