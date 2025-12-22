using System.Drawing;
using System.Numerics;
using LookOutTheWindow.Physics;

namespace LookOutTheWindow.Map;

public class MovingPlatform(MainScene scn, Sprite sprite, Sprite spriteFrozen)
{
    public Vector2 Position;
    public GameTilemap.ImportedTilemap? ParentTilemap = null;
    public bool Frozen = false;
    private bool _beforeFrostState = false;
    
    public float DirectionX = -1.0f;
    
    public BoundingBox Collider => BoundingBox.CreateFromTopLeft(
        Position - new Vector2(sprite.Width / 2.0f, -sprite.Height / 2.0f),
        new Vector2(sprite.Width, sprite.Height));
    
    public void Update(GameWindow window, float dt)
    {
        var bbFrost = scn.FrostSys.WorldAABBToFrostAABB(Collider, window);
        Frozen = scn.FrostSys.IntersectsFrostSpace(bbFrost);
        
        if (_beforeFrostState != Frozen)
        {
            if (Frozen)
            {
                Game.Instance.AudioManager.PlaySFX("event:/sfx/freeze");
                scn.ParticleSys.EmitParticle(20, Position.X, Position.Y, Color.FromArgb(255, 0, 205, 249),
                    0.3f);
            }
            else
            {
                Game.Instance.AudioManager.PlaySFX("event:/sfx/unfreeze");
                scn.ParticleSys.EmitParticle(20, Position.X, Position.Y, Color.FromArgb(255, 255, 255, 255),
                    0.3f);
            }
        }
        
        _beforeFrostState = Frozen;
        
        if (Frozen) return;
        
        //Position.Y = 50.0f;
        Position.X += DirectionX * dt * 15.0f;
        
        // X Axis Movement
        (bool isCollideWithTilemapX, BoundingBox? collideWithX) = scn.Tilemap.Intersects(window, Collider, false);
            
        if (isCollideWithTilemapX && collideWithX != Collider)
        {
            if (DirectionX > 0.0f)
            {
                // Moving Left
                Position.X = collideWithX!.Left - sprite.Width/2.0f - 1.5f;
                
                DirectionX = -1.0f;
            }
            else if (DirectionX < 0.0f)
            {
                // Moving Right
                Position.X = collideWithX!.Right + sprite.Width/2.0f + 1.5f;
                
                DirectionX = 1.0f;
            }
        }
    }
    
    public void Draw(GameWindow window)
    {
        window.DrawSprite(
            Frozen ? spriteFrozen : sprite,
            Position.X,
            Position.Y,
            Color.White);
    }
}