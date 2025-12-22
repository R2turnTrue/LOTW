using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace LookOutTheWindow;

public class Sprite : IDisposable
{
    private Game _game;
    private GL _gl;
    
    public uint Texture;
    
    public int TexX { get; set; }
    public int TexY { get; set; }
    
    public int Width { get; set; }
    public int Height { get; set; }

    public int TextureWidth { get; }
    public int TextureHeight { get; }
    
    public Sprite(Game game, string path)
    {
        _game = game;
        var gl = game.Gl;
        
        Texture = gl.GenTexture();
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(GLEnum.Texture2D, Texture);
        
        var result = ImageResult.FromMemory(File.ReadAllBytes(path),
            ColorComponents.RedGreenBlueAlpha);
        
        Width = result.Width;
        Height = result.Height;
        TextureWidth = result.Width;
        TextureHeight = result.Height;

        unsafe
        {
            fixed (byte* ptr = result.Data)
            {
                gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba,
                    (uint) result.Width,
                    (uint) result.Height, 0, PixelFormat.Rgba, GLEnum.UnsignedByte, ptr);
            }
        }
        
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.Repeat);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.Repeat);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
        gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        
        gl.BindTexture(GLEnum.Texture2D, 0);
        
        _gl = gl;
    }

    public void Draw(GameWindow window, float x, float y, Color tint, float scaleX = 1.0f, float scaleY = 1.0f)
    {
        _gl.BindTexture(GLEnum.Texture2D, Texture);
        _gl.ActiveTexture(TextureUnit.Texture0);

        
        // var proj = Matrix4x4.CreateOrthographicOffCenter(
        //     0.0f,
        //     window.Viewport.Width,
        //     0.0f,
        //     window.Viewport.Height,
        //     -1.0f,
        //     1.0f);
        var proj = Matrix4x4.CreateOrthographic(
            window.Viewport.Width,
            window.Viewport.Height, -1f, 1f);
        var view = 
            Matrix4x4.CreateTranslation(window.SpriteOffset.X, window.SpriteOffset.Y, 0.0f);
        var model = Matrix4x4.CreateScale(Width/2 * scaleX, -Height/2 * scaleY, 1f) *
            Matrix4x4.CreateTranslation(x, y, 0f);

        _game.SpriteShader.Use();
        _game.SpriteShader.SetMatrix4("model", model);
        _game.SpriteShader.SetMatrix4("view", view);
        _game.SpriteShader.SetMatrix4("projection", proj);

        _game.SpriteShader.SetInteger("spriteTexture", 0);
        _game.SpriteShader.SetVector4("tintColor", new Vector4(
            tint.R / 255.0f,
            tint.G / 255.0f,
            tint.B / 255.0f,
            tint.A / 255.0f));
        _game.SpriteShader.SetVector4("spriteTexture_ST", new Vector4(
            (float)Width / TextureWidth,
            (float)Height / TextureHeight,
            (float)TexX / TextureWidth,
            (float)TexY / TextureHeight
        ));
        
        //Console.WriteLine("Drawing sprite at tex coords " + ((float)TexX / TextureWidth) + ", " + ((float)TexY / TextureHeight));

        window.LocalQuad.Draw(_gl);

        _gl.BindTexture(GLEnum.Texture2D, 0);
    }
    
    public void Dispose()
    {
        _gl.DeleteTexture(Texture);
    }
}