using Extism.Sdk;
using Extism.Sdk.Native;

using Godot;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

public partial class mod_manager : Node2D
{
    private readonly Mod[] _mods;
    public List<PowerUp> PowerUps { get; } = new List<PowerUp>();

    public mod_manager()
    {
        _mods = Directory.EnumerateFiles("assets/mods/", "*.wasm").Select(f => new Mod(File.ReadAllBytes(f), this)).ToArray();
    }

    public override void _Ready()
    {
        var timer = new Timer
        {
            Autostart = true,
            OneShot = false,
            WaitTime = 1
        };

        AddChild(timer);
        timer.Connect("timeout", Callable.From(() =>
        {
            var idx = Random.Shared.Next(_mods.Length);
            var mod = _mods[idx];

            var powerup = new PowerUp(mod);
            powerup.TreeExited += Powerup_TreeExited;

            AddChild(powerup);
            PowerUps.Add(powerup);

            void Powerup_TreeExited()
            {
                powerup.TreeExited -= Powerup_TreeExited;
                PowerUps.Remove(powerup);
            }
        }));
    }
}

public partial class PowerUp : Area2D
{
    private Sprite2D _sprite;
    private Mod _mod;

    private static int _id = 0;

    public PowerUp(Mod mod)
    {
        _mod = mod;
        Id = _id++;
        Info = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new PowerUpInfo(Id)));
    }

    public int Id { get; set; }
    public byte[] Info { get; }

    public override void _Ready()
    {
        _sprite = new Sprite2D();
        _sprite.Texture = _mod.GetSpriteTexture(this);
        _sprite.Position = new Vector2(10, 10);

        var collision = new CollisionShape2D();
        collision.Shape = new CircleShape2D
        {
            Radius = 10,
        };

        var notifier = new VisibleOnScreenEnabler2D();
        notifier.Connect("screen_exited", Callable.From(() =>
        {
            QueueFree();
        }));

        Connect("body_entered", Callable.From((Node body) =>
        {
            if (body.Name != "Player")
            {
                return;
            }

            RemoveChild(_sprite);
            RemoveChild(collision);
            RemoveChild(notifier);
            _mod.Activate(this);
        }));

        AddChild(_sprite);
        AddChild(collision);
        AddChild(notifier);

        var viewPort = GetViewportRect();

        GlobalPosition = viewPort.Position + new Vector2(Random.Shared.Next(10, (int)viewPort.Size.X - 10), 10);
    }

    public void SetImage(byte[] buffer)
    {
        var image = new Image();
        image.LoadPngFromBuffer(buffer);
        var imageTexture = ImageTexture.CreateFromImage(image);

        _sprite.Texture = imageTexture;
    }

    public override void _PhysicsProcess(double delta)
    {
        var speed = 200;
        GlobalPosition = new(GlobalPosition.X, GlobalPosition.Y + (float)(speed * delta));
    }
}

public class Mod
{
    private readonly Plugin _extismPlugin;
    private readonly mod_manager _manager;

    public Mod(byte[] wasm, mod_manager manager)
    {
        var hostFunctions = new HostFunction[]
        {
            HostFunction.FromMethod("print", IntPtr.Zero, (CurrentPlugin cp, long offs) =>
            {
                var message = cp.ReadString(offs);
                GD.Print(message);
            }),

            new HostFunction(
                "show_sprite",
                new ExtismValType[] { ExtismValType.I32, ExtismValType.I64, ExtismValType.F32, ExtismValType.F32 },
                new Span<ExtismValType>{ },
                IntPtr.Zero,
                (CurrentPlugin cp, Span<ExtismVal> inputs, Span<ExtismVal> outputs) =>
                {
                    var id = inputs[0].v.i32;
                    var offs = inputs[1].v.i64;
                    var x = inputs[2].v.f32;
                    var y = inputs[3].v.f32;

                    var name = cp.ReadBytes(offs).ToArray();

                    CallPluginFunction(() =>
                    {
                        var resourceBuffer = _extismPlugin.Call("load_resource", name).ToArray();

                        var sprite = LoadSprite(resourceBuffer);
                        sprite.Name = Encoding.UTF8.GetString(name);
                        sprite.GlobalPosition = new Vector2(x, y);

                        var node = manager.PowerUps.FirstOrDefault(p => p.Id == id);
                        node.AddChild(sprite);
                    });
                }),

            HostFunction.FromMethod("create_reminder", IntPtr.Zero, (CurrentPlugin cp, float seconds, long offset) =>
            {
                var input = cp.ReadBytes(offset).ToArray();

                var timer = _manager.GetTree().CreateTimer(seconds);
                timer.Connect("timeout", Callable.From(() =>
                {
                    _extismPlugin.Call("reminder", input);
                }));
            }),

            HostFunction.FromMethod("die", IntPtr.Zero, (CurrentPlugin cp, int id) =>
            {
                var powerup = _manager.PowerUps.FirstOrDefault(p => p.Id == id);
                if (powerup != null)
                {
                    powerup.QueueFree();
                }
            }),

            HostFunction.FromMethod("get_viewport", IntPtr.Zero, (CurrentPlugin cp) =>
            {
                var viewport = _manager.GetViewportRect();
                var rect = new Rect(viewport.Position.X, viewport.Position.Y, viewport.Size.X, viewport.Size.Y);

                var json = JsonSerializer.Serialize(rect);
                return cp.WriteString(json);
            }),

            HostFunction.FromMethod("get_enemies", IntPtr.Zero, (CurrentPlugin cp) =>
            {
                var enemies = new List<Enemy>();
                foreach (var enemy in _manager.GetParent().FindChild("EnemyContainer").GetChildren().OfType<Area2D>())
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

                foreach (var enemy in _manager.GetParent().FindChild("EnemyContainer").GetChildren().OfType<Area2D>())
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
                var player = (CharacterBody2D)_manager.GetParent().FindChild("Player");
                var info = new PlayerInfo(player.GlobalPosition.X, player.GlobalPosition.Y);

                var json = JsonSerializer.Serialize(info);
                return cp.WriteString(json);
            }),
        };

        foreach (var hostFunction in hostFunctions)
        {
            hostFunction.SetNamespace("host");
        }

        _extismPlugin = new Plugin(new Manifest(new ByteArrayWasmSource(wasm, "power up")), hostFunctions, withWasi: true);
        _manager = manager;
    }

    public void Activate(PowerUp powerUp)
    {
        _extismPlugin.Call("activate", powerUp.Info);
    }

    internal Texture2D GetSpriteTexture(PowerUp powerUp)
    {
        var spriteName = _extismPlugin.Call("get_sprite", powerUp.Info).ToArray();
        var spriteBuffer = _extismPlugin.Call("load_resource", spriteName).ToArray();
        var image = new Image();
        image.LoadPngFromBuffer(spriteBuffer);
        var imageTexture = ImageTexture.CreateFromImage(image);

        return imageTexture;
    }

    private void CallPluginFunction(Action action)
    {
        // HACK: calling a plugin function within a host function causes a deadlock!

        var timer = _manager.GetTree().CreateTimer(0.0001);
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
}

public record PowerUpInfo(int id);
public record Rect(float x, float y, float width, float height);
public record Enemy(float x, float y, int id, int hp, int bounty);
public record PlayerInfo(float x, float y);