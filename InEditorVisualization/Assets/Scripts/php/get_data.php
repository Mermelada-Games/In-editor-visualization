<?php
require 'db_config.php';

$type = $_GET['type'] ?? 'list';
$sessionId = $_GET['session_id'] ?? 0;

try {
    $data = [];

    if ($type == 'list') {
        $sql = "SELECT s.session_id, p.username, s.level_name, s.start_time 
                FROM game_sessions s 
                JOIN players p ON s.player_id = p.player_id 
                ORDER BY s.session_id";
        $stmt = $pdo->query($sql);
        $data = $stmt->fetchAll(PDO::FETCH_ASSOC);
    } 
    
    elseif ($type == 'global') {
        $sqlPos = "SELECT pos_x, pos_y, pos_z, current_state FROM player_positions ORDER BY timestamp DESC LIMIT 10000";
        $stmt = $pdo->query($sqlPos);
        $positions = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $sqlEvents = "
            SELECT 'COMBAT' as cat, event_type as type, pos_x, pos_y, pos_z FROM combat_events
            UNION ALL
            SELECT 'ITEM' as cat, action_type as type, pos_x, pos_y, pos_z FROM item_events
            ORDER BY type LIMIT 5000
        ";
        $stmt = $pdo->query($sqlEvents);
        $events = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $data = [ 
            ["positions" => $positions, "events" => $events] 
        ];
    }

    elseif ($type == 'data' && $sessionId > 0) {
        $sqlPos = "SELECT pos_x, pos_y, pos_z, current_state, area_name FROM player_positions WHERE session_id = ? ORDER BY timestamp ASC";
        $stmt = $pdo->prepare($sqlPos);
        $stmt->execute([$sessionId]);
        $positions = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $sqlEvents = "SELECT 'COMBAT' as cat, event_type as type, pos_x, pos_y, pos_z FROM combat_events WHERE session_id = ? UNION ALL SELECT 'ITEM' as cat, action_type as type, pos_x, pos_y, pos_z FROM item_events WHERE session_id = ?";
        $stmt = $pdo->prepare($sqlEvents);
        $stmt->execute([$sessionId, $sessionId]);
        $events = $stmt->fetchAll(PDO::FETCH_ASSOC);
        
        $data = [ 
            ["positions" => $positions, "events" => $events] 
        ];
    }

    echo json_encode(["items" => $data]);

} catch (PDOException $e) {
    echo json_encode(["error" => $e->getMessage()]);
}
?>