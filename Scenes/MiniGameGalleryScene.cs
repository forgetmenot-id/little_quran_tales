using System;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LittleQuranTales.Scenes;

public class MiniGameGalleryScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private Texture2D _bg, _iconHome, _panelDialog, _gameBg;
    private SoundEffect _sfxClick;

    private readonly Color _cream = new(234, 230, 223);
    private readonly Color _accent = new(212, 175, 55);

    private float _inputCooldown;
    private Rectangle _backRect;
    private bool _hoverBack;

    private Rectangle _normalBtn, _endlessBtn;
    private bool _hoverNormal, _hoverEndless;

    private const string ScoreKey = "ababil_defense";

    private bool _unlocked;
    private SaveManager Save => _game.Save;

    public MiniGameGalleryScene(Game1 game) { _game = game; }

    public void Load()
    {
        _unlocked = Save.IsChapterCompleted("al-fil");
        _font = _game.Content.Load<SpriteFont>("Fonts/GameFont");
        _bg = _game.Content.Load<Texture2D>("Images/UI/menu_bg");
        _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _panelDialog = _game.Content.Load<Texture2D>("Images/UI/panel_dialog");
        _gameBg = _game.Content.Load<Texture2D>("Images/BGs/bg_game");
        _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");

        var cardX = 70;
        var cardY = 150;
        var infoY = cardY + 248;
        var btnY2 = infoY + 130;
        var btnW = 145;
        var btnH = 44;
        _normalBtn = new Rectangle(cardX + 18, btnY2, btnW, btnH);
        _endlessBtn = new Rectangle(cardX + 380 - 18 - btnW, btnY2, btnW, btnH);
    }

    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        var touch = _game.GetTouch();
        var mp = touch.Position;

        _hoverBack = _backRect.Contains(mp);
        _hoverNormal = _normalBtn.Contains(mp);
        _hoverEndless = _endlessBtn.Contains(mp);

        if (_inputCooldown > 0) return;

        if (touch.IsDown)
        {
            if (_hoverBack)
            {
                _game.Audio.PlaySfx(_sfxClick);
                _inputCooldown = 0.3f;
                _game.SceneManager.SwitchTo("menu");
                return;
            }

            if (_hoverNormal)
            {
                _game.Audio.PlaySfx(_sfxClick);
                _inputCooldown = 0.3f;
                if (_unlocked)
                    _game.SceneManager.SwitchTo("minigame");
                return;
            }

            if (_hoverEndless)
            {
                _game.Audio.PlaySfx(_sfxClick);
                _inputCooldown = 0.3f;
                if (_unlocked)
                {
                    ((MiniGameScene)_game.SceneManager.GetScene("minigame")).Difficulty = "endless";
                    _game.SceneManager.SwitchTo("minigame");
                }
            }
        }

        var kb = Keyboard.GetState();
        if ((kb.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) && _inputCooldown <= 0)
        {
            _game.Audio.PlaySfx(_sfxClick);
            _inputCooldown = 0.3f;
            _game.SceneManager.SwitchTo("menu");
        }
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        b.Begin();
        var loc = _game.Loc;

        b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.65f);

        var title = loc.Get("mini_games_title");
        var tSz = _font.MeasureString(title);
        b.DrawString(_font, title, new Vector2((_game.Width - tSz.X) / 2, 30), _accent);

        DrawGameCard(b);

        var iconSz = 48;
        var iconX = _game.Width - iconSz - 12;
        _backRect = new Rectangle(iconX, 12, iconSz, iconSz);
        var iCol = _hoverBack ? _accent : Color.White * 0.8f;
        b.Draw(_iconHome, _backRect, iCol);

        b.End();
    }

    private void DrawGameCard(SpriteBatch b)
    {
        var loc = _game.Loc;
        var cardX = 70;
        var cardY = 150;
        var cardW = 380;
        var thumbW = cardW - 24;
        var thumbH = 220;
        var infoY = cardY + thumbH + 28;
        var infoX = cardX + 16;

        var cardH = infoY + 185 - cardY;
        b.Draw(_panelDialog, new Rectangle(cardX, cardY, cardW, cardH), Color.White);

        var thumbX = cardX + 12;
        var thumbY = cardY + 12;
        b.Draw(_gameBg, new Rectangle(thumbX, thumbY, thumbW, thumbH), Color.White);

        if (!_unlocked)
        {
            b.Draw(_game.WhitePixel, new Rectangle(thumbX, thumbY, thumbW, thumbH), Color.Black * 0.6f);
            var lockTxt = "LOCKED";
            var lkSz = _font.MeasureString(lockTxt);
            b.DrawString(_font, lockTxt, new Vector2(thumbX + (thumbW - lkSz.X) / 2, thumbY + (thumbH - lkSz.Y) / 2), Color.White * 0.7f, 0, Vector2.Zero, 0.9f, SpriteEffects.None, 0);
        }

        b.DrawString(_font, loc.Get("ababil_defense"), new Vector2(infoX + 2, infoY), Color.White, 0, Vector2.Zero, 1.0f, SpriteEffects.None, 0);
        b.DrawString(_font, loc.Get("ababil_desc"), new Vector2(infoX + 2, infoY + 32), Color.White * 0.7f, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);

        var normalScore = Save.GetHighScore(ScoreKey + "_normal");
        var endlessScore = Save.GetHighScore(ScoreKey + "_endless");

        b.DrawString(_font, loc.Format("high_score_normal", normalScore), new Vector2(infoX + 2, infoY + 72), Color.White * 0.8f, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0);
        b.DrawString(_font, loc.Format("high_score_endless", endlessScore), new Vector2(infoX + 2, infoY + 94), Color.White * 0.8f, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0);

        b.DrawString(_font, loc.Get("endless_desc"), new Vector2(infoX + 2, infoY + 116), new Color(212, 175, 55) * 0.7f, 0, Vector2.Zero, 0.6f, SpriteEffects.None, 0);

        var btnAlpha = _unlocked ? 1f : 0.4f;
        DrawButton(b, _normalBtn, loc.Get("normal"), _hoverNormal && _unlocked, new Color(62, 39, 35), btnAlpha);
        DrawButton(b, _endlessBtn, loc.Get("endless"), _hoverEndless && _unlocked, new Color(120, 50, 30), btnAlpha);

        if (!_unlocked)
        {
            var hint = loc.Get("status_locked_hint");
            var hintSz = _font.MeasureString(hint);
            b.DrawString(_font, hint, new Vector2(cardX + (cardW - hintSz.X) / 2, _endlessBtn.Y + _endlessBtn.Height + 14), Color.Gray * 0.6f, 0, Vector2.Zero, 0.65f, SpriteEffects.None, 0);
        }
    }

    private void DrawButton(SpriteBatch b, Rectangle rect, string text, bool hover, Color bgColor, float alpha)
    {
        var borderCol = hover ? _accent : new Color(160, 140, 100);
        var textCol = hover ? _accent : _cream;

        b.Draw(_game.WhitePixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), borderCol * alpha);
        b.Draw(_game.WhitePixel, new Rectangle(rect.X, rect.Y + rect.Height - 2, rect.Width, 2), borderCol * alpha);
        b.Draw(_game.WhitePixel, rect, bgColor * alpha);

        var tSz = _font.MeasureString(text);
        b.DrawString(_font, text, new Vector2(
            rect.X + (rect.Width - tSz.X) / 2,
            rect.Y + (rect.Height - tSz.Y) / 2), textCol * alpha);
    }

    public void Unload() { }
}
