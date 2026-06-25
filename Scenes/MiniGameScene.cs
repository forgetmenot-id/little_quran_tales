using System;
using System.Collections.Generic;
using LittleQuranTales.Data;
using LittleQuranTales.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace LittleQuranTales.Scenes;

public class MiniGameScene : IScene
{
    private readonly Game1 _game;
    private SpriteFont _font;
    private Random _rng = new();

    public string Difficulty { get; set; } = "normal";
    private bool _isEndless => Difficulty == "endless";

    private SaveManager Save => _game.Save;
    private AudioManager Audio => _game.Audio;

    private Texture2D _birdTex, _elephantTex;
    private int _birdFw, _birdFh, _birdFrames = 8, _birdCols = 4;
    private int _eleFw, _eleFh, _eleFrames = 8, _eleCols = 4;

    private float _birdX, _birdY, _birdFlyT, _birdFrameT;
    private int _birdFrame;
    private float _birdVx, _birdSpeed = 400f;
    private float _dropCooldown;
    private Texture2D _circleTex, _stoneGradTex;
    private int _ammo, _maxAmmo, _combo;
    private float _turbulence;
    private bool _isCharging;
    private float _chargeTime;
    private const float MaxChargeTime = 2f;

    private List<Stone> _stones = new();
    private List<Enemy> _enemies = new();
    private int _currentWave, _totalWaves = 5;
    private int _spawnedThisWave, _enemiesPerWave;
    private float _spawnTimer, _spawnInterval;
    private bool _waveActive, _waveTransition;
    private float _waveTransitionT;

    private int _kabahHp = 10, _maxKabahHp = 10;
    private int _score, _totalKills;
    private int _victoryPhase;
    private float _phaseTimer;
    private float _whiteOverA;
    private bool _gameOver, _victory;
    private float _gameOverT;
    private bool _murottalPlaying;

    private float _elapsed;
    private float _inputCooldown;

    private List<Particle> _particles = new();
    private Texture2D _bgGame, _panelChapter, _sprKabah, _iconHome;

    private Rectangle _backRect;
    private bool _hoverBack;
    private SoundEffect _sfxClick, _sfxDrop, _sfxHit;
    private Song _bgm, _murottal;

    private const float Gravity = 500f;
    private const int GroundH = 50;
    private const float RiseOffset = 180f;
    private const int HudPanelH = 48;
    private int GroundY => _game.Height - GroundH;

    private const float SwipeThreshold = 30f;

    private bool _wasPressing;
    private bool _prevSpace;
    private Vector2 _touchStartPos;

    private class Stone
    {
        public float X, Y, Vy;
        public bool Alive = true;
        public float AoeRadius;
        public int AoeDamage;
        public bool PierceShield;
    }

    private class Enemy
    {
        public float X, Y, W, H, Speed;
        public int Hp = 1, MaxHp = 1;
        public int Shield;
        public float Flash;
        public bool Alive = true;
        public bool IsBoss;
        public float AnimT;
    }

    private class Particle
    {
        public float X, Y, Vx, Vy, Life, MaxLife;
        public Color Color;
    }

    public MiniGameScene(Game1 game) { _game = game; }

