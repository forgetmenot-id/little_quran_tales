using System;
using System.Collections.Generic;
using LittleQuranTales.Data;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace LittleQuranTales.Scenes;

public class LibraryScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private Texture2D _bg, _iconHome, _panelDialog, _settingBorder;
    private SoundEffect _sfxClick;
    private Song _menuBgm, _murottal;

    private readonly Color _cream = new(234, 230, 223);
    private readonly Color _darkBrown = new(62, 39, 35);
    private readonly Color _accent = new(212, 175, 55);

    private float _inputCooldown;
    private Rectangle _backRect;
    private bool _hoverBack;

    private LibraryItem[] _items;
    private int _selectedIndex = -1;

    private bool _reading;
    private SurahData _readingSurah;
    private float _scrollOffset, _playTimer;
    private int _prevScroll;
    private bool _isPlaying;

    private const float AyahGap = 100f;

    private SaveManager Save => _game.Save;
    private AudioManager Audio => _game.Audio;

    private class LibraryItem
    {
        public string Id;
        public bool IsSurah;
        public SurahData Surah;
    }

    public LibraryScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>("Fonts/GameFont");
        _bg = _game.Content.Load<Texture2D>("Images/UI/menu_bg");
        _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _panelDialog = _game.Content.Load<Texture2D>("Images/UI/panel_dialog");
        _settingBorder = _game.Content.Load<Texture2D>("Images/UI/setting_border");
        _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");
        _menuBgm = _game.Content.Load<Song>("Audio/BGM/bgm_menu");
        try { _murottal = _game.Content.Load<Song>("Audio/library/al-fil"); }
        catch { _murottal = null; }

        var list = new List<LibraryItem>();
        list.Add(new LibraryItem { Id = "prolog", IsSurah = false });
        foreach (var s in QuranSurahs.All)
            list.Add(new LibraryItem { Id = s.Id, IsSurah = true, Surah = s });
        _items = list.ToArray();

        _selectedIndex = -1;
        _reading = false;
        _readingSurah = null;
        _scrollOffset = 0;
        _isPlaying = false;
        _prevScroll = Mouse.GetState().ScrollWheelValue;
    }

    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);

        if (_isPlaying)
        {
            _playTimer += dt;
        }

        var touch = _game.GetTouch();
        var mp = touch.Position;
        _hoverBack = _backRect.Contains(mp);

        if (_reading)
        {
            if (_readingSurah?.Ayahs == null) { ExitReading(); return; }
            var scroll = Mouse.GetState().ScrollWheelValue;
            var delta = _prevScroll - scroll;
            _prevScroll = scroll;
            _scrollOffset += delta * 0.05f;
            var maxOff = Math.Max(0, _readingSurah.Ayahs.Length * AyahGap - 320);
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, maxOff);

            if (_inputCooldown > 0) return;

            if (touch.IsDown)
            {
                if (_hoverBack)
                {
                    ExitReading();
                    Audio.PlaySfx(_sfxClick);
                    _inputCooldown = 0.3f;
                    _game.SceneManager.SwitchTo("menu");
                    return;
                }

                var playBtn = new Rectangle(_game.Width - 150, _game.Height - 50, 130, 36);
                if (playBtn.Contains(mp) && _murottal != null)
                {
                    Audio.PlaySfx(_sfxClick);
                    _inputCooldown = 0.3f;
                    TogglePlayback();
                    return;
                }

                ExitReading();
                _inputCooldown = 0.2f;
            }

            if ((Keyboard.GetState().IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) && _inputCooldown <= 0)
            {
                ExitReading();
                _inputCooldown = 0.2f;
            }
            return;
        }

        _prevScroll = Mouse.GetState().ScrollWheelValue;

        if (_inputCooldown > 0) return;

        if (touch.IsDown)
        {
            if (_hoverBack)
            {
                Audio.PlaySfx(_sfxClick);
                _inputCooldown = 0.3f;
                _game.SceneManager.SwitchTo("menu");
                return;
            }

            if (_selectedIndex >= 0)
            {
                var detailRect = GetDetailRect();
                if (detailRect.Contains(mp))
                {
                    var item = _items[_selectedIndex];
                    if (IsUnlocked(item) && item.IsSurah && _items[_selectedIndex].Surah?.Ayahs?.Length > 0)
                    {
                        Audio.PlaySfx(_sfxClick);
                        _readingSurah = item.Surah;
                        _reading = true;
                        _scrollOffset = 0;
                        _prevScroll = Mouse.GetState().ScrollWheelValue;
                        _playTimer = 0;
                        _isPlaying = false;
                        _inputCooldown = 0.3f;
                    }
                }
                else
                {
                    _selectedIndex = -1;
                    _inputCooldown = 0.1f;
                }
                return;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (GetChapterRect(i).Contains(mp))
                {
                    Audio.PlaySfx(_sfxClick);
                    _selectedIndex = i;
                    _inputCooldown = 0.3f;
                    break;
                }
            }
        }

        var kb = Keyboard.GetState();
        if ((kb.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) && _inputCooldown <= 0)
        {
            if (_selectedIndex >= 0) { _selectedIndex = -1; _inputCooldown = 0.2f; }
            else
            {
                Audio.PlaySfx(_sfxClick);
                _inputCooldown = 0.3f;
                _game.SceneManager.SwitchTo("menu");
            }
        }
    }

    private bool IsUnlocked(LibraryItem item)
    {
        return item.Id == "prolog" || Save.IsChapterCompleted(item.Id);
    }

    private void ExitReading()
    {
        if (_isPlaying) StopPlayback();
        _reading = false;
        _readingSurah = null;
        _selectedIndex = -1;
        _scrollOffset = 0;
    }

    private void TogglePlayback()
    {
        if (_isPlaying) StopPlayback();
        else StartPlayback();
    }

    private void StartPlayback()
    {
        if (_murottal == null) return;
        Audio.PlayBgm(_murottal, false);
        _isPlaying = true;
        _playTimer = 0;
    }

    private void StopPlayback()
    {
        Audio.StopBgm();
        Audio.PlayBgm(_menuBgm);
        _isPlaying = false;
    }

    private Rectangle GetChapterRect(int i)
    {
        var cx = _game.Width / 2;
        var startY = 180;
        var gap = 80;
        return new Rectangle(cx - 250, startY + i * gap, 500, 60);
    }

    private Rectangle GetDetailRect()
    {
        var pw = Math.Min(_game.Width - 80, 620);
        var ph = 340;
        return new Rectangle(
            (_game.Width - pw) / 2,
            (_game.Height - ph) / 2,
            pw, ph);
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        b.Begin();
        var loc = _game.Loc;

        b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);

        if (_reading)
        {
            DrawSurahReading(b);
        }
        else if (_selectedIndex >= 0)
        {
            DrawDetail(b);
        }
        else
        {
            DrawChapterList(b);
        }

        var iconSz = 48;
        var iconX = _game.Width - iconSz - 12;
        _backRect = new Rectangle(iconX, 12, iconSz, iconSz);
        var iCol = _hoverBack ? _accent : Color.White * 0.8f;
        b.Draw(_iconHome, _backRect, iCol);

        b.End();
    }

    private void DrawChapterList(SpriteBatch b)
    {
        var loc = _game.Loc;

        var title = loc.Get("library_title");
        var tSz = _font.MeasureString(title);
        b.DrawString(_font, title, new Vector2((_game.Width - tSz.X) / 2, 60), _accent);

        var startY = 140;
        var endY = 180 + _items.Length * 80 - 20;
        var pw = 560;
        var ph = endY - startY + 60;
        var px = (_game.Width - pw) / 2;
        var py = startY - 30;
        var br = 16;
        var innerX = px + br;
        var innerY = py + br;
        var innerW = pw - br * 2;
        var innerH = ph - br * 2;

        b.Draw(_settingBorder, new Rectangle(px, py, pw, ph),
            new Rectangle(0, 0, _settingBorder.Width, _settingBorder.Height), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(innerX, innerY, innerW, innerH), new Color(38, 28, 22, 230));

        for (var i = 0; i < _items.Length; i++)
            DrawChapterCard(b, i);
    }

    private void DrawChapterCard(SpriteBatch b, int i)
    {
        var loc = _game.Loc;
        var item = _items[i];
        var key = item.IsSurah ? item.Surah.TitleKey : item.Id + "_title";
        var subKey = item.IsSurah ? item.Surah.SubtitleKey : item.Id + "_subtitle";
        var r = GetChapterRect(i);
        var unlocked = IsUnlocked(item);

        b.Draw(_game.WhitePixel, r, new Color(40, 30, 25, 200));

        var status = unlocked ? loc.Get("status_unlocked") : loc.Get("status_locked");
        b.DrawString(_font, status, new Vector2(r.X + 10, r.Y + 6), unlocked ? Color.Gold : Color.Gray, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);

        var col = unlocked ? _cream : Color.Gray;
        b.DrawString(_font, loc.Get(key), new Vector2(r.X + 50, r.Y + 6), col, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        b.DrawString(_font, loc.Get(subKey), new Vector2(r.X + 50, r.Y + 32), col * 0.7f, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0);
    }

    private void DrawDetail(SpriteBatch b)
    {
        var loc = _game.Loc;
        var item = _items[_selectedIndex];
        var key = item.IsSurah ? item.Surah.TitleKey : item.Id + "_title";
        var subKey = item.IsSurah ? item.Surah.SubtitleKey : item.Id + "_subtitle";
        var unlocked = IsUnlocked(item);

        var pw = Math.Min(_game.Width - 80, 620);
        var ph = 340;
        var px = (_game.Width - pw) / 2;
        var py = (_game.Height - ph) / 2;

        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.5f);
        b.Draw(_panelDialog, new Rectangle(px, py, pw, ph), Color.White);

        var ix = px + 20;
        var iy = py + 20;
        var iw = pw - 40;

        b.DrawString(_font, loc.Get(key), new Vector2(ix + (iw - _font.MeasureString(loc.Get(key)).X) / 2, iy + 10), Color.White, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
        b.DrawString(_font, loc.Get(subKey), new Vector2(ix + (iw - _font.MeasureString(loc.Get(subKey)).X) / 2, iy + 44), Color.White * 0.7f, 0, Vector2.Zero, 0.75f, SpriteEffects.None, 0);

        b.Draw(_game.WhitePixel, new Rectangle(ix + 40, iy + 75, iw - 80, 1), Color.White * 0.15f);

        if (unlocked)
        {
            var statusText = Save.IsChapterCompleted(item.Id) ? loc.Get("status_completed") : loc.Get("status_incomplete");
            b.DrawString(_font, statusText, new Vector2(ix + 20, iy + 95), Color.White * 0.8f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);

            if (item.IsSurah && item.Surah?.Ayahs?.Length > 0)
            {
                var readHint = loc.Get("tap_read");
                var rhSz = _font.MeasureString(readHint);
                b.DrawString(_font, readHint, new Vector2(ix + (iw - rhSz.X) / 2, iy + 145), _accent, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);

                var arrow = ">";
                var aSz = _font.MeasureString(arrow) * 0.8f;
                b.DrawString(_font, arrow, new Vector2(ix + (iw - aSz.X) / 2, iy + 185), _accent * 0.6f, 0, Vector2.Zero, 1.4f, SpriteEffects.None, 0);
            }
        }
        else
        {
            var hint = loc.Get("lock_hint");
            var lines = hint.Split('\n');
            for (var i = 0; i < lines.Length; i++)
                b.DrawString(_font, lines[i], new Vector2(ix + 20, iy + 95 + i * 25), Color.White * 0.7f);
        }

        var exit = loc.Get("click_outside_hint");
        var exitSz = _font.MeasureString(exit);
        b.DrawString(_font, exit, new Vector2((_game.Width - exitSz.X) / 2, py + ph + 10), Color.White * 0.5f, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
    }

    private void DrawSurahReading(SpriteBatch b)
    {
        if (_readingSurah?.Ayahs == null) return;
        var loc = _game.Loc;
        var ayahs = _readingSurah.Ayahs;

        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), new Color(0, 0, 0, 235));

        var title = loc.Get(_readingSurah.TitleKey);
        var tSz = _font.MeasureString(title);
        b.DrawString(_font, title, new Vector2((_game.Width - tSz.X) / 2, 20), _accent);

        var subtitle = loc.Get(_readingSurah.SubtitleKey);
        var stSz = _font.MeasureString(subtitle);
        b.DrawString(_font, subtitle, new Vector2((_game.Width - stSz.X) / 2, 52), _cream * 0.6f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

        var startY = 100;
        var panelW = _game.Width - 120;
        var panelX = 60;

        for (var i = 0; i < ayahs.Length; i++)
        {
            var ayah = ayahs[i];
            var ayY = (int)(startY + i * (AyahGap - 30) - _scrollOffset);
            if (ayY < startY - AyahGap || ayY > _game.Height + AyahGap) continue;

            var numStr = $"{i + 1}.";
            var trans = loc.Get(ayah.TransKey);
            var numSz = _font.MeasureString(numStr) * 0.7f;

            b.DrawString(_font, numStr, new Vector2(panelX + 8, ayY + 6), _accent, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
            b.DrawString(_font, trans, new Vector2(panelX + 8 + numSz.X + 8, ayY + 8), Color.White * 0.85f, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0);
        }

        var btnY = _game.Height - 50;
        var btnX = _game.Width - 150;
        var btnW = 130;
        var btnH = 36;

        if (_murottal != null)
        {
            var isHover = new Rectangle(btnX, btnY, btnW, btnH).Contains(Mouse.GetState().X, Mouse.GetState().Y);
            var btnBg = _isPlaying ? new Color(160, 60, 50) : new Color(62, 39, 35);
            var btnBorder = isHover ? _accent : _darkBrown;

            b.Draw(_game.WhitePixel, new Rectangle(btnX, btnY, btnW, btnH), btnBg);
            b.Draw(_game.WhitePixel, new Rectangle(btnX, btnY, btnW, 2), btnBorder);
            b.Draw(_game.WhitePixel, new Rectangle(btnX, btnY + btnH - 2, btnW, 2), btnBorder);

            var btnText = _isPlaying ? loc.Get("stop") : loc.Get("play");
            var btnSz = _font.MeasureString(btnText);
            b.DrawString(_font, btnText, new Vector2(btnX + (btnW - btnSz.X) / 2, btnY + (btnH - btnSz.Y) / 2), _cream);
        }

        var exitHint = loc.Get("tap_exit_read");
        var ehSz = _font.MeasureString(exitHint);
        b.DrawString(_font, exitHint, new Vector2((_game.Width - ehSz.X) / 2, _game.Height - 30), Color.White * 0.4f, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
    }

    public void Unload()
    {
        if (_isPlaying) StopPlayback();
    }
}
