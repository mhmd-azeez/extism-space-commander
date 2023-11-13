package modding_api

import pdk "github.com/extism/go-pdk"

//go:wasm-module host
//export print
func printImpl(offs uint64)

func Print(message string) {
	mem := pdk.AllocateString(message)
	printImpl(mem.Offset())
}

//go:wasm-module host
//export player_clear_muzzles
func PlayerClearMuzzles()

//go:wasm-module host
//export player_add_muzzle
func PlayerAddMuzzle(x float32, y float32)

//go:wasm-module host
//export player_change_sprite
func PlayerChangeSprite(nameOffs uint64)

//go:wasm-module host
//export show_sprite
func ShowSprite(powerupId int, nameOffs uint64, x float32, y float32)

//go:wasm-module host
//export show_global_sprite
func ShowGlobalSprite(powerupId int, nameOffs uint64, x float32, y float32)

//go:wasm-module host
//export get_viewport
func GetViewport() uint64

//go:wasm-module host
//export get_player_info
func GetPlayerInfo() uint64

//go:wasm-module host
//export create_reminder
func CreateReminder(seconds float32, dataOffs uint64)

//go:wasm-module host
//export die
func Die(powerupId int)

//go:wasm-module host
//export get_enemies
func GetEnemies() uint64

//go:wasm-module host
//export enemy_take_damage
func EnemyTakeDamage(enemyId int, amount int)
