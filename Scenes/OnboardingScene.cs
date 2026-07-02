using LittleQuranTales.Data;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace LittleQuranTales.Scenes;

public class OnboardingScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private Texture2D _bg;
    private SoundEffect _sfxClick;

    private readonly Color _terracotta  = new(194, 74, 47);
    private readonly Color _burntSienna = new(233, 116, 81);
    private readonly Color _cream       = new(255, 248, 225);
    private readonly Color _gold        = new(212, 175, 55);
    private readonly Color _darkBrown   = new(62, 39, 35);

    private const int PanelW = 640;
    private const int PanelH = 420;
    private int _panelX, _panelY;
    private Rectangle _panelRect;

    private Rectangle _privacyCheck, _termsCheck;
    private Rectangle _privacyLink, _termsLink;
    private Rectangle _doneBtn;
    private bool _privacyChecked, _termsChecked;
    private bool _hoverDone;
    private bool _hoverPrivacyLink, _hoverTermsLink;

    private float _inputCooldown;

    private const string PrivacyUrl = "https://www.forgetmenot.id/privacy-policy/littlequrantales/";
    private const string TermsUrl = "https://www.forgetmenot.id/terms/littlequrantales/";

    public OnboardingScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _bg = _game.Content.Load<Texture2D>("Images/UI/menu_bg");
        try { _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click"); } catch { }

        _panelX = (_game.Width  - PanelW) / 2;
        _panelY = (_game.Height - PanelH) / 2;
        _panelRect = new Rectangle(_panelX, _panelY, PanelW, PanelH);

        var checkY1 = _panelY + 155;
        var checkY2 = _panelY + 215;
        var checkX = _panelX + 80;
        var checkSz = 28;

        _privacyCheck = new Rectangle(checkX, checkY1, checkSz, checkSz);
        _privacyLink  = new Rectangle(checkX + checkSz + 12, checkY1 - 4, 320, checkSz + 8);

        _termsCheck   = new Rectangle(checkX, checkY2, checkSz, checkSz);
        _termsLink    = new Rectangle(checkX + checkSz + 12, checkY2 - 4, 320, checkSz + 8);

        _doneBtn = new Rectangle(_panelX + PanelW / 2 - 100, _panelY + PanelH - 70, 200, 48);

        _privacyChecked = false;
        _termsChecked = false;
    }

    public void Update(float deltaTime)
    {
        _inputCooldown = MathHelper.Max(0, _inputCooldown - deltaTime);

        var touch = _game.GetTouch();
        var mp = touch.Position;

        if (!touch.IsDown) return;
        if (_inputCooldown > 0) return;

        // checkbox toggles
        _hoverPrivacyLink = _privacyLink.Contains(mp);
        _hoverTermsLink = _termsLink.Contains(mp);
        _hoverDone = _doneBtn.Contains(mp);

        if (_privacyCheck.Contains(mp))
        {
            _privacyChecked = !_privacyChecked;
            _game.Audio.PlaySfx(_sfxClick);
            _inputCooldown = GameConfig.ClickCooldown;
            return;
        }

        if (_termsCheck.Contains(mp))
        {
            _termsChecked = !_termsChecked;
            _game.Audio.PlaySfx(_sfxClick);
            _inputCooldown = GameConfig.ClickCooldown;
            return;
        }

        // privacy link
        if (_privacyLink.Contains(mp))
        {
            OpenUrl(PrivacyUrl);
            _inputCooldown = GameConfig.ClickCooldown;
            return;
        }

        // terms link
        if (_termsLink.Contains(mp))
        {
            OpenUrl(TermsUrl);
            _inputCooldown = GameConfig.ClickCooldown;
            return;
        }

        // done button
        if (_hoverDone && _privacyChecked && _termsChecked)
        {
            _game.Audio.PlaySfx(_sfxClick);
            _game.Save.Data.HasAgreedToTerms = true;
            _game.Save.Save();
            _game.SceneManager.SwitchTo(SceneId.Menu);
        }
    }

    private static void OpenUrl(string url)
    {
#if ANDROID
        try
        {
            var intent = new Android.Content.Intent(
                Android.Content.Intent.ActionView,
                Android.Net.Uri.Parse(url));
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
        catch { }
#else
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url)
                { UseShellExecute = true });
        }
        catch { }
#endif
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        var loc = _game.Loc;

        b.Begin();

        b.Draw(_bg, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.75f);

        // panel bg
        b.Draw(_game.WhitePixel, _panelRect, _darkBrown * 0.95f);
        DrawBorder(b, _panelRect, _gold * 0.6f, 2);

        // title
        var title = loc.Get("onboarding_title");
        var tSz = _font.MeasureString(title);
        b.DrawString(_font, title,
            new Vector2(_panelX + (PanelW - tSz.X) / 2, _panelY + 25), _gold);

        // desc
        var desc = loc.Get("onboarding_desc");
        var dSz = _font.MeasureString(desc);
        b.DrawString(_font, desc,
            new Vector2(_panelX + (PanelW - dSz.X) / 2, _panelY + 75), _cream * 0.85f);

        // privacy row
        DrawCheckbox(b, _privacyCheck, _privacyChecked);
        var privacyText = loc.Get("agree_privacy");
        b.DrawString(_font, privacyText,
            new Vector2(_privacyLink.X, _privacyLink.Y + 4),
            _hoverPrivacyLink ? _gold : _cream);

        // terms row
        DrawCheckbox(b, _termsCheck, _termsChecked);
        var termsText = loc.Get("agree_terms");
        b.DrawString(_font, termsText,
            new Vector2(_termsLink.X, _termsLink.Y + 4),
            _hoverTermsLink ? _gold : _cream);

        // done button
        var canDone = _privacyChecked && _termsChecked;
        var btnColor = canDone
            ? (_hoverDone ? _burntSienna : _terracotta)
            : new Color(80, 70, 60);
        b.Draw(_game.WhitePixel, _doneBtn, btnColor * (canDone ? 1f : 0.5f));
        var doneLabel = loc.Get("onboarding_done");
        var dlSz = _font.MeasureString(doneLabel);
        b.DrawString(_font, doneLabel,
            new Vector2(
                _doneBtn.X + (_doneBtn.Width - dlSz.X) / 2,
                _doneBtn.Y + (_doneBtn.Height - dlSz.Y) / 2),
            canDone ? _cream : _cream * 0.4f);

        b.End();
    }

    private void DrawCheckbox(SpriteBatch b, Rectangle rect, bool checked_)
    {
        b.Draw(_game.WhitePixel, rect, Color.White * 0.2f);
        DrawBorder(b, rect, _gold * 0.6f, 2);
        if (checked_)
        {
            b.Draw(_game.WhitePixel,
                new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8),
                _gold * 0.85f);
            // simple checkmark lines
            var cx = rect.X + 4;
            var cy = rect.Y + 4;
            var cw = rect.Width - 8;
            var ch = rect.Height - 8;
            b.Draw(_game.WhitePixel, new Rectangle(cx + 2, cy + ch / 2, cw / 3, 3), _darkBrown);
            b.Draw(_game.WhitePixel, new Rectangle(cx + cw / 3 - 1, cy + ch / 3, 3, ch / 2 + 2), _darkBrown);
        }
    }

    private void DrawBorder(SpriteBatch b, Rectangle r, Color c, int thickness)
    {
        b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c);
        b.Draw(_game.WhitePixel, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        b.Draw(_game.WhitePixel, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c);
    }

    public void Unload() { }
}
