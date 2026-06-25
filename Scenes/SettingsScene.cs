using System;
using LittleQuranTales.Data;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LittleQuranTales.Scenes;

public class SettingsScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private SpriteFont _settingsFont;
    private Texture2D _bg, _iconHome, _settingBorder;
    private SoundEffect _sfxClick;

    private readonly Color _cream = new(234, 230, 223);
    private readonly Color _accent = new(212, 175, 55);
    private readonly Color _darkAccent = new(170, 140, 40);
    private readonly Color _creamDim = new(200, 190, 170);
    private readonly Color _overlay = new(0, 0, 0, 180);

    private float _inputCooldown;

    private Rectangle _backRect;
    private bool _hoverBack;

    private Rectangle _bgmTrack, _bgmThumb;
    private Rectangle _sfxTrack, _sfxThumb;
    private bool _draggingBgm, _draggingSfx;

    private Rectangle _langIdBtn, _langEnBtn;
    private bool _hoverLangId, _hoverLangEn;

    private int _panelX, _panelY, _panelW, _panelH;

    private AudioManager Audio => _game.Audio;
    private SaveManager Save => _game.Save;

    private const int BorderW = 16;

    public SettingsScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _settingsFont = _game.Content.Load<SpriteFont>(FontPath.SettingsFont);
        _bg = _game.Content.Load<Texture2D>("Images/UI/menu_bg");
        _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _settingBorder = _game.Content.Load<Texture2D>("Images/UI/setting_border");
        _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");

        _panelW = 520;
        _panelH = 440;
        _panelX = (_game.Width - _panelW) / 2;
        _panelY = (_game.Height - _panelH) / 2;

        var cx = _panelX + _panelW / 2;
        var trackW = 280;
        var trackH = 10;
        var thumbSz = 20;

        _bgmTrack = new Rectangle(cx - trackW / 2, _panelY + 138, trackW, trackH);
        _bgmThumb = new Rectangle(0, _panelY + 138 - (thumbSz - trackH) / 2, thumbSz, thumbSz);

        _sfxTrack = new Rectangle(cx - trackW / 2, _panelY + 218, trackW, trackH);
        _sfxThumb = new Rectangle(0, _panelY + 218 - (thumbSz - trackH) / 2, thumbSz, thumbSz);

        var btnW = 200;
        var btnH = 44;
        var halfGap = 10;
        _langIdBtn = new Rectangle(cx - btnW - halfGap, _panelY + 335, btnW, btnH);
        _langEnBtn = new Rectangle(cx + halfGap, _panelY + 335, btnW, btnH);

        UpdateThumbs();
    }

    private void UpdateThumbs()
    {
        _bgmThumb.X = (int)(_bgmTrack.X + (_bgmTrack.Width - _bgmThumb.Width) * Audio.BgmVolume);
        _sfxThumb.X = (int)(_sfxTrack.X + (_sfxTrack.Width - _sfxThumb.Width) * Audio.SfxVolume);
    }

    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        var touch = _game.GetTouch();
        var mp = touch.Position;

        _hoverBack = _backRect.Contains(mp);
        _hoverLangId = _langIdBtn.Contains(mp);
        _hoverLangEn = _langEnBtn.Contains(mp);

        var inTrackBgm = _bgmTrack.Contains(mp) || _bgmThumb.Contains(mp);
        var inTrackSfx = _sfxTrack.Contains(mp) || _sfxThumb.Contains(mp);

        if (touch.IsDown)
        {
            if (inTrackBgm || _draggingBgm)
            {
                _draggingBgm = true;
                var rel = Math.Clamp((mp.X - _bgmTrack.X) / (float)_bgmTrack.Width, 0f, 1f);
                Audio.BgmVolume = rel;
                UpdateThumbs();
            }
            else if (inTrackSfx || _draggingSfx)
            {
                _draggingSfx = true;
                var rel = Math.Clamp((mp.X - _sfxTrack.X) / (float)_sfxTrack.Width, 0f, 1f);
                Audio.SfxVolume = rel;
                UpdateThumbs();
            }
        }
        else
        {
            _draggingBgm = false;
            _draggingSfx = false;
        }

        if (_inputCooldown > 0) return;

        if (touch.IsDown)
        {
            if (_hoverBack)
            {
                _game.Audio.PlaySfx(_sfxClick);
                _inputCooldown = GameConfig.ClickCooldown;
                _game.SceneManager.SwitchTo(SceneId.Menu);
                return;
            }

            if (_hoverLangId)
            {
                _game.Audio.PlaySfx(_sfxClick);
                SetLanguage("id");
                _inputCooldown = GameConfig.ClickCooldown;
            }
            else if (_hoverLangEn)
            {
                _game.Audio.PlaySfx(_sfxClick);
                SetLanguage("en");
                _inputCooldown = GameConfig.ClickCooldown;
            }
        }

        var kb = Keyboard.GetState();
        if ((kb.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) && _inputCooldown <= 0)
        {
            _game.Audio.PlaySfx(_sfxClick);
            _inputCooldown = GameConfig.ClickCooldown;
            _game.SceneManager.SwitchTo(SceneId.Menu);
        }
    }

    private void SetLanguage(string lang)
    {
        _game.Loc.SetLanguage(lang);
        Save.Data.Language = lang;
        Save.Save();
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        b.Begin();
        var loc = _game.Loc;

        b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), _overlay);

        DrawPanel(b);

        var title = loc.Get("settings_title");
        var tSz = _settingsFont.MeasureString(title);
        b.DrawString(_settingsFont, title, new Vector2(
            _panelX + (_panelW - tSz.X) / 2, _panelY + 28), _accent);

        DrawSlider(b, loc.Get("bgm_volume"), _bgmTrack, _bgmThumb, Audio.BgmVolume, _panelY + 95);

        DrawSlider(b, loc.Get("sfx_volume"), _sfxTrack, _sfxThumb, Audio.SfxVolume, _panelY + 175);

        var langLabel = loc.Get("language");
        var lSz = _settingsFont.MeasureString(langLabel);
        b.DrawString(_settingsFont, langLabel, new Vector2(
            _panelX + (_panelW - lSz.X) / 2, _panelY + 285), _cream);

        DrawLangButtons(b);

        var iconSz = 48;
        var iconX = _game.Width - iconSz - 12;
        _backRect = new Rectangle(iconX, 12, iconSz, iconSz);
        var iCol = _hoverBack ? _accent : Color.White * 0.8f;
        b.Draw(_iconHome, _backRect, iCol);

        b.End();
    }

    private void DrawPanel(SpriteBatch b)
    {
        var br = BorderW;
        var innerX = _panelX + br;
        var innerY = _panelY + br;
        var innerW = _panelW - br * 2;
        var innerH = _panelH - br * 2;

        b.Draw(_settingBorder, new Rectangle(_panelX, _panelY, _panelW, _panelH),
            new Rectangle(0, 0, _settingBorder.Width, _settingBorder.Height),
            Color.White);

        b.Draw(_game.WhitePixel, new Rectangle(innerX, innerY, innerW, innerH),
            new Color(38, 28, 22, 230));
    }

    private void DrawLangButtons(SpriteBatch b)
    {
        var current = _game.Loc.CurrentLang;

        var activeBg = new Color(62, 39, 35);
        var inactiveBg = new Color(45, 35, 28);

        DrawLangButton(b, _langIdBtn, "Indonesia", _hoverLangId,
            current == "id" ? activeBg : inactiveBg,
            current == "id");

        DrawLangButton(b, _langEnBtn, "English", _hoverLangEn,
            current == "en" ? activeBg : inactiveBg,
            current == "en");
    }

    private void DrawLangButton(SpriteBatch b, Rectangle r, string text, bool hover, Color bg, bool active)
    {
        b.Draw(_game.WhitePixel, r, bg);

        if (active)
        {
            b.Draw(_game.WhitePixel,
                new Rectangle(r.X, r.Y, r.Width, 2), _accent);
            b.Draw(_game.WhitePixel,
                new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), _accent);
        }

        var col = active ? _accent : (hover ? _cream : _creamDim);
        var sz = _settingsFont.MeasureString(text);
        b.DrawString(_settingsFont, text,
            new Vector2(r.X + (r.Width - sz.X) / 2, r.Y + (r.Height - sz.Y) / 2), col);
    }

    private void DrawSlider(SpriteBatch b, string label, Rectangle track, Rectangle thumb, float val, int labelY)
    {
        var lSz = _settingsFont.MeasureString(label);
        b.DrawString(_settingsFont, label, new Vector2(
            _panelX + (_panelW - lSz.X) / 2, labelY), _cream);

        b.Draw(_game.WhitePixel, track, new Color(60, 48, 38));
        var fillW = (int)(track.Width * val);
        if (fillW > 0)
            b.Draw(_game.WhitePixel, new Rectangle(track.X, track.Y, fillW, track.Height), _accent);

        b.Draw(_game.WhitePixel, thumb, Color.White);

        var pct = $"{(int)(val * 100)}%";
        var pSz = _settingsFont.MeasureString(pct);
        b.DrawString(_settingsFont, pct, new Vector2(track.X + track.Width + 14, track.Y - 2), _accent);
    }

    public void Unload()
    {
        Save.Data.BgmVolume = Audio.BgmVolume;
        Save.Data.SfxVolume = Audio.SfxVolume;
        Save.Save();
    }
}
