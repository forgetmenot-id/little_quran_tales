using LittleQuranTales.Data;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace LittleQuranTales.Scenes;

public class MenuScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;

    private Texture2D _bg;
    private Texture2D _logo;
    private Texture2D _gradientOverlay;
    private Texture2D _panelChapter;
    private Texture2D _hoverBtn;
    private Texture2D[] _icons;

    private Rectangle[] _btnRects;
    private Rectangle[] _hoverRects;
    private int _hoveredIndex = -1;
    private int _clickedIndex = -1;
    private bool _clickPending;
    private float _clickCooldown;

    private Song _bgm;
    private SoundEffect _sfxClick;

    private readonly Color _cream = new(234, 230, 223);
    private readonly Color _darkBrown = new(62, 39, 35);
    private readonly Color _shadowColor = new(30, 20, 10, 100);
    private readonly Color _gold = new(212, 175, 55);

    private readonly string[] _btnKeys = { SceneKey.MainStory, SceneKey.MiniGames, SceneKey.Library, SceneKey.Settings };
    private readonly string[] _btnScenes = { SceneId.StorySelection, SceneId.MinigameGallery, SceneId.Library, SceneId.Settings };

    private const float MenuTextScale = 1.3f;
    private Rectangle _chapterRect;

    private SaveManager Save => _game.Save;

    private static readonly (string id, string langKey)[] _chapterOrder = {
        ("prolog",  "prolog_title"),
        ("al-fil",  "chapter_1_title"),
        ("al-alaq", "chapter_2_title"),
    };

    private string GetNextChapterKey()
    {
        foreach (var (id, key) in _chapterOrder)
        {
            if (!Save.Data.CompletedChapters.Contains(id))
                return key;
        }
        return "chapter_3_title";
    }

    public MenuScene(Game1 game)
    {
        _game = game;
    }

    private bool _loaded;

    public void Load()
    {
        LogHelper.Trace($"MenuScene.Load loaded={_loaded}");
        if (_loaded) { LogHelper.Trace("MenuScene.Load early return"); return; }
        _loaded = true;
        LogHelper.Trace("MenuScene.Load loading assets");
        _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _bg = _game.Content.Load<Texture2D>("Images/UI/menu_bg");
        _logo = _game.Content.Load<Texture2D>("Images/UI/menu_logo");
        _panelChapter = _game.Content.Load<Texture2D>("Images/UI/panel_chapter");
        _hoverBtn = _game.Content.Load<Texture2D>("Images/UI/hover_button");

        _icons = new[]
        {
            _game.Content.Load<Texture2D>("Images/UI/icon_story"),
            _game.Content.Load<Texture2D>("Images/UI/icon_games"),
            _game.Content.Load<Texture2D>("Images/UI/icon_library"),
            _game.Content.Load<Texture2D>("Images/UI/icon_settings"),
        };

        var btnX = 110;
        var btnStartY = 290;
        var spacing = 100;
        var btnW = 280;
        var btnH = 56;

        _btnRects = new Rectangle[4];
        _hoverRects = new Rectangle[4];
        for (int i = 0; i < 4; i++)
        {
            _btnRects[i] = new Rectangle(btnX, btnStartY + i * spacing, btnW, btnH);
            var padX = -16;
            var padY = -10;
            _hoverRects[i] = new Rectangle(
                btnX + padX,
                btnStartY + i * spacing + padY,
                btnW - padX * 2,
                btnH - padY * 2);
        }

        _gradientOverlay = new Texture2D(_game.GraphicsDevice, 256, 1);
        var gradData = new Color[256];
        for (int i = 0; i < 256; i++)
            gradData[i] = new Color(0, 0, 0, (int)(200 * (1f - i / 255f)));
        _gradientOverlay.SetData(gradData);

        _chapterRect = new Rectangle(_game.Width - 350, _game.Height - 130, 320, 65);

        try
        {
            _bgm = _game.Content.Load<Song>("Audio/BGM/bgm_menu");
            if (MediaPlayer.State != MediaState.Playing)
                _game.Audio.PlayBgm(_bgm);
        }
        catch { }

        try
        {
            _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");
        }
        catch { }

        LogHelper.Trace("MenuScene.Load done");
    }

    public void Update(float deltaTime)
    {
        _clickCooldown = MathHelper.Max(0, _clickCooldown - deltaTime);

        if (_clickPending)
        {
            _clickPending = false;
            _game.Audio.PlaySfx(_sfxClick);
            var target = _btnScenes[_clickedIndex];
            if (!string.IsNullOrEmpty(target))
                _game.SceneManager.SwitchTo(target);
            return;
        }

        if (_bgm != null && MediaPlayer.State != MediaState.Playing)
        {
            _game.Audio.StopBgm();
            _game.Audio.PlayBgm(_bgm);
        }

        var touch = _game.GetTouch();
        var mp = touch.Position;

        _hoveredIndex = -1;
        for (int i = 0; i < _btnRects.Length; i++)
        {
            if (_btnRects[i].Contains(mp))
                _hoveredIndex = i;
        }

        if (_clickCooldown > 0) return;

        if (touch.IsDown && _hoveredIndex >= 0)
        {
            _clickedIndex = _hoveredIndex;
            _clickPending = true;
            _clickCooldown = GameConfig.ClickCooldown;
        }
    }

    public void Draw()
    {
        var batch = _game.SpriteBatch;
        batch.Begin();
        try
        {
            var loc = _game.Loc;
            batch.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);

            if (_gradientOverlay != null)
                batch.Draw(_gradientOverlay, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);

            var logoW = _logo?.Width ?? 400;
            var logoH = _logo?.Height ?? 80;
            var logoScale = 1f;
            if (logoW > 500) logoScale = 500f / logoW;
            batch.Draw(_logo,
                new Vector2(30, 30),
                null, Color.White, 0, Vector2.Zero, logoScale, SpriteEffects.None, 0);

            for (int i = 0; i < 4; i++)
            {
                var rect = _btnRects[i];
                var isHovered = _hoveredIndex == i;
                var isClicked = _clickPending && _clickedIndex == i;

                if (_hoverBtn != null && (isHovered || isClicked))
                {
                    var hRect = _hoverRects[i];
                    batch.Draw(_hoverBtn, hRect, null, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
                }

                var iconRect = new Rectangle(rect.X + 4, rect.Y + 10, 36, 36);
                if (_icons[i] != null)
                    batch.Draw(_icons[i], iconRect, Color.White);

                var textColor = isHovered ? Color.Black : _cream;
                var label = loc.Get(_btnKeys[i]);
                var txtSize = _font.MeasureString(label) * MenuTextScale;
                var textPos = new Vector2(rect.X + 52, rect.Y + 10 + (36 - txtSize.Y) / 2);
                DrawText(batch, label, textPos, textColor, MenuTextScale);
            }

            if (_panelChapter != null)
            {
                batch.Draw(_panelChapter, _chapterRect, new Color(255, 255, 255, 80));
            }

            var chapterKey = GetNextChapterKey();
            var chapterText = loc.Get(chapterKey);
            var chapterSize = _font.MeasureString(chapterText);
            DrawText(batch, chapterText,
                new Vector2(
                    _chapterRect.X + (_chapterRect.Width - chapterSize.X) / 2,
                    _chapterRect.Y + (_chapterRect.Height - chapterSize.Y) / 2),
                Color.Black, 0.8f);
        }
        finally
        {
            batch.End();
        }
    }

    public void Unload()
    {
        _loaded = false;
        _gradientOverlay?.Dispose();
        _gradientOverlay = null;
    }

    private void DrawText(SpriteBatch batch, string text, Vector2 pos, Color color, float scale = 1f)
    {
        batch.DrawString(_font, text, pos + new Vector2(1, 1), _shadowColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        batch.DrawString(_font, text, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
}
