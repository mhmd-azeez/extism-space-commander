package main

import (
	_ "embed"
	"encoding/json"
	"fmt"
	"math"

	mod "modding_api"

	pdk "github.com/extism/go-pdk"
)

//go:embed bolt_gold.png
var bolt_gold []byte

//go:embed kaboom.png
var kaboom []byte

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

	if name == "bolt_gold" {
		mem := pdk.AllocateBytes(bolt_gold)
		pdk.OutputMemory(mem)
		return
	} else if name == "kaboom" {
		mem := pdk.AllocateBytes(kaboom)
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
	pdk.OutputString("bolt_gold")
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

	var viewport Rect

	viewportJson := pdk.FindMemory(mod.GetViewport())
	err = json.Unmarshal(viewportJson.ReadBytes(), &viewport)
	if err != nil {
		print(fmt.Sprintf("error while trying to deserialize view port: %v", err))
		return
	}

	mem := pdk.AllocateString("kaboom")
	mod.ShowSprite(powerupInfo.ID, mem.Offset(), viewport.X, viewport.Y)

	request := ReminderRequest{Name: "die", ID: powerupInfo.ID}
	requestJson, err := json.Marshal(request)
	mem = pdk.AllocateBytes(requestJson)
	mod.CreateReminder(0.4, mem.Offset())

	var player PlayerInfo

	playerJson := pdk.FindMemory(mod.GetPlayerInfo())
	err = json.Unmarshal(playerJson.ReadBytes(), &player)
	if err != nil {
		print(fmt.Sprintf("error while trying to deserialize player info: %v", err))
		return
	}

	var enemies []Enemy

	enemiesJson := pdk.FindMemory(mod.GetEnemies())
	err = json.Unmarshal(enemiesJson.ReadBytes(), &enemies)
	if err != nil {
		print(fmt.Sprintf("error while trying to deserialize enemy list: %v", err))
		return
	}

	for _, enemy := range enemies {
		xDistance := player.X - enemy.X
		yDistance := player.Y - enemy.Y

		xDistanceSquared := math.Pow(float64(xDistance), 2)
		yDistanceSquared := math.Pow(float64(yDistance), 2)

		sumSquaredDistances := xDistanceSquared + yDistanceSquared

		distance := math.Sqrt(sumSquaredDistances)

		if distance < 300 {
			// kill enemy
			mod.EnemyTakeDamage(enemy.ID, 100000)
		}
	}
}

//export on_ready
func on_ready() {

}

type PlayerTakingDamagePayload struct {
	Amount int
}

//export player_taking_damage
func player_taking_damage() {
	// dont change payload
	payloadJson := pdk.Input()
	pdk.Output(payloadJson)
}

func main() {}
