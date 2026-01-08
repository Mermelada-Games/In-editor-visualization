<?php
require 'db_config.php';

$sessionId = $_POST['session_id'];
$x = $_POST['x'];
$y = $_POST['y'];
$z = $_POST['z'];
$currentHealth = isset($_POST['current_health']) ? $_POST['current_health'] : null;
$currentState = isset($_POST['current_state']) ? $_POST['current_state'] : null;

$sql = "INSERT INTO player_positions (session_id, pos_x, pos_y, pos_z, current_health, current_state) VALUES (?, ?, ?, ?, ?, ?)";
$pdo->prepare($sql)->execute([$sessionId, $x, $y, $z, $currentHealth, $currentState]);
?>