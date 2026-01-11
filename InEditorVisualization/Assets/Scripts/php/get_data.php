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
        $sqlPos = "SELECT pos_x, pos_y, pos_z, current_state, area_name, timestamp FROM player_positions ORDER BY timestamp DESC LIMIT 10000";
        $stmt = $pdo->query($sqlPos);
        $positions = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $sqlEvents = "
            SELECT 'COMBAT' as cat, c.event_type as type, c.pos_x, c.pos_y, c.pos_z, e.enemy_name as aux_data 
            FROM combat_events c
            LEFT JOIN enemies e ON c.enemy_id = e.enemy_id
            UNION ALL
            SELECT 'ITEM' as cat, i.action_type as type, i.pos_x, i.pos_y, i.pos_z, it.item_name as aux_data 
            FROM item_events i
            LEFT JOIN items it ON i.item_id = it.item_id
            ORDER BY type LIMIT 5000
        ";
        $stmt = $pdo->query($sqlEvents);
        $events = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $data = [ 
            ["positions" => $positions, "events" => $events] 
        ];
    }

    elseif ($type == 'data' && $sessionId > 0) {
        $sqlPos = "SELECT pos_x, pos_y, pos_z, current_state, area_name, timestamp FROM player_positions WHERE session_id = ? ORDER BY timestamp ASC";
        $stmt = $pdo->prepare($sqlPos);
        $stmt->execute([$sessionId]);
        $positions = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $sqlEvents = "
            SELECT 'COMBAT' as cat, c.event_type as type, c.pos_x, c.pos_y, c.pos_z, e.enemy_name as aux_data 
            FROM combat_events c
            LEFT JOIN enemies e ON c.enemy_id = e.enemy_id
            WHERE c.session_id = ?
            UNION ALL
            SELECT 'ITEM' as cat, i.action_type as type, i.pos_x, i.pos_y, i.pos_z, it.item_name as aux_data 
            FROM item_events i
            LEFT JOIN items it ON i.item_id = it.item_id
            WHERE i.session_id = ?
        ";
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