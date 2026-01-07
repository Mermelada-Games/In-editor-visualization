<?php
require 'db_config.php';

$session_id = $_POST['session_id'];
$type = $_POST['type']; // Valores: 'COMBAT', 'ITEM', 'CHECKPOINT', 'PERFORMANCE'
$posX = $_POST['x'];
$posY = $_POST['y'];
$posZ = $_POST['z'];

function getOrInsertId($pdo, $table, $colName, $colId, $value) {
    $stmt = $pdo->prepare("SELECT $colId FROM $table WHERE $colName = ?");
    $stmt->execute([$value]);
    $row = $stmt->fetch();
    if ($row) return $row[$colId];
    
    $stmt = $pdo->prepare("INSERT INTO $table ($colName) VALUES (?)");
    $stmt->execute([$value]);
    return $pdo->lastInsertId();
}

try {
    switch ($type) {
        case 'COMBAT':
            $enemyName = $_POST['context_1']; // Unity envía "Goblin"
            $eventType = $_POST['context_2']; // Unity envía "ENEMY_KILLED" o "PLAYER_DIED"

            $enemyId = getOrInsertId($pdo, 'enemies', 'enemy_name', 'enemy_id', $enemyName);
            
            $stmt = $pdo->prepare("INSERT INTO combat_events (session_id, event_type, enemy_id, pos_x, pos_y, pos_z) VALUES (?, ?, ?, ?, ?, ?)");
            $stmt->execute([$session_id, $eventType, $enemyId, $posX, $posY, $posZ]);
            break;

        case 'ITEM':
            $itemName = $_POST['context_1']; // Unity envía "HealthPotion"
            $action = $_POST['context_2'];   // Unity envía "PICKUP"

            $itemId = getOrInsertId($pdo, 'items', 'item_name', 'item_id', $itemName);
            
            $stmt = $pdo->prepare("INSERT INTO item_events (session_id, item_id, action_type, pos_x, pos_y, pos_z) VALUES (?, ?, ?, ?, ?, ?)");
            $stmt->execute([$session_id, $itemId, $action, $posX, $posY, $posZ]);
            break;

        case 'CHECKPOINT':
            $cpName = $_POST['context_1']; // Unity envía "BossRoomEntrance"
            $time = $_POST['context_2'];   // Unity envía el tiempo (float)
            
            $stmt = $pdo->prepare("INSERT INTO checkpoint_events (session_id, checkpoint_name, time_reached) VALUES (?, ?, ?)");
            $stmt->execute([$session_id, $cpName, $time]);
            break;

        case 'PERFORMANCE':
             $fps = $_POST['context_1']; 
             $stmt = $pdo->prepare("INSERT INTO performance_metrics (session_id, pos_x, pos_y, pos_z, fps_value) VALUES (?, ?, ?, ?, ?)");
             $stmt->execute([$session_id, $posX, $posY, $posZ, $fps]);
             break;
    }
    echo json_encode(["status" => "success"]);

} catch (Exception $e) {
    echo json_encode(["status" => "error", "message" => $e->getMessage()]);
}
?>