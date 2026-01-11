using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class DataVisualizationDebugger : MonoBehaviour
{
    [Header("Database Config")]
    public string baseUrl = "https://citmalumnes.upc.es/~sergiofc6/get_data.php";

    public int targetSessionId = -1;

    [HideInInspector] public List<SessionInfo> availableSessions = new List<SessionInfo>();
    [HideInInspector] public List<WorldEvent> recordedEvents = new List<WorldEvent>();
    [HideInInspector] public List<WorldEvent> allLoadedEvents = new List<WorldEvent>();

    [HideInInspector] public bool showMarkers = true;
    [HideInInspector] [Range(0.1f, 5f)] public float globalSize = 1.0f;
    [HideInInspector] public bool showLabels = true;
    [HideInInspector] public Color labelColor = Color.white;
    [HideInInspector] [Range(8, 32)] public int labelFontSize = 12;
    [HideInInspector] public List<CategorySettings> categorySettings = new List<CategorySettings>();

    [HideInInspector] public bool showHeatmap = false;
    [HideInInspector] public int hmWidth = 50;
    [HideInInspector] public int hmDepth = 50;
    [HideInInspector] public float hmCellSize = 2f;
    [HideInInspector] public Gradient hmGradient;
    [HideInInspector] [Range(0, 1)] public float hmTransparency = 0.5f;
    
    [HideInInspector] public EventCategory hmFilter = EventCategory.PlayerPosition | EventCategory.PlayerDeath; 
    
    [Header("Heatmap Brush")]
    [HideInInspector] public int hmIntensity = 15; 
    [HideInInspector] public int hmFullRange = 1; 
    [HideInInspector] public int hmTotalRange = 3; 

    [HideInInspector] public bool enableTimeFilter = false;
    [HideInInspector] public float minTimestamp = 0f;
    [HideInInspector] public float maxTimestamp = 100f;
    [HideInInspector] public float currentMinTime = 0f;
    [HideInInspector] public float currentMaxTime = 100f;

    private Grid runtimeGrid;
    private bool needsRebuildHeatmap = false;

    private void OnEnable()
    {
        if (categorySettings == null || categorySettings.Count == 0) InitializeSettings();
        
        if (hmGradient == null)
        {
            hmGradient = new Gradient();
            hmGradient.colorKeys = new GradientColorKey[] { 
                new GradientColorKey(Color.green, 0.0f), 
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(Color.red, 1.0f) 
            };
            hmGradient.alphaKeys = new GradientAlphaKey[] { 
                new GradientAlphaKey(0.0f, 0.0f), 
                new GradientAlphaKey(0.8f, 1.0f) 
            };
        }
    }

    public void InitializeSettings()
    {
        categorySettings = new List<CategorySettings>();
        foreach (EventCategory cat in System.Enum.GetValues(typeof(EventCategory)))
        {
            if (cat == EventCategory.None) continue;
            
            Color defColor = Color.white;
            MarkerShape defShape = MarkerShape.Sphere;
            float defSize = 1.0f;
            bool defPath = false;

            switch (cat)
            {
                case EventCategory.PlayerPosition:
                    defColor = new Color(0, 0.5f, 1f, 0.3f); 
                    defShape = MarkerShape.Sphere;
                    defSize = 0.3f;
                    defPath = true;
                    break;
                case EventCategory.PlayerDeath:
                    defColor = Color.red; 
                    defShape = MarkerShape.Sphere;
                    break;
                case EventCategory.EnemyDefeated:
                    defColor = new Color(1f, 0.5f, 0f); 
                    defShape = MarkerShape.Sphere;
                    break;
                case EventCategory.ItemPickup:
                    defColor = Color.green;
                    defShape = MarkerShape.Sphere;
                    break;
                case EventCategory.ItemInteract:
                    defColor = Color.cyan;
                    defShape = MarkerShape.Sphere;
                    break;
                case EventCategory.ItemHeal:
                    defColor = new Color(1f, 0.2f, 0.6f); 
                    defShape = MarkerShape.Sphere; 
                    defSize = 1.2f;
                    break;
            }
            
            categorySettings.Add(new CategorySettings
            {
                category = cat,
                isVisible = true,
                showPath = defPath,
                color = defColor,
                shape = defShape,
                useSizeOverride = false,
                size = defSize
            });
        }
    }

    public void LoadSessionList()
    {
        string url = baseUrl + "?type=list";
        StartRequest(url, HandleSessionListResponse);
    }

    public void LoadDataFromDB()
    {
        string url = (targetSessionId == -1) ? baseUrl + "?type=global" : baseUrl + "?type=data&session_id=" + targetSessionId;
        StartRequest(url, HandleDataResponse);
    }

    private void StartRequest(string url, Action<string> onSuccess)
    {
        var request = UnityWebRequest.Get(url);
        
#if UNITY_EDITOR
        var op = request.SendWebRequest();
        EditorApplication.CallbackFunction updateLoop = null;
        updateLoop = () =>
        {
            if (request.isDone)
            {
                EditorApplication.update -= updateLoop;
                if (request.result == UnityWebRequest.Result.Success)
                    onSuccess?.Invoke(request.downloadHandler.text);
                else
                    Debug.LogError("Web Request Error: " + request.error);
                
                request.Dispose();
            }
        };
        EditorApplication.update += updateLoop;
#else
        StartCoroutine(WaitForRequest(request, onSuccess));
#endif
    }

    private System.Collections.IEnumerator WaitForRequest(UnityWebRequest req, Action<string> onSuccess)
    {
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke(req.downloadHandler.text);
        else
            Debug.LogError("Web Request Error: " + req.error);
        req.Dispose();
    }

    private void HandleSessionListResponse(string json)
    {
        try
        {
            Wrapper<SessionInfo> wrapper = JsonUtility.FromJson<Wrapper<SessionInfo>>(json);
            if (wrapper != null && wrapper.items != null)
            {
                availableSessions = wrapper.items;
            }
        }
        catch (Exception e) { Debug.LogError("Error parsing Session List: " + e.Message); }
    }

    private void HandleDataResponse(string json)
    {
        try 
        {
            Wrapper<SessionFullData> wrapper = JsonUtility.FromJson<Wrapper<SessionFullData>>(json);
            if (wrapper != null && wrapper.items != null && wrapper.items.Count > 0)
            {
                ProcessLoadedData(wrapper.items[0]);
            }
            else
            {
                Debug.LogWarning("[EventDebugger] JSON received but empty items.");
            }
        }
        catch (Exception e) { Debug.LogError("Error parsing Data JSON: " + e.Message); }
    }

    private void ProcessLoadedData(SessionFullData data)
    {
        allLoadedEvents.Clear();
        float minTime = 0;
        float eventIndex = 0;
        
        if (data.positions != null)
        {
            foreach (var pos in data.positions)
            {
                WorldEvent evt = new WorldEvent();
                evt.id = Guid.NewGuid().ToString();
                evt.position = new Vector3(pos.pos_x, pos.pos_y, pos.pos_z);
                evt.category = EventCategory.PlayerPosition;
                evt.message = $"State: {pos.current_state}\nArea: {pos.area_name}";
                evt.timestamp = eventIndex;
                
                allLoadedEvents.Add(evt);
                eventIndex++;
            }
        }

        if (data.events != null)
        {
            foreach (var e in data.events)
            {
                WorldEvent evt = new WorldEvent();
                evt.id = Guid.NewGuid().ToString();
                evt.position = new Vector3(e.pos_x, e.pos_y, e.pos_z);
                evt.timestamp = eventIndex;
                
                evt.category = EventCategory.None;
                
                if (e.type == "PLAYER_DIED") evt.category = EventCategory.PlayerDeath;
                else if (e.type == "ENEMY_KILLED") evt.category = EventCategory.EnemyDefeated;
                else if (e.cat == "ITEM") 
                {
                    if (e.type == "PICKUP") evt.category = EventCategory.ItemPickup;
                    else if (e.type == "INTERACT") evt.category = EventCategory.ItemInteract;
                    else if (e.type == "HEAL") evt.category = EventCategory.ItemHeal;
                    else evt.category = EventCategory.ItemPickup;
                }

                if (evt.category != EventCategory.None)
                {
                    string extraInfo = string.IsNullOrEmpty(e.aux_data) ? "" : $" ({e.aux_data})";
                    evt.message = $"{e.type}{extraInfo}";
                    
                    allLoadedEvents.Add(evt);
                    eventIndex++;
                }
            }
        }

        if (allLoadedEvents.Count > 0)
        {
            minTimestamp = minTime;
            maxTimestamp = eventIndex - 1;
            currentMinTime = minTime;
            currentMaxTime = maxTimestamp;
        }
        
        ApplyTimeFilter();
        MarkHeatmapDirty();
        
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }

    public void ApplyTimeFilter()
    {
        if (!enableTimeFilter || allLoadedEvents.Count == 0)
        {
            recordedEvents = new List<WorldEvent>(allLoadedEvents);
        }
        else
        {
            recordedEvents = new List<WorldEvent>();
            foreach (var evt in allLoadedEvents)
            {
                if (evt.timestamp >= currentMinTime && evt.timestamp <= currentMaxTime)
                {
                    recordedEvents.Add(evt);
                }
            }
        }
        
        MarkHeatmapDirty();
    }

    public void RebuildHeatmap()
    {
        if (recordedEvents == null) return;

        runtimeGrid = new Grid(hmWidth, hmDepth, hmCellSize, this.transform.position);

        foreach (var evt in recordedEvents)
        {
            if ((hmFilter & evt.category) != 0)
            {
                int finalIntensity = hmIntensity;
                if (evt.category == EventCategory.PlayerDeath) finalIntensity *= 2; 

                runtimeGrid.AddValue(evt.position, finalIntensity, hmFullRange, hmTotalRange);
            }
        }
    }

    public void MarkHeatmapDirty() => needsRebuildHeatmap = true;

    private void OnDrawGizmos()
    {
        if (showHeatmap)
        {
            if (runtimeGrid == null || needsRebuildHeatmap)
            {
                RebuildHeatmap();
                needsRebuildHeatmap = false;
            }

            if (runtimeGrid != null)
            {
                for (int x = 0; x < runtimeGrid.GetWidth(); x++)
                {
                    for (int z = 0; z < runtimeGrid.GetDepth(); z++)
                    {
                        int value = runtimeGrid.GetValue(x, z);
                        if (value <= 0) continue;

                        float normalizedValue = (float)value / Grid.HEAT_MAP_MAX_VALUE;
                        
                        Color c = hmGradient.Evaluate(normalizedValue);
                        c.a = hmTransparency; 
                        Gizmos.color = c;

                        Vector3 cellCenter = runtimeGrid.GetWorldPosition(x, z) + new Vector3(hmCellSize, 0, hmCellSize) * 0.5f;
                        Gizmos.DrawCube(cellCenter, new Vector3(hmCellSize, 0.1f, hmCellSize));
                    }
                }
                
                Gizmos.color = Color.white;
                Vector3 gridCenter = transform.position; 
                Gizmos.DrawWireCube(gridCenter, new Vector3(hmWidth * hmCellSize, 1f, hmDepth * hmCellSize));
            }
        }

        if (showMarkers && recordedEvents != null)
        {
            foreach (var settings in categorySettings)
            {
                if (settings.isVisible && settings.showPath)
                {
                    Gizmos.color = settings.color;
                    Vector3? prevPos = null;

                    foreach (var evt in recordedEvents)
                    {
                        if (evt.category == settings.category)
                        {
                            if (prevPos.HasValue)
                            {
                                Gizmos.DrawLine(prevPos.Value, evt.position);
                            }
                            prevPos = evt.position;
                        }
                    }
                }
            }

            foreach (var evt in recordedEvents)
            {
                var settings = categorySettings.Find(x => x.category == evt.category);
                if (settings == null || !settings.isVisible) continue;

                Gizmos.color = settings.color;
                float finalSize = settings.useSizeOverride ? settings.size : globalSize;

                switch (settings.shape)
                {
                    case MarkerShape.Sphere: Gizmos.DrawSphere(evt.position, finalSize * 0.5f); break;
                    case MarkerShape.Cube: Gizmos.DrawCube(evt.position, Vector3.one * finalSize); break;
                    case MarkerShape.WireSphere: Gizmos.DrawWireSphere(evt.position, finalSize * 0.5f); break;
                    case MarkerShape.WireCube: Gizmos.DrawWireCube(evt.position, Vector3.one * finalSize); break;
                }

#if UNITY_EDITOR
                if (showLabels)
                {
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = labelColor;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = labelFontSize;
                    string labelText = $"{evt.category}\n{evt.message}";
                    Handles.Label(evt.position + Vector3.up * finalSize, labelText, style);
                }
#endif
            }
        }
    }
}
