<?php
$host = 'citmalumnes.upc.es';
$dbname = 'sergiofc6o';
$username = 'sergiofc6';
$password = '4HMCaBXhhna3';

try {
    $pdo = new PDO("mysql:host=$host;dbname=$dbname;charset=utf8", $username, $password);

    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
} catch (PDOException $e) {
    die(json_encode(["error" => "Connection failed: " . $e->getMessage()]));
}
?>