[OPENGL VERSION]

#ifdef GL_ES
    precision mediump float;
#endif

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 BrightColor;

struct Material {
    sampler2D specular;
    sampler2D diffuse;
    sampler2D emission;
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

in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
} fs_in;

uniform vec2 TEXELSHADOWSIZE;

uniform sampler2D shadowMap;

uniform mat4 PROJECTION;
uniform mat4 VIEW;
uniform mat4 PVM;

uniform vec3 lightPos;
uniform vec3 viewPos;
uniform vec4 COLOR;
uniform vec3 EMISSION;
uniform float EMISSIONUSINGALBEDOINTESITY;
uniform int numDirLights;
uniform DirLight dirLight;
uniform int numPointLights;
uniform PointLight pointLights[10];
uniform Material material;

uniform sampler2D SCREENTEXTURE;
uniform float TIME;
uniform vec2 distortion;

const float gamma = 2.2;

vec3 emission;

vec3 lerp(vec3 _min, vec3 _max, float _fraction);
float ShadowCalculation(vec4 fragPosLightSpace);
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir);
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir);

float rand(vec2 co)
{
    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

void main()
{           
    // properties
    emission = texture(material.emission, fs_in.TexCoords).rgb;
    vec3 norm = fs_in.Normal;
    vec3 viewDir = normalize(viewPos - fs_in.FragPos);

    vec3 result = vec3(0.0, 0.0, 0.0);
    if (numDirLights > 0)
        result = CalcDirLight(dirLight, norm, viewDir);
    
    for(int i = 0; i < numPointLights; i++)
        result += CalcPointLight(pointLights[i], norm, fs_in.FragPos, viewDir);

    // alpha
    float alpha = min(COLOR.a, texture(material.diffuse, fs_in.TexCoords).a);

    //vec3 result = pow(dirResult, vec3(1.0 / gamma));

    if (alpha >= 1.0)
        BrightColor = vec4(0.0, 0.0, 0.0, 0.0);

    // check whether result is higher than some threshold, if so, output as bloom threshold COLOR
    float brightness = dot(result, vec3(0.2126, 0.7152, 0.0722));
    if(brightness > 1.0) {
        BrightColor = vec4(result+0.3, alpha);
    }
    else {
        BrightColor = vec4(0.0, 0.0, 0.0, alpha);
    }

    if (emission.r > 0.0)
    {
        BrightColor = vec4(EMISSION * emission, 1.0);
    }

    if (EMISSIONUSINGALBEDOINTESITY > 0.0) {
        BrightColor = vec4(EMISSIONUSINGALBEDOINTESITY * result * emission, 1.0);
    }

    FragColor = vec4(result, alpha);
}

vec3 lerp(vec3 _min, vec3 _max, float _fraction)
{
    return _min + _fraction * (_max - _min);
}

float ShadowCalculation(vec4 fragPosLightSpace)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;

    // keep the shadow at 0.0 when outside the far_plane region of the light's frustum.
    if(projCoords.z > 1.0)
        return 0.0;
    
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // calculate bias (based on depth map resolution and slope)
    vec3 normal = fs_in.Normal;
    vec3 lightDir = normalize(lightPos - fs_in.FragPos);
    float bias = max(0.0005 * (1.0 - dot(normal, lightDir)), 0.002);
    //float bias = 0.001;
    // check whether current frag pos is in shadow
    //return currentDepth - 0.0000000001 > closestDepth  ? 1.0 : 0.0;
    // PCF
    float shadow = 0.0;
    float pcfDepth = 0.0;
    /*for(int x = -1; x <= 1; ++x)
    {
        for(int y = -1; y <= 1; ++y)
        {
            pcfDepth = texture(shadowMap, projCoords.xy + vec2(x, y) * TEXELSHADOWSIZE).r; 
            shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;        
        }    
    }*/

    pcfDepth = texture(shadowMap, projCoords.xy + vec2(-1, -1) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0; 
    pcfDepth = texture(shadowMap, projCoords.xy + vec2(-1, 0) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0; 
    pcfDepth = texture(shadowMap, projCoords.xy + vec2(-1, 1) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0; 

    pcfDepth = texture(shadowMap, projCoords.xy + vec2(0, -1) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0;
    pcfDepth = texture(shadowMap, projCoords.xy + vec2(0, 1) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0; 

    pcfDepth = texture(shadowMap, projCoords.xy + vec2(1, -1) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0; 
    pcfDepth = texture(shadowMap, projCoords.xy + vec2(1, 0) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0; 
    pcfDepth = texture(shadowMap, projCoords.xy + vec2(1, 1) * TEXELSHADOWSIZE).r; 
    shadow += currentDepth - bias > pcfDepth  ? 1.0 : 0.0; 
        
    return shadow/9.0;
}

// calculates the COLOR when using a directional light.
vec3 CalcDirLight(DirLight light, vec3 normal, vec3 viewDir)
{
    // diffuse shading
    float diff = max(dot(normal, -light.direction), 0.0);
    // specular shading
    vec3 reflectDir = reflect(light.direction, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    // combine results
    vec3 baseColor = (vec3(COLOR) * vec3(texture(material.diffuse, fs_in.TexCoords)));
    vec3 ambient = light.ambient * baseColor;
    vec3 diffuse = light.diffuse * diff * baseColor;
    vec3 specular = light.specular * spec * vec3(texture(material.specular, fs_in.TexCoords));

    float shadow = ShadowCalculation(fs_in.FragPosLightSpace) - length(emission);
    return (ambient + (1.0 - max(0.0, shadow*0.75)) * (diffuse + specular));
}

// calculates the color when using a point light.
vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);

    // specular shading
    //vec3 reflectDir = reflect(-lightDir, normal);
    //float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

    // attenuation
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));

    if (attenuation <= 0.0001)
        return vec3(0.0, 0.0, 0.0);
    
    // specular shading blinn-phong
    vec3 halfwayDir = normalize(lightDir + viewDir);  
    float spec = pow(max(dot(normal, halfwayDir), 0.0), material.shininess);

    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);

    // combine results
    vec3 ambient = light.ambient * (vec3(COLOR) * vec3(texture(material.diffuse, fs_in.TexCoords)));
    vec3 diffuse = light.diffuse * diff * (vec3(COLOR) * vec3(texture(material.diffuse, fs_in.TexCoords)));
    vec3 specular = light.specular * spec * vec3(texture(material.specular, fs_in.TexCoords));

    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}