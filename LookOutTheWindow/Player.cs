using System.Drawing;
using System.Numerics;
using System.Text.Json;
using LookOutTheWindow.Data;
using LookOutTheWindow.Physics;
using Silk.NET.Input;

namespace LookOutTheWindow;

public class Player(MainScene scn) : IDisposable
{
    public Sprite PlayerSprite { get; set; }
    public Sprite OutlineSprite { get; set; }
    public Sprite FrozenSprite { get; set; }
    
    public PlayerProperty Properties { get; set; }

    public bool IsGround = false;
    
    public Vector2 Pivot = new Vector2(0.0f, -32.0f);
    
    public Vector2 Position = Vector2.Zero;
    public Vector2 Velocity = Vector2.Zero;
    
    public Vector2 Acceleration = new Vector2(0.0f, -9.8f);
    
    public Vector2 Size => new Vector2(12, 30);
    public BoundingBox Collider => BoundingBox.CreateFromTopLeft(Position + new Vector2(-Size.X / 2, Size.Y), Size);

    public float WalkParticleTimer = 0.0f;
    
    public bool CurrentFrost = false;
    
    private bool _beforeFrostState = true;
    private int _lastDirection = 1;
    
    public void Load()
    {
        Properties = JsonSerializer.Deserialize<PlayerProperty>(
            File.ReadAllText("assets/data/player.json"))!;
        PlayerSprite = new Sprite(Game.Instance, "assets/sprites/player.png");
        OutlineSprite = new Sprite(Game.Instance, "assets/sprites/player_outline.png");
        FrozenSprite = new Sprite(Game.Instance, "assets/sprites/player_frozen.png");
        
        Acceleration.Y = Properties.GravityY;
    }
    
    public void MoveAndSlide(GameWindow window, double deltaTime)
    {
        Position.X += Velocity.X * (float)deltaTime;
        
        // X Axis Movement
        var colX = BoundingBox.CreateFromTopLeft(Collider.Position, Collider.Size);
        colX.Position += new Vector2(0.0f, 1f);
        (bool isCollideWithTilemapX, BoundingBox? collideWithX) = scn.Tilemap.Intersects(window, colX);
        
        if (isCollideWithTilemapX)
        {
            if (Velocity.X > 0.0f)
            {
                // Moving Right
                Position.X = collideWithX!.Left - Size.X / 2 - 0.01f;
            }
            else if (Velocity.X < 0.0f)
            {
                // Moving Left
                Position.X = collideWithX!.Right + Size.X / 2 + 0.01f;
            }
            Velocity.X = 0.0f;
        }
        
        Position.Y += Velocity.Y * (float)deltaTime;
        
        // Y Axis Movement
        (bool isCollideWithTilemapY, BoundingBox? collideWithY) = scn.Tilemap.Intersects(window, Collider);
            
        if (isCollideWithTilemapY)
        {
            if (Velocity.Y > 0.0f)
            {
                // Moving Up
                Position.Y = collideWithY!.Bottom - Size.Y - 0.01f;
            }
            else if (Velocity.Y < 0.0f)
            {
                // Moving Left
                Position.Y = collideWithY!.Top + 0.01f;
            }
            Velocity.Y = 0.0f;
        }

        IsGround = isCollideWithTilemapY;
    }

    public void HandleGravity(double deltaTime)
    {
        Velocity += Acceleration * (float)deltaTime;
    }
    
    public void HandleJump(GameWindow window, double deltaTime)
    {
        if (IsGround && Velocity.Y <= 0.0f)
        {
            if (window.IsKeyDown(Key.Space))
            {
                Game.Instance.AudioManager.PlaySFX("event:/sfx/jump");
                scn.ParticleSys.EmitParticle(16, Position.X, Position.Y, Color.FromArgb(255, 255, 255, 255),
                    0.1f);
                Velocity.Y = Properties.JumpForce;
            }
        }
    }
    
    public void HandleAcceleration(GameWindow window, double deltaTime)
    {
        if (window.IsKey(Key.A))
        {
            _lastDirection = -1;
            Velocity.X -= Properties.Acceleration;
        }
        if (window.IsKey(Key.D))
        {
            _lastDirection = 1;
            Velocity.X += Properties.Acceleration;
        }
    }

