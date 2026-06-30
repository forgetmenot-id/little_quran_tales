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
    private Texture2D _bg, _iconHome, _settingBorder, _panelCard;
    private SoundEffect _sfxClick;

    private readonly Color _cream       = new(255, 248, 225);
    private readonly Color _gold        = new(212, 175, 55);
    private readonly Color _terracotta  = new(194, 74, 47);
    private readonly Color _burntSienna = new(233, 116, 81);
    private readonly Color _darkBrown   = new(62, 39, 35);
    private readonly Color _darkGold    = new(180, 140, 30);
    private readonly Color _grayText    = new(170, 170, 170);

    private const int PanelW = 640;
    private const int PanelH = 520;
    private const int BorderW = 16;
    private const int ItemH = 52;
    private const int ItemGap = 4;

    private int _panelX, _panelY;
    private Rectangle _panelRect, _innerRect;

    private ChapterEntry[] _entries;
    private int _selectedIndex = -1;

    private bool _reading;
    private SurahInfo _readingSurah;
    private List<VerseInfo> _readingVerses;
    private float _scrollOffset, _readingContentMaxY;
    private int _prevScroll;
    private float _readingEnterTime;

    private float _listScrollOffset;
    private int _prevListScroll;
    private bool _isPlaying;
    private float _playTimer;
    private Song _menuBgm, _murottal;

    public int VictorySurahNumber { get; set; }
    private bool _autoPlayMode;
    private int _victorySurah;

    private Rectangle _backRect;
    private bool _hoverBack;

    private bool _hoverDetailBtn;
    private bool _hoverCloseBtn;

    private float _inputCooldown;

    private SaveManager Save => _game.Save;
    private AudioManager Audio => _game.Audio;
    private QuranDbService Quran => _game.Quran;

    private struct ChapterEntry
    {
        public string ChapterId;
        public int? SurahNumber;
        public SurahInfo Surah;
        public string TitleKey;
        public string SubKey;
    }

    private static readonly (string id, int? surahNo, string titleKey, string subKey)[] _chapterDefs = {
        ("prolog",    null,  "prolog_title",    "prolog_subtitle"),
        ("al-fil",    105,   "chapter_1_title",  "chapter_1_sub"),
        ("al-alaq",   96,    "chapter_2_title",  "chapter_2_sub"),
        ("al-fatihah", 1,    "chapter_3_title",  "chapter_3_sub"),
        ("al-lahab",  111,   "chapter_4_title",  "chapter_4_sub"),
        ("an-nashr",  110,   "chapter_5_title",  "chapter_5_sub"),
    };

    public LibraryScene(Game1 game) { _game = game; }

    public void Load()
    {
        LogHelper.Trace($"LibraryScene.Load entered, VictorySurahNumber={VictorySurahNumber}");
        _font         = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _bg           = _game.Content.Load<Texture2D>("Images/BGs/bg_kayu");
        _iconHome     = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _settingBorder= _game.Content.Load<Texture2D>("Images/UI/setting_border");
        _panelCard    = _game.Content.Load<Texture2D>("Images/UI/panel_card");
        _sfxClick     = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");
        _menuBgm      = _game.Content.Load<Song>("Audio/BGM/bgm_menu");
        _murottal = null;
        // Loaded later after _victorySurah is set

        _panelX = (_game.Width  - PanelW) / 2;
        _panelY = (_game.Height - PanelH) / 2;
        _panelRect  = new Rectangle(_panelX, _panelY, PanelW, PanelH);
        _innerRect  = new Rectangle(_panelX + BorderW + 8, _panelY + 62, PanelW - (BorderW + 8) * 2, PanelH - 62 - BorderW);

        var list = new List<ChapterEntry>();
        foreach (var def in _chapterDefs)
        {
            var entry = new ChapterEntry
            {
                ChapterId = def.id,
                SurahNumber = def.surahNo,
                TitleKey = def.titleKey,
                SubKey = def.subKey,
            };
            if (def.surahNo.HasValue)
                entry.Surah = Quran.GetSurah(def.surahNo.Value);
            list.Add(entry);
        }
        _entries = list.ToArray();

        _selectedIndex = -1;
        _reading = false;
        _readingSurah = null;
        _scrollOffset = 0;
        _listScrollOffset = 0;
        var initScroll = Mouse.GetState().ScrollWheelValue;
        _prevScroll = initScroll;
        _prevListScroll = initScroll;

        _autoPlayMode = false;
        _victorySurah = VictorySurahNumber;
        VictorySurahNumber = 0;
        if (_victorySurah > 0)
        {
            _autoPlayMode = true;
            var surah = Quran.GetSurah(_victorySurah);
            var murottalKey = _victorySurah == 96 ? "al-alaq" : "al-fil";
            if (_murottal != null) { _murottal.Dispose(); _murottal = null; }
            try { _murottal = _game.Content.Load<Song>($"Audio/library/{murottalKey}"); }
            catch { _murottal = null; }
            if (surah != null)
            {
                StartReading(surah);
                StartPlayback();
            }
        }
    }

    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        if (_isPlaying) _playTimer += dt;

        var touch = _game.GetTouch();
        var mp = touch.Position;
        _hoverBack = _backRect.Contains(mp);

        if (_reading)
        {
            UpdateReading(dt, touch, mp);
            return;
        }

        UpdateListScroll();

        if (_inputCooldown > 0) return;

        if (_selectedIndex >= 0)
        {
            UpdateDetailPanel(touch, mp);
            return;
        }

        if (touch.IsDown)
        {
            if (_hoverBack)
            {
                Audio.PlaySfx(_sfxClick);
                _inputCooldown = GameConfig.ClickCooldown;
                _game.SceneManager.SwitchTo(SceneId.Menu);
                return;
            }

            for (var i = 0; i < _entries.Length; i++)
            {
                if (GetItemRect(i).Contains(mp))
                {
                    Audio.PlaySfx(_sfxClick);
                    _selectedIndex = i;
                    _inputCooldown = GameConfig.ClickCooldown;
                    return;
                }
            }
        }

        var kb = Keyboard.GetState();
        if ((kb.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) && _inputCooldown <= 0)
        {
            Audio.PlaySfx(_sfxClick);
            _inputCooldown = GameConfig.ClickCooldown;
            _game.SceneManager.SwitchTo(SceneId.Menu);
        }
    }

    private void UpdateListScroll()
    {
        var scroll = Mouse.GetState().ScrollWheelValue;
        var delta = _prevListScroll - scroll;
        _prevListScroll = scroll;
        _listScrollOffset += delta * 0.05f;
        var contentH = _entries.Length * ItemH + (_entries.Length - 1) * ItemGap;
        var maxOff = Math.Max(0, contentH - _innerRect.Height);
        _listScrollOffset = MathHelper.Clamp(_listScrollOffset, 0, maxOff);
    }

    private void UpdateDetailPanel(Game1.TouchState touch, Point mp)
    {
        var pw = 520;
        var ph = 340;
        var px = (_game.Width  - pw) / 2;
        var py = (_game.Height - ph) / 2;

        var cbRect = new Rectangle(px + pw - 36, py + 8, 28, 28);
        var btnRect = GetDetailBtnRect(px, pw, py);
        var panelRect = new Rectangle(px, py, pw, ph);

        _hoverCloseBtn = cbRect.Contains(mp);
        _hoverDetailBtn = btnRect.Contains(mp);

        if (!touch.IsDown) return;

        if (_hoverBack)
        {
            Audio.PlaySfx(_sfxClick);
            _inputCooldown = GameConfig.ClickCooldown;
            _game.SceneManager.SwitchTo(SceneId.Menu);
            return;
        }

        // close button (only X closes the panel)
        if (cbRect.Contains(mp))
        {
            Audio.PlaySfx(_sfxClick);
            _selectedIndex = -1;
            _inputCooldown = GameConfig.ClickCooldown;
            return;
        }

        // action button (inside panel)
        if (btnRect.Contains(mp))
        {
            Audio.PlaySfx(_sfxClick);
            _inputCooldown = GameConfig.ClickCooldown;

            var entry = _entries[_selectedIndex];

            if (entry.Surah != null && IsChapterUnlocked(entry.ChapterId) && entry.Surah.VersesCount > 0)
            {
                StartReading(entry.Surah);
            }
            else
            {
                _game.SceneManager.SwitchTo(SceneId.StorySelection);
            }
        }
    }

    private Rectangle GetDetailBtnRect(int px, int pw, int py)
    {
        return new Rectangle(px + (pw - 200) / 2, py + 250, 200, 42);
    }

    private void UpdateReading(float dt, Game1.TouchState touch, Point mp)
    {
        if (_readingVerses == null || _readingVerses.Count == 0) { ExitReading(); return; }

        var scroll = Mouse.GetState().ScrollWheelValue;
        var delta = _prevScroll - scroll;
        _prevScroll = scroll;
        _scrollOffset += delta * 0.05f;

        var innerPad = 12;
        var panelPaddingX = 28;
        var panelY = 62;
        var panelW = _game.Width - panelPaddingX * 2;
        var panelH = _game.Height - panelY - 56;
        var innerY = panelY + innerPad;
        var innerH = panelH - innerPad * 2;

        var maxOff = Math.Max(0, _readingContentMaxY - innerY - innerH - 8);
        _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, maxOff);

        if (_inputCooldown > 0) { _readingEnterTime += dt; return; }
        _readingEnterTime += dt;

        if (_autoPlayMode && _isPlaying && _playTimer > 1f)
        {
            if (_murottal == null || MediaPlayer.State == MediaState.Stopped)
            {
                ExitToDialogue();
                return;
            }
        }

        if (touch.IsDown)
        {
            if (_hoverBack)
            {
                ExitReading();
                Audio.PlaySfx(_sfxClick);
                _inputCooldown = GameConfig.ClickCooldown;
                _game.SceneManager.SwitchTo(SceneId.Menu);
                return;
            }

            var playBtn = new Rectangle(_game.Width - 150, _game.Height - 50, 130, 36);
            if (playBtn.Contains(mp) && _murottal != null)
            {
                Audio.PlaySfx(_sfxClick);
                _inputCooldown = GameConfig.ClickCooldown;
                TogglePlayback();
                return;
            }

            if (_readingEnterTime > 0.5f)
            {
                if (_autoPlayMode)
                {
                    ExitToDialogue();
                    return;
                }
                ExitReading();
                _inputCooldown = 0.2f;
            }
        }

        if ((Keyboard.GetState().IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) && _inputCooldown <= 0)
        {
            if (_autoPlayMode)
            {
                ExitToDialogue();
                return;
            }
            ExitReading();
            _inputCooldown = 0.2f;
        }
    }

    private void StartReading(SurahInfo surah)
    {
        _readingSurah = surah;
        _readingVerses = Quran.GetAyahs(surah.Number);
        _reading = true;
        _scrollOffset = 0;
        _readingContentMaxY = 0;
        _prevScroll = Mouse.GetState().ScrollWheelValue;
        _playTimer = 0;
        _isPlaying = false;
        _readingEnterTime = 0;
    }

    private void ExitReading()
    {
        if (_isPlaying) StopPlayback();
        _reading = false;
        _readingSurah = null;
        _readingVerses = null;
        _selectedIndex = -1;
        _scrollOffset = 0;
    }

    private void ExitToDialogue()
    {
        Audio.StopBgm();
        var isAlaq = _victorySurah == 96;
        _isPlaying = false;
        _reading = false;
        _readingSurah = null;
        _readingVerses = null;
        _autoPlayMode = false;
        _victorySurah = 0;
        _selectedIndex = -1;
        _scrollOffset = 0;

        var dialogue = _game.SceneManager.GetScene<DialogueScene>(SceneId.Dialogue);
        if (dialogue != null)
            dialogue.LoadChapterFile(isAlaq ? ChapterPath.AlAlaqEnd : ChapterPath.Ending);
        _game.SceneManager.SwitchTo(SceneId.Dialogue);
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

    private bool IsChapterUnlocked(string chapterId)
    {
        if (chapterId == "prolog") return true;
        if (chapterId is "al-fatihah" or "al-lahab" or "an-nashr") return false;
        return Save.IsChapterCompleted(chapterId);
    }

    private bool IsChapterCompleted(string chapterId)
    {
        return Save.IsChapterCompleted(chapterId);
    }

    private Rectangle GetItemRect(int i)
    {
        var y = _innerRect.Y + i * (ItemH + ItemGap) - (int)_listScrollOffset;
        var w = _innerRect.Width;
        return new Rectangle(_innerRect.X, y, w, ItemH);
    }

    // ───── DRAW ─────

    public void Draw()
    {
        var b = _game.SpriteBatch;
        b.Begin();

        b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);

        if (_reading)
        {
            DrawSurahReading(b);
        }
        else
        {
            b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.7f);
            if (_selectedIndex >= 0)
                DrawDetail(b);
            else
                DrawChapterList(b);
        }

        var iconSz = 48;
        var iconX = _game.Width - iconSz - 12;
        _backRect = new Rectangle(iconX, 12, iconSz, iconSz);
        b.Draw(_iconHome, _backRect, _hoverBack ? _gold : Color.White * 0.8f);

        b.End();
    }

    private void DrawChapterList(SpriteBatch b)
    {
        var loc = _game.Loc;

        // panel border
        b.Draw(_settingBorder, _panelRect,
            new Rectangle(0, 0, _settingBorder.Width, _settingBorder.Height),
            Color.White);

        // title
        var title = loc.Get("library_title");
        var tSz = _font.MeasureString(title);
        b.DrawString(_font, title,
            new Vector2(_panelX + (PanelW - tSz.X) / 2 + 1, _panelY + 22 + 1),
            Color.Black * 0.6f);
        b.DrawString(_font, title,
            new Vector2(_panelX + (PanelW - tSz.X) / 2, _panelY + 22),
            _gold);

        // list items (visibility-culled to avoid drawing outside panel)
        for (var i = 0; i < _entries.Length; i++)
            DrawChapterItem(b, i);
    }

    private void DrawChapterItem(SpriteBatch b, int i)
    {
        var r = GetItemRect(i);
        if (r.Bottom < _innerRect.Top || r.Top > _innerRect.Bottom) return;

        var loc = _game.Loc;
        var entry = _entries[i];
        var unlocked = IsChapterUnlocked(entry.ChapterId);
        var completed = IsChapterCompleted(entry.ChapterId);

        // item background
        var bgColor = completed ? new Color(30, 45, 25, 200) : new Color(40, 35, 30, 200);
        if (_selectedIndex == i) bgColor = new Color(55, 50, 40, 220);
        b.Draw(_game.WhitePixel, r, bgColor);

        var textCol = unlocked ? _cream : new Color(160, 150, 140);

        var leftX = r.X + 16;
        var rightX = r.Right - 16;

        if (entry.Surah != null)
        {
            // surah number + name (left) — scale down if too long
            var label = $"{entry.Surah.Number}. {entry.Surah.EnglishName}";
            var labelFull = _font.MeasureString(label).X;
            var maxLabelW = rightX - leftX - 80f;
            var labelScale = labelFull > maxLabelW ? maxLabelW / labelFull : 0.85f;
            b.DrawString(_font, label, new Vector2(leftX, r.Y + 4), textCol,
                0, Vector2.Zero, labelScale, SpriteEffects.None, 0);

            // verse count below name
            var ayahStr = $"{entry.Surah.AyahCount} ayat";
            b.DrawString(_font, ayahStr, new Vector2(leftX, r.Y + 28),
                (unlocked ? _cream : Color.Gray) * 0.55f,
                0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);

            // completed checkmark
            if (completed)
            {
                var done = loc.Get("status_completed").Substring(0, 3);
                b.DrawString(_font, done, new Vector2(leftX + 90, r.Y + 28),
                    _gold * 0.8f, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
            }

            // arabic name (right)
            if (_game.ArabicText != null)
            {
                var arabic = entry.Surah.NameArabic;
                var aSz = _game.ArabicText.MeasureString(arabic, 20f);
                var aCol = unlocked ? _gold : _gold * 0.4f;
                // vertically center arabic text within item
                var aY = r.Y + (ItemH - aSz.Y) / 2;
                _game.ArabicText.DrawString(b, arabic, 20f,
                    new Vector2(rightX - aSz.X, aY), aCol);
            }
            else
            {
                b.DrawString(_font, entry.Surah.NameArabic,
                    new Vector2(rightX - _font.MeasureString(entry.Surah.NameArabic).X, r.Y + 4),
                    unlocked ? _gold : _gold * 0.4f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
            }
        }
        else
        {
            // prolog / non-surah: show chapter title as section header
            var titleStr = loc.Get(entry.TitleKey);
            b.DrawString(_font, titleStr, new Vector2(leftX, r.Y + 4), _gold,
                0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);

            var subStr = loc.Get(entry.SubKey);
            b.DrawString(_font, subStr, new Vector2(leftX, r.Y + 28),
                _cream * 0.65f, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);

            if (completed)
            {
                var done = loc.Get("status_completed").Substring(0, 3);
                b.DrawString(_font, done, new Vector2(rightX - 40, r.Y + 8),
                    _gold * 0.8f, 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);
            }
        }

        // lock indicator for locked items (second line so it doesn't overlap arabic name)
        if (!unlocked)
        {
            b.DrawString(_font, "[X]", new Vector2(rightX - 38, r.Y + 28),
                Color.Gray * 0.6f, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
        }

        // gold decorative divider
        var divY = r.Bottom;
        if (divY < _innerRect.Bottom - 2)
        {
            b.Draw(_game.WhitePixel,
                new Rectangle(_innerRect.X + 12, divY, _innerRect.Width - 24, 1),
                _gold * 0.25f);
            // small diamond/dot ornament in center
            b.Draw(_game.WhitePixel,
                new Rectangle(_panelX + PanelW / 2 - 2, divY - 2, 4, 4),
                _gold * 0.35f);
        }
    }

    private void DrawDetail(SpriteBatch b)
    {
        var loc = _game.Loc;
        var entry = _entries[_selectedIndex];
        var unlocked = IsChapterUnlocked(entry.ChapterId);
        var completed = IsChapterCompleted(entry.ChapterId);

        var pw = 520;
        var ph = 340;
        var px = (_game.Width  - pw) / 2;
        var py = (_game.Height - ph) / 2;
        var ix = px + 24;
        var iw = pw - 48;

        // overlay
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.7f);

        // parchment panel
        b.Draw(_panelCard, new Rectangle(px, py, pw, ph), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(px, py, pw, ph), Color.Black * 0.12f);

        // close button (X)
        var cbRect = new Rectangle(px + pw - 36, py + 8, 28, 28);
        b.Draw(_game.WhitePixel, cbRect, _hoverCloseBtn ? _gold * 0.3f : Color.Black * 0.1f);
        b.DrawString(_font, "X", new Vector2(cbRect.X + 7, cbRect.Y + 3),
            _hoverCloseBtn ? _gold : _grayText * 0.8f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

        if (entry.Surah != null)
        {
            // Arabic name (top, dark gold, large)
            if (_game.ArabicText != null)
            {
                var aSz = _game.ArabicText.MeasureString(entry.Surah.NameArabic, 26f);
                _game.ArabicText.DrawString(b, entry.Surah.NameArabic, 26f,
                    new Vector2(px + (pw - aSz.X) / 2, py + 24), _darkGold);
            }

            // number + name (dark brown, large) — more spacing from Arabic name
            var title = $"{entry.Surah.Number}. {entry.Surah.EnglishName}";
            var tSz = _font.MeasureString(title);
            b.DrawString(_font, title, new Vector2(px + pw / 2, py + 72),
                _darkBrown, 0, tSz / 2, 1.1f, SpriteEffects.None, 0);

            // meaning (smaller, gray)
            var sub = entry.Surah.EnglishNameTranslation;
            if (!string.IsNullOrEmpty(sub))
            {
                var subSz = _font.MeasureString(sub);
                b.DrawString(_font, sub, new Vector2(px + pw / 2, py + 102),
                    _grayText * 0.7f, 0, subSz / 2, 0.65f, SpriteEffects.None, 0);
            }

            // gold divider
            var divY = py + 130;
            b.Draw(_game.WhitePixel, new Rectangle(ix + 30, divY, iw - 60, 1), _gold * 0.35f);

            if (unlocked)
            {
                // info row
                var revelation = entry.Surah.RevelationType == "makkah" ? "Makkiyah" : "Madaniyah";
                var infoText = $"{entry.Surah.AyahCount} ayat  |  {revelation}";
                var infoSz = _font.MeasureString(infoText);
                b.DrawString(_font, infoText, new Vector2(px + pw / 2, py + 145),
                    _grayText * 0.8f, 0, infoSz / 2, 0.7f, SpriteEffects.None, 0);

                // status
                var statusText = completed ? loc.Get("status_completed") : loc.Get("status_incomplete");
                var stSz = _font.MeasureString(statusText);
                b.DrawString(_font, statusText, new Vector2(px + pw / 2, py + 170),
                    completed ? _darkGold * 0.9f : _grayText * 0.55f, 0, stSz / 2, 0.6f, SpriteEffects.None, 0);
            }
            else
            {
                // locked warning (auto-wrap at middle space)
                var warnText = loc.Get("lock_hint");
                string[] warnLines;
                if (warnText.Length > 28)
                {
                    int breakAt = warnText.LastIndexOf(' ', 28);
                    if (breakAt > 0)
                        warnLines = new[] { warnText[..breakAt], warnText[(breakAt + 1)..] };
                    else
                        warnLines = new[] { warnText };
                }
                else
                    warnLines = new[] { warnText };

                var lineY = py + 145;
                foreach (var line in warnLines)
                {
                    var lSz = _font.MeasureString(line);
                    b.DrawString(_font, line, new Vector2(px + pw / 2, lineY),
                        new Color(120, 90, 70), 0, lSz / 2, 0.7f, SpriteEffects.None, 0);
                    lineY += 26;
                }
            }

            // action button
            var btnText = unlocked ? "Baca Surah" : "Buka Chapter";
            DrawPopupButton(b, GetDetailBtnRect(px, pw, py), btnText, _hoverDetailBtn);
        }
        else
        {
            // non-surah (prolog)
            var titleStr = loc.Get(entry.TitleKey);
            var tSz2 = _font.MeasureString(titleStr);
            b.DrawString(_font, titleStr, new Vector2(px + pw / 2, py + 36),
                _gold, 0, tSz2 / 2, 1.0f, SpriteEffects.None, 0);

            var subStr = loc.Get(entry.SubKey);
            var subSz2 = _font.MeasureString(subStr);
            b.DrawString(_font, subStr, new Vector2(px + pw / 2, py + 68),
                _grayText * 0.7f, 0, subSz2 / 2, 0.7f, SpriteEffects.None, 0);

            b.Draw(_game.WhitePixel, new Rectangle(ix + 40, py + 100, iw - 80, 1), _gold * 0.35f);

            if (completed)
            {
                var doneText = loc.Get("status_completed");
                var dtSz = _font.MeasureString(doneText);
                b.DrawString(_font, doneText, new Vector2(px + pw / 2, py + 140),
                    _gold * 0.8f, 0, dtSz / 2, 0.7f, SpriteEffects.None, 0);
            }
            else
            {
                var playText = "Mainkan Prolog";
                var ptSz = _font.MeasureString(playText);
                b.DrawString(_font, playText, new Vector2(px + pw / 2, py + 134),
                    _grayText * 0.6f, 0, ptSz / 2, 0.6f, SpriteEffects.None, 0);

                DrawPopupButton(b, GetDetailBtnRect(px, pw, py), "Mainkan Prolog", _hoverDetailBtn);
            }
        }

        // close hint (inside panel, bottom)
        var closeHint = "Klik X untuk tutup";
        var chSz = _font.MeasureString(closeHint);
        b.DrawString(_font, closeHint, new Vector2(px + pw / 2, py + ph - 16),
            _grayText * 0.45f, 0, chSz / 2, 0.5f, SpriteEffects.None, 0);
    }

    private void DrawPopupButton(SpriteBatch b, Rectangle r, string text, bool hover)
    {
        var bg = hover ? _burntSienna : _terracotta;
        b.Draw(_game.WhitePixel, r, bg);
        b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y, r.Width, 2), Color.White * 0.2f);
        b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), Color.Black * 0.2f);

        var sz = _font.MeasureString(text);
        var pos = new Vector2(r.Center.X, r.Center.Y);
        b.DrawString(_font, text, pos, _cream, 0, sz / 2, 0.8f, SpriteEffects.None, 0);
    }

    private void DrawSurahReading(SpriteBatch b)
    {
        if (_readingVerses == null || _readingVerses.Count == 0) return;
        var loc = _game.Loc;

        // dark overlay
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.65f);

        // surah header (fixed, never scrolls) — scale down if too wide
        var title = $"{_readingSurah.Number}. {_readingSurah.EnglishName}";
        var tSz = _font.MeasureString(title);
        var maxTitleW = _game.Width - 56f;
        var titleScale = tSz.X > maxTitleW ? maxTitleW / tSz.X : 1f;
        b.DrawString(_font, title,
            new Vector2((_game.Width - tSz.X * titleScale) / 2, 12),
            _gold, 0, Vector2.Zero, titleScale, SpriteEffects.None, 0);

        if (_game.ArabicText != null)
        {
            var arabicTitle = _readingSurah.NameArabic;
            var aSz = _game.ArabicText.MeasureString(arabicTitle, 18f);
            var maxAraW = _game.Width - 56f;
            var araScale = aSz.X > maxAraW ? 18f * maxAraW / aSz.X : 18f;
            _game.ArabicText.DrawString(b, arabicTitle, araScale,
                new Vector2((_game.Width - _game.ArabicText.MeasureString(arabicTitle, araScale).X) / 2, 32), _grayText * 0.6f);
        }

        // ─── MAIN CONTENT PANEL ───
        var innerPad = 12;
        var panelPaddingX = 28;
        var panelY = 62;
        var panelW = _game.Width - panelPaddingX * 2;
        var panelH = _game.Height - panelY - 56;

        // border frame
        b.Draw(_settingBorder,
            new Rectangle(panelPaddingX, panelY, panelW, panelH),
            new Rectangle(0, 0, _settingBorder.Width, _settingBorder.Height),
            Color.White);

        // inner dark-warm fill
        var innerX = panelPaddingX + innerPad;
        var innerY = panelY + innerPad;
        var innerW = panelW - innerPad * 2;
        var innerH = panelH - innerPad * 2;
        b.Draw(_game.WhitePixel, new Rectangle(innerX, innerY, innerW, innerH),
            new Color(40, 28, 22, 245));

        // ── scissored verse content ──
        b.End();

        var prevRaster = b.GraphicsDevice.RasterizerState;
        var sRaster = new RasterizerState { ScissorTestEnable = true };
        var prevScissor = b.GraphicsDevice.ScissorRectangle;
        b.GraphicsDevice.ScissorRectangle = new Rectangle(innerX, innerY, innerW, innerH);
        b.GraphicsDevice.RasterizerState = sRaster;
        b.Begin(SpriteSortMode.Deferred, null, null, null, sRaster, null);

        var contentX = innerX + 10;
        var contentW = innerW - 20;
        var topBound = innerY + 4;
        var bottomBound = innerY + innerH - 4;

        var lang = _game.Loc.CurrentLang;
        var currentY = (float)(topBound);

        var leftW = (int)(contentW * 0.45f);
        var rightW = contentW - leftW - 12;
        var rightX = contentX + leftW + 12;

        for (var i = 0; i < _readingVerses.Count; i++)
        {
            var ayY = (int)(currentY - _scrollOffset);
            if (ayY > bottomBound + 200) break;

            var ayah = _readingVerses[i];
            var baseY = (float)(ayY + 2);
            var leftY = baseY;
            var rightY = baseY;

            // ── RIGHT COLUMN: Arabic + Latin ──
            if (_game.ArabicText != null)
            {
                var arabic = ayah.TextUthmani;
                var arabicLines = WrapArabicText(arabic, rightW, 16f);
                foreach (var (line, lineW) in arabicLines)
                {
                    _game.ArabicText.DrawString(b, line, 16f,
                        new Vector2(rightX + rightW - lineW, rightY), _cream * 0.85f);
                    rightY += 26;
                }
            }

            if (!string.IsNullOrEmpty(ayah.Transliteration))
            {
                var tlLines = WrapText(ayah.Transliteration, rightW, 0.45f);
                foreach (var tl in tlLines)
                {
                    b.DrawString(_font, tl, new Vector2(rightX, rightY + 2),
                        _gold * 0.65f, 0, Vector2.Zero, 0.45f, SpriteEffects.None, 0);
                    rightY += 14;
                }
            }

            // ── LEFT COLUMN: Number + Translation ──
            var numStr = $"{i + 1}.";
            b.DrawString(_font, numStr, new Vector2(contentX, leftY), _gold,
                0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);
            leftY += 16;

            var trans = lang == "id" ? ayah.TranslationId : ayah.TranslationEn;
            if (string.IsNullOrEmpty(trans))
                trans = lang == "id" ? ayah.TranslationEn : ayah.TranslationId;
            if (!string.IsNullOrEmpty(trans))
            {
                var transLines = WrapText(trans, leftW, 0.55f);
                foreach (var tl in transLines)
                {
                    b.DrawString(_font, tl, new Vector2(contentX, leftY),
                        Color.White * 0.85f, 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);
                    leftY += 18;
                }
            }

            var colHeight = Math.Max(leftY - baseY, rightY - baseY);
            currentY += colHeight + 12;

            if (i < _readingVerses.Count - 1)
            {
                var divY = (int)(currentY - 8 - _scrollOffset);
                if (divY > innerY && divY < innerY + innerH)
                    b.Draw(_game.WhitePixel, new Rectangle(contentX + 20, divY, contentW - 40, 1), _gold * 0.08f);
            }
        }

        _readingContentMaxY = currentY;

        b.End();
        b.GraphicsDevice.ScissorRectangle = prevScissor;
        b.GraphicsDevice.RasterizerState = prevRaster;
        b.Begin();

        // ─── BOTTOM CONTROLS ───
        var btnY = _game.Height - 46;
        var btnH = 32;
        var btnW2 = 130;

        // play/stop (right)
        if (_murottal != null)
        {
            var playBtnX = _game.Width - panelPaddingX - 8 - btnW2;
            var playBtnRect = new Rectangle(playBtnX, btnY, btnW2, btnH);
            var playHover = playBtnRect.Contains(Mouse.GetState().X, Mouse.GetState().Y);
            var playBg = _isPlaying ? new Color(160, 60, 50) : (playHover ? _burntSienna : _terracotta);
            b.Draw(_game.WhitePixel, playBtnRect, playBg);
            b.Draw(_game.WhitePixel, new Rectangle(playBtnRect.X, playBtnRect.Y, playBtnRect.Width, 2), Color.White * 0.2f);
            b.Draw(_game.WhitePixel, new Rectangle(playBtnRect.X, playBtnRect.Y + btnH - 2, playBtnRect.Width, 2), Color.Black * 0.2f);
            var btnText = _isPlaying ? loc.Get("stop") : loc.Get("play");
            var btnSz = _font.MeasureString(btnText);
            b.DrawString(_font, btnText, new Vector2(playBtnRect.Center.X, playBtnRect.Center.Y),
                _cream, 0, btnSz / 2, 0.7f, SpriteEffects.None, 0);
        }

        // exit hint
        var exitHint = loc.Get("tap_exit_read");
        var ehSz = _font.MeasureString(exitHint);
        b.DrawString(_font, exitHint, new Vector2((_game.Width - ehSz.X) / 2, _game.Height - 24),
            Color.White * 0.3f, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
    }

    private string[] WrapText(string text, float maxWidth, float scale)
    {
        if (string.IsNullOrEmpty(text)) return [text];
        var words = text.Split(' ');
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var test = string.IsNullOrEmpty(current) ? word : current + " " + word;
            if (_font.MeasureString(test).X * scale > maxWidth && !string.IsNullOrEmpty(current))
            {
                lines.Add(current);
                current = word;
            }
            else current = test;
        }
        if (!string.IsNullOrEmpty(current)) lines.Add(current);
        return lines.ToArray();
    }

    private List<(string Text, float Width)> WrapArabicText(string text, float maxWidth, float fontSize)
    {
        var result = new List<(string, float)>();
        if (string.IsNullOrEmpty(text) || _game.ArabicText == null)
        {
            if (!string.IsNullOrEmpty(text))
                result.Add((text, _game.ArabicText?.MeasureString(text, fontSize).X ?? 0));
            return result;
        }
        var words = text.Split(' ');
        var current = "";
        foreach (var word in words)
        {
            var test = string.IsNullOrEmpty(current) ? word : current + " " + word;
            var sz = _game.ArabicText.MeasureString(test, fontSize);
            if (sz.X > maxWidth && !string.IsNullOrEmpty(current))
            {
                result.Add((current, _game.ArabicText.MeasureString(current, fontSize).X));
                current = word;
            }
            else current = test;
        }
        if (!string.IsNullOrEmpty(current))
            result.Add((current, _game.ArabicText.MeasureString(current, fontSize).X));
        return result;
    }

    public void Unload()
    {
        if (_isPlaying) StopPlayback();
    }
}
