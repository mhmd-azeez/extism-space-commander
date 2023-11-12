extends Control

@onready var score = $Score:
	set(value):
		score.text = "SCORE " + str(value)