    public void Load()
    {
        _font = _game.Content.Load<SpriteFont>(FontPath.GameFont);
        _sfxClick = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_click");
        _sfxDrop = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_stone_drop");
        _sfxHit = _game.Content.Load<SoundEffect>("Audio/SFX/sfx_stone_hit");
        _bgm = _game.Content.Load<Song>("Audio/BGM/bgm_minigame");
        try { _murottal = _game.Content.Load<Song>("Audio/library/al-fil"); }
        catch { _murottal = null; }
        _birdTex = _game.Content.Load<Texture2D>("Images/Sprites/spr_bird_anim");
        _elephantTex = _game.Content.Load<Texture2D>("Images/Sprites/spr_elephant_anim");
        _bgGame = _game.Content.Load<Texture2D>("Images/BGs/bg_game");
        _panelChapter = _game.Content.Load<Texture2D>("Images/UI/panel_chapter");
        _sprKabah = _game.Content.Load<Texture2D>("Images/Sprites/spr_kabah");
        _iconHome = _game.Content.Load<Texture2D>("Images/UI/icon_home");

        _birdFw = _birdTex.Width / _birdCols;
        _birdFh = _birdTex.Height / (_birdFrames / _birdCols);
        _eleFw = _elephantTex.Width / _eleCols;
        _eleFh = _elephantTex.Height / (_eleFrames / _eleCols);

        Audio.PlayBgm(_bgm);

        _circleTex?.Dispose();
        _stoneGradTex?.Dispose();
        var texSz = 32;
        var radius = texSz / 2f;

        _circleTex = new Texture2D(_game.GraphicsDevice, texSz, texSz);
        var cd = new Color[texSz * texSz];
        for (var i = 0; i < cd.Length; i++)
        {
            var x = i % texSz; var y = i / texSz;
            var dx = x - radius + 0.5f; var dy = y - radius + 0.5f;
            var dist = (float)Math.Sqrt(dx * dx + dy * dy) / radius;
            if (dist <= 1f)
            {
                var a = Math.Max(0f, 1f - dist * dist * 0.8f);
                cd[i] = Color.White * a;
            }
            else
                cd[i] = Color.Transparent;
        }
        _circleTex.SetData(cd);

        _stoneGradTex = new Texture2D(_game.GraphicsDevice, texSz, texSz);
        cd = new Color[texSz * texSz];
        for (var i = 0; i < cd.Length; i++)
        {
            var x = i % texSz; var y = i / texSz;
            var dx = x - radius + 0.5f; var dy = y - radius + 0.5f;
            var dist = (float)Math.Sqrt(dx * dx + dy * dy) / radius;
            if (dist <= 1f)
            {
                var t = dist * dist;
                cd[i] = Color.Lerp(Color.White, Color.Black, t);
            }
            else
                cd[i] = Color.Transparent;
        }
        _stoneGradTex.SetData(cd);

        Reset();
    }

    private void Reset()
    {
        _birdX = _game.Width / 2f;
        _birdY = _game.Height * 0.3f;
        _birdVx = 0;
        _birdFlyT = 0;
        _birdFrame = 0;
        _birdFrameT = 0;
        _dropCooldown = 0;
        _stones.Clear();
        _enemies.Clear();
        _particles.Clear();
        _currentWave = 0;
        _spawnedThisWave = 0;
        _enemiesPerWave = 0;
        _spawnTimer = 0;
        _spawnInterval = 0;
        _waveActive = false;
        _waveTransition = true;
        _waveTransitionT = 0;
        _kabahHp = _maxKabahHp = _isEndless ? 20 : 10;
        _score = 0;
        _totalKills = 0;
        _gameOver = false;
        _victory = false;
        _gameOverT = 0;
        _victoryPhase = 0;
        _phaseTimer = 0;
        _whiteOverA = 0;
        _murottalPlaying = false;
        _elapsed = 0;
        _inputCooldown = 0;
        _ammo = 0; _maxAmmo = 10; _combo = 0; _turbulence = 0;
        _backRect = new Rectangle(20, 16, 200, 32);
        _hoverBack = false;
        _isCharging = false;
        _chargeTime = 0;
        _wasPressing = false;
        _prevSpace = false;
        _touchStartPos = Vector2.Zero;
    }

