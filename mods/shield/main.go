package main

import (
	_ "embed"
	"encoding/json"
	"fmt"

	mod "modding_api"

	pdk "github.com/extism/go-pdk"
)

//go:embed shield_silver.png
var shield_silver []byte

type ReminderRequest struct {
	Name string
	ID   int
}

//export reminder
func reminder() {
	requestJson := pdk.Input()

	var request ReminderRequest

	err := json.Unmarshal(requestJson, &request)
	if err != nil {
		print(fmt.Sprintf("failed to deserialize reminder request: %v", err))
		return
	}

	if request.Name == "die" {
		mod.Die(request.ID)
	}
}

//export load_resource
func load_resource() {
	name := pdk.InputString()

	if name == "shield_silver" {
		mem := pdk.AllocateBytes(shield_silver)
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
	pdk.OutputString("shield_silver")
}

type PowerUpInfo struct {
	ID int
}

//export activate
func activate() {
	var powerupInfo PowerUpInfo

	powerupJson := pdk.Input()
	err := json.Unmarshal(powerupJson, &powerupInfo)
	if err != nil {
		print(fmt.Sprintf("error while trying to deserialize powerup info: %v", err))
		return
	}

	request := ReminderRequest{Name: "die", ID: powerupInfo.ID}
	requestJson, err := json.Marshal(request)
	mem := pdk.AllocateBytes(requestJson)
	mod.CreateReminder(10, mem.Offset())

	var viewport Rect

	viewportJson := pdk.FindMemory(mod.GetViewport())
	err = json.Unmarshal(viewportJson.ReadBytes(), &viewport)
	if err != nil {
		print(fmt.Sprintf("error while trying to deserialize view port: %v", err))
		return
	}

	sprite_name := pdk.AllocateString("shield_silver")
	mod.ShowGlobalSprite(powerupInfo.ID, sprite_name.Offset(), viewport.Width-20, 20)
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
