using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LittleQuranTales.Scenes;

public class LoadingScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private Texture2D _bg;
    private Texture2D _logo;
    private string _targetId;
    private float _timer;
    private bool _loadingStarted;

    private const float TotalTime = 0.8f;
    private const float LoadDelay = 0.25f;

    public LoadingScene(Game1 game) { _game = game; }

    public void SetTarget(string id) { _targetId = id; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>("Fonts/GameFont");
        _bg = _game.Content.Load<Texture2D>("Images/UI/LoadingScreen");
        _logo = _game.Content.Load<Texture2D>("Images/UI/menu_logo");
        _timer = 0;
        _loadingStarted = false;
    }

    public void Update(float deltaTime)
    {
        _timer += deltaTime;

        if (!_loadingStarted && _timer >= LoadDelay)
        {
            _loadingStarted = true;
            var target = _game.SceneManager.GetScene(_targetId);
            target.Load();
        }

        if (_timer >= TotalTime)
            _game.SceneManager.SwitchTo(_targetId);
    }

    public void Draw()
    {
        var device = _game.GraphicsDevice;
        var batch = _game.SpriteBatch;
        var wp = _game.WhitePixel;
        var w = _game.Width;
        var h = _game.Height;

        device.Clear(Color.Black);
        batch.Begin();

        if (_bg != null)
            batch.Draw(_bg, new Rectangle(0, 0, w, h), Color.White);

        if (_logo != null)
        {
            float ls = 1f / 3f;
            batch.Draw(_logo, new Vector2(20, 20), null, Color.White, 0, Vector2.Zero, ls, SpriteEffects.None, 0);
        }

        var barW = 360;
        var barH = 24;
        var barX = (w - barW) / 2;
        var barY = h - 60;
        var progress = MathHelper.Min(1f, _timer / TotalTime);
        var fillW = (int)(barW * progress);

        var gray = new Color(60, 60, 60);
        var borderColor = Color.White * 0.5f;
        var gold = new Color(212, 175, 55);

        batch.Draw(wp, new Rectangle(barX, barY, barW, barH), gray);
        batch.Draw(wp, new Rectangle(barX, barY, barW, 2), borderColor);
        batch.Draw(wp, new Rectangle(barX, barY + barH - 2, barW, 2), borderColor);
        batch.Draw(wp, new Rectangle(barX, barY, 2, barH), borderColor);
        batch.Draw(wp, new Rectangle(barX + barW - 2, barY, 2, barH), borderColor);

        if (fillW > 4)
            batch.Draw(wp, new Rectangle(barX + 2, barY + 2, fillW - 4, barH - 4), gold);

        var pctStr = $"{(int)(progress * 100)}%";
        var pctSz = _font.MeasureString(pctStr);
        var pctPos = new Vector2(barX + (barW - pctSz.X) / 2, barY + (barH - pctSz.Y) / 2);
        batch.DrawString(_font, pctStr, pctPos, Color.White);

        batch.End();
    }

    public void Unload() { }
}