    public void Update(float dt)
    {
        _inputCooldown = Math.Max(0, _inputCooldown - dt);
        _elapsed += dt;

        var kb = Keyboard.GetState();
        var touch = _game.GetTouch();
        _hoverBack = _backRect.Contains(touch.Position);

        if (kb.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (touch.IsDown && _hoverBack && _inputCooldown <= 0))
        {
            Audio.PlaySfx(_sfxClick);
            Audio.StopBgm();
            _inputCooldown = GameConfig.ClickCooldown;
            var key = $"ababil_defense_{Difficulty}";
            Save.SetHighScore(key, _score);
            _game.SceneManager.SwitchTo(SceneId.Menu);
            return;
        }

        if (_gameOver)
        {
            _gameOverT += dt;

            if (_victoryPhase == 0)
            {
                if (_gameOverT < 0.1f)
                {
                    var key = $"ababil_defense_{(_isEndless ? "endless" : "normal")}";
                    Save.SetHighScore(key, _score);
                }

                if (_isEndless)
                {
                    if (_gameOverT > 2f && (kb.IsKeyDown(Keys.Space) || _game.GetTouch().IsDown))
                    {
                        Audio.PlaySfx(_sfxClick);
                        Audio.StopBgm();
                        _game.SceneManager.SwitchTo(SceneId.Menu);
                        return;
                    }
                }
                else
                {
                    if (_victory && !_murottalPlaying && _gameOverT >= 0.5f)
                    {
                        _murottalPlaying = true;
                        if (_murottal != null)
                        {
                            Audio.StopBgm();
                            Audio.PlayBgm(_murottal, false);
                        }
                    }

                    if (_gameOverT > 2f && (kb.IsKeyDown(Keys.Space) || _game.GetTouch().IsDown))
                    {
                        Audio.PlaySfx(_sfxClick);
                        if (_victory)
                        {
                            _victoryPhase = 1;
                            _phaseTimer = 0;
                        }
                        else
                        {
                            Audio.StopBgm();
                            Reset(); _waveTransition = true;
                        }
                    }
                }
            }
            else if (_victoryPhase == 1)
            {
                _phaseTimer += dt;
                _whiteOverA = Math.Min(1f, _phaseTimer / 0.5f);
                if (_phaseTimer >= 1.2f)
                {
                    _victoryPhase = 2;
                    _phaseTimer = 0;
                }
            }
            else if (_victoryPhase == 2)
            {
                _phaseTimer += dt;
                if (_phaseTimer < 0.5f)
                    _whiteOverA = Math.Max(0, 1f - _phaseTimer / 0.5f);
                else
                    _whiteOverA = 0;

                if (_phaseTimer > 2f && (_murottal == null || MediaPlayer.State == MediaState.Stopped) || _phaseTimer > 120f)
                {
                    _victoryPhase = 3;
                }
            }
            else if (_victoryPhase == 3)
            {
                Save.MarkChapterCompleted("al-fil");
                ((DialogueScene)_game.SceneManager.GetScene(SceneId.Dialogue)).LoadChapterFile(ChapterPath.Ending);
                _game.SceneManager.SwitchTo(SceneId.Dialogue);
            }

            return;
        }

        if (_waveTransition)
        {
            _waveTransitionT += dt;
            if (_waveTransitionT > 2f)
            {
                _waveTransition = false;
                _waveTransitionT = 0;
                StartWave();
            }
            return;
        }

        UpdateInput(dt, kb, touch);
        UpdateBird(dt);
        UpdateStones(dt);
        UpdateEnemies(dt);
        UpdateWave(dt);
        UpdateParticles(dt);
    }

    private void UpdateInput(float dt, KeyboardState kb, Game1.TouchState touch)
    {
        var onUi = _hoverBack;
        var pressing = touch.IsDown && !onUi;
        var spacePressed = kb.IsKeyDown(Keys.Space);

        var pressThisFrame = pressing && !_wasPressing;
        var releaseThisFrame = !pressing && _wasPressing;
        var spacePressThisFrame = spacePressed && !_prevSpace;
        var spaceReleaseThisFrame = !spacePressed && _prevSpace;

        if (pressThisFrame)
            _touchStartPos = new Vector2(touch.Position.X, touch.Position.Y);

        var chargeReleased = false;

        if ((pressThisFrame || spacePressThisFrame) && _dropCooldown <= 0 && !_isCharging)
        {
            _isCharging = true;
            _chargeTime = 0;
        }

        if (_isCharging && (pressing || spacePressed))
        {
            _chargeTime = Math.Min(MaxChargeTime, _chargeTime + dt);
        }

        if (_isCharging && !pressing && !spacePressed)
        {
            ReleaseChargedStone(_chargeTime);
            _isCharging = false;
            _chargeTime = 0;
            _dropCooldown = 0.2f;
            chargeReleased = true;
        }

        if (!chargeReleased)
        {
            if (releaseThisFrame && _isCharging)
            {
                ReleaseChargedStone(_chargeTime);
                _isCharging = false;
                _chargeTime = 0;
                _dropCooldown = 0.2f;
            }
            else if (spaceReleaseThisFrame && _isCharging)
            {
                ReleaseChargedStone(_chargeTime);
                _isCharging = false;
                _chargeTime = 0;
                _dropCooldown = 0.2f;
            }
        }

        _wasPressing = pressing;
        _prevSpace = spacePressed;
    }

    private void StartWave()
    {
        _currentWave++;
        var w = _currentWave;
        if (_isEndless)
        {
            _totalWaves = int.MaxValue;
            _enemiesPerWave = 2 + w * 2;
            _spawnedThisWave = 0;
            _spawnTimer = 0;
            _spawnInterval = Math.Max(0.3f, 1.8f - w * 0.08f);
        }
        else
        {
            _totalWaves = 5;
            _enemiesPerWave = 3 + w * 2;
            _spawnedThisWave = 0;
            _spawnTimer = 0;
            _spawnInterval = Math.Max(0.35f, 1.5f - w * 0.12f);
        }
        _maxAmmo = 10 + w * 2;
        _ammo = _maxAmmo;
        _combo = 0;
        _turbulence = _isEndless ? 0 : w * 15f;
        _waveActive = true;
    }

