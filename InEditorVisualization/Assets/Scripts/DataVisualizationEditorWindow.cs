using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
public class DataVisualizationEditorWindow : EditorWindow
{
    private DataVisualizationDebugger manager;
    private Vector2 scrollPos;
    private int selectedTab = 0;
    private string[] tabs = new string[] { "Markers", "Heatmap", "Tools" };

    [MenuItem("Tools/Data Visualization")]
    public static void ShowWindow()
    {
        GetWindow<DataVisualizationEditorWindow>("Data Visualization");
    }

    private void OnEnable()
    {
        manager = FindFirstObjectByType<DataVisualizationDebugger>();
    }

    private void OnGUI()
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<DataVisualizationDebugger>();
        }

        if (manager == null)
        {
            EditorGUILayout.HelpBox("No 'DataVisualizationDebugger' found in scene.", MessageType.Warning);
            if (GUILayout.Button("Create"))
            {
                GameObject go = new GameObject("DataVisualizationDebugger");
                manager = go.AddComponent<DataVisualizationDebugger>();
                manager.InitializeSettings();
                Undo.RegisterCreatedObjectUndo(go, "Create Data Visualization");
            }
            return;
        }

        Undo.RecordObject(manager, "Change Visualization Settings");

        EditorGUILayout.Space(10);
        selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(30));
        EditorGUILayout.Space(10);

        if (selectedTab == 0) DrawMarkersTab();
        else if (selectedTab == 1) DrawHeatmapTab();
        else if (selectedTab == 2) DrawToolsTab();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(manager);
            SceneView.RepaintAll();
        }
    }

    private void DrawMarkersTab()
    {
        EditorGUILayout.LabelField("Event Markers Settings", EditorStyles.boldLabel);

        manager.showMarkers = EditorGUILayout.ToggleLeft("Show Markers", manager.showMarkers, EditorStyles.boldLabel);

        if (!manager.showMarkers) return;

        EditorGUILayout.BeginVertical("box");
        manager.globalSize = EditorGUILayout.Slider("Global Size", manager.globalSize, 0.1f, 5f);
        
        EditorGUILayout.BeginHorizontal();
        manager.showLabels = EditorGUILayout.Toggle("Show Labels", manager.showLabels);
        manager.labelColor = EditorGUILayout.ColorField(GUIContent.none, manager.labelColor, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
        
        if (manager.showLabels)
        {
            manager.labelFontSize = EditorGUILayout.IntSlider("Label Font Size", manager.labelFontSize, 8, 32);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Vis", GUILayout.Width(25));
        EditorGUILayout.LabelField("Category", GUILayout.Width(110));
        EditorGUILayout.LabelField("Color", GUILayout.Width(50));
        EditorGUILayout.LabelField("Path", GUILayout.Width(35));
        EditorGUILayout.LabelField("Icon", GUILayout.Width(70));
        EditorGUILayout.LabelField("Override Size", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if (manager.categorySettings != null)
        {
            foreach (var setting in manager.categorySettings)
            {
                EditorGUILayout.BeginHorizontal("box");
                setting.isVisible = EditorGUILayout.Toggle(setting.isVisible, GUILayout.Width(25));

                using (new EditorGUI.DisabledScope(!setting.isVisible))
                {
                    EditorGUILayout.LabelField(setting.category.ToString(), EditorStyles.boldLabel, GUILayout.Width(110));
                    setting.color = EditorGUILayout.ColorField(setting.color, GUILayout.Width(50));

                    setting.showPath = EditorGUILayout.Toggle(setting.showPath, GUILayout.Width(35));

                    setting.shape = (MarkerShape)EditorGUILayout.EnumPopup(setting.shape, GUILayout.Width(70));

                    EditorGUILayout.BeginHorizontal(GUILayout.Width(60));
                    setting.useSizeOverride = EditorGUILayout.Toggle(setting.useSizeOverride, GUILayout.Width(15));
                    if (setting.useSizeOverride) setting.size = EditorGUILayout.FloatField(setting.size, GUILayout.Width(40));
                    else 
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.FloatField(manager.globalSize, GUILayout.Width(40));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeatmapTab()
    {
        EditorGUILayout.LabelField("Heatmap Visualization", EditorStyles.boldLabel);

        bool prevShow = manager.showHeatmap;
        manager.showHeatmap = EditorGUILayout.ToggleLeft("Enable Heatmap", manager.showHeatmap, EditorStyles.boldLabel);

        if (prevShow != manager.showHeatmap) manager.MarkHeatmapDirty();

        if (!manager.showHeatmap) return;

        EditorGUILayout.BeginVertical("box");
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        manager.hmWidth = EditorGUILayout.IntSlider("Width (Cells)", manager.hmWidth, 10, 200);
        manager.hmDepth = EditorGUILayout.IntSlider("Depth (Cells)", manager.hmDepth, 10, 200);
        manager.hmCellSize = EditorGUILayout.Slider("Cell Size", manager.hmCellSize, 0.5f, 10f);
        
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Brush Algorithm", EditorStyles.boldLabel);
        manager.hmIntensity = EditorGUILayout.IntSlider("Intensity per Event", manager.hmIntensity, 1, 50);
        manager.hmFullRange = EditorGUILayout.IntSlider("Full Value Radius", manager.hmFullRange, 0, 5);
        manager.hmTotalRange = EditorGUILayout.IntSlider("Total Fade Radius", manager.hmTotalRange, 1, 10);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Visuals", EditorStyles.boldLabel);
        manager.hmGradient = EditorGUILayout.GradientField("Color Gradient", manager.hmGradient);
        manager.hmTransparency = EditorGUILayout.Slider("Transparency", manager.hmTransparency, 0f, 1f);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Data Filtering", EditorStyles.boldLabel);
        manager.hmFilter = (EventCategory)EditorGUILayout.EnumFlagsField("Event Categories", manager.hmFilter);
        
        if (EditorGUI.EndChangeCheck())
        {
            manager.MarkHeatmapDirty();
        }

        EditorGUILayout.HelpBox($"Grid covers area: {manager.hmWidth * manager.hmCellSize}m x {manager.hmDepth * manager.hmCellSize}m", MessageType.Info);

        if (GUILayout.Button("Force Rebuild Heatmap"))
        {
            manager.MarkHeatmapDirty();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawToolsTab()
    {
        EditorGUILayout.LabelField("Database Connection", EditorStyles.boldLabel);
        manager.baseUrl = EditorGUILayout.TextField("PHP URL", manager.baseUrl);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Refresh Session List - Fetch from Server"))
        {
            manager.LoadSessionList();
        }

        List<string> options = new List<string>
        {
            "Global Data - All Sessions"
        };
        
        int selectedIndex = 0;

        if (manager.availableSessions != null)
        {
            for (int i = 0; i < manager.availableSessions.Count; i++)
            {
                var s = manager.availableSessions[i];
                options.Add($"#{s.session_id} | {s.username} | {s.level_name}");

                if (manager.targetSessionId == s.session_id)
                {
                    selectedIndex = i + 1;
                }
            }
        }

        int newIndex = EditorGUILayout.Popup("Target Session", selectedIndex, options.ToArray());

        if (newIndex != selectedIndex)
        {
            if (newIndex == 0)
            {
                manager.targetSessionId = -1;
            }
            else
            {
                manager.targetSessionId = manager.availableSessions[newIndex - 1].session_id;
            }
        }

        string btnText = (manager.targetSessionId == -1) ? "Download GLOBAL Data" : $"Download Session #{manager.targetSessionId}";
        if (GUILayout.Button(btnText, GUILayout.Height(30)))
        {
            manager.LoadDataFromDB();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Time Filter", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        bool prevEnableTimeFilter = manager.enableTimeFilter;
        manager.enableTimeFilter = EditorGUILayout.ToggleLeft("Enable Time Range Filter", manager.enableTimeFilter);
        
        if (prevEnableTimeFilter != manager.enableTimeFilter)
        {
            manager.ApplyTimeFilter();
        }

        if (manager.enableTimeFilter && manager.allLoadedEvents.Count > 0)
        {
            EditorGUILayout.Space(5);

            TimeSpan minTime = TimeSpan.FromSeconds(manager.minTimestamp);
            TimeSpan maxTime = TimeSpan.FromSeconds(manager.maxTimestamp);
            TimeSpan currentMinTime = TimeSpan.FromSeconds(manager.currentMinTime);
            TimeSpan currentMaxTime = TimeSpan.FromSeconds(manager.currentMaxTime);
            
            EditorGUILayout.LabelField($"Data Range: {minTime:hh\\:mm\\:ss} to {maxTime:hh\\:mm\\:ss}", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Selected Time Range:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"From: {currentMinTime:hh\\:mm\\:ss}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"To:   {currentMaxTime:hh\\:mm\\:ss}", EditorStyles.miniLabel);
            
            EditorGUILayout.Space(5);
            
            float newMinTime = manager.currentMinTime;
            float newMaxTime = manager.currentMaxTime;
            
            EditorGUILayout.MinMaxSlider(
                "Time Range", 
                ref newMinTime, 
                ref newMaxTime, 
                manager.minTimestamp, 
                manager.maxTimestamp
            );

            EditorGUILayout.Space(3);
            newMinTime = EditorGUILayout.Slider("Start Time", newMinTime, manager.minTimestamp, manager.maxTimestamp);
            newMaxTime = EditorGUILayout.Slider("End Time", newMaxTime, manager.minTimestamp, manager.maxTimestamp);
            
            if (newMinTime != manager.currentMinTime || newMaxTime != manager.currentMaxTime)
            {
                manager.currentMinTime = newMinTime;
                manager.currentMaxTime = newMaxTime;
                manager.ApplyTimeFilter();
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Reset to Full Range"))
            {
                manager.currentMinTime = manager.minTimestamp;
                manager.currentMaxTime = manager.maxTimestamp;
                manager.ApplyTimeFilter();
            }
            
            EditorGUILayout.EndVertical();
            
            float duration = manager.currentMaxTime - manager.currentMinTime;
            TimeSpan span = TimeSpan.FromSeconds(duration);
            EditorGUILayout.HelpBox($"Selected Duration: {(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}", MessageType.Info);
        }
        else if (manager.enableTimeFilter)
        {
            EditorGUILayout.HelpBox("Load data first to enable time filtering", MessageType.Warning);
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);
        
        int totalEvents = manager.allLoadedEvents.Count;
        int filteredEvents = manager.recordedEvents.Count;
        string eventInfo = manager.enableTimeFilter && totalEvents > 0 
            ? $"Showing {filteredEvents} of {totalEvents} events ({(float)filteredEvents/totalEvents*100:F1}%)"
            : $"Total Events Loaded: {totalEvents}";
            
        EditorGUILayout.HelpBox(eventInfo, MessageType.Info);
    }
}
#endif
