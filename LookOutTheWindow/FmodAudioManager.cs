using FMOD.Studio;

namespace LookOutTheWindow;

/// <summary>
/// FMOD Audio Manager.
/// LOTS OF VIBE CODING
/// </summary>
public class FmodAudioManager
{
    public static FmodAudioManager Instance { get; private set; }

    private FMOD.Studio.System studioSystem;
    private FMOD.System coreSystem;

    private Dictionary<string, EventInstance> bgmMap = new();

    public void Init()
    {
        if (Instance != null)
            return;

        Instance = this;

        FMOD.Studio.System.create(out studioSystem);

        studioSystem.getCoreSystem(out coreSystem);

        studioSystem.initialize(
            maxchannels: 1024,
            flags: FMOD.INITFLAGS.NORMAL,
            studioflags: FMOD.Studio.INITFLAGS.NORMAL,
            extradriverdata: IntPtr.Zero
        );

        LoadBanks();

        Console.WriteLine("FMOD Initialized");
    }

    private void LoadBanks()
    {
        studioSystem.loadBankFile("assets/banks/Master.bank", LOAD_BANK_FLAGS.NORMAL, out _);
        studioSystem.loadBankFile("assets/banks/Master.strings.bank", LOAD_BANK_FLAGS.NORMAL, out _);
    }

    public void PlayBGM(string eventPath)
    {
        if (bgmMap.ContainsKey(eventPath))
            return;

        studioSystem.getEvent(eventPath, out EventDescription desc);

        desc.createInstance(out EventInstance instance);
        instance.start();

        bgmMap.Add(eventPath, instance);
    }

    public void StopBGM(string eventPath, bool fadeOut = true)
    {
        if (!bgmMap.TryGetValue(eventPath, out var instance))
            return;

        instance.stop(fadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
        instance.release();

        bgmMap.Remove(eventPath);
    }

    public void StopAllBGM(bool fadeOut = true)
    {
        foreach (var inst in bgmMap.Values)
        {
            inst.stop(fadeOut ? STOP_MODE.ALLOWFADEOUT : STOP_MODE.IMMEDIATE);
            inst.release();
        }

        bgmMap.Clear();
    }

    public void PlaySFX(string eventPath)
    {
        studioSystem.getEvent(eventPath, out EventDescription desc);

        desc.createInstance(out EventInstance instance);
        instance.start();
        instance.release();
    }

    public void Update()
    {
        studioSystem.update();
    }

    public void Shutdown()
    {
        StopAllBGM(false);

        studioSystem.unloadAll();
        studioSystem.release();

        Console.WriteLine("FMOD Shutdown");
    }
}