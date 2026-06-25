using System;
using LittleQuranTales.Data;
using LittleQuranTales.Scenes;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace LittleQuranTales;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SceneManager _sceneManager;
    private RenderTarget2D _virtualTarget;

    public int Width => GameConfig.VirtualWidth;
    public int Height => GameConfig.VirtualHeight;
    public SpriteBatch SpriteBatch => _spriteBatch;
    public SceneManager SceneManager => _sceneManager;
    public Texture2D WhitePixel { get; private set; }

    public float ScaleX => _scaleX;
    public float ScaleY => _scaleY;
    private float _scaleX, _scaleY;

    public struct TouchState
    {
        public Point Position;
        public bool IsDown;
    }

    public TouchState GetTouch()
    {
#if ANDROID
        var touches = TouchPanel.GetState();
        if (touches.Count > 0)
        {
            var t = touches[0];
            var pos = ScreenToVirtual((int)t.Position.X, (int)t.Position.Y);
            return new TouchState { Position = pos, IsDown = t.State != TouchLocationState.Released };
        }
        return new TouchState { Position = Point.Zero, IsDown = false };
#else
        var ms = Mouse.GetState();
        var pos = ScreenToVirtual(ms.X, ms.Y);
        return new TouchState { Position = pos, IsDown = ms.LeftButton == ButtonState.Pressed };
#endif
    }

    public Point ScreenToVirtual(int sx, int sy)
    {
        float scale;
#if ANDROID
        scale = Math.Max(_scaleX, _scaleY);
#else
        scale = Math.Min(_scaleX, _scaleY);
#endif
        var dx = (int)((Window.ClientBounds.Width - 1280 * scale) / 2);
        var dy = (int)((Window.ClientBounds.Height - 720 * scale) / 2);
        return new Point((int)((sx - dx) / scale), (int)((sy - dy) / scale));
    }

    public AudioManager Audio { get; private set; }
    public SaveManager Save { get; private set; }
    public LocalizationManager Loc { get; private set; }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
#if ANDROID
        _graphics.IsFullScreen = true;
        IsMouseVisible = false;
#else
        IsMouseVisible = true;
#endif
    }

    protected override void Initialize()
    {
#if ANDROID
        _graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
#else
        _graphics.PreferredBackBufferWidth = GameConfig.VirtualWidth;
        _graphics.PreferredBackBufferHeight = GameConfig.VirtualHeight;
#endif
        _graphics.ApplyChanges();

        Audio = new AudioManager();
        Save = new SaveManager();
        Loc = new LocalizationManager();
        Loc.Load("Data/lang.json");
        Loc.SetLanguage(Save.Data.Language);

        Audio.BgmVolume = Save.Data.BgmVolume;
        Audio.SfxVolume = Save.Data.SfxVolume;

        _sceneManager = new SceneManager();
        _sceneManager.Register(SceneId.Splash, new SplashScene(this));
        _sceneManager.Register(SceneId.Menu, new MenuScene(this));
        _sceneManager.Register(SceneId.Title, new TitleScene(this));
        _sceneManager.Register(SceneId.Dialogue, new DialogueScene(this));
        _sceneManager.Register(SceneId.Minigame, new MiniGameScene(this));
        _sceneManager.Register(SceneId.Settings, new SettingsScene(this));
        _sceneManager.Register(SceneId.Library, new LibraryScene(this));
        _sceneManager.Register(SceneId.MinigameGallery, new MiniGameGalleryScene(this));
        _sceneManager.Register(SceneId.Loading, new LoadingScene(this));

        _sceneManager.SwitchTo(SceneId.Splash);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        WhitePixel.SetData(new[] { Color.White });
        _virtualTarget = new RenderTarget2D(GraphicsDevice, GameConfig.VirtualWidth, GameConfig.VirtualHeight);
    }

    protected override void Update(GameTime gameTime)
    {
        _sceneManager.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_virtualTarget);
        _sceneManager.Draw();
        GraphicsDevice.SetRenderTarget(null);

        GraphicsDevice.Clear(Color.Black);
        var bw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var bh = GraphicsDevice.PresentationParameters.BackBufferHeight;
        _scaleX = (float)bw / 1280;
        _scaleY = (float)bh / 720;
        float scale;
#if ANDROID
        scale = Math.Max(_scaleX, _scaleY);
#else
        scale = Math.Min(_scaleX, _scaleY);
#endif
        var destW = (int)(1280 * scale);
        var destH = (int)(720 * scale);
        var destX = (bw - destW) / 2;
        var destY = (bh - destH) / 2;
        _spriteBatch.Begin();
        _spriteBatch.Draw(_virtualTarget, new Rectangle(destX, destY, destW, destH), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
