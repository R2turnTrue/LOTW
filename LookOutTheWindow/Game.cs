using System.Drawing;
using LookOutTheWindow.Util;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace LookOutTheWindow;

public class Game : IDisposable
{
    public static Color ClearColor = Color.FromArgb(200, 230, 255);
    public static Game Instance { get; private set; } = new Game();

    public FmodAudioManager AudioManager { get; } = new FmodAudioManager();
    public GameWindow MainWindow;

    public Scene CurrentScene { get; set; }
    
    public Game()
    {
        MainWindow = new GameWindow(
            this,
            WindowOptions.Default with
            {
                Size = new Vector2D<int>(1024, 1024),
                Title = "game",
                WindowBorder = WindowBorder.Fixed
            },
            new Size(256, 256));
    }
    
    /// <summary>
    /// Child windows opened from the main window. MUST HAVE GLOBAL OPENGL AS SHARED CONTEXT!!
    /// </summary>
    public List<GameWindow> ChildWindows = new();

    public GL Gl => MainWindow.Gl;
    
    public ShaderProgram FrostShader { get; set; }
    public ShaderProgram SpriteShader { get; set; }
    public ShaderProgram FullScreenShader { get; set; }
    
    public void Run()
    {
        MainWindow.Load += Init;
        MainWindow.RenderInViewport += DrawViewport;
        MainWindow.Run();
    }

    public void DrawViewport(double dt)
    {
        foreach (var child in ChildWindows)
        {
            child.OnRenderWindow(dt);
        }
        
        MainWindow.SilkWindow.MakeCurrent();
        
        Gl.ClearColor(ClearColor);
        Gl.Clear(ClearBufferMask.ColorBufferBit);
        
        CurrentScene.Render(dt);
        
        AudioManager.Update();
    }

    public void Init()
    {
        AudioManager.Init();
        
        FrostShader = new ShaderProgram(Gl, 
            File.ReadAllText("assets/shaders/frost.vert"),
            File.ReadAllText("assets/shaders/frost.frag"));
        SpriteShader = new ShaderProgram(Gl, 
            File.ReadAllText("assets/shaders/sprite.vert"),
            File.ReadAllText("assets/shaders/sprite.frag"));
        FullScreenShader = new ShaderProgram(Gl, 
            File.ReadAllText("assets/shaders/fullscreen.vert"),
            File.ReadAllText("assets/shaders/fullscreen.frag"));
        
        CurrentScene = new MainScene();
        CurrentScene.Load();
        
        AudioManager.PlayBGM("event:/bgm/main_bgm");
    }

    public void Dispose()
    {
        MainWindow.Dispose();
        CurrentScene.Dispose();
        SpriteShader.Dispose();
        FullScreenShader.Dispose();
    }
}