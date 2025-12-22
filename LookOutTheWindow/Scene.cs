namespace LookOutTheWindow;

public abstract class Scene : IDisposable
{
    public abstract void Load();
    public abstract void Render(double deltaTime);

    public abstract void Dispose();
}