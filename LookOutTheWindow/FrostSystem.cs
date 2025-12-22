using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using LookOutTheWindow.Data;
using LookOutTheWindow.Physics;
using Silk.NET.OpenGL;

namespace LookOutTheWindow;

public class FrostSystem : IDisposable
{
    public struct FrostSeed
    {
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public float Strength { get; set; }
        public float GrowthBias { get; set; }
    }
    
    public const int MaskWidth = 128;
    public const int MaskHeight = 128;
    
    public FrostProperty Properties { get; set; }
    
    public Perlin PerlinNoise = new Perlin();
    public float[] FrostArray = new float[MaskWidth * MaskHeight];
    public float[] NoiseArray = new float[MaskWidth * MaskHeight];
    public List<FrostSeed> Seeds = new List<FrostSeed>();
    
    private float[] GrowthMap = new float[MaskWidth * MaskHeight];
    private float _globalTime = 0.0f;

    public uint Texture;
    
    private void GenerateGrowthMap()
    {
        float scale = 0.08f; // 작을수록 큰 구간
        for (int y = 0; y < MaskHeight; y++)
        {
            for (int x = 0; x < MaskWidth; x++)
            {
                float n = (float)PerlinNoise.Noise(x * scale, y * scale); // 0~1
                GrowthMap[y * MaskWidth + x] = float.Lerp(0.6f, 1.4f, n);
            }
        }
    }
    
    private void GenerateNoise()
    {
        for (int i = 0; i < NoiseArray.Length; i++)
            NoiseArray[i] = Random.Shared.NextSingle();
    }

    private void GenerateInitialSeeds()
    {
        Seeds.Clear();

        for (int i = 0; i < Properties.FrostCount; i++)
        {
            Seeds.Add(new FrostSeed
            {
                Position = new Vector2(
                    Random.Shared.NextSingle() * MaskWidth,
                    Random.Shared.NextSingle() * MaskHeight),
                Radius = Random.Shared.NextSingle() *
                         (Properties.InitialSizeMax - Properties.InitialSizeMin) +
                         Properties.InitialSizeMin,
                Strength = Random.Shared.NextSingle() *
                           (Properties.InitialStrengthMax - Properties.InitialStrengthMin) +
                           Properties.InitialStrengthMin,
                GrowthBias = Random.Shared.NextSingle() * 0.4f + 0.8f
            });
        }
    }

    public void CreateTexture()
    {
        var gl = Game.Instance.Gl;
        Texture = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, Texture);
        
