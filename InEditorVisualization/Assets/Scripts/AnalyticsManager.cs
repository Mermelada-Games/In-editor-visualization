using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;
using Gamekit3D;

public class AnalyticsManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController; 
    
    public static AnalyticsManager Instance;
    private string baseUrl = "https://citmalumnes.upc.es/~sergiofc6/";

    private int currentSessionId = -1;
    private float positionTimer = 0f;
    
    [Header("Sampling Settings")]
    [Tooltip("Enviar posición cada X segundos")]
    public float positionInterval = 1.0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(StartSession("Player1", "Level01"));
    }

    void Update()
    {
        if (currentSessionId != -1)
        {
            positionTimer += Time.deltaTime;
            if (positionTimer >= positionInterval)
            {
                StartCoroutine(SendPosition(playerController.transform.position));
                positionTimer = 0f;
            }
        }
    }

    // 1. INICIAR SESIÓN
    IEnumerator StartSession(string username, string level)
    {
        Debug.Log("Intentando conectar con: " + baseUrl + "start_session.php"); 

        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("level_name", level);

        using (UnityWebRequest www = UnityWebRequest.Post(baseUrl + "start_session.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Analytics Conexión: " + www.error + " | " + www.downloadHandler.text);
            }
            else
            {
                string responseText = www.downloadHandler.text;
                responseText = responseText.Trim(); 

                if (int.TryParse(responseText, out int id))
                {
                    currentSessionId = id;
                    Debug.Log("¡ÉXITO! Sesión iniciada con ID: " + currentSessionId);
                }
                else
                {
                    Debug.LogError("Error al leer ID. El servidor respondió: '" + responseText + "'");
                }
            }
        }
    }

    // 2. ENVIAR POSICIÓN (Heatmaps)
    IEnumerator SendPosition(Vector3 pos)
    {
        WWWForm form = new WWWForm();
        form.AddField("session_id", currentSessionId);
        
        form.AddField("x", pos.x.ToString(CultureInfo.InvariantCulture));
        form.AddField("y", pos.y.ToString(CultureInfo.InvariantCulture));
        form.AddField("z", pos.z.ToString(CultureInfo.InvariantCulture));

        int currentHealth = 0;
        string currentState = "UNKNOWN";
        string currentArea = "Unknown";

        if (playerController != null)
        {
            var damageable = playerController.GetComponent<Damageable>();
            if (damageable != null)
            {
                currentHealth = damageable.currentHitPoints;
            }
            currentState = playerController.GetPlayerStateString();
            currentArea = playerController.GetCurrentAreaName();
        }

        form.AddField("current_health", currentHealth);
        form.AddField("current_state", currentState);
        form.AddField("area_name", currentArea);

        using (UnityWebRequest www = UnityWebRequest.Post(baseUrl + "track_position.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Envío Pos: " + www.error);
            }
        }
    }

    // 3. ENVIAR EVENTO (Muerte, Item, etc.)
    public void SendEvent(string eventType, string data1, string data2, Vector3 pos)
    {
        if (currentSessionId == -1) return;
        StartCoroutine(PostEvent(eventType, data1, data2, pos));
    }

    IEnumerator PostEvent(string eventType, string data1, string data2, Vector3 pos)
    {
        WWWForm form = new WWWForm();
        form.AddField("session_id", currentSessionId);
        form.AddField("event_type", eventType);
        form.AddField("data_1", data1);
        form.AddField("data_2", data2);
        
        form.AddField("x", pos.x.ToString(CultureInfo.InvariantCulture));
        form.AddField("y", pos.y.ToString(CultureInfo.InvariantCulture));
        form.AddField("z", pos.z.ToString(CultureInfo.InvariantCulture));

        using (UnityWebRequest www = UnityWebRequest.Post(baseUrl + "track_event.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) 
            {
                Debug.Log("Evento Enviado OK: " + data2);
            }
            else 
            {
                Debug.LogError("Error enviando evento: " + www.error + " | " + www.downloadHandler.text);
            }
        }
    }
    public void TrackItemPickup(string itemName)
    {
        Vector3 pos = (playerController != null) ? playerController.transform.position : transform.position;
        SendEvent("ITEM", itemName, "PICKUP", pos);
    }
    public void TrackInteraction(string objectName)
    {
        Vector3 pos = (playerController != null) ? playerController.transform.position : transform.position;
        SendEvent("ITEM", objectName, "INTERACT", pos);
    }
    public void TrackHealthPickup(string sourceName)
    {
        Vector3 pos = (playerController != null) ? playerController.transform.position : transform.position;
        SendEvent("ITEM", sourceName, "HEAL", pos);
    }

    private void OnApplicationQuit()
    {
        if (currentSessionId != -1)
        {
            StartCoroutine(EndSession());
        }
    }

    IEnumerator EndSession()
    {
        WWWForm form = new WWWForm();
        form.AddField("session_id", currentSessionId);

        using (UnityWebRequest www = UnityWebRequest.Post(baseUrl + "end_session.php", form))
        {
            yield return www.SendWebRequest();
        }
    }
}
