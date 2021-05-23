﻿using Helion.Render.OpenGL.Shaders;
using Helion.Render.OpenGL.Shaders.Uniforms;

namespace Helion.Render.OpenGL.Modern.Renderers.Hud.Textures
{
    public class HudTextureShader : ShaderProgram
    {
        public readonly UniformMatrix4 Mvp = new();

        protected override string VertexShader()
        {
            return @"
                #version 440 core

                #extension GL_NV_gpu_shader5 : require

                layout(location = 0) in vec3 pos;
                layout(location = 1) in vec2 uv;
                layout(location = 2) in vec4 rgba;
                layout(location = 3) in float alpha;
                layout(location = 4) in uvec2 textureHandle;

                out vec2 uvFrag;
                out flat vec3 rgbFrag;
                out flat float alphaFrag;
                out flat uvec2 textureHandleFrag;

                uniform mat4 mvp;

                void main() {
                    uvFrag = uv;
                    rgbFrag = rgba.xyz;
                    alphaFrag = alpha;
                    textureHandleFrag = textureHandle;

                    gl_Position = mvp * vec4(pos.xyz, 1);
                }
            ";
        }

        protected override string FragmentShader()
        {
            return @"
                #version 440 core

                #extension GL_ARB_bindless_texture : require
                #extension GL_ARB_shader_image_load_store : require
                #extension GL_ARB_shader_storage_buffer_object : require
                #extension GL_NV_gpu_shader5 : require

                in vec2 uvFrag;
                in flat vec3 rgbFrag;
                in flat float alphaFrag;
                in flat uvec2 textureHandleFrag;

                out vec4 FragColor;

                void main() {
                    uint64_t handle = packUint2x32(textureHandleFrag);
                    sampler2D s = sampler2D(handle);

                    FragColor = texture(s, uvFrag);
                    FragColor *= vec4(rgbFrag, 1);
                    FragColor.w *= alphaFrag;
                }
            ";
        }
    }
}
