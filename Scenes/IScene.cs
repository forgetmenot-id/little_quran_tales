namespace LittleQuranTales.Scenes;

public interface IScene
{
    void Load();
    void Update(float deltaTime);
    void Draw();
    void Unload();
}
