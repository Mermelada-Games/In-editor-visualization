using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(AnalyticsVisualizer))]
public class AnalyticsVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AnalyticsVisualizer script = (AnalyticsVisualizer)target;
        GUILayout.Space(15);

        GUILayout.Label("Modo Sesión Única", EditorStyles.boldLabel);
        if (GUILayout.Button("Refrescar Lista")) script.StartCoroutine(script.GetSessionList());

        if (script.availableSessions != null && script.availableSessions.Count > 0)
        {
            List<string> options = new List<string>();
            foreach (var s in script.availableSessions) options.Add(s.GetDisplayName());
            script.selectedIndex = EditorGUILayout.Popup("Elegir Sesión:", script.selectedIndex, options.ToArray());

            if (GUILayout.Button("Cargar Sesión Seleccionada"))
            {
                int id = script.availableSessions[script.selectedIndex].session_id;
                script.LoadSession(id);
            }
        }

        GUILayout.Space(15);
        
        GUILayout.Label("Modo Big Data (Acumulativo)", EditorStyles.boldLabel);
        GUI.backgroundColor = Color.cyan; 
        if (GUILayout.Button("CARGAR TODO (Mapa de Calor Global)"))
        {
            script.LoadSession(-1);
        }
        GUI.backgroundColor = Color.white;
    }
}