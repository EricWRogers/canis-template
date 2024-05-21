#ifdef _WIN32
#include <windows.h>
#endif

#include <Canis/App.hpp>
#include <Canis/Yaml.hpp>
#include <Canis/ECS/Decode.hpp>
#include <Canis/ECS/Encode.hpp>

#include <Canis/ECS/Systems/UISliderSystem.hpp>
#include <Canis/ECS/Systems/UISliderKnobSystem.hpp>

#include "ECS/ScriptableEntities/DebugCamera2D.hpp"
#include "ECS/ScriptableEntities/SplashLoader.hpp"
#include "ECS/ScriptableEntities/MainMenuButtons.hpp"

//////////////// HELL

struct TestComponent
{
    int number = 0;
    std::string name;

    static void RegisterProperties()
    {
        REGISTER_PROPERTY(TestComponent, number, int);
        REGISTER_PROPERTY(TestComponent, name, std::string);
    }
};

// Ensure properties are registered
namespace
{
    bool registeredTestComponent = (TestComponent::RegisterProperties(), true);
    bool registeredRectTransformComponent = (Canis::RectTransformComponent::RegisterProperties(), true);
    bool registeredColorComponent = (Canis::ColorComponent::RegisterProperties(), true);
    bool registeredButtonComponent = (Canis::ButtonComponent::RegisterProperties(), true);
    bool refisteredCamera2DComponent = (Canis::Camera2DComponent::RegisterProperties(), true);
}

template <typename ComponentType>
void DecodeComponent(YAML::Node &_n, Canis::Entity &_entity, Canis::SceneManager *_sceneManager)
{
    Canis::Log("Component Name: " + std::string(type_name<ComponentType>()));

    if (auto componentNode = _n[std::string(type_name<ComponentType>())])
    {
        auto &component = _entity.AddComponent<ComponentType>();
        auto &registry = GetPropertyRegistry<ComponentType>();

        for (const auto &[propertyName, setter] : registry.setters)
        {
            if (componentNode[propertyName])
            {
                Canis::Log("Component Name: " + std::string(type_name<ComponentType>()) + " Property Name: " + propertyName);
                YAML::Node propertyNode = componentNode[propertyName];
                setter(propertyNode, &component, _sceneManager);
            }
        }
    }
}

//////////////// EXIT HELL

//////////////// DECODE

template <typename ComponentType>
bool DecodeScriptComponent(const std::string &_name, Canis::Entity &_entity)
{
    if (_name == std::string(type_name<ComponentType>()))
    {
        Canis::ScriptComponent scriptComponent = {};
        scriptComponent.Bind<ComponentType>();
        _entity.AddComponent<Canis::ScriptComponent>(scriptComponent);
        return true;
    }
    return false;
}

template <typename ComponentType>
std::function<bool(const std::string &, Canis::Entity &)> CreateDecodeFunction()
{
    return [](const std::string &_name, Canis::Entity &_entity) -> bool
    {
        return DecodeScriptComponent<ComponentType>(_name, _entity);
    };
}

#define REGISTER_DECODE_FUNCTION(AppInstance, ComponentType) \
    AppInstance.AddDecodeScriptableEntity(CreateDecodeFunction<ComponentType>())

//////////////// ENCODE

template <typename ComponentType>
bool EncodeScriptComponent(YAML::Emitter &_out, Canis::ScriptableEntity *_scriptableEntity)
{
    if (auto castedEntity = dynamic_cast<ComponentType *>(_scriptableEntity))
    {
        _out << YAML::Key << "Canis::ScriptComponent" << YAML::Value << std::string(type_name<ComponentType>()); // typeid(ComponentType).name();
        return true;
    }
    return false;
}

template <typename ComponentType>
std::function<bool(YAML::Emitter &, Canis::ScriptableEntity *)> CreateEncodeFunction()
{
    return [](YAML::Emitter &_out, Canis::ScriptableEntity *_scriptableEntity) -> bool
    {
        return EncodeScriptComponent<ComponentType>(_out, _scriptableEntity);
    };
}

#define REGISTER_ENCODE_FUNCTION(AppInstance, ComponentType) \
    AppInstance.AddEncodeScriptableEntity(CreateEncodeFunction<ComponentType>())

int main(int argc, char *argv[])
{
    Canis::App app("SuperPupStudio", "StopTheSlimesDemo");

    // decode system
    app.AddDecodeSystem(Canis::DecodeButtonSystem);

    // decode render system
    app.AddDecodeRenderSystem(Canis::DecodeRenderHUDSystem);
    app.AddDecodeRenderSystem(Canis::DecodeRenderTextSystem);
    app.AddDecodeRenderSystem(Canis::DecodeSpriteRenderer2DSystem);

    // decode scriptable entities
    REGISTER_DECODE_FUNCTION(app, DebugCamera2D);
    REGISTER_DECODE_FUNCTION(app, SplashLoader);
    REGISTER_DECODE_FUNCTION(app, MainMenuButtons);

    // decode component
    app.AddDecodeComponent(Canis::DecodeTagComponent);
    app.AddDecodeComponent(DecodeComponent<Canis::Camera2DComponent>);
    app.AddDecodeComponent(DecodeComponent<Canis::RectTransformComponent>);
    app.AddDecodeComponent(DecodeComponent<Canis::ColorComponent>);
    app.AddDecodeComponent(Canis::DecodeTextComponent);
    app.AddDecodeComponent(DecodeComponent<Canis::ButtonComponent>);
    app.AddDecodeComponent(Canis::DecodeSprite2DComponent);
    app.AddDecodeComponent(Canis::DecodeUIImageComponent);
    app.AddDecodeComponent(Canis::DecodeUISliderComponent);
    app.AddDecodeComponent(Canis::DecodeSpriteAnimationComponent);
    app.AddDecodeComponent(DecodeComponent<TestComponent>);

    // encode component
    app.AddEncodeComponent(Canis::EncodeTransformComponent);
    app.AddEncodeComponent(Canis::EncodeRectTransformComponent);
    app.AddEncodeComponent(Canis::EncodeColorComponent);
    app.AddEncodeComponent(Canis::EncodeTagComponent);
    app.AddEncodeComponent(Canis::EncodeTextComponent);
    app.AddEncodeComponent(Canis::EncodeButtonComponent);

    // encode scriptable component
    REGISTER_ENCODE_FUNCTION(app, DebugCamera2D);
    REGISTER_ENCODE_FUNCTION(app, SplashLoader);
    REGISTER_ENCODE_FUNCTION(app, MainMenuButtons);

    // add scene
    // app.AddSplashScene(new Canis::Scene("engine_splash", "assets/scenes/engine_splash.scene"));
    app.AddSplashScene(new Canis::Scene("main_menu", "assets/scenes/main_menu.scene"));

    app.Run("Canis | Stop The Slimes", "main_menu");

    return 0;
}