class_name Enemy extends Area2D

signal killed(bounty)

@export var speed = 150
@export var hp = 1
@export var bounty = 100

func _physics_process(delta):
	global_position.y += speed * delta

func _on_visible_on_screen_notifier_2d_screen_exited():
	queue_free()

func take_damage(amount):
	hp -= amount
	if hp <= 0:
		die()

func die():
	killed.emit(bounty)
	queue_free()

func _on_body_entered(body):
	if body is Player:
		body.die()
		die()
