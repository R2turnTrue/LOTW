using System.Drawing;
using System.Numerics;
using LookOutTheWindow.Physics;

namespace LookOutTheWindow;

public class Hangju(FrostSystem frost) : IDisposable
{
    public Sprite HangjuSprite { get; set; }

    public Vector2 Position;

    public void Load()
    {
        HangjuSprite = new Sprite(Game.Instance, "assets/sprites/hangju.png");
    }
    
    public void Update(GameWindow window, double deltaTime)
    {
        var mouseScreen = window.GetMousePosition();
        var mouseWorld = window.ScreenPosToWorld(mouseScreen);

        Position = Vector2.Lerp(Position, mouseWorld, 12f * (float)deltaTime);
        
        var ratioMaskScr = new Vector2(FrostSystem.MaskWidth, FrostSystem.MaskHeight) /
                           new Vector2(window.Viewport.Width, window.Viewport.Height);

        var vp = window.WorldToViewportPos(Position);
        
        Rectangle screenSpaceRect = new Rectangle(
            (int)(vp.X * ratioMaskScr.X) - (int)(HangjuSprite.Width * ratioMaskScr.X / 2.0f),
            FrostSystem.MaskHeight - (int)(vp.Y * ratioMaskScr.Y) - (int)(HangjuSprite.Height * ratioMaskScr.Y / 2.0f),
            (int)(HangjuSprite.Width * ratioMaskScr.X),
            (int)(HangjuSprite.Height * ratioMaskScr.Y));
        //Console.WriteLine($"Hangju Screen Rect: {screenSpaceRect}");
        
        frost.RemoveFrostAABB(screenSpaceRect);
    }
    
    public void Draw(GameWindow window)
    {
        HangjuSprite.Draw(
            window,
            Position.X - window.SpriteOffset.X,
            Position.Y - window.SpriteOffset.Y,
            Color.FromArgb(255, 255, 255, 255));
    }
    
    public void Dispose()
    {
        HangjuSprite.Dispose();
    }
}