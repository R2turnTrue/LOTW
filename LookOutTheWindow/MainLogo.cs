using System.Drawing;

namespace LookOutTheWindow;

public class MainLogo : IDisposable
{
    public bool Visible = true;
    public Sprite FsCharacter;
    public Sprite GameLogo;
    public Sprite Tutorial;
    
    private float _characterOffset = 0.0f;
    private float _logoOffset = 0.0f;
    private float _tutOffset = 0.0f;
    
    public void Load()
    {
        FsCharacter = new Sprite(Game.Instance, "assets/sprites/fs_character.png");
        GameLogo = new Sprite(Game.Instance, "assets/sprites/logo.png");
        Tutorial = new Sprite(Game.Instance, "assets/sprites/tut.png");

        _characterOffset = -240.0f;
        _logoOffset = 110.0f;
    }
    
    public void Update(GameWindow window, float deltaTime)
    {
        var characterTarget = -240.0f;
        var logoTarget = 110.0f;
        var tutTarget = 0.0f;
        
        if (Visible)
        {
            characterTarget = 0.0f;
            logoTarget = 0.0f;
            tutTarget = 100.0f;
        }
        
        _characterOffset = float.Lerp(_characterOffset, characterTarget, deltaTime * 3f);
        _logoOffset = float.Lerp(_logoOffset, logoTarget, deltaTime * 3f);
        _tutOffset = float.Lerp(_tutOffset, tutTarget, deltaTime * 3f);
    }
    
    public void DrawBeforeFrost(GameWindow window, float deltaTime)
    {
        FsCharacter.Draw(window, 0.0f, 0.0f + _characterOffset, Color.White, ignoreOffset: true);
    }
    
    public void DrawAfterFrost(GameWindow window, float deltaTime)
    {
        GameLogo.Draw(window, 0.0f, 0.0f + _logoOffset, Color.White, ignoreOffset: true);
        
        if (!Visible)
        {
            Tutorial.Draw(window, 0.0f + _tutOffset, 0.0f, Color.White, ignoreOffset: false);
        }
    }

    public void Dispose()
    {
        FsCharacter.Dispose();
        GameLogo.Dispose();
        Tutorial.Dispose();
    }
}