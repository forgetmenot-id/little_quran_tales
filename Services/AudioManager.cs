using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace LittleQuranTales.Services;

public class AudioManager
{
    private Song _currentBgm;
    private float _bgmVolume = 0.5f;
    private float _sfxVolume = 0.5f;

    public float BgmVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = Math.Clamp(value, 0f, 1f);
            if (MediaPlayer.State == MediaState.Playing)
                MediaPlayer.Volume = _bgmVolume;
        }
    }

    public float SfxVolume
    {
        get => _sfxVolume;
        set => _sfxVolume = Math.Clamp(value, 0f, 1f);
    }

    public void PlayBgm(Song song, bool repeat = true)
    {
        if (song == null) return;
        if (song == _currentBgm && MediaPlayer.State == MediaState.Playing) return;

        if (MediaPlayer.State != MediaState.Stopped)
            MediaPlayer.Stop();

        _currentBgm = song;
        MediaPlayer.IsRepeating = repeat;
        MediaPlayer.Volume = _bgmVolume;
        MediaPlayer.Play(song);
    }

    public void StopBgm()
    {
        if (MediaPlayer.State != MediaState.Stopped)
            MediaPlayer.Stop();
        _currentBgm = null;
    }

    public void PlaySfx(SoundEffect sfx)
    {
        if (sfx == null) return;
        sfx.Play(_sfxVolume, 0f, 0f);
    }
}
