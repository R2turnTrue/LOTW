using System.Drawing;
using System.Numerics;

namespace LookOutTheWindow;

public class Particle
{
    public float LifeTime { get; set; } = 1.0f;
    public float Scale { get; set; } = 1.0f;
    public Vector2 Position = Vector2.Zero;
    public Vector2 Velocity = Vector2.Zero;
    public Color ParticleColor = Color.White;
    
    public void Update(float deltaTime)
    {
        LifeTime -= deltaTime;
        Position += Velocity * deltaTime;
        Scale -= deltaTime * 1f;
        if (Scale < 0.0f) Scale = 0.0f;
    }

    public void Draw(GameWindow window, Sprite spr)
    {
        window.DrawSprite(spr,
            Position.X,
            Position.Y,
            ParticleColor,
            Scale,
            Scale);
    }
}