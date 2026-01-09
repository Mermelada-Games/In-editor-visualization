using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class AnalyticsVisualizer : MonoBehaviour
{
    private string baseUrl = "https://citmalumnes.upc.es/~sergiofc6/get_data.php";

    [Header("Filtros de Visualización")]
    public bool showPath = true;
    public bool showPlayerDeaths = true;
    public bool showEnemyDeaths = true;  
    public bool showItems = true;
    
    [Header("Timeline (Secuencia)")]
    [Range(0.0f, 1.0f)] public float timeSlider = 1.0f;

    [HideInInspector] public List<SessionInfo> availableSessions = new List<SessionInfo>();
    [HideInInspector] public int selectedIndex = 0;

    private List<PositionData> loadedPositions = new List<PositionData>();
    private List<EventData> loadedEvents = new List<EventData>();

    public IEnumerator GetSessionList()
    {
         using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "?type=list"))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                Wrapper<SessionInfo> wrapper = JsonUtility.FromJson<Wrapper<SessionInfo>>(www.downloadHandler.text);
                availableSessions = wrapper.items;
            }
        }
    }

    public void LoadSession(int sessionId)
    {
        string url = (sessionId == -1) ? baseUrl + "?type=global" : baseUrl + "?type=data&session_id=" + sessionId;
        StartCoroutine(DownloadData(url));
    }

    IEnumerator DownloadData(string url)
    {
        Debug.Log("Cargando: " + url);
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                try 
                {
                    Wrapper<SessionFullData> wrapper = JsonUtility.FromJson<Wrapper<SessionFullData>>(www.downloadHandler.text);
                    if (wrapper != null && wrapper.items != null && wrapper.items.Count > 0)
                    {
                        loadedPositions = wrapper.items[0].positions;
                        loadedEvents = wrapper.items[0].events;
                        Debug.Log($"¡ÉXITO! Recibidos {loadedPositions.Count} puntos y {loadedEvents.Count} eventos.");
                    }
                }
                catch (System.Exception e) { Debug.LogError("Error JSON: " + e.Message); }
            }
            else { Debug.LogError("ERROR: " + www.error); }
        }
    }

    void OnDrawGizmos()
    {
        if (loadedPositions == null || loadedPositions.Count == 0) return;

        int limitPos = (int)(loadedPositions.Count * timeSlider);
        int limitEvents = (int)(loadedEvents.Count * timeSlider);

        if (showPath)
        {
            for (int i = 0; i < limitPos - 1; i++)
            {
                Vector3 p1 = new Vector3(loadedPositions[i].pos_x, loadedPositions[i].pos_y, loadedPositions[i].pos_z);
                Vector3 p2 = new Vector3(loadedPositions[i+1].pos_x, loadedPositions[i+1].pos_y, loadedPositions[i+1].pos_z);
                Gizmos.color = new Color(0, 1, 1, 0.5f);
                Gizmos.DrawLine(p1, p2);
            }
        }

        for (int i = 0; i < limitEvents; i++)
        {
            var evt = loadedEvents[i];
            Vector3 pos = new Vector3(evt.pos_x, evt.pos_y, evt.pos_z);

            if (showPlayerDeaths && evt.type == "PLAYER_DIED")
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(pos, 0.4f);
            }
            else if (showEnemyDeaths && evt.type == "ENEMY_KILLED")
            {
                Gizmos.color = new Color(1f, 0.5f, 0f); 
                Gizmos.DrawCube(pos, Vector3.one * 0.4f);
            }
            else if (showItems && evt.cat == "ITEM")
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }

        if (limitPos > 0 && limitPos < loadedPositions.Count)
        {
            Vector3 currentHead = new Vector3(loadedPositions[limitPos-1].pos_x, loadedPositions[limitPos-1].pos_y, loadedPositions[limitPos-1].pos_z);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(currentHead, 1.0f);
        }
    }
}