package main

import (
	_ "embed"
	"encoding/json"
	"fmt"

	mod "modding_api"

	pdk "github.com/extism/go-pdk"
)

//go:embed star_gold.png
var star_gold []byte

//go:embed plane.png
var plane []byte

type ReminderRequest struct {
	Name string
	ID   int
}

//export reminder
func reminder() {

}

//export load_resource
func load_resource() {
	name := pdk.InputString()

	if name == "star_gold" {
		mem := pdk.AllocateBytes(star_gold)
		pdk.OutputMemory(mem)
		return
	} else if name == "plane" {
		mem := pdk.AllocateBytes(plane)
		pdk.OutputMemory(mem)
		return
	}

	panic(fmt.Sprintf("Cant find resource: %s", name))
}

type Enemy struct {
	X      float32
	Y      float32
	ID     int
	HP     int
	Bounty int
}

type Rect struct {
	X      float32
	Y      float32
	Width  float32
	Height float32
}

type PlayerInfo struct {
	X float32
	Y float32
}

//export get_sprite
func get_sprite() {
	pdk.OutputString("star_gold")
}

type PowerUpInfo struct {
	ID int
}

//export activate
func activate() {
	sprite_name := pdk.AllocateString("plane")
	mod.PlayerChangeSprite(sprite_name.Offset())

	mod.PlayerClearMuzzles()
	mod.PlayerAddMuzzle(-30, 10)
	mod.PlayerAddMuzzle(0, 10)
	mod.PlayerAddMuzzle(30, 10)
}

//export on_ready
func on_ready() {

}

type PlayerTakingDamagePayload struct {
	Amount int
}

//export player_taking_damage
func player_taking_damage() {
	var payload PlayerTakingDamagePayload

	payloadJson := pdk.Input()

	err := json.Unmarshal(payloadJson, &payload)

	payload.Amount = 0

	payloadJson, err = json.Marshal(payload)
	if err != nil {
		print(fmt.Sprintf("Failed to serialize response, %v", err))
		return
	}

	pdk.Output(payloadJson)
}

func main() {}
