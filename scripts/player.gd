class_name Player extends CharacterBody2D

signal laser_shot(laser_scene, location)

@export var speed = 300
@export var shots_per_second := 4.0

@onready var muzzle = $Muzzle
var laser_scene = preload("res://scenes/laser.tscn")

var shoot_cooldown := false

func _process(delta):
	if Input.is_action_pressed("shoot"):
		if not shoot_cooldown:
			shoot_cooldown = true
			shoot()
			await get_tree().create_timer(1.0 / shots_per_second).timeout
			shoot_cooldown = false

func _physics_process(delta):
	var direction = Vector2(
		Input.get_axis("move_left", "move_right"),
		Input.get_axis("move_up", "move_down"))

	velocity = direction * speed
	move_and_slide()
	global_position = global_position.clamp(Vector2(50, 50), get_viewport_rect().size - Vector2(50, 50))
	
func shoot():
	laser_shot.emit(laser_scene, muzzle.global_position)

func die():
	get_tree().reload_current_scene()