    public void HandleDeceleration(GameWindow window, double deltaTime)
    {
        if (!window.IsKey(Key.A) && !window.IsKey(Key.D))
        {
            float decel;
            
            if (IsGround)
                decel = Properties.Deceleration;
            else 
                decel = Properties.DecelerationAir;

            if (Velocity.X > 0.0f)
            {
                Velocity.X -= decel * (float)deltaTime;
                if (Velocity.X < 0.0f)
                    Velocity.X = 0.0f;
            }
            else
            {
                Velocity.X += decel * (float)deltaTime;
                if (Velocity.X > 0.0f)
                    Velocity.X = 0.0f;
            }
        }
    }
    
    public void MaxSpeedClamp()
    {
        if (MathF.Abs(Velocity.X) >= Properties.MaxSpeed)
        {
            Velocity.X = float.Sign(Velocity.X) * Properties.MaxSpeed;
        }
    }

    public void Die(GameWindow window)
    {
        Game.Instance.AudioManager.PlaySFX("event:/sfx/die");
        scn.ParticleSys.EmitParticle(24, Position.X, Position.Y + Size.Y / 2.0f, Color.FromArgb(255, 0, 205, 249),
            0.5f);
        
        Position = window.ViewportPosToWorld(scn.LastTilemap?.Spawn ?? scn.Tilemap.TiledMaps[0].Spawn);
        Velocity = Vector2.Zero;
    }
    
    public void Update(GameWindow window, double deltaTime)
    {
        var bbFrost = scn.FrostSys.WorldAABBToFrostAABB(Collider, window);
        CurrentFrost = scn.FrostSys.IntersectsFrostSpace(bbFrost);
        if (CurrentFrost != _beforeFrostState)
        {
            if (CurrentFrost)
            {
                Game.Instance.AudioManager.PlaySFX("event:/sfx/freeze");
                scn.ParticleSys.EmitParticle(20, Position.X, Position.Y + Size.Y / 2.0f, Color.FromArgb(255, 255, 255, 255),
                    0.3f);
            }
            else
            {
                Game.Instance.AudioManager.PlaySFX("event:/sfx/unfreeze");
                scn.ParticleSys.EmitParticle(20, Position.X, Position.Y + Size.Y / 2.0f, Color.FromArgb(255, 0, 205, 255),
                    0.3f);
            }
        }

        _beforeFrostState = CurrentFrost;
        
        if (CurrentFrost)
        {
            return;
        }
        
        WalkParticleTimer -= (float)deltaTime;
        
        if (IsGround && MathF.Abs(Velocity.X) > 0.1f && WalkParticleTimer <= 0.0f)
        {
            Game.Instance.AudioManager.PlaySFX("event:/sfx/footstep");
            scn.ParticleSys.EmitParticle(8, Position.X, Position.Y, Color.FromArgb(255, 255, 255, 255),
                0.1f);
            WalkParticleTimer = 0.3f;
        }
        
        HandleGravity(deltaTime);
        HandleJump(window, deltaTime);

        HandleAcceleration(window, deltaTime);
        HandleDeceleration(window, deltaTime);
        MaxSpeedClamp();

        MoveAndSlide(window, deltaTime);
        
        if (Position.Y < -110.0f || Position.Y >= 120.0f)
        {
            Die(window);
        }
    }
    
    public void Draw(GameWindow window)
    {
        window.DrawSprite(PlayerSprite, Position.X - Pivot.X, Position.Y - Pivot.Y, 
            Color.White, 1.0f * _lastDirection, 1.0f);
    }
    
    public void DrawOutline(GameWindow window)
    {
        window.DrawSprite(OutlineSprite, Position.X - Pivot.X, Position.Y - Pivot.Y, 
            Color.White, 1.0f * _lastDirection, 1.0f);

        var bbFrost = scn.FrostSys.WorldAABBToFrostAABB(Collider, window);
        if (scn.FrostSys.IntersectsFrostSpace(bbFrost))
        {
            window.DrawSprite(FrozenSprite, Position.X - Pivot.X, Position.Y - Pivot.Y, 
                Color.White, 1.0f * _lastDirection, 1.0f);
        }
    }

    public void Dispose()
    {
        PlayerSprite.Dispose();
        OutlineSprite.Dispose();
        FrozenSprite.Dispose();
    }
}