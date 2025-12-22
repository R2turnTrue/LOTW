#version 330 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D frostTexture;

void main()
{
    vec4 texColor = texture(frostTexture, TexCoord);
    FragColor = vec4(1.0, 1.0, 1.0, texColor.r * 0.75);
}