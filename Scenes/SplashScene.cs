using LittleQuranTales.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LittleQuranTales.Scenes;

public class SplashScene : IScene
{
    private readonly Game1 _game;
    private Texture2D _logo;
    private float _timer;
    private float _alpha;
    private float _scale = 0.8f;
    private SplashState _state = SplashState.FadeIn;
    private bool _transitioning;

    private enum SplashState
    {
        FadeIn,
        Hold,
        FadeOut
    }

    private const float FadeInDuration = 0.8f;
    private const float HoldDuration = 1.5f;
    private const float FadeOutDuration = 0.6f;

    public SplashScene(Game1 game)
    {
        _game = game;
    }

    public void Load()
    {
        _logo = _game.Content.Load<Texture2D>("Images/UI/Splash");
        _timer = 0;
        _alpha = 0;
        _scale = 0.8f;
        _state = SplashState.FadeIn;
        _transitioning = false;
    }

    public void Update(float deltaTime)
    {
        _timer += deltaTime;

        switch (_state)
        {
            case SplashState.FadeIn:
                _alpha = MathHelper.Min(1, _timer / FadeInDuration);
                _scale = MathHelper.Lerp(0.8f, 1.0f, _alpha);
                if (_timer >= FadeInDuration)
                {
                    _state = SplashState.Hold;
                    _timer = 0;
                }
                break;

            case SplashState.Hold:
                if (_timer >= HoldDuration)
                {
                    _state = SplashState.FadeOut;
                    _timer = 0;
                }
                break;

            case SplashState.FadeOut:
                _alpha = 1 - MathHelper.Min(1, _timer / FadeOutDuration);
                if (_timer >= FadeOutDuration && !_transitioning)
                {
                    _transitioning = true;
                    var loading = (LoadingScene)_game.SceneManager.GetScene(SceneId.Loading);
                    loading.SetTarget(SceneId.Menu);
                    _game.SceneManager.SwitchTo(SceneId.Loading);
                }
                break;
        }
    }

    public void Draw()
    {
        _game.GraphicsDevice.Clear(Color.Black);

        var batch = _game.SpriteBatch;
        batch.Begin();

        if (_logo != null && _alpha > 0)
        {
            var destWidth = (int)(_logo.Width * _scale);
            var destHeight = (int)(_logo.Height * _scale);
            var destX = (_game.Width - destWidth) / 2;
            var destY = (_game.Height - destHeight) / 2;

            batch.Draw(
                _logo,
                new Rectangle(destX, destY, destWidth, destHeight),
                Color.White * _alpha);
        }

        batch.End();
    }

    public void Unload() { }
}
