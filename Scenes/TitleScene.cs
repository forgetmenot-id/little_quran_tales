using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace LittleQuranTales.Scenes;

public class TitleScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private float _timer;

    public TitleScene(Game1 game)
    {
        _game = game;
    }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>("Fonts/GameFont");
    }

    public void Update(float deltaTime)
    {
        _timer += deltaTime;

        if (Keyboard.GetState().IsKeyDown(Keys.Enter) ||
            _game.GetTouch().IsDown)
        {
            _game.SceneManager.SwitchTo("dialogue");
        }
    }

    public void Draw()
    {
        _game.GraphicsDevice.Clear(new Color(30, 30, 60));

        var batch = _game.SpriteBatch;
        batch.Begin();

        var title = "Little Quran Tales";
        var titleSize = _font.MeasureString(title);
        var titlePos = new Vector2(
            _game.Width / 2 - titleSize.X / 2,
            _game.Height / 3 - titleSize.Y / 2);
        batch.DrawString(_font, title, titlePos, Color.Gold);

        var subtitle = "Chapter Al-Fil";
        var subSize = _font.MeasureString(subtitle);
        var subPos = new Vector2(
            _game.Width / 2 - subSize.X / 2,
            _game.Height / 2 - subSize.Y / 2);
        batch.DrawString(_font, subtitle, subPos, Color.White);

        var blink = (int)(_timer * 2) % 2 == 0;
        if (blink)
        {
            var prompt = "Press ENTER to start";
            var promptSize = _font.MeasureString(prompt);
            var promptPos = new Vector2(
                _game.Width / 2 - promptSize.X / 2,
                _game.Height * 2 / 3 - promptSize.Y / 2);
            batch.DrawString(_font, prompt, promptPos, Color.LightGray);
        }

        batch.End();
    }

    public void Unload() { }
}
