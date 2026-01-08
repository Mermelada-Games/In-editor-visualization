<?php
require 'db_config.php';

$player_name = $_POST['username'] ?? 'Guest';
$level_name = $_POST['level_name'] ?? 'UnknownLevel';

try {
    $stmt = $pdo->prepare("SELECT player_id FROM players WHERE username = ?");
    $stmt->execute([$player_name]);
    $player_id = $stmt->fetchColumn();

    if (!$player_id) {
        $insertPlayer = $pdo->prepare("INSERT INTO players (username) VALUES (?)");
        $insertPlayer->execute([$player_name]);
        $player_id = $pdo->lastInsertId();
    }

    $insertSession = $pdo->prepare("INSERT INTO game_sessions (player_id, level_name) VALUES (?, ?)");
    $insertSession->execute([$player_id, $level_name]);
    
    echo $pdo->lastInsertId(); 

} catch (Exception $e) {
    http_response_code(500);
    echo "Error: " . $e->getMessage();
}
?>