using System.Drawing;
using System.Numerics;
using LookOutTheWindow.Physics;

namespace LookOutTheWindow.Map;

public class MovingSpike(MainScene scn, Sprite sprite, Sprite spriteFrozen)
{
    public Vector2 Position;
    public GameTilemap.ImportedTilemap? ParentTilemap = null;
    public bool Frozen = false;
    private bool _beforeFrostState = false;
    
    public float DirectionY = -1.0f;
    
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
        
        if (Collider.Intersects(scn.PlayerObj.Collider))
        {
            scn.PlayerObj.Die(window);
        }
        
        if (Frozen) return;
        
        //Position.Y = 50.0f;
        Position.Y += DirectionY * dt * 10.0f;
        
        // Y Axis Movement
        (bool isCollideWithTilemapY, BoundingBox? collideWithY) = scn.Tilemap.Intersects(window, Collider);
            
        if (isCollideWithTilemapY)
        {
            if (DirectionY > 0.0f)
            {
                // Moving Up
                Position.Y = collideWithY!.Bottom - sprite.Height/2.0f - 1f;
                
                DirectionY = -1.0f;
            }
            else if (DirectionY < 0.0f)
            {
                // Moving Down
                Position.Y = collideWithY!.Top + sprite.Height/2.0f + 1f;
                
                DirectionY = 1.0f;
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