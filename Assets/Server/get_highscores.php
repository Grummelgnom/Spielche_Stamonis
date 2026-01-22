<?php
require_once 'db_config.php';

$stmt = $pdo->query("SELECT id, player_name, score, created_at FROM highscores ORDER BY score DESC LIMIT 10");
$rows = $stmt->fetchAll(PDO::FETCH_ASSOC);

echo json_encode(['ok' => true, 'highscores' => $rows]);
?>
