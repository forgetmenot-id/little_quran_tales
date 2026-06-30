using System;
using LittleQuranTales.Data;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LittleQuranTales.Scenes;

public class StorySelectionScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private Texture2D _bgWood, _iconHome, _panelCard, _hoverBtn;
    private SoundEffect _sfxClick;

    // ── Colour palette ──────────────────────────────────────────────────
    private readonly Color _darkBrown     = new(33, 33, 33);
    private readonly Color _mediumBrown   = new(93, 64, 55);
    private readonly Color _gold          = new(212, 175, 55);
    private readonly Color _terracotta    = new(194, 74, 47);
    private readonly Color _burntSienna   = new(233, 116, 81);
    private readonly Color _cream         = new(255, 248, 225);
    private readonly Color _forestGreen   = new(46, 125, 50);
    private readonly Color _upcomingGray  = new(160, 140, 120);

    // ── Scroll state ────────────────────────────────────────────────────
    private float _scrollOffset;
    private float _maxScroll;
    private Rectangle _scrollTrackRect;
    private int _prevScrollWheel;
    private float _inputCooldown;

    // ── Home button ─────────────────────────────────────────────────────
    private Rectangle _backRect;
    private bool _hoverBack;

    // ── Chapter data ────────────────────────────────────────────────────
    private ChapterEntry[] _chapters;
    private SaveManager Save => _game.Save;

    public enum ChapterStatus { Available, Completed, Upcoming }

    public struct ChapterEntry
    {
        public string Id;
        public string Title;
        public string SubKey;
        public string ChapterPath;
        public int GridRow;
        public int GridCol;
        public ChapterStatus Status;
        public bool HoverCard;
        public bool HoverPlay;
    }

    // ── Layout constants ────────────────────────────────────────────────
    private const int Cols         = 2;
    private const int CardW        = 560;
    private const int CardH        = 165;
    private const int CardX0       = 50;
    private const int CardX1       = 670;   // 50 + 560 + 60
    private const int CardVGap     = 28;
    private const int CardStartY   = 135;
    private const int PadLeft      = 22;
    private const int PadTop       = 16;
    private const float SubScale   = 0.7f;
    private const float BtnScale   = 0.8f;
    private const float BadgeScale = 0.65f;
    private const float UpScale    = 0.7f;
    private const int BtnW         = 110;
    private const int BtnH         = 38;
    private const int BtnRMarg     = 14;
    private const int btnBMarg     = 14;
    private const float HoverLift  = -4f;
    private const float TitleScale = 1.4f;

    public StorySelectionScene(Game1 game) { _game = game; }
    private bool _loaded;

    // ── Load ────────────────────────────────────────────────────────────
    public void Load()
    {
        if (_loaded) return;
        _loaded = true;

        _font      = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _bgWood    = _game.Content.Load<Texture2D>("Images/BGs/bg_kayu");
        _iconHome  = _game.Content.Load<Texture2D>("Images/UI/icon_home");
        _panelCard = _game.Content.Load<Texture2D>("Images/UI/panel_card");
        _hoverBtn  = _game.Content.Load<Texture2D>("Images/UI/hover_button");
        _sfxClick  = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");

        BuildChapters();
    }

    private void BuildChapters()
    {
        var defs = new (string id, string title, string subKey, string path, int row, int col)[]
        {
            ("al-fil",     "Chapter 1: Al-Fil",     "chapter_1_sub",     ChapterPath.Prolog,  0, 0),
            ("al-alaq",    "Chapter 2: Al-Alaq",    "chapter_2_sub",     ChapterPath.AlAlaq,  0, 1),
            ("al-fatihah", "Chapter 3: Al-Fatihah", "chapter_3_sub",     null,                1, 0),
            ("al-lahab",   "Chapter 4: Al-Lahab",   "chapter_4_sub",     null,                1, 1),
            ("an-nashr",   "Chapter 5: An-Nashr",   "chapter_5_sub",     null,                2, 0),
        };

        string[] order = { "al-fil", "al-alaq", "al-fatihah", "al-lahab", "an-nashr" };

        var list = new System.Collections.Generic.List<ChapterEntry>();
        for (int i = 0; i < defs.Length; i++)
        {
            var d = defs[i];
            ChapterStatus status;

            if (string.IsNullOrEmpty(d.path))
            {
                status = ChapterStatus.Upcoming;
            }
            else if (Save.IsChapterCompleted(d.id))
            {
                status = ChapterStatus.Completed;
            }
            else if (i == 0)
            {
                status = ChapterStatus.Available;
            }
            else if (Save.IsChapterCompleted(order[i - 1]))
            {
                status = ChapterStatus.Available;
            }
            else
            {
                status = ChapterStatus.Upcoming;
            }

            list.Add(new ChapterEntry
            {
                Id = d.id,
                Title = d.title,
                SubKey = d.subKey,
                ChapterPath = d.path,
                GridRow = d.row,
                GridCol = d.col,
                Status = status,
            });
        }

        _chapters = list.ToArray();

        int rows = 3;
        int totalH = rows * (CardH + CardVGap) - CardVGap;
        int visibleH = _game.Height - CardStartY - 30;
        _maxScroll = Math.Max(0, totalH - visibleH);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, _maxScroll);
    }

    // ── Geometry helpers ────────────────────────────────────────────────
    private int GetCellX(int col) => col == 0 ? CardX0 : CardX1;
    private int GetCellY(int row) => CardStartY + row * (CardH + CardVGap);
    private Rectangle CardRect(int row, int col) => new(GetCellX(col), GetCellY(row), CardW, CardH);
    private Rectangle PlayRect(int row, int col, int drawY) => new(
        GetCellX(col) + CardW - BtnW - BtnRMarg,
        drawY + CardH - BtnH - btnBMarg, BtnW, BtnH);

    // ── Update ──────────────────────────────────────────────────────────
    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        var touch = _game.GetTouch();
        var mp    = touch.Position;

        // scroll wheel
        var ms = Mouse.GetState();
        int wheel = ms.ScrollWheelValue;
        if (_prevScrollWheel == 0) _prevScrollWheel = wheel;
        _scrollOffset = Math.Clamp(_scrollOffset + (_prevScrollWheel - wheel) * 0.3f, 0, _maxScroll);
        _prevScrollWheel = wheel;

        _hoverBack = _backRect.Contains(mp);

        // chapter hover (scroll‑aware)
        if (_chapters != null)
        {
            for (int i = 0; i < _chapters.Length; i++)
            {
                var e = _chapters[i];
                var r = CardRect(e.GridRow, e.GridCol);
                int lift  = e.HoverCard ? (int)HoverLift : 0;
                int drawY = r.Y + lift - (int)_scrollOffset;
                var hit   = new Rectangle(r.X, drawY, CardW, CardH);

                e.HoverCard = hit.Contains(mp);
                if (e.Status != ChapterStatus.Upcoming)
                {
                    var pRect = PlayRect(e.GridRow, e.GridCol, drawY);
                    e.HoverPlay = pRect.Contains(mp);
                }
                else e.HoverPlay = false;

                _chapters[i] = e;
            }
        }

        if (_inputCooldown > 0) return;

        if (touch.IsDown)
        {
            if (_hoverBack)
            {
                Click();
                _game.SceneManager.SwitchTo(SceneId.Menu);
                return;
            }
            if (_chapters != null)
            {
                for (int i = 0; i < _chapters.Length; i++)
                {
                    var e = _chapters[i];
                    if (e.Status == ChapterStatus.Upcoming || string.IsNullOrEmpty(e.ChapterPath))
                        continue;
                    if (e.HoverCard || e.HoverPlay)
                    {
                        Click();
                        var d = _game.SceneManager.GetScene(SceneId.Dialogue) as DialogueScene;
                        d?.LoadChapterFile(e.ChapterPath);
                        if (d != null) _game.SceneManager.SwitchTo(SceneId.Dialogue);
                        return;
                    }
                }
            }
        }

        var kb = Keyboard.GetState();
        if ((kb.IsKeyDown(Keys.Escape) ||
             GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            && _inputCooldown <= 0)
        {
            _inputCooldown = GameConfig.ClickCooldown;
            _game.SceneManager.SwitchTo(SceneId.Menu);
        }
    }

    private void Click() { _game.Audio.PlaySfx(_sfxClick); _inputCooldown = GameConfig.ClickCooldown; }

    // ── Draw ────────────────────────────────────────────────────────────
    public void Draw()
    {
        var b   = _game.SpriteBatch;
        var loc = _game.Loc;

        // ── Layer 1 – background + vignette ─────────────────────────────
        b.Begin();
        b.Draw(_bgWood, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.3f);
        b.End();

        // ── Layer 2 – title ─────────────────────────────────────────────
        b.Begin();
        var title  = loc.Get("chapter_select");
        var tSz    = _font.MeasureString(title);
        float tX   = (_game.Width - tSz.X * TitleScale) / 2;
        float tY   = 26;
        b.DrawString(_font, title, new Vector2(tX + 2, tY + 2), Color.Black * 0.6f, 0, Vector2.Zero, TitleScale, SpriteEffects.None, 0);
        b.DrawString(_font, title, new Vector2(tX, tY), _gold, 0, Vector2.Zero, TitleScale, SpriteEffects.None, 0);
        float lineW = tSz.X * TitleScale * 0.55f;
        float lineY = tY + tSz.Y * TitleScale + 10;
        b.Draw(_game.WhitePixel, new Rectangle((int)((_game.Width - lineW) / 2), (int)lineY, (int)lineW, 2), _gold * 0.6f);
        b.End();

        // ── Layer 3 – scrollable card grid ──────────────────────────────
        var prevRaster  = b.GraphicsDevice.RasterizerState;
        var sRaster     = new RasterizerState { ScissorTestEnable = true };
        var prevScissor = b.GraphicsDevice.ScissorRectangle;

        int sY = CardStartY - 10;
        int sH = _game.Height - sY - 30;
        b.GraphicsDevice.ScissorRectangle = new Rectangle(0, sY, _game.Width, sH);
        b.GraphicsDevice.RasterizerState  = sRaster;

        // ONE Begin/End pair for ALL card content (background + text + buttons)
        b.Begin(SpriteSortMode.Deferred, null, null, null, sRaster, null);

        if (_chapters != null)
        {
            for (int i = 0; i < _chapters.Length; i++)
            {
                var e      = _chapters[i];
                var r      = CardRect(e.GridRow, e.GridCol);
                int lift   = e.HoverCard ? (int)HoverLift : 0;
                float dY   = r.Y + lift - _scrollOffset;

                if (dY + CardH < sY || dY > sY + sH) continue;

                bool isUpcoming = e.Status == ChapterStatus.Upcoming;
                float alpha = isUpcoming ? 0.55f : 1f;

                var cardRect = new Rectangle(r.X, (int)dY, CardW, CardH);

                // ── 3a. parchment card background (DRAWN FIRST) ─────────
                b.Draw(_panelCard, cardRect, Color.White * alpha);

                // ── 3b. dim overlay for upcoming ────────────────────────
                if (isUpcoming)
                    b.Draw(_game.WhitePixel, cardRect, Color.Black * 0.4f);

                // ── 3c. chapter title ───────────────────────────────────
                var titleCol = isUpcoming ? _upcomingGray * 0.8f : (Color)_darkBrown;
                b.DrawString(_font, e.Title,
                    new Vector2(cardRect.X + PadLeft, cardRect.Y + PadTop),
                    titleCol);

                // ── 3d. subtitle (from localization) ────────────────────
                var subText = loc.Get(e.SubKey);
                if (!string.IsNullOrEmpty(subText))
                {
                    var subCol = isUpcoming ? _upcomingGray * 0.6f : (Color)_mediumBrown;
                    b.DrawString(_font, subText,
                        new Vector2(cardRect.X + PadLeft, cardRect.Y + PadTop + 38),
                        subCol, 0, Vector2.Zero, SubScale, SpriteEffects.None, 0);
                }

                // ── 3e. completed badge (top‑right) ─────────────────────
                if (e.Status == ChapterStatus.Completed)
                {
                    var cText = loc.Get("chapter_completed");
                    var cSz   = _font.MeasureString(cText);
                    float cX  = cardRect.X + CardW - cSz.X * BadgeScale - 14;
                    float cY  = cardRect.Y + 12;
                    b.DrawString(_font, cText, new Vector2(cX, cY), _forestGreen,
                        0, Vector2.Zero, BadgeScale, SpriteEffects.None, 0);
                }

                // ── 3f. play button / upcoming label (bottom‑right) ─────
                if (!isUpcoming)
                {
                    var pRect = PlayRect(e.GridRow, e.GridCol, (int)dY);
                    var bg    = e.HoverPlay ? _burntSienna : _terracotta;

                    b.Draw(_game.WhitePixel, pRect, bg);
                    b.Draw(_game.WhitePixel, new Rectangle(pRect.X, pRect.Y, pRect.Width, 2), Color.White * 0.25f);
                    b.Draw(_game.WhitePixel, new Rectangle(pRect.X, pRect.Y + pRect.Height - 2, pRect.Width, 2), Color.Black * 0.25f);

                    var txt  = loc.Get("play_chapter") ?? "Mainkan";
                    var tSz2 = _font.MeasureString(txt);
                    float tx2 = pRect.X + (pRect.Width - tSz2.X * BtnScale) / 2;
                    float ty = pRect.Y + (pRect.Height - tSz2.Y * BtnScale) / 2 - 3;
                    b.DrawString(_font, txt, new Vector2(tx2, ty), _cream, 0, Vector2.Zero, BtnScale, SpriteEffects.None, 0);
                }
                else
                {
                    var upText = "Segera Hadir";
                    var uSz    = _font.MeasureString(upText);
                    float ux   = cardRect.X + CardW - uSz.X * UpScale - 16;
                    float uy   = cardRect.Y + CardH - uSz.Y * UpScale - 14;
                    b.DrawString(_font, upText, new Vector2(ux, uy), _upcomingGray * 0.75f,
                        0, Vector2.Zero, UpScale, SpriteEffects.None, 0);
                }
            }
        }

        // ── 3g. custom thin scrollbar ──────────────────────────────────
        if (_maxScroll > 0.5f)
        {
            float sbX = _game.Width - 6f - 8f;
            _scrollTrackRect = new Rectangle((int)sbX, sY, 6, sH);
            b.Draw(_game.WhitePixel, _scrollTrackRect, new Color(60, 40, 20) * 0.3f);

            float thumbH = sH * (sH / (sH + _maxScroll));
            float thumbY = sY + (_scrollOffset / _maxScroll) * (sH - thumbH);
            b.Draw(_game.WhitePixel, new Rectangle((int)sbX, (int)thumbY, 6, (int)thumbH), _gold * 0.6f);
        }

        b.End();

        b.GraphicsDevice.ScissorRectangle = prevScissor;
        b.GraphicsDevice.RasterizerState  = prevRaster;

        // ── Layer 4 – home icon ─────────────────────────────────────────
        b.Begin();
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
        _loaded = false;
        _prevScrollWheel = 0;
    }
}
