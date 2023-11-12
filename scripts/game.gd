extends Node2D

@export var enemy_scenes: Array[PackedScene] = []

@onready var player_spawn_pos = $PlayerSpawnPos
@onready var player = $Player
@onready var laser_container = $LaserContainer
@onready var enemy_container = $EnemyContainer
@onready var hud = $UILayer/HUD

var mod_manager : Node

var score := 0:
	set(value):
		score = value
		hud.score = value

# Called when the node enters the scene tree for the first time.
func _ready():
	player.global_position = player_spawn_pos.global_position
	player.laser_shot.connect(_on_player_laser_shot)
	
	print("Loading mods...")
	var mod_manager_script = load("res://scripts/mod_manager.cs")
	mod_manager = mod_manager_script.new()
	self.add_child(mod_manager)
	
#	player = get_tree().get_first_node_in_group("player")
#	assert(player != null)

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if Input.is_action_just_pressed("quit"):
		get_tree().quit()
	elif Input.is_action_just_pressed("reset"):
		get_tree().reload_current_scene()

func _on_player_laser_shot(laser_scene, location):
	var laser = laser_scene.instantiate()
	laser.global_position = location
	laser_container.add_child(laser)

func _on_enemy_spawn_timer_timeout():
	var enemy = enemy_scenes.pick_random().instantiate()
	enemy.global_position = Vector2(randf_range(50, 500), -50)
	enemy.killed.connect(_on_enemy_killed)
	enemy_container.add_child(enemy)

func _on_enemy_killed(bounty):
	score += bounty
