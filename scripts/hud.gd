extends Control

@onready var score = $Score:
	set(value):
		score.text = "SCORE " + str(value)

@onready var fps = $Fps
func _process(delta):
	fps.text = "FPS: " + str(Engine.get_frames_per_second())
