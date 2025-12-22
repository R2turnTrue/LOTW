#version 330 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D spriteTexture;
uniform vec4 tintColor;

uniform vec4 spriteTexture_ST;

void main()
{
    vec2 scaledTexCoord = TexCoord * spriteTexture_ST.xy + spriteTexture_ST.zw;
    vec4 texColor = texture(spriteTexture, vec2(scaledTexCoord.x, scaledTexCoord.y));
    FragColor = texColor * tintColor;
}