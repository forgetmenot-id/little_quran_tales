using System;
using LittleQuranTales.Data;
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
    private Texture2D _bgWood, _iconHome, _settingBorder;
    private Texture2D _bgGame, _bgGua, _loadingScreen;
    private SoundEffect _sfxClick;

    private readonly Color _darkBrown    = new(33, 33, 33);
    private readonly Color _mediumBrown  = new(93, 64, 55);
    private readonly Color _gold         = new(212, 175, 55);
    private readonly Color _terracotta   = new(194, 74, 47);
    private readonly Color _burntSienna  = new(233, 116, 81);
    private readonly Color _cream        = new(255, 248, 225);
    private readonly Color _upcomingGray = new(160, 140, 120);

    private float _scrollOffset, _targetScrollOffset, _velocity;
    private float _maxScroll;
    private bool _isDragging;
    private bool _wasTouching;
    private float _touchDownX, _dragStartX, _scrollStartX;
    private float _inputCooldown;
    private bool _hoverBack;
    private Rectangle _backRect;

    private Rectangle _arrowLeftRect, _arrowRightRect;
    private bool _hoverArrowLeft, _hoverArrowRight;

    private const int CardW       = 360;
    private const int CardH       = 460;
    private const int CardSpacing = 28;
    private const int StartX      = 50;
    private const int CardStartY  = 135;
    private const int PreviewH    = 200;
    private const int BorderThk   = 16;
    private const int PadLeft     = 22;
    private const int PadTop      = 12;
    private const float TitleScale = 0.75f;
    private const float DescScale  = 0.55f;
    private const float ScoreScale = 0.55f;
    private const int BtnW        = 130;
    private const int BtnH        = 38;
    private const int BtnBottomMargin = 40;
    private const float DragThreshold = 8f;
    private const int ArrowBtnSize = 44;

    private struct MiniGameEntry
    {
        public string Id;
        public string TitleKey;
        public string DescKey;
        public string ScoreKey;
        public string PreviewAsset;
        public int ButtonCount;
        public bool IsDummy;
    }

    private MiniGameEntry[] _games;
    private SaveManager Save => _game.Save;

    public MiniGameGalleryScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _bgWood = _game.Content.Load<Texture2D>("Images/BGs/bg_kayu");
        _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _settingBorder = _game.Content.Load<Texture2D>("Images/UI/setting_border");
        _bgGame = _game.Content.Load<Texture2D>("Images/BGs/bg_game");
        _bgGua = _game.Content.Load<Texture2D>("Images/BGs/bg_gua");
        _loadingScreen = _game.Content.Load<Texture2D>("Images/UI/LoadingScreen");
        _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");

        BuildGames();

        _scrollOffset = 0;
        _targetScrollOffset = 0;
        int total = _games.Length * (CardW + CardSpacing) - CardSpacing;
        int visible = _game.Width - StartX * 2;
        _maxScroll = Math.Max(0, total - visible);

        _arrowLeftRect = new Rectangle(8, CardStartY + CardH / 2 - ArrowBtnSize / 2, ArrowBtnSize, ArrowBtnSize);
        _arrowRightRect = new Rectangle(_game.Width - 8 - ArrowBtnSize, CardStartY + CardH / 2 - ArrowBtnSize / 2, ArrowBtnSize, ArrowBtnSize);

        _inputCooldown = GameConfig.ClickCooldown;
        _wasTouching = true;
    }

    private void BuildGames()
    {
        _games = new MiniGameEntry[5]
        {
            new() { Id = "ababil",     TitleKey = "ababil_defense",      DescKey = "ababil_desc",      ScoreKey = "ababil_defense", PreviewAsset = "bg_game",       ButtonCount = 2, IsDummy = false },
            new() { Id = "word_order", TitleKey = "word_order_title",    DescKey = "word_order_desc",  ScoreKey = "word_order",      PreviewAsset = "bg_gua",       ButtonCount = 2, IsDummy = false },
            new() { Id = "dummy_1",    TitleKey = "mini_game_dummy_title", DescKey = "mini_game_dummy_desc", ScoreKey = "", PreviewAsset = "loadingscreen", ButtonCount = 0, IsDummy = true },
            new() { Id = "dummy_2",    TitleKey = "mini_game_dummy_title", DescKey = "mini_game_dummy_desc", ScoreKey = "", PreviewAsset = "loadingscreen", ButtonCount = 0, IsDummy = true },
            new() { Id = "dummy_3",    TitleKey = "mini_game_dummy_title", DescKey = "mini_game_dummy_desc", ScoreKey = "", PreviewAsset = "loadingscreen", ButtonCount = 0, IsDummy = true },
        };
    }

    private float CardX(int index) => StartX + index * (CardW + CardSpacing) - _scrollOffset;

    private Rectangle CardRect(int index) => new(
        (int)CardX(index), CardStartY, CardW, CardH);

    private Rectangle PreviewRect(int index)
    {
        var cr = CardRect(index);
        return new Rectangle(cr.X + BorderThk, cr.Y + BorderThk, CardW - 2 * BorderThk, PreviewH);
    }

    private int BtnY(int index)
    {
        return CardStartY + CardH - BtnH - BtnBottomMargin;
    }

    private Rectangle BtnRect(int index, int btnIndex)
    {
        int count = _games[index].ButtonCount;
        int totalW = count * BtnW + (count - 1) * 10;
        int bx = (int)CardX(index) + (CardW - totalW) / 2;
        int by = BtnY(index);
        return new Rectangle(bx + btnIndex * (BtnW + 10), by, BtnW, BtnH);
    }

    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        var touch = _game.GetTouch();
        var mp = touch.Position;
        var ms = Mouse.GetState();
        bool isPressed = touch.IsDown;

        _hoverBack = _backRect.Contains(mp);
        _hoverArrowLeft = _maxScroll > 0 && _targetScrollOffset > 0 && _arrowLeftRect.Contains(mp);
        _hoverArrowRight = _maxScroll > 0 && _targetScrollOffset < _maxScroll && _arrowRightRect.Contains(mp);

        // scroll wheel
        if (!_isDragging)
        {
            int wheel = ms.ScrollWheelValue;
            if (_prevScrollWheel != 0)
            {
                int delta = _prevScrollWheel - wheel;
                _targetScrollOffset = Math.Clamp(_targetScrollOffset + delta * 0.15f, 0, _maxScroll);
            }
            _prevScrollWheel = wheel;
        }

        // smooth scroll
        _scrollOffset += (_targetScrollOffset - _scrollOffset) * Math.Min(1, dt * 10);
        if (Math.Abs(_scrollOffset - _targetScrollOffset) < 0.5f)
            _scrollOffset = _targetScrollOffset;
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);

        // drag start detection
        if (isPressed && !_wasTouching)
        {
            _touchDownX = mp.X;
            _dragStartX = mp.X;
            _scrollStartX = _scrollOffset;
            _isDragging = false;
            _velocity = 0;
        }

        // activate drag only past threshold
        if (isPressed && !_isDragging)
        {
            if (Math.Abs(mp.X - _touchDownX) > DragThreshold)
            {
                _isDragging = true;
            }
        }

        // drag update
        if (isPressed && _isDragging)
        {
            float dragDelta = _dragStartX - mp.X;
            _targetScrollOffset = Math.Clamp(_scrollStartX + dragDelta, 0, _maxScroll);
            _velocity = dragDelta * 3;
        }

        // release
        if (!isPressed)
        {
            if (_isDragging && Math.Abs(_velocity) > 20)
            {
                _targetScrollOffset = Math.Clamp(_targetScrollOffset + _velocity * 0.5f, 0, _maxScroll);
            }
            _isDragging = false;
        }

        // card + button hover
        for (int i = 0; i < _games.Length; i++)
        {
            for (int b = 0; b < _games[i].ButtonCount; b++)
            {
                var br = BtnRect(i, b);
                _setHover(i, b, br.Contains(mp) && !_isDragging);
            }
        }

        _wasTouching = isPressed;

        if (_inputCooldown > 0) return;

        // click handling (on press, not drag)
        if (isPressed && !_isDragging)
        {
            if (_hoverBack)
            {
                Click();
                _game.SceneManager.SwitchTo(SceneId.Menu);
                return;
            }

            if (_hoverArrowLeft)
            {
                Click();
                _targetScrollOffset = Math.Clamp(_targetScrollOffset - CardW - CardSpacing, 0, _maxScroll);
                return;
            }
            if (_hoverArrowRight)
            {
                Click();
                _targetScrollOffset = Math.Clamp(_targetScrollOffset + CardW + CardSpacing, 0, _maxScroll);
                return;
            }

            for (int i = 0; i < _games.Length; i++)
            {
                if (_games[i].IsDummy) continue;
                // button-level hit
                for (int b = 0; b < _games[i].ButtonCount; b++)
                {
                    if (BtnRect(i, b).Contains(mp))
                    {
                        Click();
                        HandleButton(i, b);
                        return;
                    }
                }
                // card-level hit (tap anywhere on the card)
                if (CardRect(i).Contains(mp))
                {
                    Click();
                    HandleCardClick(i);
                    return;
                }
            }
        }

        // keyboard
        var kb = Keyboard.GetState();
        if (_inputCooldown <= 0)
        {
            if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D))
            {
                _targetScrollOffset = Math.Clamp(_targetScrollOffset + CardW + CardSpacing, 0, _maxScroll);
                _inputCooldown = 0.25f;
            }
            if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A))
            {
                _targetScrollOffset = Math.Clamp(_targetScrollOffset - CardW - CardSpacing, 0, _maxScroll);
                _inputCooldown = 0.25f;
            }
            if ((kb.IsKeyDown(Keys.Escape) ||
                 GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed) &&
                _inputCooldown <= 0)
            {
                _inputCooldown = GameConfig.ClickCooldown;
                _game.SceneManager.SwitchTo(SceneId.Menu);
            }
        }
    }

    private int _prevScrollWheel;
    private bool[,] _hoverBtns = new bool[5, 2];

    private void _setHover(int card, int btn, bool val)
    {
        if (card < 0 || card >= 5) return;
        if (btn < 0 || btn >= 2) return;
        _hoverBtns[card, btn] = val;
    }

    private bool _getHover(int card, int btn)
    {
        if (card < 0 || card >= 5 || btn < 0 || btn >= 2) return false;
        return _hoverBtns[card, btn];
    }

    private bool IsGameUnlocked(string id) => id switch
    {
        "ababil" => Save.IsChapterCompleted("al-fil"),
        "word_order" => Save.IsChapterCompleted("al-alaq"),
        _ => false
    };

    private void Click()
    {
        _game.Audio.PlaySfx(_sfxClick);
        _inputCooldown = GameConfig.ClickCooldown;
    }

    private void HandleButton(int cardIndex, int btnIndex)
    {
        var g = _games[cardIndex];
        if (g.Id == "ababil")
        {
            if (!IsGameUnlocked(g.Id)) return;
            var mg = (MiniGameScene)_game.SceneManager.GetScene(SceneId.Minigame);
            mg.Difficulty = btnIndex == 0 ? "normal" : "endless";
            _game.SceneManager.SwitchTo(SceneId.Minigame);
        }
        else if (g.Id == "word_order")
        {
            var wo = (WordOrderScene)_game.SceneManager.GetScene(SceneId.WordOrder);
            wo.SetEndlessMode(btnIndex != 0);
            _game.SceneManager.SwitchTo(SceneId.WordOrder);
        }
    }

    private void HandleCardClick(int cardIndex)
    {
        var g = _games[cardIndex];
        if (g.Id == "ababil")
        {
            if (!IsGameUnlocked(g.Id)) return;
            var mg = (MiniGameScene)_game.SceneManager.GetScene(SceneId.Minigame);
            mg.Difficulty = "normal";
            _game.SceneManager.SwitchTo(SceneId.Minigame);
        }
        else if (g.Id == "word_order")
        {
            var wo = (WordOrderScene)_game.SceneManager.GetScene(SceneId.WordOrder);
            wo.SetEndlessMode(false);
            _game.SceneManager.SwitchTo(SceneId.WordOrder);
        }
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        var loc = _game.Loc;

        // ── Layer 1 – background + vignette ──────────────────────────────
        b.Begin();
        b.Draw(_bgWood, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.3f);
        b.End();

        // ── Layer 2 – title ──────────────────────────────────────────────
        b.Begin();
        var title = loc.Get("mini_games_title");
        var tSz = _font.MeasureString(title);
        const float titleScale = 1.4f;
        float tX = (_game.Width - tSz.X * titleScale) / 2;
        b.DrawString(_font, title, new Vector2(tX + 2, 28), Color.Black * 0.6f, 0, Vector2.Zero, titleScale, SpriteEffects.None, 0);
        b.DrawString(_font, title, new Vector2(tX, 26), _gold, 0, Vector2.Zero, titleScale, SpriteEffects.None, 0);
        float lineW = tSz.X * titleScale * 0.55f;
        float lineY = 26 + tSz.Y * titleScale + 10;
        b.Draw(_game.WhitePixel, new Rectangle((int)((_game.Width - lineW) / 2), (int)lineY, (int)lineW, 2), _gold * 0.6f);
        b.End();

        // ── Layer 3 – scissored scrollable cards ─────────────────────────
        var prevRaster = b.GraphicsDevice.RasterizerState;
        var sRaster = new RasterizerState { ScissorTestEnable = true };
        var prevScissor = b.GraphicsDevice.ScissorRectangle;

        int sY = CardStartY - 10;
        int sH = _game.Height - sY - 30;
        b.GraphicsDevice.ScissorRectangle = new Rectangle(0, sY, _game.Width, sH);
        b.GraphicsDevice.RasterizerState = sRaster;
        b.Begin(SpriteSortMode.Deferred, null, null, null, sRaster, null);

        for (int i = 0; i < _games.Length; i++)
        {
            var g = _games[i];
            var cr = CardRect(i);

            if (cr.X + CardW < 0 || cr.X > _game.Width) continue;

            float alpha = g.IsDummy ? 0.5f : 1f;

            // card border
            b.Draw(_settingBorder, cr, Color.White * alpha);

            // preview image
            var pr = PreviewRect(i);
            Texture2D previewTex = g.PreviewAsset switch
            {
                "bg_game" => _bgGame,
                "bg_gua" => _bgGua,
                _ => _loadingScreen
            };
            b.Draw(previewTex, pr, Color.White * alpha);

            // overall dim overlay for dummy cards
            if (g.IsDummy)
            {
                b.Draw(_game.WhitePixel, cr, Color.Black * 0.25f);
                // also dim the preview area a bit more
                b.Draw(_game.WhitePixel, pr, Color.Black * 0.3f);
            }

            // locked overlay for non-dummy locked games
            if (!g.IsDummy && !IsGameUnlocked(g.Id))
            {
                b.Draw(_game.WhitePixel, pr, Color.Black * 0.6f);
                var lockTxt = loc.Get("chapter_locked");
                var lkSz = _font.MeasureString(lockTxt);
                b.DrawString(_font, lockTxt,
                    new Vector2(pr.X + (pr.Width - lkSz.X) / 2, pr.Y + (pr.Height - lkSz.Y) / 2),
                    Color.White * 0.7f);
            }

            // title
            var titleCol = g.IsDummy ? _upcomingGray * 0.7f : (Color)_cream;
            var titleStr = loc.Get(g.TitleKey);
            b.DrawString(_font, titleStr,
                new Vector2(cr.X + PadLeft, cr.Y + PadTop + PreviewH + 16),
                titleCol, 0, Vector2.Zero, TitleScale, SpriteEffects.None, 0);

            // description (cream/white with good contrast)
            if (!g.IsDummy)
            {
                var descStr = loc.Get(g.DescKey);
                b.DrawString(_font, descStr,
                    new Vector2(cr.X + PadLeft, cr.Y + PadTop + PreviewH + 42),
                    Color.White * 0.75f, 0, Vector2.Zero, DescScale, SpriteEffects.None, 0);
            }

            // high score (active games only)
            if (!g.IsDummy)
            {
                float scoreY = cr.Y + PadTop + PreviewH + 68;
                var scoreCol = _gold * 0.75f;
                var ns = loc.Format("high_score_normal", Save.GetHighScore(g.ScoreKey + "_normal"));
                b.DrawString(_font, ns,
                    new Vector2(cr.X + PadLeft, scoreY), scoreCol, 0, Vector2.Zero, ScoreScale, SpriteEffects.None, 0);
                var es = loc.Format("high_score_endless", Save.GetHighScore(g.ScoreKey + "_endless"));
                b.DrawString(_font, es,
                    new Vector2(cr.X + PadLeft, scoreY + 22), scoreCol, 0, Vector2.Zero, ScoreScale, SpriteEffects.None, 0);
            }

            // lock hint for locked games
            if (!g.IsDummy && !IsGameUnlocked(g.Id))
            {
                var hint = loc.Get("status_locked_hint");
                var hintSz = _font.MeasureString(hint);
                float hy = BtnY(i) - 16;
                b.DrawString(_font, hint,
                    new Vector2(cr.X + (CardW - hintSz.X * 0.55f) / 2, hy),
                    _upcomingGray * 0.6f, 0, Vector2.Zero, 0.55f, SpriteEffects.None, 0);
            }

            // buttons
            for (int bIdx = 0; bIdx < g.ButtonCount; bIdx++)
            {
                var br = BtnRect(i, bIdx);
                bool hover = _getHover(i, bIdx);
                bool canClick = IsGameUnlocked(g.Id);
                float ba = canClick ? 1f : 0.4f;
                var bg = hover && canClick ? _burntSienna : _terracotta;

                b.Draw(_game.WhitePixel, br, bg * ba);
                b.Draw(_game.WhitePixel, new Rectangle(br.X, br.Y, br.Width, 2), Color.White * 0.2f * ba);
                b.Draw(_game.WhitePixel, new Rectangle(br.X, br.Y + br.Height - 2, br.Width, 2), Color.Black * 0.2f * ba);

                string label = g switch
                {
                    { Id: "ababil" } => loc.Get(bIdx == 0 ? "normal" : "endless"),
                    { Id: "word_order" } => loc.Get(bIdx == 0 ? "normal" : "endless"),
                    _ => ""
                };

                var lSz = _font.MeasureString(label);
                float lx = br.X + (br.Width - lSz.X * 0.7f) / 2;
                float ly = br.Y + (br.Height - lSz.Y * 0.7f) / 2;
                b.DrawString(_font, label, new Vector2(lx, ly), _cream * ba,
                    0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
            }
        }

        b.End();
        b.GraphicsDevice.ScissorRectangle = prevScissor;
        b.GraphicsDevice.RasterizerState = prevRaster;

        // ── Layer 4 – nav arrows + home icon ─────────────────────────────
        b.Begin();

        // left arrow
        if (_maxScroll > 0 && _targetScrollOffset > 0)
        {
            float arrowAlpha = _hoverArrowLeft ? 1f : 0.5f;
            var aCol = _gold * arrowAlpha;
            var aBg = _hoverArrowLeft ? new Color(60, 40, 20) * 0.7f : Color.Transparent;

            if (_hoverArrowLeft)
                b.Draw(_game.WhitePixel, _arrowLeftRect, aBg);

            // draw triangle pointing left
            int cx = _arrowLeftRect.X + _arrowLeftRect.Width / 2;
            int cy = _arrowLeftRect.Y + _arrowLeftRect.Height / 2;
            int tip = 6;
            // simple left arrow using white pixel dots
            for (int row = -tip; row <= tip; row++)
            {
                int half = tip - Math.Abs(row);
                for (int col = -half; col <= 0; col++)
                {
                    b.Draw(_game.WhitePixel,
                        new Rectangle(cx + col, cy + row, 2, 2), aCol);
                }
            }
        }

        // right arrow
        if (_maxScroll > 0 && _targetScrollOffset < _maxScroll)
        {
            float arrowAlpha = _hoverArrowRight ? 1f : 0.5f;
            var aCol = _gold * arrowAlpha;
            var aBg = _hoverArrowRight ? new Color(60, 40, 20) * 0.7f : Color.Transparent;

            if (_hoverArrowRight)
                b.Draw(_game.WhitePixel, _arrowRightRect, aBg);

            int cx = _arrowRightRect.X + _arrowRightRect.Width / 2;
            int cy = _arrowRightRect.Y + _arrowRightRect.Height / 2;
            int tip = 6;
            for (int row = -tip; row <= tip; row++)
            {
                int half = tip - Math.Abs(row);
                for (int col = 0; col <= half; col++)
                {
                    b.Draw(_game.WhitePixel,
                        new Rectangle(cx + col, cy + row, 2, 2), aCol);
                }
            }
        }

        // home icon
        const int iconSz = 56;
        int iconX = _game.Width - iconSz - 16;
        int iconY = 16;
        _backRect = new Rectangle(iconX, iconY, iconSz, iconSz);
        if (_hoverBack)
        {
            int gs = iconSz + 16;
            for (int r = gs; r > 0; r -= 4)
            {
                float dist = (float)r / gs;
                int off = (gs - r) / 2;
                b.Draw(_game.WhitePixel,
                    new Rectangle(iconX - 8 + off, iconY - 8 + off, r, r),
                    _gold * (1f - dist) * 0.15f);
            }
        }
        float ia = _hoverBack ? 1f : 0.75f;
        b.Draw(_iconHome, _backRect, _hoverBack ? _gold : Color.White * ia);
        b.End();
    }

    public void Unload()
    {
        _prevScrollWheel = 0;
    }
}
