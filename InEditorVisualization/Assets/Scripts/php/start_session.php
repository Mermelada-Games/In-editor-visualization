<?php
require 'db_config.php';

// Recibir datos POST desde Unity
$player_name = $_POST['username'] ?? 'Guest';
$level_name = $_POST['level_name'] ?? 'UnknownLevel';

try {
    $stmt = $pdo->prepare("SELECT player_id FROM players WHERE username = ?");
    $stmt->execute([$player_name]);
    $player = $stmt->fetch(PDO::FETCH_ASSOC);

    if ($player) {
        $player_id = $player['player_id'];
    } else {
        $insertPlayer = $pdo->prepare("INSERT INTO players (username) VALUES (?)");
        $insertPlayer->execute([$player_name]);
        $player_id = $pdo->lastInsertId();
    }

    $insertSession = $pdo->prepare("INSERT INTO game_sessions (player_id, level_name) VALUES (?, ?)");
    $insertSession->execute([$player_id, $level_name]);
    $session_id = $pdo->lastInsertId();

    echo json_encode(["status" => "success", "session_id" => $session_id]);

} catch (Exception $e) {
    echo json_encode(["status" => "error", "message" => $e->getMessage()]);
}
?>