using System.Drawing;
using System.Numerics;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using TiledSharp;

namespace LookOutTheWindow;

public class MainScene : Scene
{
    public Player PlayerObj;
    public GameTilemap Tilemap;
    public FrostSystem FrostSys;
    public Hangju HangjuObj;
    public ParticleEmitterSystem ParticleSys;

    public Sprite GlassFrame;
    public Sprite FinalCredit;
    
    public GameTilemap.ImportedTilemap? LastTilemap = null;
    
    public MainLogo Logo;
    //public GameWindow SubWindow;
    
    public override void Load()
    {
        Tilemap = new GameTilemap(this);
        Tilemap.Load();
        
        PlayerObj = new Player(this);
        PlayerObj.Load();
        
        FrostSys = new FrostSystem();
        FrostSys.Load();
        
        HangjuObj = new Hangju(FrostSys);
        HangjuObj.Load();
        
        ParticleSys = new ParticleEmitterSystem();
        ParticleSys.Load();
        
        GlassFrame = new Sprite(Game.Instance, "assets/sprites/glass_frame.png");
        FinalCredit = new Sprite(Game.Instance, "assets/sprites/finalcredit.png");
        
        Logo = new MainLogo();
        Logo.Load();
        
        var mw = Game.Instance.MainWindow;
        PlayerObj.Position = mw.ViewportPosToWorld(Tilemap.TiledMaps[0].Spawn);
        //
        // // SubWindow = new GameWindow(Game.Instance,
        // //     WindowOptions.Default with
        // //     {
        // //         Size = new Silk.NET.Maths.Vector2D<int>(256, 256),
        // //         Title = "spyglass",
        // //         SharedContext = Game.Instance.MainWindow.SilkWindow.GLContext
        // //     },
        // //     new Size(128, 128));
        // // SubWindow.RenderInViewport += DrawSpyglassViewport;
        // // SubWindow.SilkWindow.Initialize();
        //
        // Game.Instance.ChildWindows.Add(SubWindow);
    }
    
    // public void DrawSpyglassViewport(double deltaTime)
    // {
    //     var gl = Game.Instance.Gl;
    //     
    //     gl.ClearColor(Game.ClearColor);
    //     gl.Clear(ClearBufferMask.ColorBufferBit);
    //     
    //     var posSub = new Vector2(SubWindow.SilkWindow.Position.X, SubWindow.SilkWindow.Position.Y);
    //     var posMain = new Vector2(Game.Instance.MainWindow.SilkWindow.Position.X, Game.Instance.MainWindow.SilkWindow.Position.Y);
    //     var offset = new Vector2(posSub.X - posMain.X, - (posSub.Y - posMain.Y));
    //
    //     var scaleRatio = new Vector2(SubWindow.Viewport.Width, SubWindow.Viewport.Height) /
    //                      new Vector2(SubWindow.SilkWindow.Size.X, SubWindow.SilkWindow.Size.Y);
    //
    //     SubWindow.SpriteOffset = Game.Instance.MainWindow.SpriteOffset + offset * scaleRatio;
    //     
    //     Tilemap.Draw(SubWindow, deltaTime);
    //     PlayerObj.Draw(SubWindow);
    //     PlayerObj.DrawOutline(SubWindow);
    //     HangjuObj.Draw(SubWindow);
    // }

    public override void Render(double deltaTime)
    {
        var gl = Game.Instance.Gl;
        
        var mw = Game.Instance.MainWindow;
        
        LastTilemap = Tilemap.GetMapContainingPoint(
            mw.WorldToViewportPos(PlayerObj.Position));
        if (LastTilemap.HasValue)
        {
            mw.SpriteOffset = 
                Vector2.Lerp(mw.SpriteOffset, -LastTilemap.Value.Offset, 6 * (float)deltaTime);
        }
        
        ParticleSys.Update(Game.Instance.MainWindow, (float) deltaTime);
        FrostSys.Update(deltaTime);
        Tilemap.Update(Game.Instance.MainWindow, deltaTime, LastTilemap);
        Tilemap.Draw(Game.Instance.MainWindow, deltaTime);
        FinalCredit.Draw(mw, Tilemap.TiledMaps[^1].Offset.X, 0.0f, Color.White);
        
        PlayerObj.Update(Game.Instance.MainWindow, deltaTime);
        if (!PlayerObj.CurrentFrost)
        {
            Logo.Visible = false;
        }
        PlayerObj.Draw(Game.Instance.MainWindow);
        
        ParticleSys.Draw(Game.Instance.MainWindow, (float) deltaTime);
        
        Logo.Update(mw, (float) deltaTime);
        Logo.DrawBeforeFrost(mw, (float) deltaTime);
        
        DrawFrost();
        
        //Tutorial.Draw(mw, 0.0f, 0.0f, Color.White);
        
        PlayerObj.DrawOutline(Game.Instance.MainWindow);
        
        Logo.DrawAfterFrost(mw, (float) deltaTime);
        
        HangjuObj.Update(Game.Instance.MainWindow, deltaTime);
        HangjuObj.Draw(Game.Instance.MainWindow);
        
        GlassFrame.Draw(
            mw,
            0.0f - mw.SpriteOffset.X,
            0.0f - mw.SpriteOffset.Y,
            Color.FromArgb(255, 255, 255, 255));
        
        if (Game.Instance.MainWindow.IsKeyDown(Silk.NET.Input.Key.Number7))
        {
            var oldPos = PlayerObj.Position;
            // Reload Whole Scene
            Dispose();
            Load();
            PlayerObj.Position = oldPos;
        }
        
        if (Game.Instance.MainWindow.IsKeyDown(Silk.NET.Input.Key.R))
        {
            if (LastTilemap.HasValue)
                Tilemap.ReloadSpecificMap(LastTilemap.Value);
            PlayerObj.Die(Game.Instance.MainWindow);
        }
        
        if (Game.Instance.MainWindow.IsKeyDown(Silk.NET.Input.Key.Number8))
        {
            PlayerObj.Position += new Vector2(256.0f, 0.0f);
        }
    }
    
    public void DrawFrost()
    {
        var mw = Game.Instance.MainWindow;
        var quad = mw.LocalQuad;
        var shader = Game.Instance.FrostShader;
        var gl = Game.Instance.Gl;
        
        shader.Use();
        shader.SetInteger("frostTexture", 0);
        
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.Texture2D, FrostSys.Texture);
        quad.Draw(gl);
    }

    public override void Dispose()
    {
        Tilemap.Dispose();
        PlayerObj.Dispose();
        FrostSys.Dispose();
        HangjuObj.Dispose();
        // SubWindow.Dispose();

        GlassFrame.Dispose();
        //Tutorial.Dispose();
        FinalCredit.Dispose();
        
        Logo.Dispose();
        
        Game.Instance.ChildWindows.Clear();
    }
}