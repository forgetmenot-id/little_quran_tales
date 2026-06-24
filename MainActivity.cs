using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace LittleQuranTales;

[Activity(
    Name = "com.littlequrantales.app.MainActivity",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode,
    ScreenOrientation = ScreenOrientation.Landscape)]
public class MainActivity : AndroidGameActivity
{
    private Game1 _game;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        var instanceField = typeof(Game).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        var activityProp = typeof(Game).GetProperty("Activity", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        activityProp.SetValue(null, this);

        _game = new Game1();
        instanceField.SetValue(null, _game);

        base.OnCreate(savedInstanceState);

        SetContentView((View)_game.Services.GetService(typeof(View)));
        _game.Run();
    }
}
