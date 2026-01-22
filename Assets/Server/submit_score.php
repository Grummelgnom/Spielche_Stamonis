<?php
require_once 'db_config.php';

$raw = file_get_contents('php://input');
$data = json_decode($raw, true);

if (!is_array($data) || !isset($data['player_name']) || !isset($data['score'])) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Missing player_name or score']);
    exit;
}

$name = trim($data['player_name']);
$score = (int)$data['score'];

if ($name === '' || mb_strlen($name) > 50) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Invalid name']);
    exit;
}

if ($score < 0) {
    http_response_code(400);
    echo json_encode(['ok' => false, 'error' => 'Invalid score']);
    exit;
}

$stmt = $pdo->prepare("INSERT INTO highscores (player_name, score) VALUES (?, ?)");
$stmt->execute([$name, $score]);

echo json_encode(['ok' => true, 'id' => $pdo->lastInsertId()]);
?>
