# Extism Space Commander
A retro space shooter game that shows how you can integrate Extism into your project to make it super moddable.
The project is built in Godot 4 and uses both GDScript (for the game logic) and C# (for integrating with Extism)

## Demo

https://github.com/mhmd-azeez/extism-space-commander/assets/16880059/1d85b399-7a34-430f-ba6d-464c8f954e4c

You can see the full demo with some explaination here:
https://www.loom.com/share/4a0bd61c002641fcacc0ee3ce394db5c?sid=ef2de775-0b08-4fc1-8c93-f23bcf6e0b48

## Structure

- [scripts](./scripts): contains the game logic
- [scripts/mod_manager.cs](./scripts/mod_manager.cs): A C# class that's responsible for loading the mods
- [assets/mods](./assets/mods): contains compiled mods (.wasm files)
- [mods](./mods): contains mod source files, they are all written in go

## How to compile a mod
```
$ cd ./mods/shield/
$ tinygo build -target wasi -o ../../assets/mods/shield.wasm main.go
```
**Note:** for compiling the mods, you need [TinyGo]([url](https://tinygo.org/)https://tinygo.org/).

## Credits

- The game is based on [Kan Alpar's Youtube tutorial]([url](https://www.youtube.com/watch?v=QoNukqpolS8)https://www.youtube.com/watch?v=QoNukqpolS8)
- The art is by [Kenney Vleugels](www.kenney.nl). (except for the ugly kaboom.png, that one is mine)

## More information

Please see https://extism.org
