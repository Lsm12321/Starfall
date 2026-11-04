using UnityEngine;

public static class GameSettings
{
    public const int DefaultMaster = 80;
    public const int DefaultMusic = 80;
    public const int DefaultSfx = 80;
    public const int DefaultQuality = 1;
    public const int DefaultFps = 60;
    public const int DefaultShake = 1;
    public const int DefaultParticles = 1;

    public static int Master = DefaultMaster;
    public static int Music = DefaultMusic;
    public static int Sfx = DefaultSfx;
    public static int Quality = DefaultQuality;
    public static int Fps = DefaultFps;
    public static bool DamageNumbers = true;
    public static int Shake = DefaultShake;
    public static int Particles = DefaultParticles;
    public static int Language = 0;
    public static bool AutoPause = true;

    public static void Load()
    {
        bool hadMaster = PlayerPrefs.HasKey("Starfall.Settings.Master");
        Master = Mathf.Clamp(PlayerPrefs.GetInt("Starfall.Settings.Master", PlayerPrefs.GetInt("Starfall.Muted", 0) == 1 ? 0 : DefaultMaster), 0, 100);
        Music = Mathf.Clamp(PlayerPrefs.GetInt("Starfall.Settings.Music", DefaultMusic), 0, 100);
        Sfx = Mathf.Clamp(PlayerPrefs.GetInt("Starfall.Settings.Sfx", DefaultSfx), 0, 100);
        Quality = Mathf.Clamp(PlayerPrefs.GetInt("Starfall.Settings.Quality", DefaultQuality), 0, 2);
        Fps = PlayerPrefs.GetInt("Starfall.Settings.Fps", DefaultFps) == 30 ? 30 : PlayerPrefs.GetInt("Starfall.Settings.Fps", DefaultFps) == 120 ? 120 : 60;
        DamageNumbers = PlayerPrefs.GetInt("Starfall.Settings.DamageNumbers", 1) != 0;
        Shake = Mathf.Clamp(PlayerPrefs.GetInt("Starfall.Settings.Shake", DefaultShake), 0, 2);
        Particles = Mathf.Clamp(PlayerPrefs.GetInt("Starfall.Settings.Particles", DefaultParticles), 0, 2);
        Language = PlayerPrefs.GetInt("Starfall.Settings.Language", 0) == 1 ? 1 : 0;
        AutoPause = PlayerPrefs.GetInt("Starfall.Settings.AutoPause", 1) != 0;
        if (!hadMaster && PlayerPrefs.GetInt("Starfall.Muted", 0) == 1) Master = 0;
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("Starfall.Settings.Master", Master);
        PlayerPrefs.SetInt("Starfall.Settings.Music", Music);
        PlayerPrefs.SetInt("Starfall.Settings.Sfx", Sfx);
        PlayerPrefs.SetInt("Starfall.Settings.Quality", Quality);
        PlayerPrefs.SetInt("Starfall.Settings.Fps", Fps);
        PlayerPrefs.SetInt("Starfall.Settings.DamageNumbers", DamageNumbers ? 1 : 0);
        PlayerPrefs.SetInt("Starfall.Settings.Shake", Shake);
        PlayerPrefs.SetInt("Starfall.Settings.Particles", Particles);
        PlayerPrefs.SetInt("Starfall.Settings.Language", Language);
        PlayerPrefs.SetInt("Starfall.Settings.AutoPause", AutoPause ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void Apply()
    {
        Localization.SetLanguage(Language);
        AudioListener.volume = Master / 100f;
        SoundBank.SetMix(Music / 100f, Sfx / 100f);
        Application.targetFrameRate = Fps;
        Effects.ShowDamageNumbers = DamageNumbers;
        Effects.ParticleDensity = Particles == 0 ? .55f : Particles == 2 ? 1.35f : 1f;
        GameBootstrap.ScreenShakeScale = Shake == 0 ? 0f : Shake == 2 ? .75f : .4f;
        int levels = QualitySettings.names.Length;
        if (levels > 0) QualitySettings.SetQualityLevel(Mathf.Clamp(Quality, 0, levels - 1), true);
    }
}
