[OPENGL VERSION]

#ifdef GL_ES
    precision mediump float;
#endif

out vec4 FragColor;

struct Material {
    sampler2D specular;
    sampler2D diffuse;
    float shininess;
}; 

struct DirLight {
    vec3 direction;
	
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct PointLight {
    vec3 position;
    
    float constant;
    float linear;
    float quadratic;
	
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

struct SpotLight {
    vec3 position;
    vec3 direction;
    float cutOff;
    float outerCutOff;
  
    float constant;
    float linear;
    float quadratic;
  
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;       
};

#define NR_POINT_LIGHTS 0

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoords;

uniform vec3 viewPos;
uniform vec4 color;
uniform int numDirLights;
uniform int numPointLights;
uniform int numSpotLights;
uniform DirLight dirLight;
uniform PointLight pointLights[4];
uniform SpotLight spotLight[4];
uniform Material material;

// function prototypes
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir);

void main()
{    
    // properties
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - FragPos);
    
    // == =====================================================
    // Our lighting is set up in 3 phases: directional, point lights and an optional flashlight
    // For each phase, a calculate function is defined that calculates the corresponding color
    // per lamp. In the main() function we take all the calculated colors and sum them up for
    // this fragment's final color.
    // == =====================================================
    // phase 1: directional lighting

    vec3 dirResult;
    if (numDirLights > 0)
        dirResult = CalcDirLight(dirLight, norm, viewDir);
    // phase 2: point lights
    vec3 pointResult;
    for(int i = 0; i < numPointLights; i++)
        pointResult += CalcPointLight(pointLights[i], norm, FragPos, viewDir);    
    // phase 3: spot light
    vec3 spotResult;
    for(int i = 0; i < numSpotLights; i++)
        spotResult += CalcSpotLight(spotLight[i], norm, FragPos, viewDir);

    vec3 result = dirResult + pointResult + spotResult;

    float alpha = min(color.a, texture(material.diffuse, TexCoords).a);
    
    FragColor = vec4(result, alpha);

    // apply gamma correction
    float gamma = 2.2;
    FragColor.rgb = pow(FragColor.rgb, vec3(1.0/gamma));
}

// calculates the color when using a directional light.
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-light.direction);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // combine results
    vec3 ambient = light.ambient * (vec3(color) * vec3(texture(material.diffuse, TexCoords)));
    //vec3 ambient = light.ambient * material.diffuse;
    vec3 diffuse = light.diffuse * diff * (vec3(color) * vec3(texture(material.diffuse, TexCoords)));
    //vec3 diffuse = light.diffuse * diff * material.diffuse;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords));
    //vec3 specular = light.specular * spec * material.specular;
    return (ambient + diffuse + specular);
}

// calculates the color when using a point light.
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // specular shading
    //vec3 reflectDir = reflect(-lightDir, normal);
    //float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    // specular shading blinn-phong
    vec3 halfwayDir = normalize(lightDir + viewDir);  
    float spec = pow(max(dot(normal, halfwayDir), 0.0), material.shininess);

    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

    // combine results
    vec3 ambient = light.ambient * (vec3(color) * vec3(texture(material.diffuse, TexCoords)));
    vec3 diffuse = light.diffuse * diff * (vec3(color) * vec3(texture(material.diffuse, TexCoords)));
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords));

    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}

// calculates the color when using a spot light.
vec3 CalcSpotLight(SpotLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));    
    // spotlight intensity
    float theta = dot(lightDir, normalize(-light.direction)); 
    float epsilon = light.cutOff - light.outerCutOff;
    float intensity = clamp((theta - light.outerCutOff) / epsilon, 0.0, 1.0);
    // combine results
    vec3 ambient = light.ambient * (vec3(color) * vec3(texture(material.diffuse, TexCoords)));
    vec3 diffuse = light.diffuse * diff * (vec3(color) * vec3(texture(material.diffuse, TexCoords)));
    vec3 specular = light.specular * spec * vec3(texture(material.specular, TexCoords));
    //vec3 ambient = light.ambient * material.diffuse;
    //vec3 diffuse = light.diffuse * diff * material.diffuse;
    //vec3 specular = light.specular * spec * material.specular;
    ambient *= attenuation * intensity;
    diffuse *= attenuation * intensity;
    specular *= attenuation * intensity;
    return (ambient + diffuse + specular);
}