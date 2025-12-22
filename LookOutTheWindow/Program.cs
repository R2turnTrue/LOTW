namespace LookOutTheWindow;

class Program
{
    static void Main(string[] args)
    {
        #if RELEASE
        try
        {
        #endif
        
            Game.Instance.Run();
        
        #if RELEASE
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled Exception: {ex}");
        }
        #endif
    }
}