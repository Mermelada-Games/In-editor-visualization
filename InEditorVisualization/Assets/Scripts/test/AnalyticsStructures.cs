using System;
using System.Collections.Generic;

[Serializable]
public class SessionInfo
{
    public int session_id;
    public string username;
    public string level_name;
    public string start_time;
    
    public string GetDisplayName()
    {
        return $"#{session_id} | {username} | {level_name} | {start_time}";
    }
}

[Serializable]
public class PositionData
{
    public float pos_x, pos_y, pos_z;
    public string current_state;
    public string area_name;
}

[Serializable]
public class EventData
{
    public string cat;
    public string type; 
    public float pos_x, pos_y, pos_z;
}

[Serializable]
public class SessionFullData
{
    public List<PositionData> positions;
    public List<EventData> events;
}

[Serializable]
public class Wrapper<T> { public List<T> items; }