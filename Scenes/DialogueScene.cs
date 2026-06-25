using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LittleQuranTales.Data;
using LittleQuranTales.Models;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace LittleQuranTales.Scenes;

public class DialogueScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;

    private ChapterData _chapter;
    private int _currentIndex;
    private string _currentChapterFile;

    private string _currentText = "";
    private float _charTimer;
    private int _charIndex;
    private bool _textComplete;
    private float _inputCooldown;
    private float _totalTime;

    private const float UIScale = 0.7f;
    private const float CharSpriteScale = 0.65f;

    private const float CharDelay = 0.03f;
    private const float InputDelay = 0.2f;
    private const float TextScale = 1.0f * UIScale;
    private const float NavScale = 0.8f;
    private const float NameTagScale = 0.85f * UIScale;
    private const float ReactScale = 0.75f * UIScale;

    private static readonly Color TextColor = new(245, 245, 220);
    private static readonly Color TextShadow = new(30, 20, 10, 160);
    private static readonly Color NavColor = new(212, 175, 55);
    private static readonly Color TagBg = new(62, 39, 35);
    private static readonly Color TagText = new(212, 175, 55);
    private static readonly Color ReactColor = new(200, 180, 140, 200);

    private const int TextBoxH = (int)(310 * UIScale);
    private const int PadX = (int)(55 * UIScale);
    private const int PadTopSpeaker = (int)(52 * UIScale);
    private const int PadTopNarration = (int)(68 * UIScale);
    private const int NavRX = 61;
    private const int TagW = (int)(175 * UIScale);
    private const int TagH = (int)(30 * UIScale);
    private const int CharRightMargin = (int)(75 * UIScale);
    private const float BrightnessFadeIn = 1.5f;

    private int BoxTop => _game.Height - TextBoxH;
    private int TagX => PadX;
    private int TagY => BoxTop - TagH + 6;
    private int NavBY => (int)(TextBoxH * 0.255f);

    private Texture2D _currentBg;
    private string _currentBgName;
    private float _bgBrightness = 1f;

    private Song _currentBgm;
    private string _currentBgmName;

    private SoundEffect _sfxClick;
    private Texture2D _iconHome;
    private Rectangle _homeRect;
    private bool _hoverHome;

    private readonly List<SpriteRender> _allSprites = new();
    private Texture2D _panelDialog;

    private string _curFx;
    private float _fxTimer;
    private float _shX, _shY, _fadeA;
    private Color _fadeCol = Color.Black;
    private float _glowT;

    private const float ShakeDur = 0.3f;
    private const float FadeDur = 0.5f;
    private const float GlowSpd = 2f;

    private bool _waitClick;

    private sealed class SpriteRender
    {
        public Texture2D Tex;
        public string Name;
        public float JsonScale;
    }

    private SaveManager Save => _game.Save;
    private AudioManager Audio => _game.Audio;

    public DialogueScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");
        _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _panelDialog = _game.Content.Load<Texture2D>("Images/UI/panel_dialog");
        _totalTime = 0;
        if (string.IsNullOrEmpty(_currentChapterFile))
            _currentChapterFile = ChapterPath.Prolog;
        LoadChapter();
    }

    public void LoadChapterFile(string path)
    {
        _currentChapterFile = path;
        LoadChapter();
    }

    private void LoadChapter()
    {
        _currentBg = null; _currentBgName = null; _currentBgmName = null;
        _allSprites.Clear(); _curFx = null; _fadeA = 0; _waitClick = false; _bgBrightness = 1f;

        using var stream = TitleContainer.OpenStream(_currentChapterFile);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _chapter = JsonSerializer.Deserialize<ChapterData>(json);
        if (_chapter?.Scenes == null || _chapter.Scenes.Count == 0)
            throw new InvalidOperationException($"Chapter file missing or invalid: {_currentChapterFile}");
        _currentIndex = 0;
        StartText();
    }

    private void StartText()
    {
        var scene = _chapter.Scenes[_currentIndex];
        var isEn = _game.Loc.CurrentLang == "en";
        _currentText = isEn && !string.IsNullOrEmpty(scene.TextEn) ? scene.TextEn : (scene.Text ?? "");
        _charIndex = 0; _charTimer = 0; _textComplete = false;
    }

    public void Update(float dt)
    {
        _totalTime += dt;
        _inputCooldown = MathHelper.Max(0, _inputCooldown - dt);

        var s = _chapter.Scenes[_currentIndex];
        ApplyData(s);
        UpdFx(dt);

        if (_bgBrightness < 1f)
            _bgBrightness = MathHelper.Min(1f, _bgBrightness + dt / BrightnessFadeIn);

        if (!_textComplete)
        {
            _charTimer += dt;
            while (_charTimer >= CharDelay && _charIndex < _currentText.Length)
            { _charTimer -= CharDelay; _charIndex++; }
            if (_charIndex >= _currentText.Length)
            { _charIndex = _currentText.Length; _textComplete = true; }
        }

        if (_inputCooldown > 0) return;

        var touch = _game.GetTouch();
        var click = Keyboard.GetState().IsKeyDown(Keys.Space) ||
                    touch.IsDown;

        var mp = touch.Position;
        _hoverHome = _homeRect.Contains(mp);

        if ((Keyboard.GetState().IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) && _inputCooldown <= 0)
        {
            Audio.PlaySfx(_sfxClick);
            Audio.StopBgm();
            _inputCooldown = GameConfig.ClickCooldown;
            _currentChapterFile = null;
            _game.SceneManager.SwitchTo(SceneId.Menu);
            return;
        }

        if (touch.IsDown && _hoverHome && _inputCooldown <= 0)
        {
            Audio.PlaySfx(_sfxClick);
            Audio.StopBgm();
            _inputCooldown = GameConfig.ClickCooldown;
            _currentChapterFile = null;
            _game.SceneManager.SwitchTo(SceneId.Menu);
            return;
        }

        if (click)
        {
            if (_waitClick) { _waitClick = false; Advance(); _inputCooldown = InputDelay; return; }

            if (_textComplete) { Audio.PlaySfx(_sfxClick); Advance(); }
            else { _charIndex = _currentText.Length; _textComplete = true; }
            _inputCooldown = InputDelay;
        }
    }

    private void ApplyData(DialogueSceneData s)
    {
        if (!string.IsNullOrEmpty(s.Background) && s.Background != _currentBgName)
        { _currentBgName = s.Background; _currentBg = _game.Content.Load<Texture2D>($"Images/BGs/{s.Background}"); _bgBrightness = 0f; }

        if (!string.IsNullOrEmpty(s.Bgm) && s.Bgm != _currentBgmName)
        {
            _currentBgmName = s.Bgm; Audio.StopBgm();
            _currentBgm = _game.Content.Load<Song>($"Audio/BGM/{s.Bgm}");
            Audio.PlayBgm(_currentBgm);
        }

        if (s.Sprites != null)
        {
            _allSprites.Clear();
            foreach (var sd in s.Sprites)
            {
                var tex = _game.Content.Load<Texture2D>($"Images/Sprites/{sd.Name}");
                _allSprites.Add(new SpriteRender
                {
                    Tex = tex,
                    Name = sd.Name,
                    JsonScale = sd.Scale
                });
            }
        }

        if (!string.IsNullOrEmpty(s.Effect) && s.Effect != _curFx)
        {
            _curFx = s.Effect; _fxTimer = 0; _waitClick = false;
            switch (_curFx)
            {
                case "fade_in": _fadeCol = Color.Black; _fadeA = 1f; break;
                case "fade_black": _fadeCol = Color.Black; _fadeA = 0f; break;
                case "fade_white": _fadeCol = Color.White; _fadeA = 0f; break;
                case "golden_glow": _glowT = 0; break;
            }
        }
    }

    private void UpdFx(float dt)
    {
        if (string.IsNullOrEmpty(_curFx)) return;
        _fxTimer += dt;

        switch (_curFx)
        {
            case "screen_shake":
                if (_fxTimer < ShakeDur)
                {
                    var i = MathHelper.Lerp(8f, 0f, _fxTimer / ShakeDur);
                    _shX = (float)(Random.Shared.NextDouble() * 2 - 1) * i;
                    _shY = (float)(Random.Shared.NextDouble() * 2 - 1) * i;
                }
                else { _shX = 0; _shY = 0; _curFx = null; }
                break;

            case "fade_in":
                _fadeA = MathHelper.Max(0, 1f - _fxTimer / FadeDur);
                if (_fxTimer >= FadeDur) { _fadeA = 0; _curFx = null; }
                break;

            case "fade_black": case "fade_white":
                _fadeA = MathHelper.Min(1f, _fxTimer / FadeDur);
                if (_fxTimer >= FadeDur)
                { _curFx = _curFx == "fade_black" ? "faded_black" : "faded_white"; _waitClick = true; }
                break;

            case "golden_glow": _glowT += dt; break;
        }
    }

    private void Advance()
    {
        _currentIndex++;
        _fadeA = 0;
        if (_currentIndex >= _chapter.Scenes.Count)
        {
            if (!string.IsNullOrEmpty(_chapter.Id))
                Save.MarkChapterCompleted(_chapter.Id);

            var n = _chapter.NextChapter;
            if (!string.IsNullOrEmpty(n) && n != "minigame")
            { _currentChapterFile = $"{ChapterPath.Directory}{n}.json"; LoadChapter(); return; }
            if (n == "minigame")
            { Audio.StopBgm(); _game.SceneManager.SwitchTo(SceneId.Minigame); return; }
            _currentChapterFile = null; Audio.StopBgm(); _game.SceneManager.SwitchTo(SceneId.Menu);
        }
        else
        { _curFx = null; _shX = 0; _shY = 0; _waitClick = false; StartText(); }
    }

    public void Draw()
    {
        _game.GraphicsDevice.Clear(Color.Black);
        var m = Matrix.CreateTranslation(_shX, _shY, 0);
        var b = _game.SpriteBatch;

        b.Begin(transformMatrix: m);
        DrawBg(b);
        DrawGlow(b);
        if (_fadeA > 0)
            b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), _fadeCol * _fadeA);
        DrawSheet(b);
        DrawReact(b);
        DrawBox(b);
        var iconSz = 40;
        _homeRect = new Rectangle(_game.Width - iconSz - 10, 10, iconSz, iconSz);
        b.Draw(_iconHome, _homeRect, _hoverHome ? Color.Gold : Color.White * 0.7f);
        b.End();
    }

    private void DrawBg(SpriteBatch b)
    {
        if (_currentBg == null) return;
        var c = Color.White * _bgBrightness;
        b.Draw(_currentBg, new Rectangle(0, 0, _game.Width, _game.Height), c);
        var dark = (1f - _bgBrightness) * 0.55f;
        if (dark > 0)
            b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * dark);
    }

    private void DrawGlow(SpriteBatch b)
    {
        if (_curFx != "golden_glow") return;
        var a = (float)(Math.Sin(_glowT * GlowSpd) * 0.25 + 0.25);
        if (a <= 0) return;

        var t = GetGlow();
        var cx = _game.Width / 2; var cy = _game.Height / 3;
        var sz = 300 + (float)Math.Sin(_glowT * 1.5f) * 80;
        var r = new Rectangle((int)(cx - sz / 2), (int)(cy - sz / 2), (int)sz, (int)sz);
        b.Draw(t, r, Color.Gold * a);
    }

    private static bool IsCharacterName(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("little_kid") || n.Contains("hudhud") || n.Contains("ababil");
    }

    private static bool IsSpeakerACharacter(string speakerLower)
    {
        return speakerLower.Contains("hudhud") || speakerLower.Contains("little") || speakerLower.Contains("kid") || speakerLower.Contains("ababil");
    }

    private static bool IsBuku(string name)
    {
        return name.ToLowerInvariant().Contains("buku_tua");
    }

    private string GetActiveSpeaker()
    {
        var s = _chapter.Scenes[_currentIndex];
        if (s.Narration) return null;
        return (s.Speaker ?? "").ToLowerInvariant();
    }

    private bool IsSpeakerMatch(string speakerLower, string spriteName)
    {
        var n = spriteName.ToLowerInvariant();
        if (speakerLower.Contains("hudhud")) return n.Contains("hudhud");
        if (speakerLower.Contains("little") || speakerLower.Contains("kid")) return n.Contains("little_kid");
        if (speakerLower.Contains("buku")) return n.Contains("buku_tua");
        if (speakerLower.Contains("ababil")) return n.Contains("ababil");
        return false;
    }

    private int GetSpriteY(SpriteRender spr, int h)
    {
        var n = spr.Name.ToLowerInvariant();
        if (n.Contains("buku_tua")) return BoxTop - h - (int)(110 * UIScale);
        if (n.Contains("hudhud")) return BoxTop - h + 0;
        if (n.Contains("little_kid")) return BoxTop - h + (int)(35 * UIScale);
        if (n.Contains("ababil")) return BoxTop - h + (int)(18 * UIScale);
        return BoxTop - h + (int)(12 * UIScale);
    }

    private void DrawSheet(SpriteBatch b)
    {
        var speaker = GetActiveSpeaker();

        foreach (var spr in _allSprites)
        {
            if (string.IsNullOrEmpty(speaker)) continue;
            if (!IsSpeakerMatch(speaker, spr.Name)) continue;

            var sc = CharSpriteScale * UIScale;
            var w = (int)(spr.Tex.Width * sc);
            var h = (int)(spr.Tex.Height * sc);
            var x = IsBuku(spr.Name)
                ? (_game.Width - w) / 2
                : _game.Width - w - CharRightMargin;
            var y = GetSpriteY(spr, h);
            b.Draw(spr.Tex, new Rectangle(x, y, w, h), Color.White);
        }
    }

    private void DrawReact(SpriteBatch b)
    {
        var speaker = GetActiveSpeaker();
        if (string.IsNullOrEmpty(speaker) || !IsSpeakerACharacter(speaker)) return;

        foreach (var spr in _allSprites)
        {
            if (!IsCharacterName(spr.Name)) continue;
            if (IsSpeakerMatch(speaker, spr.Name)) continue;

            var f = (float)(Math.Sin(_totalTime * 2.5) * 0.2 + 0.7);
            var sc = CharSpriteScale * UIScale;
            var w = (int)(spr.Tex.Width * sc);
            var h = (int)(spr.Tex.Height * sc);
            var sx = _game.Width - w - CharRightMargin;
            var sy = GetSpriteY(spr, h);

            var txt = "...";
            var sz = _font.MeasureString(txt) * ReactScale;
            var tx = sx + (w - sz.X) / 2;
            var ty = sy - sz.Y - 6;
            b.DrawString(_font, txt, new Vector2(tx, ty), ReactColor * f, 0, Vector2.Zero, ReactScale, SpriteEffects.None, 0);
        }
    }

    private void DrawBox(SpriteBatch b)
    {
        var t = BoxTop;
        b.Draw(_panelDialog, new Rectangle(0, t, _game.Width, TextBoxH), Color.White);

        var s = _chapter.Scenes[_currentIndex];
        var narr = s.Narration;
        var spk = narr ? "" : GetDisplayName(s.Speaker ?? "", _game.Loc);

        if (!string.IsNullOrEmpty(spk)) DrawTag(b, spk, t);

        var text = _currentText[.._charIndex];
        var ty = t + (narr ? PadTopNarration : PadTopSpeaker);
        DrawText(b, text, PadX, ty);

        if (_textComplete) DrawNav(b, t);
    }

    private static string GetDisplayName(string speaker, Services.LocalizationManager loc)
    {
        var s = speaker.ToLowerInvariant();
        if (s.Contains("hudhud")) return loc.Get("chara_hudhud");
        if (s.Contains("little") || s.Contains("kid")) return loc.Get("chara_little_kid");
        if (s.Contains("buku")) return loc.Get("chara_buku_tua");
        if (s.Contains("ababil")) return loc.Get("chara_ababil");
        return speaker;
    }

    private void DrawTag(SpriteBatch b, string name, int top)
    {
        var rx = TagX; var ry = TagY;
        b.Draw(_game.WhitePixel, new Rectangle(rx, ry, TagW, TagH), TagBg);

        var sz = _font.MeasureString(name) * NameTagScale;
        var tx = rx + (TagW - (int)sz.X) / 2;
        var ty = ry + (TagH - (int)sz.Y) / 2;
        b.DrawString(_font, name, new Vector2(tx, ty + 1), Color.Black * 0.25f, 0, Vector2.Zero, NameTagScale, SpriteEffects.None, 0);
        b.DrawString(_font, name, new Vector2(tx, ty), TagText, 0, Vector2.Zero, NameTagScale, SpriteEffects.None, 0);
    }

    private void DrawText(SpriteBatch b, string text, float x, float y)
    {
        var lines = text.Split('\n');
        var lh = (int)(_font.LineSpacing * TextScale);
        for (var i = 0; i < lines.Length; i++)
        {
            var p = new Vector2(x, y + i * lh);
            b.DrawString(_font, lines[i], p + Vector2.One, TextShadow, 0, Vector2.Zero, TextScale, SpriteEffects.None, 0);
            b.DrawString(_font, lines[i], p, TextColor, 0, Vector2.Zero, TextScale, SpriteEffects.None, 0);
        }
    }

    private void DrawNav(SpriteBatch b, int top)
    {
        var blink = (float)((Math.Sin(_totalTime * (2 * Math.PI / 1.2)) + 1) / 2);
        var a = MathHelper.Lerp(0.25f, 1f, blink);
        var txt = "> " + _game.Loc.Get("tap_continue");
        var sz = _font.MeasureString(txt) * NavScale;
        var x = _game.Width - sz.X - NavRX;
        var y = top + TextBoxH - sz.Y - NavBY;
        b.DrawString(_font, txt, new Vector2(x, y), NavColor * a, 0, Vector2.Zero, NavScale, SpriteEffects.None, 0);
    }

    private Texture2D _gt;
    private Texture2D GetGlow()
    {
        if (_gt == null)
        {
            _gt = new Texture2D(_game.GraphicsDevice, 64, 64);
            var d = new Color[64 * 64];
            for (var i = 0; i < d.Length; i++)
            {
                var x = i % 64; var y = i / 64;
                var dx = x - 32; var dy = y - 32;
                var dist = Math.Sqrt(dx * dx + dy * dy);
                var a = Math.Max(0.0, 1.0 - dist / 32.0);
                d[i] = Color.White * (float)(a * a);
            }
            _gt.SetData(d);
        }
        return _gt;
    }

    public void Unload()
    {
        Audio.StopBgm();
        if (_gt != null) { _gt.Dispose(); _gt = null; }
    }
}
