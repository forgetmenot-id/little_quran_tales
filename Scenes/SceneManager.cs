using System;
using System.Collections.Generic;

namespace LittleQuranTales.Scenes;

public class SceneManager
{
    private readonly Dictionary<string, IScene> _scenes = new();
    private IScene _current;
    private string _currentId;

    public IScene Current => _current;

    public void Register(string id, IScene scene) => _scenes[id] = scene;

    public T GetScene<T>(string id) where T : class, IScene => _scenes.GetValueOrDefault(id) as T;

    public IScene GetScene(string id) => _scenes.GetValueOrDefault(id);

    public void SwitchTo(string id)
    {
        if (!_scenes.ContainsKey(id)) return;

        _current?.Unload();
        _current = _scenes[id];
        _currentId = id;
        _current.Load();
    }

    public void Update(float deltaTime)
    {
        _current?.Update(deltaTime);
    }

    public void Draw()
    {
        _current?.Draw();
    }
}