    private void UpdateBird(float dt)
    {
        var kb = Keyboard.GetState();
        var touch = _game.GetTouch();
        _birdVx = 0;

        if (kb.IsKeyDown(Keys.Left) || kb.IsKeyDown(Keys.A)) _birdVx = -_birdSpeed;
        if (kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D)) _birdVx = _birdSpeed;

        if (_birdVx == 0 && touch.IsDown)
        {
            var dx = touch.Position.X - _touchStartPos.X;
            if (Math.Abs(dx) > SwipeThreshold)
                _birdVx = Math.Sign(dx) * _birdSpeed;
        }

        _birdX += _birdVx * dt;
        _birdX += (float)(Random.Shared.NextDouble() * 2 - 1) * _turbulence * dt;
        _birdX = MathHelper.Clamp(_birdX, 30, _game.Width - 30);

        _birdFlyT += dt;
        _birdY = _game.Height * 0.2f + (float)Math.Sin(_birdFlyT * 1.3f) * 40;

        _birdFrameT += dt;
        if (_birdFrameT >= 0.08f)
        {
            _birdFrameT = 0;
            _birdFrame = (_birdFrame + 1) % _birdFrames;
        }
    }

    private void ReleaseChargedStone(float charge)
    {
        if (_ammo <= 0) return;
        _ammo--;

        float radius; int aoeDmg; bool pierce; float cd;
        if (charge < 0.3f)
        {
            radius = 0; aoeDmg = 0; pierce = false; cd = 0.2f;
        }
        else if (charge < 0.8f)
        {
            radius = 50; aoeDmg = 1; pierce = false; cd = 0.35f;
        }
        else if (charge < 1.4f)
        {
            radius = 90; aoeDmg = 2; pierce = false; cd = 0.5f;
        }
        else
        {
            radius = 140; aoeDmg = 3; pierce = true; cd = 0.6f;
        }

        _stones.Add(new Stone
        {
            X = _birdX, Y = _birdY + 20, Vy = 30,
            AoeRadius = radius, AoeDamage = aoeDmg, PierceShield = pierce
        });
        Audio.PlaySfx(_sfxDrop);
        _dropCooldown = cd;
    }

    private void UpdateStones(float dt)
    {
        _dropCooldown = Math.Max(0, _dropCooldown - dt);

        for (var i = _stones.Count - 1; i >= 0; i--)
        {
            var s = _stones[i];
            if (!s.Alive) { _stones.RemoveAt(i); continue; }
            s.Vy += Gravity * dt;
            s.Y += s.Vy * dt;
            if (s.Y >= GroundY - 4)
            {
                s.Alive = false;
                _combo = 0;
                SpawnParticles(s.X, GroundY - 4, new Color(200, 180, 140), 3);
            }
        }
    }

    private void UpdateEnemies(float dt)
    {
        for (var i = _enemies.Count - 1; i >= 0; i--)
        {
            var e = _enemies[i];
            if (!e.Alive) { _enemies.RemoveAt(i); continue; }
            e.X -= e.Speed * dt;
            e.AnimT += dt;
            if (e.Flash > 0) e.Flash = Math.Max(0, e.Flash - dt * 5);

            if (e.X <= 246)
            {
                e.Alive = false;
                _kabahHp--;
                SpawnParticles(_game.Width * 0.08f, e.Y + e.H / 2, new Color(220, 60, 60), 8);
                if (_kabahHp <= 0) { _gameOver = true; _victory = false; }
            }

            foreach (var s in _stones)
            {
                if (!s.Alive) continue;
                if (s.X >= e.X && s.X <= e.X + e.W && s.Y >= e.Y && s.Y <= e.Y + e.H)
                {
                    s.Alive = false;
                    Audio.PlaySfx(_sfxHit);

                    if (e.Shield > 0)
                    {
                        e.Shield--;
                        e.Flash = 1f;
                        _combo = 0;
                        SpawnParticles(s.X, s.Y, new Color(100, 200, 255), 5);
                    }
                    else
                    {
                        e.Hp--;
                        e.Flash = 1f;
                        _score += 10;
                        _combo++;
                        if (_combo % 3 == 0)
                        {
                            if (_isEndless) _score += 5;
                            else _ammo++;
                        }
                        if (e.IsBoss) _score += 20;
                        SpawnParticles(s.X, s.Y, new Color(220, 200, 160), 5);
                    }

                    if (s.AoeRadius > 0)
                    {
                        foreach (var other in _enemies)
                        {
                            if (!other.Alive || other == e) continue;
                            var ox = other.X + other.W / 2;
                            var oy = other.Y + other.H / 2;
                            var dx = s.X - ox;
                            var dy = s.Y - oy;
                            if (Math.Sqrt(dx * dx + dy * dy) <= s.AoeRadius)
                            {
                                if (s.PierceShield) other.Shield = 0;
                                other.Hp -= s.AoeDamage;
                                other.Flash = 1f;
                                _score += 10;
                                if (other.Hp <= 0)
                                {
                                    other.Alive = false;
                                    _totalKills++;
                                    _score += other.IsBoss ? 50 : 10;
                                    SpawnParticles(other.X + other.W / 2, other.Y + other.H / 2, new Color(255, 200, 100), 10);
                                }
                            }
                        }
                    }

                    if (e.Hp <= 0)
                    {
                        e.Alive = false;
                        _totalKills++;
                        _score += e.IsBoss ? 50 : 10;
                        SpawnParticles(e.X + e.W / 2, e.Y + e.H / 2, new Color(255, 200, 100), 10);
                    }
                    break;
                }
            }
        }
    }

    private void UpdateWave(float dt)
    {
        if (!_waveActive) return;

        if (_spawnedThisWave < _enemiesPerWave)
        {
            _spawnTimer += dt;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0;
                _spawnedThisWave++;
                SpawnEnemy();
            }
        }

        if (_spawnedThisWave >= _enemiesPerWave && _enemies.Count == 0)
        {
            _waveActive = false;
            if (!_isEndless && _currentWave >= _totalWaves)
            {
                _gameOver = true;
                _victory = true;
            }
            else
            {
                _waveTransition = true;
                _waveTransitionT = 0;
            }
        }
    }

    private void SpawnEnemy()
    {
        var w = _currentWave;
        var isBoss = _spawnedThisWave == _enemiesPerWave && w % 2 == 0;
        var r = _rng.NextSingle();
        float sz, spd; int hp, shield = 0;

        if (isBoss)
        {
            sz = 1.8f; spd = 35 + w * 6; hp = 3; shield = 2;
        }
        else if (r < 0.3f)
        {
            sz = 0.5f + _rng.NextSingle() * 0.2f;
            spd = 60 + w * 10 + _rng.NextSingle() * 15;
            hp = 1;
        }
        else if (r < 0.7f)
        {
            sz = 0.8f + _rng.NextSingle() * 0.2f;
            spd = 40 + w * 8 + _rng.NextSingle() * 10;
            hp = _rng.NextSingle() < 0.3f ? 2 : 1;
        }
        else
        {
            sz = 1.3f + _rng.NextSingle() * 0.3f;
            spd = 20 + w * 6 + _rng.NextSingle() * 8;
            hp = 3;
        }

        var scale5x = 2.5f;
        var rise = 0f;
        var drawW = 55 * sz * scale5x;
        var drawSc = drawW / _eleFw;
        var drawH = _eleFh * drawSc;

        _enemies.Add(new Enemy
        {
            X = _game.Width + 30,
            Y = GroundY - drawH - rise,
            W = drawW,
            H = drawH,
            Speed = spd,
            Hp = hp,
            MaxHp = hp,
            Shield = shield,
            IsBoss = isBoss
        });
    }

    private void UpdateParticles(float dt)
    {
        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.X += p.Vx * dt;
            p.Y += p.Vy * dt;
            p.Vy += 100 * dt;
            p.Life -= dt;
            if (p.Life <= 0) _particles.RemoveAt(i);
        }
    }

    private void SpawnParticles(float x, float y, Color col, int count)
    {
        for (var i = 0; i < count; i++)
            _particles.Add(new Particle
            {
                X = x, Y = y,
                Vx = (_rng.NextSingle() - 0.5f) * 100,
                Vy = -_rng.NextSingle() * 80 - 20,
                Life = 0.4f + _rng.NextSingle() * 0.3f,
                MaxLife = 0.7f,
                Color = col
            });
    }

    public void Draw()
    {
        var b = _game.SpriteBatch;
        b.Begin();

        DrawBg(b);

        if (_waveTransition)
        {
            DrawWaveIntro(b);
            b.End();
            return;
        }

        if (_gameOver && _victoryPhase > 0)
        {
            DrawVictoryFx(b);
        }
        else
        {
            DrawKabah(b);
            DrawEnemies(b);
            DrawBird(b);
            DrawStones(b);
            DrawParticles(b);
            DrawHud(b);
        }

        if (_gameOver && _victoryPhase == 0) DrawGameOver(b);

        b.End();
    }

    private void DrawBg(SpriteBatch b)
    {
        b.Draw(_bgGame, new Rectangle(0, 0, _game.Width, _game.Height), Color.White);
    }

    private void DrawVictoryFx(SpriteBatch b)
    {
        if (_whiteOverA > 0)
            b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.White * _whiteOverA);

        if (_victoryPhase == 2)
        {
            var loc = _game.Loc;
            var title = loc.Get("listen_title");
            var sz = _font.MeasureString(title);
            b.DrawString(_font, title, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2 - 24), Color.Gold);
            var sub = loc.Get("listen_sub");
            var subSz = _font.MeasureString(sub);
            var pulse = 0.6f + (float)Math.Sin(_elapsed * 2) * 0.2f;
            b.DrawString(_font, sub, new Vector2((_game.Width - subSz.X) / 2, _game.Height / 2 + 16), Color.White * pulse);
        }
    }

    private void DrawKabah(SpriteBatch b)
    {
        var kabahSz = 210;
        var kx = 36;
        var ky = GroundY - kabahSz;
        b.Draw(_sprKabah, new Rectangle(kx, ky, kabahSz, kabahSz), Color.White);

        var hpPct = (float)_kabahHp / _maxKabahHp;
        var barW = 60;
        var barH = 6;
        var barX = kx + kabahSz / 2 - barW / 2;
        var barY = ky - 14;
        b.Draw(_game.WhitePixel, new Rectangle(barX, barY, barW, barH), new Color(40, 30, 20));
        var hpCol = hpPct > 0.5f ? Color.Gold : (hpPct > 0.25f ? Color.Orange : Color.Red);
        b.Draw(_game.WhitePixel, new Rectangle(barX, barY, (int)(barW * hpPct), barH), hpCol);
    }

    private void DrawBird(SpriteBatch b)
    {
        var src = new Rectangle(
            (_birdFrame % _birdCols) * _birdFw,
            (_birdFrame / _birdCols) * _birdFh,
            _birdFw, _birdFh);
        var sc = 0.35f;
        var w = (int)(_birdFw * sc);
        var h = (int)(_birdFh * sc);
        var flip = _birdVx < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        b.Draw(_birdTex, new Rectangle((int)_birdX - w / 2, (int)_birdY - h / 2, w, h), src, Color.White, 0, Vector2.Zero, flip, 0);

        if (_isCharging)
        {
            var pct = _chargeTime / MaxChargeTime;
            if (pct > 0.05f)
            {
                var radius = 20 + pct * 40;
                var sz = (int)(radius * 2);
                var col = GetChargeColor(pct);
                b.Draw(_circleTex, new Rectangle((int)_birdX - sz / 2, (int)_birdY - sz / 2, sz, sz),
                    col * (0.2f + pct * 0.3f));
            }
        }
    }

    private void DrawEnemies(SpriteBatch b)
    {
        foreach (var e in _enemies)
        {
            if (!e.Alive) continue;

            var frame = (int)(e.AnimT * 4) % _eleFrames;
            var src = new Rectangle(
                (frame % _eleCols) * _eleFw,
                (frame / _eleCols) * _eleFh,
                _eleFw, _eleFh);
            var sc = e.W / _eleFw;
            var w = (int)(_eleFw * sc);
            var h = (int)(_eleFh * sc);
            var sw = (int)(w * 1.4f);
            var sh = (int)(h * 0.15f);
            var sy = (int)(e.Y + h - sh * 0.6f);
            var sx = (int)(e.X + w / 2 - sw / 2);
            b.Draw(_circleTex, new Rectangle(sx, sy, sw, sh), new Color(0, 0, 0, 80));

            var col = e.Flash > 0 ? Color.Red : (e.IsBoss ? new Color(160, 100, 70) : Color.White);
            b.Draw(_elephantTex, new Rectangle((int)e.X, (int)e.Y, w, h), src, col);

            if (e.Shield > 0)
            {
                b.Draw(_circleTex, new Rectangle((int)e.X - 6, (int)e.Y - 6, (int)e.W + 12, (int)e.H + 12),
                    new Color(80, 180, 255) * (0.3f + (float)Math.Sin(_elapsed * 3) * 0.15f));
                var hpStr = $"x{e.Shield}";
                var hpSz = _font.MeasureString(hpStr) * 0.7f;
                b.DrawString(_font, hpStr, new Vector2((int)(e.X + e.W / 2 - hpSz.X / 2), (int)e.Y - 24),
                    new Color(80, 180, 255), 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
            }

            if (e.MaxHp > 1)
            {
                var hpW = 30;
                var hpH = 4;
                var hpX = (int)(e.X + e.W / 2 - hpW / 2);
                var hpY = (int)e.Y - 8;
                b.Draw(_game.WhitePixel, new Rectangle(hpX, hpY, hpW, hpH), new Color(40, 30, 20));
                b.Draw(_game.WhitePixel, new Rectangle(hpX, hpY, (int)(hpW * (float)e.Hp / e.MaxHp), hpH), new Color(220, 60, 60));
            }
        }
    }

    private void DrawStones(SpriteBatch b)
    {
        var stoneColor = new Color(140, 120, 80);
        foreach (var s in _stones)
        {
            if (!s.Alive) continue;

            var sz = s.AoeRadius > 0 ? 55 : 45;
            var r = new Rectangle((int)s.X - sz / 2, (int)s.Y - sz / 2, sz, sz);
            b.Draw(_stoneGradTex, r, stoneColor);

            if (s.AoeRadius > 0)
            {
                var glowSz = (int)(s.AoeRadius * 2);
                if (glowSz > 0)
                    b.Draw(_circleTex, new Rectangle((int)s.X - glowSz / 2, (int)s.Y - glowSz / 2, glowSz, glowSz),
                        new Color(200, 180, 140) * 0.15f);
            }
        }
    }

    private Color GetChargeColor(float pct)
    {
        if (pct < 0.15f) return Color.White;
        if (pct < 0.4f) return Color.Yellow;
        if (pct < 0.7f) return Color.Orange;
        return Color.Red;
    }

    private void DrawParticles(SpriteBatch b)
    {
        foreach (var p in _particles)
        {
            var a = p.Life / p.MaxLife;
            b.Draw(_game.WhitePixel, new Rectangle((int)p.X, (int)p.Y, 4, 4), p.Color * a);
        }
    }

    private void DrawHud(SpriteBatch b)
    {
        var loc = _game.Loc;
        var panelH = HudPanelH;
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, panelH), new Color(10, 8, 6, 220));

        if (_isEndless)
            b.DrawString(_font, $"WAVE {_currentWave}", new Vector2(10, panelH / 2 - 8), Color.White);
        else
            b.DrawString(_font, loc.Format("wave", _currentWave, _totalWaves), new Vector2(10, panelH / 2 - 8), Color.White);

        b.DrawString(_font, loc.Format("score", _score), new Vector2(180, panelH / 2 - 8), Color.Gold);
        b.DrawString(_font, loc.Format("kill", _totalKills), new Vector2(360, panelH / 2 - 8), Color.LightGray);

        if (_isEndless)
        {
            b.DrawString(_font, loc.Get("endless_mode"), new Vector2(540, panelH / 2 - 8), Color.Red);
        }

        var ammoCol = _ammo <= 3 ? Color.Red : Color.Gold;
        b.DrawString(_font, loc.Format("stone", _ammo, _maxAmmo), new Vector2(620, panelH / 2 - 8), ammoCol);

        if (_isCharging)
        {
            var pct = _chargeTime / MaxChargeTime;
            var barW = 50; var barH = 6;
            var barX = 740; var barY = panelH / 2 - barH / 2;
            b.Draw(_game.WhitePixel, new Rectangle(barX, barY, barW, barH), new Color(40, 30, 20));
            var col = GetChargeColor(pct);
            b.Draw(_game.WhitePixel, new Rectangle(barX, barY, (int)(barW * pct), barH), col);
        }

        if (_combo >= 3)
            b.DrawString(_font, loc.Format("combo", _combo), new Vector2(820, panelH / 2 - 8), Color.LimeGreen);

        var iconSz = 64;
        var iconX = _game.Width - iconSz - 8;
        var iconY = (panelH - iconSz) / 2;
        _backRect = new Rectangle(iconX, iconY, iconSz, iconSz);
        var iconCol = _hoverBack ? Color.Gold : Color.White * 0.8f;
        b.Draw(_iconHome, _backRect, iconCol);
    }

    private void DrawWaveIntro(SpriteBatch b)
    {
        var loc = _game.Loc;
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.5f);

        var diffStr = _isEndless ? loc.Get("endless_mode") : loc.Get("normal_mode");
        var diffSz = _font.MeasureString(diffStr);
        b.DrawString(_font, diffStr, new Vector2((_game.Width - diffSz.X) / 2, _game.Height / 2 - 80), _isEndless ? Color.Red : Color.Gold);

        var waveStr = _currentWave == 0 ? $"WAVE 1" : $"WAVE {_currentWave + 1}";
        var sz = _font.MeasureString(waveStr);
        b.DrawString(_font, waveStr, new Vector2((_game.Width - sz.X) / 2, _game.Height / 2 - 40), Color.Gold);
        var subKey = _currentWave == 0 ? "wave_sub_first" : "wave_sub_next";
        var sub = loc.Get(subKey);
        var subSz = _font.MeasureString(sub);
        b.DrawString(_font, sub, new Vector2((_game.Width - subSz.X) / 2, _game.Height / 2 + 10), Color.LightGray);
    }

    private void DrawGameOver(SpriteBatch b)
    {
        var loc = _game.Loc;
        b.Draw(_game.WhitePixel, new Rectangle(0, 0, _game.Width, _game.Height), Color.Black * 0.6f);
        var pw = 420; var ph = _isEndless ? 250 : 280;
        var px = (_game.Width - pw) / 2; var py = (_game.Height - ph) / 2;
        b.Draw(_panelChapter, new Rectangle(px, py, pw, ph), Color.White);
        b.Draw(_game.WhitePixel, new Rectangle(px + 4, py + 4, pw - 8, ph - 8), new Color(0, 0, 0, 100));

        if (_isEndless)
        {
            var t1 = "GAME OVER";
            var s1 = _font.MeasureString(t1);
            b.DrawString(_font, t1, new Vector2((_game.Width - s1.X) / 2, py + 30), Color.Red);

            var wStr = loc.Format("wave", _currentWave, 999);
            var wSz = _font.MeasureString(wStr);
            b.DrawString(_font, wStr, new Vector2((_game.Width - wSz.X) / 2, py + 70), Color.White);

            var fScoreStr = loc.Format("final_score", _score);
            var fScoreSz = _font.MeasureString(fScoreStr);
            b.DrawString(_font, fScoreStr, new Vector2((_game.Width - fScoreSz.X) / 2, py + 110), Color.Gold);

            var tKillsStr = loc.Format("total_elephants", _totalKills);
            var tKillsSz = _font.MeasureString(tKillsStr);
            b.DrawString(_font, tKillsStr, new Vector2((_game.Width - tKillsSz.X) / 2, py + 140), Color.LightGray);

            if (_gameOverT > 2f)
            {
                var tapStr = loc.Get("tap_replay");
                var tapSz = _font.MeasureString(tapStr);
                b.DrawString(_font, tapStr, new Vector2((_game.Width - tapSz.X) / 2, py + 190),
                    Color.White * (0.5f + (float)Math.Sin(_elapsed * 3) * 0.3f));
            }
        }
        else
        {
            var t1 = _victory ? loc.Get("victory") : loc.Get("game_over");
            var c1 = _victory ? Color.Gold : Color.Red;
            var s1 = _font.MeasureString(t1);
            b.DrawString(_font, t1, new Vector2((_game.Width - s1.X) / 2, py + 30), c1);

            if (_victory)
            {
                var sub = loc.Get("victory_sub");
                var subSz = _font.MeasureString(sub);
                b.DrawString(_font, sub, new Vector2((_game.Width - subSz.X) / 2, py + 70), Color.White);
            }

            var fScoreStr = loc.Format("final_score", _score);
            var fScoreSz = _font.MeasureString(fScoreStr);
            b.DrawString(_font, fScoreStr, new Vector2((_game.Width - fScoreSz.X) / 2, py + 120), Color.Gold);
            var tKillsStr = loc.Format("total_elephants", _totalKills);
            var tKillsSz = _font.MeasureString(tKillsStr);
            b.DrawString(_font, tKillsStr, new Vector2((_game.Width - tKillsSz.X) / 2, py + 150), Color.LightGray);

            if (_gameOverT > 2f)
            {
                var tapStr = loc.Get("tap_replay");
                var tapSz = _font.MeasureString(tapStr);
                b.DrawString(_font, tapStr, new Vector2((_game.Width - tapSz.X) / 2, py + 210),
                    Color.White * (0.5f + (float)Math.Sin(_elapsed * 3) * 0.3f));
            }
        }
    }

    public void Unload()
    {
        Audio.StopBgm();
        _circleTex?.Dispose();
        _circleTex = null;
        _stoneGradTex?.Dispose();
        _stoneGradTex = null;
    }
}