        unsafe
        {
            fixed (float* ptr = FrostArray)
            {
                gl.TexImage2D(GLEnum.Texture2D, 0,
                    InternalFormat.R16f,
                    MaskWidth,
                    MaskHeight, 0,
                    PixelFormat.Red,
                    GLEnum.Float, ptr);
            }
        }
        
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Linear);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Linear);
        
        gl.BindTexture(GLEnum.Texture2D, 0);
    }
    
    public void Load()
    {
        PerlinNoise = new Perlin();
        FrostArray = new float[MaskWidth * MaskHeight];
        
        Array.Fill(FrostArray, 1.0f);
        
        NoiseArray = new float[MaskWidth * MaskHeight];
        
        Properties = JsonSerializer.Deserialize<FrostProperty>(
            File.ReadAllText("assets/data/frost.json"))!;
        
        GenerateNoise();
        GenerateInitialSeeds();
        GenerateGrowthMap();

        CreateTexture();
    }

    public void Update(double deltaTime)
    {
        _globalTime += (float)deltaTime;
        float dt = (float)deltaTime;
        
        var span = CollectionsMarshal.AsSpan(Seeds);
        for (int i = 0; i < span.Length; i++)
        {
            span[i].Radius += Properties.GrowSpeed * dt * (1.0f + span[i].Radius * 0.15f);
            span[i].Radius = MathF.Min(span[i].Radius, Properties.InitialSizeMax);
            span[i].Strength = MathF.Min(1.0f,
                span[i].Strength + Properties.FrostIncreaseSpeed * dt);
        }
        
        for (int i = 0; i < FrostArray.Length; i++)
        {
            //FrostArray[i] *= 0.995f;
        }
        
        foreach (var seed in Seeds)
        {
            ApplySeed(seed, dt);
        }
        
        UploadTexture();
    }
    
    private void ApplySeed(FrostSeed seed, float deltaTime = 1.0f)
    {
        int minX = (int)MathF.Max(0, seed.Position.X - seed.Radius);
        int maxX = (int)MathF.Min(MaskWidth - 1, seed.Position.X + seed.Radius);
        int minY = (int)MathF.Max(0, seed.Position.Y - seed.Radius);
        int maxY = (int)MathF.Min(MaskHeight - 1, seed.Position.Y + seed.Radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - seed.Position.X;
                float dy = y - seed.Position.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq > seed.Radius * seed.Radius)
                    continue;

                float dist = MathF.Sqrt(distSq);
                float falloff = 1.0f - (dist / seed.Radius);

                int idx = y * MaskWidth + x;

                // 노이즈로 경계 찌그러뜨리기
                float noise = NoiseArray[idx] * 0.4f + 0.6f;

                float zone = float.Clamp(GrowthMap[idx] + 0.1f, 0.0f, 1.0f);
                float value = falloff * seed.Strength * noise * zone * seed.GrowthBias;
                //float wobble = 1.0f + MathF.Sin(_globalTime * 0.7f + idx * 0.01f) * 0.05f;
                //value *= wobble;
                //value *= _globalTime * 0.3f;
                
                FrostArray[idx] += float.Max(0.0f, value * deltaTime);
                FrostArray[idx] = MathF.Min(FrostArray[idx], 1.0f);
            }
        }
    }

    public void UploadTexture()
    {
        var gl = Game.Instance.Gl;
        
        gl.BindTexture(GLEnum.Texture2D, Texture);
        unsafe
        {
            fixed (float* ptr = FrostArray)
            {
                gl.TexSubImage2D(GLEnum.Texture2D, 0,
                    0, 0,
                    MaskWidth,
                    MaskHeight,
                    PixelFormat.Red,
                    GLEnum.Float, ptr);
            }
        }
        gl.BindTexture(GLEnum.Texture2D, 0);
    }
    
    public void RemoveFrostAABB(Rectangle area)
    {
        int minX = (int)MathF.Max(0, area.Left);
        int maxX = (int)MathF.Min(MaskWidth - 1, area.Right);
        int minY = (int)MathF.Max(0, area.Top);
        int maxY = (int)MathF.Min(MaskHeight - 1, area.Bottom);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int idx = y * MaskWidth + x;
                FrostArray[idx] = 0.0f;
            }
        }
    }
    
    public bool IntersectsFrostSpace(Rectangle area)
    {
        int minX = (int)MathF.Max(0, area.Left);
        int maxX = (int)MathF.Min(MaskWidth - 1, area.Right);
        int minY = (int)MathF.Max(0, area.Top);
        int maxY = (int)MathF.Min(MaskHeight - 1, area.Bottom);

        float totalPixels = (maxX - minX + 1) * (maxY - minY + 1);
        float frostedPixels = 0.0f;
        
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int idx = y * MaskWidth + x;
                if (FrostArray[idx] >= 0.95f)
                {
                    frostedPixels += 1.0f;
                }
            }
        }
        
        if (frostedPixels / totalPixels >= 0.9f)
            return true;

        return false;
    }
    
    public Rectangle WorldAABBToFrostAABB(BoundingBox worldAABB, GameWindow window)
    {
        var scaleX = MaskWidth / (float)window.Viewport.Width;
        var scaleY = MaskHeight / (float)window.Viewport.Height;
        
        var vpPos = window.WorldToViewportPos(worldAABB.Position + window.SpriteOffset) * new Vector2(scaleX, scaleY);
        var size = worldAABB.Size * new Vector2(scaleX, scaleY);
        
        return new Rectangle((int)vpPos.X, MaskHeight - (int)vpPos.Y, (int)size.X, (int)size.Y);
    }

    public void Dispose()
    {
        var gl = Game.Instance.Gl;
        gl.DeleteTexture(Texture);
        _globalTime = 0.0f;
        Seeds.Clear();
    }
}