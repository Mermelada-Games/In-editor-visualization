<?php
require 'db_config.php';

$sessionId = $_POST['session_id'];

try {
    $sql = "UPDATE game_sessions SET end_time = CURRENT_TIMESTAMP WHERE session_id = ?";
    $stmt = $pdo->prepare($sql);
    $stmt->execute([$sessionId]);

} catch (PDOException $e) {
    http_response_code(500);}
?>