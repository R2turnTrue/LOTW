using System.Drawing;
using System.Net;
using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace LookOutTheWindow;

public class GameWindow : IDisposable
{
    private bool _running = false;
    
    private GL _gl;
    private IWindow _window;
    private IInputContext _input;
    private Game _game;
    private List<Key> _downKeysThisFrame = new();
    private List<Key> _upKeysThisFrame = new();

    public Quad LocalQuad;

    public GL Gl => _gl;
    public IWindow SilkWindow => _window;
    public IInputContext Input => _input;
    
    public Vector2 SpriteOffset = Vector2.Zero;

    public RenderTexture Viewport;

    public event Action? Load
    {
        add => _window.Load += value;
        remove => _window.Load -= value;
    }

    public event Action<double>? Update
    {
        add => _window.Update += value;
        remove => _window.Update -= value;
    }

    public event Action<double>? RenderInViewport = null;

    public GameWindow(Game game, WindowOptions options, Size viewportSize)
    {
        _window = Window.Create(options);
        _game = game;

        _window.Load += () =>
        {
            //_gl = _window.CreateOpenGL();
            _gl = GL.GetApi(_window);
            LocalQuad = new Quad(_gl);
            _input = (_game.MainWindow == null || _game.MainWindow.Input == null) ? _window.CreateInput() : _game.MainWindow.Input;
            
            _input.Keyboards[0].KeyDown += (keyboard, key, keycode) =>
            {
                if (!_downKeysThisFrame.Contains(key))
                    _downKeysThisFrame.Add(key);
            };
            
            _input.Keyboards[0].KeyUp += (keyboard, key, keycode) =>
            {
                if (!_upKeysThisFrame.Contains(key))
                    _upKeysThisFrame.Add(key);
            };
            
            _window.MakeCurrent();
            Viewport = new RenderTexture(_gl, viewportSize.Width, viewportSize.Height);
            _gl.Disable(EnableCap.DepthTest); // Disable depth testing for 2D rendering
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            Console.WriteLine($"Created GameWindow with Viewport Size: {viewportSize.Width}x{viewportSize.Height}");
        };

        _window.Render += OnRenderWindow;

        _game = game;
    }
    
    public void OnRenderWindow(double delta)
    {
        _window.MakeCurrent();

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, Viewport.Framebuffer);
        _gl.Viewport(0, 0, (uint)Viewport.Width, (uint)Viewport.Height);
        RenderInViewport?.Invoke(delta);

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        _gl.Viewport(0, 0, (uint)SilkWindow.Size.X, (uint)SilkWindow.Size.Y);

        _gl.ClearColor(Color.FromArgb(100, 149, 237));
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _game.FullScreenShader.Use();

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, Viewport.Texture);
        _game.FullScreenShader.SetInteger("spriteTexture", 0);
        _game.FullScreenShader.SetVector4("tintColor", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        LocalQuad.Draw(Gl);

        _downKeysThisFrame.Clear();
        _upKeysThisFrame.Clear();
        
        if (!_running) SilkWindow.SwapBuffers();
    }
    
    /// <summary>
    /// Convert screen position to world position.
    ///
    /// Previous:
    /// X--->
    /// Y
    /// |
    /// |
    ///
    /// After:
    ///
    ///   |
    ///   Y
    ///   X-->
    ///
    /// 
    /// </summary>
    /// <param name="pos">To convert</param>
    /// <returns></returns>
    public Vector2 ScreenPosToWorld(Vector2 pos)
        => ViewportPosToWorld(pos / new Vector2((float)SilkWindow.Size.X / Viewport.Width, (float)SilkWindow.Size.Y / Viewport.Height));
    
    public Vector2 ViewportPosToWorld(Vector2 pos)
    {
        return new Vector2(
            pos.X - Viewport.Width / 2.0f,
            Viewport.Height / 2.0f - pos.Y
        );
    }
    
    public Vector2 WorldToViewportPos(Vector2 pos)
    {
        return new Vector2(
            pos.X + Viewport.Width / 2.0f,
            Viewport.Height / 2.0f - pos.Y
        );
    }

    public void DrawSprite(Sprite sprite, float x, float y, Color tint, float scaleX = 1.0f, float scaleY = 1.0f)
    {
        sprite.Draw(this, x, y, tint, scaleX, scaleY);
    }

    public void Run()
    {
        _running = true;
        _window.Run();
    }

    public void Dispose()
    {
        Viewport.Dispose();
        _window.Dispose();
    }
    
    public bool IsKey(Key key)
    {
        return _input.Keyboards[0].IsKeyPressed(key);
    }
    
    public bool IsKeyDown(Key key)
    {
        return _downKeysThisFrame.Contains(key);
    }
    
    public bool IsKeyUp(Key key)
    {
        return _upKeysThisFrame.Contains(key);
    }
    
    public Vector2 GetMousePosition()
    {
        var mouse = _input.Mice[0];
        return new Vector2((float)mouse.Position.X, (float)mouse.Position.Y);
    }
}