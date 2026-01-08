<?php
require 'db_config.php';

$sessionId = $_POST['session_id'];
$eventType = $_POST['event_type'];
$posX = $_POST['x'];
$posY = $_POST['y'];
$posZ = $_POST['z'];
$data1 = $_POST['data_1'];
$data2 = $_POST['data_2']; 

if($eventType == "COMBAT") {
    $stmt = $pdo->prepare("SELECT enemy_id FROM enemies WHERE enemy_name = ?");
    $stmt->execute([$data1]);
    $enemyId = $stmt->fetchColumn();
    if(!$enemyId){
        $stmt = $pdo->prepare("INSERT INTO enemies (enemy_name) VALUES (?)");
        $stmt->execute([$data1]);
        $enemyId = $pdo->lastInsertId();
    }
    
    $sql = "INSERT INTO combat_events (session_id, event_type, enemy_id, pos_x, pos_y, pos_z) VALUES (?, ?, ?, ?, ?, ?)";
    $pdo->prepare($sql)->execute([$sessionId, $data2, $enemyId, $posX, $posY, $posZ]);

} elseif ($eventType == "ITEM") {

    $stmt = $pdo->prepare("SELECT item_id FROM items WHERE item_name = ?");
    $stmt->execute([$data1]);
    $itemId = $stmt->fetchColumn();
    if(!$itemId){
        $stmt = $pdo->prepare("INSERT INTO items (item_name, item_type) VALUES (?, 'Unknown')");
        $stmt->execute([$data1]);
        $itemId = $pdo->lastInsertId();
    }

    $sql = "INSERT INTO item_events (session_id, item_id, action_type, pos_x, pos_y, pos_z) VALUES (?, ?, ?, ?, ?, ?)";
    $pdo->prepare($sql)->execute([$sessionId, $itemId, $data2, $posX, $posY, $posZ]);
}
?>