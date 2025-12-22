using System.Drawing;
using System.Numerics;

namespace LookOutTheWindow;

public class ParticleEmitterSystem : IDisposable
{
    public Sprite CircleSprite { get; private set; }
    public List<Particle> Particles { get; private set; } = new();
    
    public void Load()
    {
        CircleSprite = new Sprite(Game.Instance, "assets/sprites/circle.png");
    }
    
    public void EmitParticle(int count, float x, float y, Color color, float scale)
    {
        //Console.WriteLine($"Emitting {count} particles at ({x}, {y})");
        
        for (int i = 0; i < count; i++)
        {
            var velocity = new Vector2(
                (Random.Shared.NextSingle() - 0.5f) * 100.0f,
                (Random.Shared.NextSingle() - 0.5f) * 100.0f);
            
            var lifetime = Random.Shared.NextSingle() * 1.0f + 0.5f;
            var size = scale + (Random.Shared.NextSingle() - 0.5f) * 2.0f * 0.2f;
            
            Particles.Add(new Particle
            {
                Position = new Vector2(x, y),
                Velocity = velocity,
                LifeTime = lifetime,
                ParticleColor = color,
                Scale = size
            });
        }
    }

    public void Update(GameWindow window, float deltaTime)
    {
        for (int i = Particles.Count - 1; i >= 0; i--)
        {
            var p = Particles[i];
            p.Update(deltaTime);
            
            if (p.LifeTime <= 0)
            {
                int last = Particles.Count - 1;
                Particles[i] = Particles[last];
                Particles.RemoveAt(last);
                continue;
            }
        }
    }

    public void Draw(GameWindow window, float deltaTime)
    {
        foreach (var p in Particles)
        {
            p.Draw(window, CircleSprite);
        }
    }

    public void Dispose()
    {
        CircleSprite.Dispose();
        Particles.Clear();
    }
}