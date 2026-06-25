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
    private float _clickCooldown;

    private Song _bgm;
    private SoundEffect _sfxClick;

    private readonly Color _cream = new(234, 230, 223);
    private readonly Color _darkBrown = new(62, 39, 35);
    private readonly Color _shadowColor = new(30, 20, 10, 100);

    private readonly string[] _btnKeys = { SceneKey.MainStory, SceneKey.MiniGames, SceneKey.Library, SceneKey.Settings };
    private readonly string[] _btnScenes = { SceneId.Dialogue, SceneId.MinigameGallery, SceneId.Library, SceneId.Settings };

    private const float MenuTextScale = 1.3f;
    private Rectangle _chapterRect;

    public MenuScene(Game1 game)
    {
        _game = game;
    }

    private bool _loaded;

    public void Load()
    {
        if (_loaded) return;
        _loaded = true;
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
            gradData[i] = new Color(0, 0, 0, (int)(64 * (1f - i / 255f)));
        _gradientOverlay.SetData(gradData);

        _chapterRect = new Rectangle(_game.Width - 270, _game.Height - 130, 240, 60);

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
    }

    public void Update(float deltaTime)
    {
        _clickCooldown = MathHelper.Max(0, _clickCooldown - deltaTime);

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
            _game.Audio.PlaySfx(_sfxClick);
            var target = _btnScenes[_hoveredIndex];
            if (!string.IsNullOrEmpty(target))
                _game.SceneManager.SwitchTo(target);
            _clickCooldown = GameConfig.ClickCooldown;
        }
    }

    public void Draw()
    {
        var batch = _game.SpriteBatch;
        batch.Begin();

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

            if (_hoverBtn != null && isHovered)
            {
                var hRect = _hoverRects[i];
                batch.Draw(_hoverBtn, hRect, null, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0);
            }

            var iconRect = new Rectangle(rect.X + 4, rect.Y + 10, 36, 36);
            batch.Draw(_icons[i], iconRect, isHovered ? new Color(62, 39, 35) : Color.White);

            var textColor = isHovered ? _darkBrown : _cream;
            var textPos = new Vector2(rect.X + 52, rect.Y + 10);
            DrawText(batch, loc.Get(_btnKeys[i]), textPos, textColor, MenuTextScale);
        }

        var chColor = new Color(180, 155, 120, 180);
        if (_panelChapter != null)
        {
            batch.Draw(_panelChapter, _chapterRect, new Color(255, 255, 255, 80));
        }

        var chapterText = loc.Get("chapter");
        var chapterSize = _font.MeasureString(chapterText);
        DrawText(batch, chapterText,
            new Vector2(
                _chapterRect.X + (_chapterRect.Width - chapterSize.X) / 2,
                _chapterRect.Y + (_chapterRect.Height - chapterSize.Y) / 2),
            _darkBrown, 0.8f);

        batch.End();
    }

    public void Unload()
    {
        _gradientOverlay?.Dispose();
        _gradientOverlay = null;
    }

    private void DrawText(SpriteBatch batch, string text, Vector2 pos, Color color, float scale = 1f)
    {
        batch.DrawString(_font, text, pos + new Vector2(1, 1), _shadowColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        batch.DrawString(_font, text, pos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
    }
}
