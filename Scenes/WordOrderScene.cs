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

public class WordOrderScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;

    private readonly Color _cream = new(234, 230, 223);
    private readonly Color _accent = new(212, 175, 55);
    private readonly Color _correctGreen = new(100, 200, 100);
    private readonly Color _wrongRed = new(220, 80, 80);
    private readonly Color _dangerRed = new(255, 60, 60);
    private readonly Color _darkBrown = new(62, 39, 35);
    private readonly Color _softCream = new(255, 253, 208);

    private Texture2D _bg, _bgKayu, _iconHome, _panelGame1, _panelGame2;
    private SoundEffect _sfxClick, _sfxCorrect, _sfxWrong, _sfxStoneHit;
    private Song _bgm, _murottal;

    private const float SlotScale = 1.45f;
    private const int SlotW = (int)(130 * SlotScale);
    private const int SlotH = (int)(50 * SlotScale);
    private const int WordH = (int)(44 * SlotScale);
    private const float EndlessTimeLimit = 60f;

    private const int HudH = 38;
    private const int HudY = 14;
    private const int ModeW = 240;
    private const int IndW = 160;
    private const int HudGap = 8;
    private const int AyahRefY = 68;
    private const int TransY = 92;
    private const int HeaderEndY = 140;
    private const int SlotY = 145;

    private enum WordOrderMode { Normal, Story, Endless }
    private WordOrderMode _mode;
    private bool _loaded;

    private SurahInfo _surah;
    private List<VerseInfo> _verses;
    private int _currentAyahIndex;
    private VerseInfo _currentAyah;
    private int[] _selectedIndices;
    private int _wrongAttempts;
    private bool _isFailed;
    private string[] _words;
    private int[] _shuffledOrder;

    private enum SlotState { Empty, Filled }
    private SlotState[] _slotStates;
    private int[] _slotContents;
    private bool[] _slotCorrect;

    private Rectangle[] _slotRects;
    private Rectangle[] _wordRects;
    private string[] _wordLabels;

    private int _selectedWordIndex = -1;
    private int _score;
    private float _inputCooldown;
    private bool _showResult;
    private float _resultTimer;
    private bool _isVictory;

    /* drag & drop */
    private float _elapsed;
    private bool _prevTouchDown;
    private bool _isDragging;
    private int _dragWordIndex;
    private Vector2 _dragOffset;
    private Vector2 _dragPos;
    private int _dragSourceSlot;

    /* endless mode */
    private int _endlessScore;
    private float _endlessTimer;
    private Random _rng = new();

    /* story/normal mode timer */
    private float _ayahTimer;
    private bool _ayahTimeUp;

    /* scroll */
    private float _scrollOffset;
    private float _contentMaxY;
    private int _prevScrollWheel;
    private bool _isScrollDragging;
    private int _scrollDragStartY;

    private const int WordStartY = 320;
    private const int SlotRowH = (int)(55 * SlotScale);
    private const int SlotGap = 12;

    private SaveManager Save => _game.Save;
    private AudioManager Audio => _game.Audio;
    private QuranDbService Quran => _game.Quran;

    private Rectangle _backRect;
    private bool _hoverBack;
    private bool _showExitConfirm;
    private Rectangle _confirmYesRect, _confirmNoRect;
    private Rectangle _submitRect;
    private bool _hoverSubmit;

    public WordOrderScene(Game1 game) { _game = game; }

    public void Load()
    {
        try
        {
            _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
            _bg = _game.Content.Load<Texture2D>("Images/UI/menu_bg");
            _bgKayu = SafeLoad<Texture2D>("Images/BGs/bg_kayu");
            _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");
            _panelGame1 = _game.Content.Load<Texture2D>("Images/UI/panel_game1");
            _panelGame2 = _game.Content.Load<Texture2D>("Images/UI/panel_game2");
            _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");
            _sfxCorrect = SafeLoad<SoundEffect>("Audio/SFX/sfx_correct");
            _sfxWrong = SafeLoad<SoundEffect>("Audio/SFX/sfx_wrong");
            _sfxStoneHit = SafeLoad<SoundEffect>("Audio/SFX/sfx_stone_hit");
        }
        catch { _loaded = false; return; }

        _bgm = SafeLoad<Song>("Audio/BGM/bgm_minigame2");
        if (_bgm != null) Audio.PlayBgm(_bgm);

        try { _murottal = _game.Content.Load<Song>("Audio/library/al-alaq"); }
        catch { _murottal = null; }

        _elapsed = 0;
        _isDragging = false;
        _showResult = false;
        _isVictory = false;
        _score = 0;
        _endlessScore = 0;
        _endlessTimer = EndlessTimeLimit;
        _ayahTimer = 0;
        _ayahTimeUp = false;
        _scrollOffset = 0;
        _isScrollDragging = false;
        _prevScrollWheel = Mouse.GetState().ScrollWheelValue;

        var cw = 340; var ch = 140; var cx2 = (_game.Width - cw) / 2; var cy2 = (_game.Height - ch) / 2;
        _confirmYesRect = new Rectangle(cx2 + 30, cy2 + ch - 52, 120, 36);
        _confirmNoRect  = new Rectangle(cx2 + cw - 150, cy2 + ch - 52, 120, 36);
        _showExitConfirm = false;

        if (_mode == WordOrderMode.Story)
            LoadStory();
        else if (_mode == WordOrderMode.Endless)
            LoadEndless();
        else
            LoadNormal();

        _loaded = _surah != null && _verses != null;
        if (_loaded) LoadAyah(0);
    }

    public void SetStoryMode(bool storyMode)
    {
        if (storyMode)
            _mode = WordOrderMode.Story;
        else if (_mode == WordOrderMode.Story)
            _mode = WordOrderMode.Normal;
    }

    public void SetEndlessMode(bool endless)
    {
        _mode = endless ? WordOrderMode.Endless : WordOrderMode.Normal;
    }

    private void LoadNormal()
    {
        _surah = Quran.GetSurah(96);
        _verses = _surah != null ? Quran.GetAyahs(_surah.Number) : null;
        _currentAyahIndex = 0;
    }

    private void LoadStory()
    {
        _surah = Quran.GetSurah(96);
        _verses = _surah != null ? Quran.GetAyahs(_surah.Number) : null;
        if (_verses == null || _verses.Count == 0) { _selectedIndices = null; return; }
        var count = Math.Min(5, _verses.Count);
        var pool = new int[_verses.Count];
        for (var i = 0; i < pool.Length; i++) pool[i] = i;
        var rng = new Random();
        for (var i = pool.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        _selectedIndices = new int[count];
        Array.Copy(pool, _selectedIndices, count);
        Array.Sort(_selectedIndices);
        _currentAyahIndex = 0;
        _wrongAttempts = 0;
        _isFailed = false;
    }

    private void LoadEndless()
    {
        var totalSurahs = Quran.TotalSurahs;
        if (totalSurahs == 0) { _surah = null; _verses = null; return; }
        PickRandomSurah();
    }

    private void PickRandomSurah()
    {
        var sn = _rng.Next(1, Quran.TotalSurahs + 1);
        _surah = Quran.GetSurah(sn);
        if (_surah == null) { _verses = null; return; }
        _verses = Quran.GetAyahs(_surah.Number);
    }

    private T SafeLoad<T>(string path) where T : class
    {
        try { return _game.Content.Load<T>(path); }
        catch { return null; }
    }

    private void LoadAyah(int index)
    {
        if (_mode == WordOrderMode.Story && _selectedIndices != null && index >= _selectedIndices.Length)
        {
            _isVictory = true;
            return;
        }
        var actualIndex = _mode == WordOrderMode.Story && _selectedIndices != null
            ? _selectedIndices[index] : index;
        if (_verses == null || actualIndex >= _verses.Count)
        {
            if (_mode == WordOrderMode.Endless)
            {
                _isVictory = true;
                return;
            }
            _isVictory = true;
            return;
        }

        _currentAyahIndex = index;
        _currentAyah = _verses[actualIndex];
        _words = _currentAyah.GetWords();
        if (_words == null || _words.Length == 0) { AdvanceAyah(); return; }

        _wrongAttempts = 0;
        _isFailed = false;
        var n = _words.Length;
        _slotStates = new SlotState[n];
        _slotContents = new int[n];
        _slotCorrect = new bool[n];
        for (var i = 0; i < n; i++) _slotContents[i] = -1;

        _shuffledOrder = Shuffle(n);
        _wordLabels = new string[n];
        for (var i = 0; i < n; i++)
            _wordLabels[i] = _words[_shuffledOrder[i]];

        _slotRects = new Rectangle[n];
        _wordRects = new Rectangle[n];

        var cx = _game.Width / 2;
        var slotCols = Math.Max(1, Math.Min(n, (_game.Width - 40) / (SlotW + SlotGap)));
        var slotRows = (n + slotCols - 1) / slotCols;
        var slotTotalW = slotCols * (SlotW + SlotGap) - SlotGap;
        var slotStartX = cx - slotTotalW / 2;

        for (var i = 0; i < n; i++)
        {
            var revI = n - 1 - i;
            var col = i % slotCols;
            var row = i / slotCols;
            _slotRects[revI] = new Rectangle(
                slotStartX + col * (SlotW + SlotGap),
                SlotY + row * SlotRowH,
                SlotW, SlotH);
        }

        var slotBottom = SlotY + slotRows * SlotRowH;
        var wordStartY = Math.Max(WordStartY, slotBottom + 36);

        var cols = Math.Min(5, n);
        var rows = (n + cols - 1) / cols;
        var wordStartX = cx - (cols * (SlotW + SlotGap) - SlotGap) / 2;

        for (var i = 0; i < n; i++)
        {
            var col = i % cols;
            var row = i / cols;
            var wx = wordStartX + col * (SlotW + SlotGap);
            var wy = wordStartY + row * (WordH + 14);
            _wordRects[i] = new Rectangle(wx, wy, SlotW, WordH);
        }

        _selectedWordIndex = -1;
        _showResult = false;
        _inputCooldown = 0;
        _isDragging = false;
        _dragWordIndex = -1;
        _prevTouchDown = false;
        _scrollOffset = 0;
        _prevScrollWheel = Mouse.GetState().ScrollWheelValue;

        _contentMaxY = wordStartY + rows * (WordH + 10) + 40;

        if (_mode == WordOrderMode.Endless)
        {
            _endlessTimer = Math.Max(30f, _words.Length * 8f);
        }
        else
        {
            _ayahTimer = Math.Max(45f, _words.Length * 10f);
            _ayahTimeUp = false;
        }
    }

    private static int[] Shuffle(int n)
    {
        var arr = new int[n];
        for (var i = 0; i < n; i++) arr[i] = i;
        var rng = new Random();
        for (var i = n - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }

    public void Update(float dt)
    {
        if (!_loaded || _surah == null) return;
        _inputCooldown = Math.Max(0, _inputCooldown - dt);

        /* ── exit confirm popup: process FIRST, freeze everything ── */
        if (_showExitConfirm)
        {
            var vt = _game.GetTouch();
            if (vt.IsDown)
            {
                var cp = vt.Position;
                if (_confirmYesRect.Contains(cp))
                {
                    Audio.PlaySfx(_sfxClick);
                    Audio.StopBgm();
                    _inputCooldown = GameConfig.ClickCooldown;
                    _showExitConfirm = false;
                    _game.SceneManager.SwitchTo(SceneId.Menu);
                    return;
                }
                if (_confirmNoRect.Contains(cp))
                {
                    Audio.PlaySfx(_sfxClick);
                    _showExitConfirm = false;
                    _inputCooldown = GameConfig.ClickCooldown;
                    return;
                }
            }
            return;
        }

        _elapsed += dt;

        if (_isVictory || _isFailed)
        {
            if (_inputCooldown > 0) return;
                var vt = _game.GetTouch();
                if (vt.IsDown || Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    Audio.PlaySfx(_sfxClick);
                    if (_isVictory && _mode == WordOrderMode.Story)
                    {
                        Audio.StopBgm();
                        Save.MarkChapterCompleted("al-alaq");
                        LogHelper.Trace("WordOrder victory -> Library(96)");
                        var library = _game.SceneManager.GetScene(SceneId.Library) as LibraryScene;
                        if (library != null) library.VictorySurahNumber = 96;
                        LogHelper.Trace("WordOrder about to SwitchTo Library");
                        _game.SceneManager.SwitchTo(SceneId.Library);
                        LogHelper.Trace("WordOrder SwitchTo Library returned (should not see this before SceneManager trace)");
                    }
                    else
                    {
                        Audio.StopBgm();
                        _game.SceneManager.SwitchTo(SceneId.Menu);
                    }
                    _inputCooldown = GameConfig.ClickCooldown;
                }
                return;
        }

        if (_showResult)
        {
            _resultTimer -= dt;
            if (_resultTimer <= 0)
            {
                if (_ayahTimeUp)
                {
                    _ayahTimeUp = false;
                    AdvanceAyah();
                }
                else if (AllCorrect())
                {
                    if (_mode == WordOrderMode.Endless)
                    {
                        _endlessScore += 10 + _words.Length * 2;
                        _endlessTimer = Math.Max(30f, _words.Length * 8f);
                        PickRandomSurah();
                        LoadAyah(0);
                    }
                    else
                    {
                        AdvanceAyah();
                    }
                }
                else
                {
                    if (_mode == WordOrderMode.Story)
                    {
                        _wrongAttempts++;
                        if (_wrongAttempts >= 2)
                        {
                            _isFailed = true;
                            _showResult = false;
                            return;
                        }
                    }
                    ResetWrong();
                    _showResult = false;
                }
            }
            return;
        }

        /* endless mode timer */
        if (_mode == WordOrderMode.Endless)
        {
            _endlessTimer -= dt;
            if (_endlessTimer <= 0)
            {
                _isVictory = true;
                return;
            }
        }
        else if (!_ayahTimeUp && !_showResult && _slotContents != null)
        {
            _ayahTimer -= dt;
            if (_ayahTimer <= 0)
            {
                _ayahTimeUp = true;
                for (var i = 0; i < _slotContents.Length; i++)
                {
                    _slotContents[i] = i;
                    _slotStates[i] = SlotState.Filled;
                    _slotCorrect[i] = false;
                }
                _showResult = true;
                _resultTimer = 1.2f;
            }
        }

        /* scroll wheel */
        var curScroll = Mouse.GetState().ScrollWheelValue;
        var scrollDelta = _prevScrollWheel - curScroll;
        _prevScrollWheel = curScroll;
        if (scrollDelta != 0)
        {
            _scrollOffset += scrollDelta * 0.05f;
            _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, Math.Max(0, _contentMaxY - _game.Height + HeaderEndY));
        }

        var touch = _game.GetTouch();
        var mp = touch.Position;
        _hoverBack = _backRect.Contains(mp);
        var subOff = -(int)_scrollOffset;
        var tempSub = new Rectangle(_game.Width / 2 - 80, 250 + subOff, 160, 40);
        _hoverSubmit = tempSub.Contains(mp);

        if (_inputCooldown > 0) return;

        if (touch.IsDown)
        {
            if (_hoverBack)
            {
                Audio.PlaySfx(_sfxClick);
                _inputCooldown = GameConfig.ClickCooldown;
                _showExitConfirm = true;
                return;
            }

            if (_hoverSubmit && AllSlotsFilled() && !_showResult)
            {
                Audio.PlaySfx(_sfxClick);
                CheckAnswers();
                return;
            }

            /* touch scroll */
            if (!_isDragging && mp.Y > HeaderEndY && _contentMaxY > _game.Height - HeaderEndY)
            {
                var overWord = OverAnyWord(mp);
                var overSlot = OverAnySlot(mp);
                if (!overWord && !overSlot)
                {
                    if (!_isScrollDragging)
                    {
                        _isScrollDragging = true;
                        _scrollDragStartY = mp.Y;
                    }
                    else
                    {
                        var dy = _scrollDragStartY - mp.Y;
                        _scrollOffset += dy;
                        _scrollOffset = MathHelper.Clamp(_scrollOffset, 0, Math.Max(0, _contentMaxY - _game.Height + HeaderEndY));
                        _scrollDragStartY = mp.Y;
                    }
                    return;
                }
            }
        }
        else
        {
            _isScrollDragging = false;
        }

        UpdateDragDrop(dt, touch, mp);
    }

    private void UpdateDragDrop(float dt, Game1.TouchState touch, Point mp)
    {
        var wmp = ToWorld(mp, _scrollOffset);
        var justReleased = _prevTouchDown && !touch.IsDown;
        _prevTouchDown = touch.IsDown;

        if (touch.IsDown)
        {
            if (!_isDragging)
            {
                for (var i = 0; i < _wordRects.Length; i++)
                {
                    var wordIdx = _shuffledOrder[i];
                    if (!IsWordFree(wordIdx)) continue;
                    if (_wordRects[i].Contains(wmp))
                    {
                        _isDragging = true;
                        _dragWordIndex = i;
                        _dragSourceSlot = -1;
                        _dragOffset = new Vector2(wmp.X - _wordRects[i].X, wmp.Y - _wordRects[i].Y);
                        _dragPos = mp.ToVector2();
                        _selectedWordIndex = -1;
                        return;
                    }
                }

                for (var i = 0; i < _slotRects.Length; i++)
                {
                    if (_slotStates[i] == SlotState.Filled && _slotRects[i].Contains(wmp))
                    {
                        var wordIdx = _slotContents[i];
                        _isDragging = true;
                        _dragWordIndex = -1;
                        _dragSourceSlot = i;
                        _dragOffset = new Vector2(wmp.X - _slotRects[i].X, wmp.Y - _slotRects[i].Y);
                        _dragPos = mp.ToVector2();
                        for (var j = 0; j < _shuffledOrder.Length; j++)
                        {
                            if (_shuffledOrder[j] == wordIdx)
                            {
                                _dragWordIndex = j;
                                break;
                            }
                        }
                        _slotStates[i] = SlotState.Empty;
                        _slotContents[i] = -1;
                        _slotCorrect[i] = false;
                        return;
                    }
                }
            }
            else
            {
                _dragPos = mp.ToVector2();
            }
        }
        else
        {
            if (_isDragging)
            {
                DropWord(wmp);
                _isDragging = false;
                _dragWordIndex = -1;
                _dragSourceSlot = -1;
                return;
            }

            if (!justReleased) return;

            if (_selectedWordIndex >= 0)
            {
                for (var i = 0; i < _slotRects.Length; i++)
                {
                    if (_slotRects[i].Contains(wmp) && _slotStates[i] == SlotState.Empty)
                    {
                        var wordIdx = _shuffledOrder[_selectedWordIndex];
                        _slotStates[i] = SlotState.Filled;
                        _slotContents[i] = wordIdx;
                        _selectedWordIndex = -1;
                        Audio.PlaySfx(_sfxStoneHit ?? _sfxClick);
                        _inputCooldown = GameConfig.InputDelay;
                        return;
                    }
                }
                _selectedWordIndex = -1;
                _inputCooldown = GameConfig.InputDelay;
                return;
            }

            for (var i = 0; i < _wordRects.Length; i++)
            {
                var wordIdx = _shuffledOrder[i];
                if (!IsWordFree(wordIdx)) continue;
                if (_wordRects[i].Contains(wmp))
                {
                    _selectedWordIndex = i;
                    Audio.PlaySfx(_sfxClick);
                    _inputCooldown = GameConfig.InputDelay;
                    return;
                }
            }

            for (var i = 0; i < _slotRects.Length; i++)
            {
                if (_slotRects[i].Contains(wmp) && _slotStates[i] == SlotState.Filled)
                {
                    var wordIdx = _slotContents[i];
                    ReturnWord(wordIdx);
                    Audio.PlaySfx(_sfxClick);
                    _inputCooldown = GameConfig.InputDelay;
                    return;
                }
            }
        }

        if ((Keyboard.GetState().IsKeyDown(Keys.Escape)) && _inputCooldown <= 0)
        {
            _inputCooldown = GameConfig.InputDelay;
            _showExitConfirm = !_showExitConfirm;
        }
    }

    private void DropWord(Point wmp)
    {
        if (_dragWordIndex < 0) return;
        var wordIdx = _shuffledOrder[_dragWordIndex];

        for (var i = 0; i < _slotRects.Length; i++)
        {
            if (_slotRects[i].Contains(wmp) && _slotStates[i] == SlotState.Empty)
            {
                _slotStates[i] = SlotState.Filled;
                _slotContents[i] = wordIdx;
                Audio.PlaySfx(_sfxStoneHit ?? _sfxClick);
                return;
            }
        }

        if (_dragSourceSlot >= 0)
        {
            _slotStates[_dragSourceSlot] = SlotState.Empty;
            _slotContents[_dragSourceSlot] = -1;
        }
    }

    private void ReturnWord(int wordIdx)
    {
        for (var i = 0; i < _slotContents.Length; i++)
        {
            if (_slotContents[i] == wordIdx)
            {
                _slotStates[i] = SlotState.Empty;
                _slotContents[i] = -1;
                _slotCorrect[i] = false;
                break;
            }
        }
    }

    private static Point ToWorld(Point screen, float scroll) =>
        new(screen.X, (int)(screen.Y + scroll));

    private bool OverAnyWord(Point mp)
    {
        if (_wordRects == null) return false;
        for (var i = 0; i < _wordRects.Length; i++)
            if (_wordRects[i].Contains(mp)) return true;
        return false;
    }

    private bool OverAnySlot(Point mp)
    {
        if (_slotRects == null) return false;
        for (var i = 0; i < _slotRects.Length; i++)
            if (_slotRects[i].Contains(mp)) return true;
        return false;
    }

    private bool IsWordFree(int wordIdx)
    {
        for (var i = 0; i < _slotContents.Length; i++)
            if (_slotContents[i] == wordIdx) return false;
        return true;
    }

    private bool AllSlotsFilled()
    {
        for (var i = 0; i < _slotStates.Length; i++)
            if (_slotStates[i] == SlotState.Empty) return false;
        return true;
    }

    private void CheckAnswers()
    {
        var allCorrect = true;
        for (var i = 0; i < _slotContents.Length; i++)
        {
            var correct = _slotContents[i] == i;
            _slotCorrect[i] = correct;
            if (!correct) allCorrect = false;
        }

        _showResult = true;
        _resultTimer = allCorrect ? 0.8f : 1.5f;

        if (allCorrect)
        {
            Audio.PlaySfx(_sfxCorrect);
            _score += 10 + _words.Length * 2;
        }
        else Audio.PlaySfx(_sfxWrong);
    }

    private bool AllCorrect()
    {
        for (var i = 0; i < _slotCorrect.Length; i++)
            if (!_slotCorrect[i]) return false;
        return true;
    }

    private void ResetWrong()
    {
        for (var i = 0; i < _slotCorrect.Length; i++)
        {
            if (!_slotCorrect[i])
            {
                var wordIdx = _slotContents[i];
                _slotStates[i] = SlotState.Empty;
                _slotContents[i] = -1;
            }
            _slotCorrect[i] = false;
        }
    }

    private void AdvanceAyah()
    {
        var nextIndex = _currentAyahIndex + 1;
        if (_verses != null && nextIndex < _verses.Count)
            LoadAyah(nextIndex);
        else
            _isVictory = true;
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        b.Begin();

        if (!_loaded)
        {
            if (_font != null)
            {
                var msg = "Loading...";
                var sz = _font.MeasureString(msg);
                b.DrawString(_font, msg, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2), _cream);
            }
            b.End();
            return;
        }

        if (_bgKayu != null)
            b.Draw(_bgKayu, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        else if (_bg != null)
            b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.55f);

        if (_isVictory || _isFailed)
        {
            if (_isVictory) DrawVictory(b);
            else DrawFailed(b);
            DrawBackButton(b);
            if (_showExitConfirm) DrawExitConfirm(b);
            b.End();
            return;
        }

        if (_font == null || _surah == null || _words == null)
        {
            b.End();
            return;
        }

        /* ── HUD (panel-based) ── */
        int cx = _game.Width / 2;
        int modeX = cx - ModeW / 2;
        var rMode = new Rectangle(modeX, HudY, ModeW, HudH);
        b.Draw(_panelGame1, rMode, Color.White);

        var isEndless = _mode == WordOrderMode.Endless;
        if (isEndless)
        {
            DrawTextCenteredShadow(b, $"ENDLESS - {_endlessScore}", rMode, _accent, 0.75f);
        }
        else
        {
            string modeTitle;
            if (_mode == WordOrderMode.Story)
                modeTitle = _game.Loc.Get("word_order_title") ?? "SUSUN KATA";
            else
                modeTitle = _game.Loc.Get("word_order_title") ?? "SUSUN KATA";
            DrawTextCenteredShadow(b, modeTitle, rMode, _accent, 0.75f);
        }

        // Score panel (left)
        int skorX = modeX - HudGap - IndW;
        var rSkor = new Rectangle(skorX, HudY, IndW, HudH);
        b.Draw(_panelGame2, rSkor, Color.White);
        string skorText = isEndless ? $"Skor: {_endlessScore}" : $"Skor: {_score}";
        DrawTextCentered(b, skorText, rSkor, _darkBrown, 0.75f);

        // Timer panel (right)
        int timerX = modeX + ModeW + HudGap;
        var rTimer = new Rectangle(timerX, HudY, IndW, HudH);
        b.Draw(_panelGame2, rTimer, Color.White);
        float remain = isEndless ? _endlessTimer : _ayahTimer;
        string timerText = isEndless ? $"{remain:F1}s" : (_ayahTimeUp ? "HABIS!" : $"{remain:F1}s");
        var timerCol = remain < 10f && !_ayahTimeUp ? _dangerRed : _darkBrown;
        DrawTextCentered(b, timerText, rTimer, timerCol, 0.75f);

        /* ── Ayah reference & translation ── */
        if (_currentAyah != null)
        {
            var ayahRef = $"{_surah?.EnglishName ?? ""} : {_currentAyah.VerseNumber}";
            var refSz = _font.MeasureString(ayahRef);
            b.DrawString(_font, ayahRef, new Vector2(cx - refSz.X / 2, AyahRefY), _accent, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

            var lang = _game.Loc.CurrentLang;
            var translation = lang == "id" ? _currentAyah.TranslationId : _currentAyah.TranslationEn;
            if (string.IsNullOrEmpty(translation))
                translation = lang == "id" ? _currentAyah.TranslationEn : _currentAyah.TranslationId;

            float transEndY = TransY;
            if (!string.IsNullOrEmpty(translation))
            {
                var maxTransW = _game.Width - 60;
                var transScale = MathHelper.Clamp(640f / Math.Max(1, _font.MeasureString(translation).X), 0.5f, 0.8f);
                var transLines = WrapText(translation, maxTransW, transScale);
                var lineY = TransY;
                foreach (var l in transLines)
                {
                    var lSz = _font.MeasureString(l) * transScale;
                    b.DrawString(_font, l, new Vector2(cx - lSz.X / 2, lineY), _softCream, 0, Vector2.Zero, transScale, SpriteEffects.None, 0);
                    lineY += (int)lSz.Y + 3;
                }
                transEndY = lineY;
            }

            if (!string.IsNullOrEmpty(_currentAyah.Transliteration))
            {
                var maxTlW = _game.Width - 80;
                var tlScale = MathHelper.Clamp(620f / Math.Max(1, _font.MeasureString(_currentAyah.Transliteration).X), 0.45f, 0.6f);
                var tlLines = WrapText(_currentAyah.Transliteration, maxTlW, tlScale);
                var tlY = transEndY + 4;
                foreach (var l in tlLines)
                {
                    var lSz = _font.MeasureString(l) * tlScale;
                    b.DrawString(_font, l, new Vector2(cx - lSz.X / 2, tlY), _softCream * 0.8f, 0, Vector2.Zero, tlScale, SpriteEffects.None, 0);
                    tlY += (int)lSz.Y + 2;
                }
            }
        }
        else
        {
            var progressText = $"Ayat {_currentAyahIndex + 1}/{(_verses?.Count ?? 0)}";
            var pSz = _font.MeasureString(progressText);
            b.DrawString(_font, progressText, new Vector2(cx - pSz.X / 2, AyahRefY), _softCream * 0.7f, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0);
        }

        DrawSlots(b);
        DrawBank(b);

        if (AllSlotsFilled() && !_showResult)
        {
            var off = -(int)_scrollOffset;
            var subW = 160;
            var subH = 40;
            var subX = _game.Width / 2 - subW / 2;
            var subY = 250 + off;
            _submitRect = new Rectangle(subX, subY, subW, subH);
            var subCol = _hoverSubmit ? _accent : Color.White;
            b.Draw(_game.WhitePixel, _submitRect, new Color(62, 39, 35));
            b.Draw(_game.WhitePixel, new Rectangle(subX, subY, subW, 2), subCol);
            b.Draw(_game.WhitePixel, new Rectangle(subX, subY + subH - 2, subW, 2), subCol);
            var subText = _game.Loc?.Get("check") ?? "Cek";
            var subTextSz = _font.MeasureString(subText);
            b.DrawString(_font, subText, new Vector2(subX + (subW - subTextSz.X) / 2, subY + (subH - subTextSz.Y) / 2), _cream);
        }

        if (_ayahTimeUp && _showResult)
        {
            var msg = _game.Loc?.Get("times_up") ?? "Waktu Habis!";
            var sz = _font.MeasureString(msg);
            b.DrawString(_font, msg, new Vector2((_game.Width - sz.X) / 2, 250 - (int)_scrollOffset - 40),
                _dangerRed, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
        }

        if (_isDragging && _dragWordIndex >= 0)
        {
            var wordIdx = _shuffledOrder[_dragWordIndex];
            if (IsWordFree(wordIdx) || _dragSourceSlot >= 0)
            {
                var at = _game.ArabicText;
                var text = _wordLabels[_dragWordIndex];
                var fontSize = 18f;
                var tSz = at != null ? at.MeasureString(text, fontSize) : Vector2.Zero;
                var scale = Math.Min(0.65f, (SlotW - 8f) / Math.Max(1, tSz.X));
                var dw = (int)(SlotW * scale);
                var dh = (int)(WordH * scale);
                var dRect = new Rectangle((int)_dragPos.X - (int)_dragOffset.X, (int)_dragPos.Y - (int)_dragOffset.Y, SlotW, WordH);
                b.Draw(_game.WhitePixel, dRect, _accent * 0.8f);
                b.Draw(_game.WhitePixel, new Rectangle(dRect.X, dRect.Y, dRect.Width, 2), Color.Gold);
                if (at != null)
                    at.DrawString(b, text, fontSize, new Vector2(dRect.X + dRect.Width / 2, dRect.Y + dRect.Height / 2), _cream, 0, new Vector2(tSz.X / 2, tSz.Y / 2), scale, SpriteEffects.None, 0);
            }
        }

        DrawBackButton(b);
        if (_showExitConfirm) DrawExitConfirm(b);
        b.End();
    }

    private void DrawSlots(SpriteBatch b)
    {
        var at = _game.ArabicText;
        var off = -(int)_scrollOffset;
        for (var i = 0; i < _slotRects.Length; i++)
        {
            if (_isDragging && _dragSourceSlot == i) continue;

            var r = _slotRects[i];
            var sr = new Rectangle(r.X, r.Y + off, r.Width, r.Height);
            var col = _slotStates[i] == SlotState.Empty ? new Color(55, 45, 38) : new Color(45, 35, 28);
            if (_showResult)
                col = _slotCorrect[i] ? _correctGreen : _wrongRed;

            b.Draw(_game.WhitePixel, sr, col);
            b.Draw(_game.WhitePixel, new Rectangle(sr.X, sr.Y, sr.Width, 3), _accent * 0.6f);
            b.Draw(_game.WhitePixel, new Rectangle(sr.X, sr.Y + sr.Height - 3, sr.Width, 3), _accent * 0.6f);

            var numText = $"{i + 1}";
            var numSz = _font.MeasureString(numText);
            b.DrawString(_font, numText, new Vector2(sr.Right - numSz.X - 5, sr.Y + 4), _accent * 0.6f, 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);

            if (at != null && _slotStates[i] == SlotState.Filled && _slotContents[i] >= 0 && _slotContents[i] < _words.Length)
            {
                var text = _words[_slotContents[i]];
                var fontSize = 22f;
                var tSz = at.MeasureString(text, fontSize);
                var maxW = Math.Max(SlotW - 16f, _game.Width * 0.45f);
                var scale = MathHelper.Clamp(maxW / Math.Max(1, tSz.X), 0.55f, 0.85f);
                at.DrawString(b, text, fontSize, new Vector2(sr.X + sr.Width / 2, sr.Y + sr.Height / 2), _cream, 0, new Vector2(tSz.X / 2, tSz.Y / 2), scale, SpriteEffects.None, 0);
            }
        }
    }

    private void DrawBank(SpriteBatch b)
    {
        var at = _game.ArabicText;
        var off = -(int)_scrollOffset;
        for (var i = 0; i < _wordRects.Length; i++)
        {
            if (_isDragging && _dragWordIndex == i) continue;

            var wordIdx = _shuffledOrder[i];
            if (!IsWordFree(wordIdx)) continue;

            var r = _wordRects[i];
            var wr = new Rectangle(r.X, r.Y + off, r.Width, r.Height);
            var isSelected = i == _selectedWordIndex;
            var col = isSelected ? _accent : new Color(68, 50, 38);
            b.Draw(_game.WhitePixel, wr, col);
            b.Draw(_game.WhitePixel, new Rectangle(wr.X, wr.Y, wr.Width, 3), _accent * 0.3f);
            b.Draw(_game.WhitePixel, new Rectangle(wr.X, wr.Y + wr.Height - 3, wr.Width, 3), _accent * 0.3f);

            if (isSelected)
                b.Draw(_game.WhitePixel, new Rectangle(wr.X + 2, wr.Y, wr.Width - 4, 3), Color.Gold);

            if (at == null) continue;

            var text = _wordLabels[i];
            var fontSize = 22f;
            var tSz = at.MeasureString(text, fontSize);
            var maxW = Math.Max(SlotW - 16f, _game.Width * 0.45f);
            var scale = MathHelper.Clamp(maxW / Math.Max(1, tSz.X), 0.55f, 0.85f);
            at.DrawString(b, text, fontSize, new Vector2(wr.X + wr.Width / 2, wr.Y + wr.Height / 2), _cream, 0, new Vector2(tSz.X / 2, tSz.Y / 2), scale, SpriteEffects.None, 0);
        }
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

    private void DrawVictory(SpriteBatch b)
    {
        var loc = _game.Loc;
        if (_mode == WordOrderMode.Endless)
        {
            b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), new Color(0, 0, 0, 220));
            var msg = $"{loc?.Get("times_up") ?? "Waktu Habis!"} Skor: {_endlessScore}";
            var sz = _font.MeasureString(msg);
            b.DrawString(_font, msg, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2 - 40), _accent, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
            var subMsg = loc?.Get("tap_replay") ?? "Tap untuk kembali";
            var subSz = _font.MeasureString(subMsg);
            b.DrawString(_font, subMsg, new Vector2((_game.Width - subSz.X) / 2, _game.Height / 2 + 20), _cream * 0.7f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
            return;
        }

        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), new Color(0, 0, 0, 220));
        {
            var msg = $"Selesai! Surah {_surah?.EnglishName ?? "Al-Alaq"}";
            var sz = _font.MeasureString(msg);
            b.DrawString(_font, msg, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2 - 40), _accent, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
            var subMsg = loc?.Get("tap_replay") ?? "Tap untuk lanjut";
            var subSz = _font.MeasureString(subMsg);
            b.DrawString(_font, subMsg, new Vector2((_game.Width - subSz.X) / 2, _game.Height / 2 + 20), _cream * 0.7f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
        }
    }

    private void DrawFailed(SpriteBatch b)
    {
        var loc = _game.Loc;
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), new Color(0, 0, 0, 220));
        var msg = "Gagal! 2x salah dalam satu ayat.";
        var sz = _font.MeasureString(msg);
        b.DrawString(_font, msg, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2 - 40), _wrongRed, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
        var subMsg = loc?.Get("tap_replay") ?? "Tap untuk kembali";
        var subSz = _font.MeasureString(subMsg);
        b.DrawString(_font, subMsg, new Vector2((_game.Width - subSz.X) / 2, _game.Height / 2 + 20), _cream * 0.7f, 0, Vector2.Zero, 0.8f, SpriteEffects.None, 0);
    }

    private void DrawBackButton(SpriteBatch b)
    {
        if (_isDragging) return;
        var iconSz = 48;
        var iconX = _game.Width - iconSz - 12;
        _backRect = new Rectangle(iconX, 12, iconSz, iconSz);
        var iCol = _hoverBack ? _accent : Color.White * 0.8f;
        b.Draw(_iconHome, _backRect, iCol);
    }

    private void DrawTextCentered(SpriteBatch b, string text, Rectangle rect, Color color, float scale = 1f)
    {
        if (string.IsNullOrEmpty(text)) return;
        var sz = _font.MeasureString(text) * scale;
        var x = rect.X + (rect.Width - sz.X) / 2;
        var y = rect.Y + (rect.Height - sz.Y) / 2;
        b.DrawString(_font, text, new Vector2(x, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private void DrawTextCenteredShadow(SpriteBatch b, string text, Rectangle rect, Color color, float scale = 1f)
    {
        if (string.IsNullOrEmpty(text)) return;
        var off = 1.5f * scale;
        var sz = _font.MeasureString(text) * scale;
        var x = rect.X + (rect.Width - sz.X) / 2;
        var y = rect.Y + (rect.Height - sz.Y) / 2;
        b.DrawString(_font, text, new Vector2(x + off, y + off), Color.Black * 0.35f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        b.DrawString(_font, text, new Vector2(x, y), color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }

    private void DrawExitConfirm(SpriteBatch b)
    {
        var cw = 340; var ch = 140; var cx2 = (_game.Width - cw) / 2; var cy2 = (_game.Height - ch) / 2;
        var panelR = new Rectangle(cx2, cy2, cw, ch);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.65f);
        b.Draw(_game.WhitePixel, panelR, new Color(50, 38, 30));
        b.Draw(_game.WhitePixel, new Rectangle(panelR.X, panelR.Y, panelR.Width, 3), _accent);
        b.Draw(_game.WhitePixel, new Rectangle(panelR.X, panelR.Bottom - 3, panelR.Width, 3), _accent);

        var msg = "Apakah yakin keluar?";
        var mSz = _font.MeasureString(msg);
        b.DrawString(_font, msg, new Vector2(cx2 + (cw - mSz.X) / 2, cy2 + 16), _cream);

        PopupBtn(b, _confirmYesRect, "Ya", true);
        PopupBtn(b, _confirmNoRect, "Tidak", false);
    }

    private void PopupBtn(SpriteBatch b, Rectangle r, string text, bool isYes)
    {
        var bg = isYes ? new Color(194, 74, 47) : new Color(80, 70, 60);
        b.Draw(_game.WhitePixel, r, bg);
        b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y, r.Width, 2), Color.White * 0.2f);
        b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), Color.Black * 0.2f);
        var sz = _font.MeasureString(text);
        b.DrawString(_font, text, new Vector2(r.Center.X, r.Center.Y), _cream, 0, sz / 2, 0.8f, SpriteEffects.None, 0);
    }

    public void Unload()
    {
        Audio.StopBgm();
    }
}
