using Extism.Sdk;
using Extism.Sdk.Native;

using Godot;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

public partial class mod_manager : Node
{
    private readonly List<PowerUpMod> _powerUpMods = new();

    public override void _Ready()
    {
        foreach (var file in Directory.EnumerateFiles("assets/mods/", "*.wasm"))
        {
            var mod = new PowerUpMod(file);
            _powerUpMods.Add(mod);
            AddChild(mod);
        }
    }
}

public partial class PowerUpMod : Area2D
{
    private readonly Plugin _extismPlugin;

    public PowerUpMod(string wasmPath)
    {
        var hostFunctions = new HostFunction[]
        {
            HostFunction.FromMethod("print", IntPtr.Zero, (CurrentPlugin cp, long offs) =>
            {
                var message = cp.ReadString(offs);
                GD.Print(message);
            }),

			// TODO: Use HostFunction.FromMethod after https://github.com/extism/dotnet-sdk/pull/35 is merged
			new HostFunction(
                "show_sprite",
                new ExtismValType[] { ExtismValType.I64, ExtismValType.F32, ExtismValType.F32 },
                new Span<ExtismValType>{ },
                IntPtr.Zero,
                (CurrentPlugin cp, Span<ExtismVal> inputs, Span<ExtismVal> outputs) =>
                {
                    var offs = inputs[0].v.i64;
                    var x = inputs[1].v.f32;
                    var y = inputs[2].v.f32;

                    var name = cp.ReadBytes(offs).ToArray();

                    CallPluginFunction(() =>
                    {
                        var resourceBuffer = _extismPlugin.Call("load_resource", name).ToArray();

                        var sprite = LoadSprite(resourceBuffer);
                        sprite.Name = Encoding.UTF8.GetString(name);
                        sprite.GlobalPosition = new Vector2(x, y);
                        AddChild(sprite);
                    });
                }),

            HostFunction.FromMethod("create_reminder", IntPtr.Zero, (CurrentPlugin cp, float seconds, long offset) =>
            {
                var input = cp.ReadBytes(offset).ToArray();

                var timer = GetTree().CreateTimer(seconds);
                timer.Connect("timeout", Callable.From(() =>
                {
                    _extismPlugin.Call("reminder", input);
                }));
            }),

            HostFunction.FromMethod("die", IntPtr.Zero, (CurrentPlugin cp) =>
            {
               QueueFree();
            }),

            HostFunction.FromMethod("get_viewport", IntPtr.Zero, (CurrentPlugin cp) =>
            {
                var viewport = GetViewportRect();
                var rect = new Rect(viewport.Position.X, viewport.Position.Y, viewport.Size.X, viewport.Size.Y);

                var json = JsonSerializer.Serialize(rect);
                return cp.WriteString(json);
            }),

            HostFunction.FromMethod("get_enemies", IntPtr.Zero, (CurrentPlugin cp) =>
            {
                var enemies = new List<Enemy>();
                foreach (var enemy in GetParent().GetParent().FindChild("EnemyContainer").GetChildren().OfType<Area2D>())
                {
                    enemies.Add(new Enemy(
                       enemy.GlobalPosition.X,
                       enemy.GlobalPosition.Y,
                        enemy.Get("id").AsInt32(),
                        enemy.Get("hp").AsInt32(),
                        enemy.Get("bounty").AsInt32()));
                }

                var json = JsonSerializer.Serialize(enemies);
                return cp.WriteString(json);
            }),

            HostFunction.FromMethod("enemy_take_damage", IntPtr.Zero, (CurrentPlugin cp, int id, int amount) =>
            {
                var enemies = new List<Enemy>();

                foreach (var enemy in GetParent().GetParent().FindChild("EnemyContainer").GetChildren().OfType<Area2D>())
                {
                    var enemyId = enemy.Get("id").AsInt32();
                     enemy.Call("take_damage", amount);
                    if (id == enemyId)
                    {
                        enemy.Call("take_damage", amount);

                        break;
                    }
                }
            }),

            HostFunction.FromMethod("get_player_info", IntPtr.Zero, (CurrentPlugin cp) =>
            {
                var parent = GetParent().GetParent();
                var player = (CharacterBody2D)GetParent().GetParent().FindChild("Player");
                var info = new PlayerInfo(player.GlobalPosition.X, player.GlobalPosition.Y);

                var json = JsonSerializer.Serialize(info);
                return cp.WriteString(json);
            }),
        };

        foreach (var hostFunction in hostFunctions)
        {
            hostFunction.SetNamespace("host");
        }

        _extismPlugin = new Plugin(new Manifest(new PathWasmSource(wasmPath)), hostFunctions, withWasi: true);
    }

    private void CallPluginFunction(Action action)
    {
        // HACK: calling a plugin function within a host function causes a deadlock!

        var timer = GetTree().CreateTimer(0.0001);
        timer.Connect("timeout", Callable.From(() =>
        {
            action();
        }));
    }

    private Sprite2D LoadSprite(byte[] data)
    {
        var image = new Image();
        image.LoadPngFromBuffer(data);
        var imageTexture = ImageTexture.CreateFromImage(image);

        var sprite = new Sprite2D();
        sprite.Texture = imageTexture;

        return sprite;
    }

    public override void _Ready()
    {
        _extismPlugin.Call("on_ready", Array.Empty<byte>());

        var spriteName = _extismPlugin.Call("get_sprite", Array.Empty<byte>()).ToArray();
        var spriteBuffer = _extismPlugin.Call("load_resource", spriteName).ToArray();

        var image = new Image();
        image.LoadPngFromBuffer(spriteBuffer);
        var imageTexture = ImageTexture.CreateFromImage(image);

        var sprite = new Sprite2D();
        sprite.Texture = imageTexture;
        sprite.Position = new Vector2(10, 10);

        var collision = new CollisionShape2D();
        collision.Shape = new CircleShape2D
        {
            Radius = 10,
        };

        var notifier = new VisibleOnScreenEnabler2D();
        notifier.Connect("screen_exited", Callable.From(() =>
        {
            RemoveChild(sprite);
            RemoveChild(collision);
            RemoveChild(notifier);
            _extismPlugin.Dispose();
            QueueFree();
        }));

        Connect("body_entered", Callable.From((Node body) =>
        {
            _extismPlugin.Call("activate", Array.Empty<byte>());
            RemoveChild(sprite);
            RemoveChild(collision);
            RemoveChild(notifier);
        }));

        AddChild(sprite);
        AddChild(collision);
        AddChild(notifier);

        var viewPort = GetViewportRect();

        GlobalPosition = viewPort.Position + new Vector2(Random.Shared.Next(10, (int)viewPort.Size.X - 10), 10);
    }

    public override void _PhysicsProcess(double delta)
    {
        var speed = 200;
        GlobalPosition = new(GlobalPosition.X, GlobalPosition.Y + (float)(speed * delta));
    }
}

public record Rect(float x, float y, float width, float height);
public record Enemy(float x, float y, int id, int hp, int bounty);
public record PlayerInfo(float x, float y);