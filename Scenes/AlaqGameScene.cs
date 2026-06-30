using System;
using System.Collections.Generic;
using System.Linq;
using LittleQuranTales.Data;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace LittleQuranTales.Scenes;

public class AlaqGameScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;

    private readonly Color _cream = new(234, 230, 223);
    private readonly Color _accent = new(212, 175, 55);
    private readonly Color _darkBrown = new(62, 39, 35);
    private readonly Color _terracotta = new(180, 100, 70);
    private readonly Color _correctGreen = new(80, 190, 80);
    private readonly Color _wrongRed = new(220, 60, 60);

    private Texture2D _bg, _bgGua, _iconHome;
    private SoundEffect _sfxClick, _sfxCorrect, _sfxWrong;
    private Song _bgm, _murottal;

    private SurahInfo _surah;
    private List<VerseInfo> _verses;
    private int _currentAyahIndex;
    private QuizData[] _quizData;
    private int _selectedAnswer = -1;
    private int _wrongAttempts;
    private bool _answered;
    private bool _isCorrect;
    private float _resultTimer;

    private Rectangle[] _answerRects;
    private int _hoveredAnswer = -1;

    private bool _gameOver;
    private float _gameOverT;
    private int _victoryPhase;
    private float _phaseTimer;
    private float _whiteOverA;
    private bool _murottalPlaying;

    private float _elapsed;
    private float _inputCooldown;
    private bool _loaded;

    private SaveManager Save => _game.Save;
    private AudioManager Audio => _game.Audio;
    private QuranDbService Quran => _game.Quran;

    private Rectangle _backRect;
    private bool _hoverBack;

    private class QuizData
    {
        public int AyahIndex;
        public string Question;
        public string QuestionEn;
        public string[] Answers;
        public string[] AnswersEn;
        public int CorrectIndex;
    }

    public AlaqGameScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _bgGua = SafeLoad<Texture2D>("Images/BGs/bg_gua");
        _bg = SafeLoad<Texture2D>("Images/UI/menu_bg");
        _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");
        _sfxCorrect = SafeLoad<SoundEffect>("Audio/SFX/sfx_correct");
        _sfxWrong = SafeLoad<SoundEffect>("Audio/SFX/sfx_wrong");
        _bgm = SafeLoad<Song>("Audio/BGM/bgm_minigame2");
        try { _murottal = _game.Content.Load<Song>("Audio/library/al-alaq"); }
        catch { _murottal = null; }

        if (_bgm != null) Audio.PlayBgm(_bgm);

        _surah = Quran.GetSurah(96);
        _verses = _surah != null ? Quran.GetAyahs(_surah.Number) : null;
        _loaded = _surah != null && _verses != null && _verses.Count > 0;

        if (_loaded)
            InitQuiz();

        _currentAyahIndex = 0;
        _gameOver = false;
        _gameOverT = 0;
        _victoryPhase = 0;
        _phaseTimer = 0;
        _whiteOverA = 0;
        _murottalPlaying = false;
        _elapsed = 0;
        _inputCooldown = 0;
        _backRect = new Rectangle(20, 16, 200, 32);
        _hoverBack = false;
    }

    private T SafeLoad<T>(string path) where T : class
    {
        try { return _game.Content.Load<T>(path); }
        catch { return null; }
    }

    private void InitQuiz()
    {
        var list = new List<QuizData>();

        void AddQ(int ayahIdx, string q, string qEn, string[] ans, string[] ansEn, int correct)
        {
            list.Add(new QuizData
            {
                AyahIndex = ayahIdx,
                Question = q,
                QuestionEn = qEn,
                Answers = ans,
                AnswersEn = ansEn,
                CorrectIndex = correct
            });
        }

        AddQ(0,
            "Apa kata pertama yang diperintahkan dalam surah ini?",
            "What is the first command in this surah?",
            new[] { "Bacalah", "Tulislah", "Sembahlah", "Berjalanlah" },
            new[] { "Read", "Write", "Worship", "Walk" },
            0);

        AddQ(1,
            "Dari apakah Allah SWT menciptakan manusia?",
            "From what did Allah create man?",
            new[] { "Segumpal darah", "Tanah liat", "Air", "Api" },
            new[] { "A clot of blood", "Clay", "Water", "Fire" },
            0);

        AddQ(2,
            "Sifat Allah apa yang disebut dalam ayat ketiga?",
            "Which attribute of Allah is mentioned in the third verse?",
            new[] { "Maha Mulia", "Maha Besar", "Maha Esa", "Maha Kuasa" },
            new[] { "The Most Generous", "The Greatest", "The One", "The All-Powerful" },
            0);

        AddQ(3,
            "Apa alat yang digunakan untuk mengajar menurut ayat ini?",
            "What tool is used for teaching according to this verse?",
            new[] { "Pena (Qalam)", "Lisan", "Kitab", "Suara" },
            new[] { "The Pen (Qalam)", "The Tongue", "The Book", "The Voice" },
            0);

        AddQ(4,
            "Apa yang Allah ajarkan kepada manusia?",
            "What did Allah teach mankind?",
            new[] { "Apa yang tidak diketahui", "Bahasa Arab", "Tulisan", "Matematika" },
            new[] { "What he did not know", "Arabic", "Writing", "Mathematics" },
            0);

        AddQ(5,
            "Apa sifat buruk manusia yang disebut dalam surah ini?",
            "What negative trait of man is mentioned in this surah?",
            new[] { "Melampaui batas", "Malas", "Pengecut", "Pendendam" },
            new[] { "Transgresses", "Lazy", "Cowardly", "Vengeful" },
            0);

        AddQ(6,
            "Kapan manusia melampaui batas?",
            "When does man transgress?",
            new[] { "Saat merasa cukup", "Saat miskin", "Saat sakit", "Saat tidur" },
            new[] { "When he thinks he is self-sufficient", "When poor", "When sick", "When asleep" },
            0);

        AddQ(18,
            "Apa perintah di akhir surah Al-Alaq?",
            "What is the command at the end of Surah Al-Alaq?",
            new[] { "Sujud dan mendekatlah", "Berdirilah", "Berlari", "Bersembunyi" },
            new[] { "Prostrate and draw near", "Stand up", "Run", "Hide" },
            0);

        _quizData = list.ToArray();
    }

    public void Update(float dt)
    {
        if (!_loaded) return;
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        _elapsed += dt;

        var kb = Keyboard.GetState();
        var touch = _game.GetTouch();

        if (_gameOver)
        {
            UpdateVictory(dt, kb, touch);
            return;
        }

        _hoverBack = _backRect.Contains(touch.Position);

        if (touch.IsDown && _hoverBack && _inputCooldown <= 0)
        {
            Audio.PlaySfx(_sfxClick);
            Audio.StopBgm();
            _inputCooldown = GameConfig.ClickCooldown;
            _game.SceneManager.SwitchTo(SceneId.StorySelection);
            return;
        }

        if (kb.IsKeyDown(Keys.Escape) && _inputCooldown <= 0)
        {
            _inputCooldown = GameConfig.InputDelay;
            Audio.StopBgm();
            _game.SceneManager.SwitchTo(SceneId.StorySelection);
            return;
        }

        if (_answered)
        {
            _resultTimer -= dt;
            if (_resultTimer <= 0)
            {
                if (_isCorrect || _wrongAttempts >= 2)
                    AdvanceAyah();
                else
                {
                    _answered = false;
                    _selectedAnswer = -1;
                }
            }
            return;
        }

        UpdateAnswers(touch);
    }

    private void UpdateVictory(float dt, KeyboardState kb, Game1.TouchState touch)
    {
        _gameOverT += dt;

        if (_victoryPhase == 0)
        {
            if (!_murottalPlaying && _gameOverT >= 0.5f)
            {
                _murottalPlaying = true;
                if (_murottal != null)
                {
                    Audio.StopBgm();
                    Audio.PlayBgm(_murottal, false);
                }
            }

            if (_gameOverT > 2f && (kb.IsKeyDown(Keys.Space) || touch.IsDown))
            {
                Audio.PlaySfx(_sfxClick);
                _victoryPhase = 1;
                _phaseTimer = 0;
            }
        }
        else if (_victoryPhase == 1)
        {
            _phaseTimer += dt;
            _whiteOverA = Math.Min(1f, _phaseTimer / 0.5f);
            if (_phaseTimer >= 1.2f)
            {
                _victoryPhase = 2;
                _phaseTimer = 0;
            }
        }
        else if (_victoryPhase == 2)
        {
            _phaseTimer += dt;
            if (_phaseTimer < 0.5f)
                _whiteOverA = Math.Max(0, 1f - _phaseTimer / 0.5f);
            else
                _whiteOverA = 0;

            var murottalDone = _murottal == null ||
                MediaPlayer.State == MediaState.Stopped;
            if ((_phaseTimer > 2f && murottalDone) || _phaseTimer > 120f)
            {
                _victoryPhase = 3;
            }
        }
        else if (_victoryPhase == 3)
        {
            Save.MarkChapterCompleted("al-alaq");
            var dialogue = _game.SceneManager.GetScene<DialogueScene>(SceneId.Dialogue);
            if (dialogue != null)
                dialogue.LoadChapterFile(ChapterPath.AlAlaqEnd);
            _game.SceneManager.SwitchTo(SceneId.Dialogue);
        }
    }

    private void UpdateAnswers(Game1.TouchState touch)
    {
        var mp = touch.Position;
        _hoveredAnswer = -1;

        if (_answerRects == null) return;

        for (var i = 0; i < _answerRects.Length; i++)
        {
            if (_answerRects[i].Contains(mp))
            {
                _hoveredAnswer = i;
                break;
            }
        }

        if (touch.IsDown && _hoveredAnswer >= 0 && _inputCooldown <= 0)
        {
            Audio.PlaySfx(_sfxClick);
            _inputCooldown = GameConfig.ClickCooldown;
            CheckAnswer(_hoveredAnswer);
        }
    }

    private void CheckAnswer(int idx)
    {
        var q = _quizData[_currentAyahIndex];
        _selectedAnswer = idx;
        _isCorrect = idx == q.CorrectIndex;
        _answered = true;
        _resultTimer = 1.2f;

        if (_isCorrect)
        {
            Audio.PlaySfx(_sfxCorrect ?? _sfxClick);
        }
        else
        {
            _wrongAttempts++;
            Audio.PlaySfx(_sfxWrong ?? _sfxClick);
        }
    }

    private void AdvanceAyah()
    {
        _currentAyahIndex++;
        _selectedAnswer = -1;
        _wrongAttempts = 0;
        _answered = false;
        _isCorrect = false;
        _resultTimer = 0;

        if (_currentAyahIndex >= _quizData.Length)
        {
            _gameOver = true;
            _gameOverT = 0;
        }
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        b.Begin();

        DrawBg(b);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.55f);

        if (!_loaded)
        {
            var msg = "Loading...";
            var sz = _font.MeasureString(msg);
            b.DrawString(_font, msg, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2), _cream);
            b.End();
            return;
        }

        if (_gameOver && _victoryPhase > 0)
        {
            DrawVictoryFx(b);
            DrawBackButton(b);
            b.End();
            return;
        }
        else if (_gameOver && _victoryPhase == 0)
        {
            DrawVictory(b);
            DrawBackButton(b);
            b.End();
            return;
        }

        DrawHeader(b);
        DrawAyahPanel(b);
        DrawAnswers(b);
        DrawBackButton(b);
        b.End();
    }

    private void DrawBg(SpriteBatch b)
    {
        if (_bgGua != null)
            b.Draw(_bgGua, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        else if (_bg != null)
            b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
    }

    private void DrawHeader(SpriteBatch b)
    {
        var title = $"{_surah.Number}. {_surah.EnglishName}";
        if (_verses != null && _currentAyahIndex < _quizData.Length)
        {
            var q = _quizData[_currentAyahIndex];
            var ayahNum = q.AyahIndex + 1;
            title += $" : {ayahNum}";
        }
        var tSz = _font.MeasureString(title);
        b.DrawString(_font, title, new Vector2((_game.Width - tSz.X) / 2, 12), _accent, 0, Vector2.Zero, 0.85f, SpriteEffects.None, 0);

        var loc = _game.Loc;
        var progress = loc.Format("alaq_progress", _currentAyahIndex + 1, _quizData.Length);
        var pSz = _font.MeasureString(progress);
        b.DrawString(_font, progress, new Vector2((_game.Width - pSz.X) / 2, 40), _cream * 0.6f, 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);

        if (_wrongAttempts > 0)
        {
            var attemptsLeft = 2 - _wrongAttempts;
            var attStr = loc.Format("alaq_attempts", attemptsLeft);
            var aSz = _font.MeasureString(attStr);
            var aCol = attemptsLeft == 1 ? _wrongRed : _cream * 0.6f;
            b.DrawString(_font, attStr, new Vector2((_game.Width - aSz.X) / 2, 60), aCol, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
        }
    }

    private void DrawAyahPanel(SpriteBatch b)
    {
        if (_verses == null || _currentAyahIndex >= _quizData.Length) return;

        var q = _quizData[_currentAyahIndex];
        if (q.AyahIndex < 0 || q.AyahIndex >= _verses.Count) return;

        var verse = _verses[q.AyahIndex];

        // panel background
        var pw = Math.Min(700, _game.Width - 40);
        var px = (_game.Width - pw) / 2;
        var py = 90;
        var ph = 160;

        b.Draw(_game.WhitePixel, new Rectangle(px, py, pw, ph), new Color(0, 0, 0, 120));
        b.Draw(_game.WhitePixel, new Rectangle(px, py, pw, 1), _accent * 0.4f);
        b.Draw(_game.WhitePixel, new Rectangle(px, py + ph - 1, pw, 1), _accent * 0.4f);

        // Arabic text
        var at = _game.ArabicText;
        var verseText = verse.Arabic;
        if (at != null && !string.IsNullOrEmpty(verseText))
        {
            var fontSize = 22f;
            var tSz = at.MeasureString(verseText, fontSize);
            var maxW = pw - 24;
            var sc = Math.Min(1f, maxW / Math.Max(1, tSz.X));
            at.DrawString(b, verseText, fontSize,
                new Vector2(px + pw / 2, py + 28),
                Color.Gold, 0, new Vector2(tSz.X / 2, tSz.Y / 2), sc, SpriteEffects.None, 0);
        }

        // translation
        var lang = _game.Loc.CurrentLang;
        var trans = lang == "id" ? verse.TranslationId : verse.TranslationEn;
        if (string.IsNullOrEmpty(trans))
            trans = lang == "id" ? verse.TranslationEn : verse.TranslationId;
        if (!string.IsNullOrEmpty(trans))
        {
            var transSz = _font.MeasureString(trans);
            var transScale = Math.Min(0.7f, (pw - 24) / Math.Max(1, transSz.X));
            b.DrawString(_font, trans, new Vector2(px + pw / 2, py + 60),
                _cream * 0.8f, 0, new Vector2(transSz.X / 2, 0), transScale, SpriteEffects.None, 0);
        }

        // transliteration
        if (!string.IsNullOrEmpty(verse.Transliteration))
        {
            var tl = verse.Transliteration;
            var tlSz = _font.MeasureString(tl);
            var tlScale = Math.Min(0.6f, (pw - 24) / Math.Max(1, tlSz.X));
            b.DrawString(_font, tl, new Vector2(px + pw / 2, py + 88),
                _accent * 0.7f, 0, new Vector2(tlSz.X / 2, 0), tlScale, SpriteEffects.None, 0);
        }

        // divider
        b.Draw(_game.WhitePixel, new Rectangle(px + 30, py + 118, pw - 60, 1), _accent * 0.25f);

        // question
        var question = lang == "id" ? q.Question : q.QuestionEn;
        var qSz = _font.MeasureString(question);
        var qScale = Math.Min(0.8f, (pw - 24) / Math.Max(1, qSz.X));
        b.DrawString(_font, question, new Vector2(px + pw / 2, py + 130),
            _cream, 0, new Vector2(qSz.X / 2, 0), qScale, SpriteEffects.None, 0);
    }

    private void DrawAnswers(SpriteBatch b)
    {
        if (_currentAyahIndex >= _quizData.Length) return;

        var q = _quizData[_currentAyahIndex];
        var lang = _game.Loc.CurrentLang;
        var answers = lang == "id" ? q.Answers : q.AnswersEn;
        var n = answers.Length;

        var pw = Math.Min(700, _game.Width - 40);
        var px = (_game.Width - pw) / 2;
        var btnW = 320;
        var btnH = 48;
        var gap = 12;
        var cols = 2;
        var totalW = cols * btnW + (cols - 1) * gap;
        var startX = (_game.Width - totalW) / 2;
        var startY = 280;
        var rows = (n + cols - 1) / cols;

        _answerRects = new Rectangle[n];

        for (var i = 0; i < n; i++)
        {
            var col = i % cols;
            var row = i / cols;
            var bx = startX + col * (btnW + gap);
            var by = startY + row * (btnH + gap);
            _answerRects[i] = new Rectangle(bx, by, btnW, btnH);
        }

        for (var i = 0; i < n; i++)
        {
            var r = _answerRects[i];
            Color bg;

            if (_answered)
            {
                if (i == q.CorrectIndex)
                    bg = _correctGreen;
                else if (i == _selectedAnswer)
                    bg = _wrongRed;
                else
                    bg = _darkBrown * 0.7f;
            }
            else if (i == _hoveredAnswer)
                bg = _terracotta;
            else
                bg = _darkBrown;

            b.Draw(_game.WhitePixel, r, bg);
            b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y, r.Width, 2), _accent * 0.4f);
            b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y + r.Height - 2, r.Width, 2), _accent * 0.4f);

            var label = answers[i];
            var lSz = _font.MeasureString(label);
            var lScale = Math.Min(0.7f, (btnW - 16) / Math.Max(1, lSz.X));
            b.DrawString(_font, label, new Vector2(r.Center.X, r.Center.Y),
                _cream, 0, lSz / 2, lScale, SpriteEffects.None, 0);
        }
    }

    private void DrawVictory(SpriteBatch b)
    {
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), new Color(0, 0, 0, 200));
        var loc = _game.Loc;
        var msg = loc.Get("alaq_victory");
        var sz = _font.MeasureString(msg);
        b.DrawString(_font, msg, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2 - 60), _accent, 0, Vector2.Zero, 1.1f, SpriteEffects.None, 0);

        var subMsg = loc.Get("alaq_read_sub");
        var subSz = _font.MeasureString(subMsg);
        b.DrawString(_font, subMsg, new Vector2((_game.Width - subSz.X) / 2, _game.Height / 2 - 10), _cream * 0.8f, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);

        var tapMsg = loc.Get("tap_replay");
        var tapSz = _font.MeasureString(tapMsg);
        b.DrawString(_font, tapMsg, new Vector2((_game.Width - tapSz.X) / 2, _game.Height / 2 + 50), _cream * 0.5f, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);
    }

    private void DrawVictoryFx(SpriteBatch b)
    {
        if (_whiteOverA > 0)
            b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.White * _whiteOverA);

        if (_victoryPhase == 2)
        {
            var loc = _game.Loc;
            var title = loc.Get("alaq_listen_title");
            var sz = _font.MeasureString(title);
            b.DrawString(_font, title, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2 - 24), Color.Gold);
            var sub = loc.Get("alaq_listen_sub");
            var subSz = _font.MeasureString(sub);
            var pulse = 0.6f + (float)Math.Sin(_elapsed * 2) * 0.2f;
            b.DrawString(_font, sub, new Vector2((_game.Width - subSz.X) / 2, _game.Height / 2 + 16), Color.White * pulse);
        }
    }

    private void DrawBackButton(SpriteBatch b)
    {
        var iconSz = 48;
        var iconX = _game.Width - iconSz - 12;
        _backRect = new Rectangle(iconX, 12, iconSz, iconSz);
        var iCol = _hoverBack ? _accent : Color.White * 0.8f;
        b.Draw(_iconHome, _backRect, iCol);
    }

    public void Unload()
    {
        Audio.StopBgm();
    }
}
