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
    private Texture2D _bg, _iconHome, _settingBorder, _panelCard, _thumbCircle;
    private SoundEffect _sfxClick;

    private readonly Color _terracotta  = new(194, 74, 47);
    private readonly Color _burntSienna = new(233, 116, 81);
    private readonly Color _cream       = new(255, 248, 225);
    private readonly Color _gold        = new(212, 175, 55);
    private readonly Color _darkBrown   = new(62, 39, 35);

    private const int BorderW = 16;
    private const int PanelW  = 560;
    private const int PanelH  = 500;

    private int _panelX, _panelY;
    private Rectangle _panelRect;

    private const int TrackW = 300;
    private const int TrackH = 8;
    private const int ThumbSz = 20;
    private Rectangle _bgmTrack, _bgmThumb;
    private Rectangle _sfxTrack, _sfxThumb;
    private bool _draggingBgm, _draggingSfx;

    private Rectangle _langIdBtn, _langEnBtn;
    private bool _hoverLangId, _hoverLangEn;

    private Rectangle _resetBtn;
    private bool _hoverReset;

    private bool _showResetPopup;
    private Rectangle _popupRect, _popupYesBtn, _popupNoBtn;
    private bool _hoverPopupYes, _hoverPopupNo;

    private Rectangle _backRect;
    private bool _hoverBack;

    private float _inputCooldown;

    private AudioManager Audio => _game.Audio;
    private SaveManager Save => _game.Save;

    public SettingsScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font         = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _settingsFont = _game.Content.Load<SpriteFont>(FontPath.SettingsFont);
        _bg           = _game.Content.Load<Texture2D>("Images/UI/menu_bg");
        _iconHome     = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _settingBorder= _game.Content.Load<Texture2D>("Images/UI/setting_border");
        _panelCard    = _game.Content.Load<Texture2D>("Images/UI/panel_card");
        _sfxClick     = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");

        if (_thumbCircle == null)
        {
            _thumbCircle = new Texture2D(_game.GraphicsDevice, ThumbSz, ThumbSz);
            var data = new Color[ThumbSz * ThumbSz];
            var c = new Vector2(ThumbSz / 2f, ThumbSz / 2f);
            float r = ThumbSz / 2f - 1;
            for (int y = 0; y < ThumbSz; y++)
                for (int x = 0; x < ThumbSz; x++)
                    if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) <= r)
                        data[y * ThumbSz + x] = Color.White;
            _thumbCircle.SetData(data);
        }

        _panelX = (_game.Width  - PanelW) / 2;
        _panelY = (_game.Height - PanelH) / 2;
        _panelRect = new Rectangle(_panelX, _panelY, PanelW, PanelH);

        var cx = _panelX + PanelW / 2;

        _bgmTrack = new Rectangle(cx - TrackW / 2, _panelY + 120, TrackW, TrackH);
        _bgmThumb = new Rectangle(0, _panelY + 120 + (TrackH - ThumbSz) / 2, ThumbSz, ThumbSz);

        _sfxTrack = new Rectangle(cx - TrackW / 2, _panelY + 210, TrackW, TrackH);
        _sfxThumb = new Rectangle(0, _panelY + 210 + (TrackH - ThumbSz) / 2, ThumbSz, ThumbSz);

        var btnW = 190;
        var btnH = 42;
        int halfGap = 10;
        _langIdBtn = new Rectangle(cx - btnW - halfGap, _panelY + 305, btnW, btnH);
        _langEnBtn = new Rectangle(cx + halfGap, _panelY + 305, btnW, btnH);

        _resetBtn = new Rectangle(cx - 110, _panelY + PanelH - 76, 220, 42);

        UpdateThumbs();
    }

    private void UpdateThumbs()
    {
        _bgmThumb.X = (int)(_bgmTrack.X + (_bgmTrack.Width - ThumbSz) * Audio.BgmVolume);
        _sfxThumb.X = (int)(_sfxTrack.X + (_sfxTrack.Width - ThumbSz) * Audio.SfxVolume);
    }

    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        var touch = _game.GetTouch();
        var mp = touch.Position;

        _hoverBack    = _backRect.Contains(mp);
        _hoverLangId  = _langIdBtn.Contains(mp);
        _hoverLangEn  = _langEnBtn.Contains(mp);
        _hoverReset   = _resetBtn.Contains(mp);

        if (_showResetPopup)
        {
            if (!touch.IsDown) return;

            _hoverPopupYes = _popupYesBtn.Contains(mp);
            _hoverPopupNo  = _popupNoBtn.Contains(mp);

            if (_hoverPopupYes)
            {
                _game.Audio.PlaySfx(_sfxClick);
                Save.ResetAll();
                _showResetPopup = false;
                _inputCooldown = GameConfig.ClickCooldown;
            }
            else if (_hoverPopupNo)
            {
                _game.Audio.PlaySfx(_sfxClick);
                _showResetPopup = false;
                _inputCooldown = GameConfig.ClickCooldown;
            }
            return;
        }

        // slider dragging
        var inBgm = _bgmTrack.Contains(mp) || _bgmThumb.Contains(mp);
        var inSfx = _sfxTrack.Contains(mp) || _sfxThumb.Contains(mp);

        if (touch.IsDown)
        {
            if (inBgm || _draggingBgm)
            {
                _draggingBgm = true;
                Audio.BgmVolume = Math.Clamp((mp.X - _bgmTrack.X) / (float)TrackW, 0f, 1f);
                UpdateThumbs();
            }
            else if (inSfx || _draggingSfx)
            {
                _draggingSfx = true;
                Audio.SfxVolume = Math.Clamp((mp.X - _sfxTrack.X) / (float)TrackW, 0f, 1f);
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

            if (_hoverReset)
            {
                _game.Audio.PlaySfx(_sfxClick);
                _showResetPopup = true;
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
        var loc = _game.Loc;

        b.Begin();

        // background (senja)
        b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);

        // dark overlay
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.7f);

        DrawPanel(b);

        // title
        var title = loc.Get("settings_title");
        var tSz = _settingsFont.MeasureString(title);
        b.DrawString(_settingsFont, title, new Vector2(
            _panelX + (PanelW - tSz.X) / 2 + 1, _panelY + 30 + 1), Color.Black * 0.6f);
        b.DrawString(_settingsFont, title, new Vector2(
            _panelX + (PanelW - tSz.X) / 2, _panelY + 30), _gold);

        DrawSlider(b, loc.Get("bgm_volume"), _bgmTrack, Audio.BgmVolume, _panelY + 88);
        DrawSlider(b, loc.Get("sfx_volume"), _sfxTrack, Audio.SfxVolume, _panelY + 178);

        // language label
        var langLabel = loc.Get("language");
        var lSz = _settingsFont.MeasureString(langLabel);
        b.DrawString(_settingsFont, langLabel, new Vector2(
            _panelX + (PanelW - lSz.X) / 2, _panelY + 272), _cream);

        DrawLangButtons(b);

        DrawTerracottaButton(b, _resetBtn, loc.Get("reset_progress"),
            _hoverReset, _terracotta, _burntSienna, 0.85f);

        // home icon
        var iconSz = 48;
        var iconX = _game.Width - iconSz - 12;
        _backRect = new Rectangle(iconX, 12, iconSz, iconSz);
        var iCol = _hoverBack ? _gold : Color.White * 0.8f;
        b.Draw(_iconHome, _backRect, iCol);

        b.End();

        // popup overlay
        if (_showResetPopup)
            DrawResetPopup(b);
    }

    private void DrawPanel(SpriteBatch b)
    {
        b.Draw(_settingBorder, _panelRect,
            new Rectangle(0, 0, _settingBorder.Width, _settingBorder.Height),
            Color.White);
    }

    private void DrawSlider(SpriteBatch b, string label, Rectangle track, float val, int labelY)
    {
        // label
        var lSz = _settingsFont.MeasureString(label);
        b.DrawString(_settingsFont, label, new Vector2(
            _panelX + (PanelW - lSz.X) / 2, labelY), _cream);

        // track bg
        b.Draw(_game.WhitePixel, track, new Color(40, 30, 20, 180));

        // track fill
        int fillW = (int)(TrackW * val);
        if (fillW > 0)
            b.Draw(_game.WhitePixel,
                new Rectangle(track.X, track.Y, fillW, track.Height),
                _gold * 0.85f);

        // thumb
        int tx = (int)(track.X + (TrackW - ThumbSz) * val);
        int ty = track.Y + (track.Height - ThumbSz) / 2;
        b.Draw(_thumbCircle, new Rectangle(tx, ty, ThumbSz, ThumbSz), _cream);

        // percentage
        var pct = $"{(int)(val * 100)}%";
        var pSz = _settingsFont.MeasureString(pct);
        b.DrawString(_settingsFont, pct,
            new Vector2(track.X + TrackW + 14, track.Y - 1),
            new Color(255, 253, 208));
    }

    private void DrawLangButtons(SpriteBatch b)
    {
        var current = _game.Loc.CurrentLang;

        DrawTerracottaButton(b, _langIdBtn, "Indonesia",
            _hoverLangId, _terracotta, _burntSienna, 0.85f,
            current == "id" ? _gold : null);

        DrawTerracottaButton(b, _langEnBtn, "English",
            _hoverLangEn, _terracotta, _burntSienna, 0.85f,
            current == "en" ? _gold : null);
    }

    private void DrawTerracottaButton(SpriteBatch b, Rectangle r, string text,
        bool hover, Color idleColor, Color hoverColor, float textScale,
        Color? activeOutline = null)
    {
        float ba = activeOutline.HasValue ? 1f : 0.55f;
        var bg = hover ? hoverColor : idleColor;

        b.Draw(_game.WhitePixel, r, bg * ba);
        b.Draw(_game.WhitePixel,
            new Rectangle(r.X, r.Y, r.Width, 2), Color.White * 0.2f * ba);
        b.Draw(_game.WhitePixel,
            new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), Color.Black * 0.2f * ba);

        if (activeOutline.HasValue)
        {
            b.Draw(_game.WhitePixel,
                new Rectangle(r.X, r.Y, r.Width, 2), activeOutline.Value * 0.8f);
            b.Draw(_game.WhitePixel,
                new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), activeOutline.Value * 0.8f);
            b.Draw(_game.WhitePixel,
                new Rectangle(r.X, r.Y, 2, r.Height), activeOutline.Value * 0.5f);
            b.Draw(_game.WhitePixel,
                new Rectangle(r.X + r.Width - 2, r.Y, 2, r.Height), activeOutline.Value * 0.5f);
        }

        var tCol = activeOutline.HasValue ? _cream : _cream * 0.7f;
        var sz = _settingsFont.MeasureString(text);
        var tx = r.X + (r.Width  - sz.X * textScale) / 2;
        var ty = r.Y + (r.Height - sz.Y * textScale) / 2;
        b.DrawString(_settingsFont, text, new Vector2(tx, ty), tCol,
            0, Vector2.Zero, textScale, SpriteEffects.None, 0);
    }

    private void DrawResetPopup(SpriteBatch b)
    {
        var loc = _game.Loc;

        // overlay
        b.Begin();
        b.Draw(_game.WhitePixel,
            new Rectangle(0, 0, _game.Width, _game.Height),
            Color.Black * 0.6f);

        // popup panel
        int pw = 420;
        int ph = 160;
        int px = (_game.Width  - pw) / 2;
        int py = (_game.Height - ph) / 2;
        _popupRect = new Rectangle(px, py, pw, ph);

        b.Draw(_panelCard, _popupRect, Color.White);

        // inner dim
        b.Draw(_game.WhitePixel, _popupRect, Color.Black * 0.2f);

        // confirm text
        var confirm = loc.Get("confirm_reset");
        var cSz = _settingsFont.MeasureString(confirm);
        b.DrawString(_settingsFont, confirm,
            new Vector2(px + (pw - cSz.X) / 2, py + 28),
            new Color(255, 248, 225));

        // yes / no buttons
        int btnW = 140;
        int btnH = 40;
        int gap = 20;
        int btnY = py + ph - btnH - 24;
        int btnCX = px + pw / 2;
        _popupYesBtn = new Rectangle(btnCX - btnW - gap / 2, btnY, btnW, btnH);
        _popupNoBtn  = new Rectangle(btnCX + gap / 2, btnY, btnW, btnH);

        _hoverPopupYes = _popupYesBtn.Contains(_game.GetTouch().Position);
        _hoverPopupNo  = _popupNoBtn.Contains(_game.GetTouch().Position);

        DrawTerracottaButton(b, _popupYesBtn, loc.Get("yes"),
            _hoverPopupYes, _terracotta, _burntSienna, 0.85f);

        var grayIdle  = new Color(80, 70, 60);
        var grayHover = new Color(120, 105, 90);
        DrawTerracottaButton(b, _popupNoBtn, loc.Get("no"),
            _hoverPopupNo, grayIdle, grayHover, 0.85f);

        b.End();
    }

    public void Unload()
    {
        Save.Data.BgmVolume = Audio.BgmVolume;
        Save.Data.SfxVolume = Audio.SfxVolume;
        Save.Save();
    }
}
