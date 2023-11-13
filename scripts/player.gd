class_name Player extends CharacterBody2D

signal laser_shot(laser_scene, location)
signal took_damage(amount)

@export var muzzles: Array[Marker2D] = []
@export var speed = 300
@export var shots_per_second := 4.0

var laser_scene = preload("res://scenes/laser.tscn")

var shoot_cooldown := false

func add_muzzle(x, y):
	var m = Marker2D.new()
	m.global_position = Vector2(x, y)
	add_child(m)
	muzzles.append(m)

func clear_muzzles():
	muzzles.clear()

func change_sprite(t):
	$Sprite2D.texture = t

func _ready():
	muzzles.append($Muzzle)

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
	for m in muzzles:
		laser_shot.emit(laser_scene, m.global_position)

func take_damage():
	took_damage.emit(1)

func die():
	get_tree().reload_current_scene()
