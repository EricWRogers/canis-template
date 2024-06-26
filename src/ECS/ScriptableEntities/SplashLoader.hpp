#pragma once

class SplashLoader : public Canis::ScriptableEntity
{

private:
    float timer = 0.0f;
    bool hasCalledLoad = false;

public:

    void OnCreate()
    {
        Canis::Log("Create");
    }

    void OnReady()
    {
        
    }
    
    void OnDestroy()
    {

    }

    void OnUpdate(float _dt)
    {
        timer+=_dt;

        if (timer >= 3.0f)
        {
            Canis::Log("load main menu");
            GetSceneManager().Load("main_menu");
        }
    }
};

bool DecodeSplashLoader(const std::string &_name, Canis::Entity &_entity)
{
    if (_name == "SplashLoader")
    {
        Canis::ScriptComponent scriptComponent = {};
        scriptComponent.Bind<SplashLoader>();
        _entity.AddComponent<Canis::ScriptComponent>(scriptComponent);
        return true;
    }
    return false;
}