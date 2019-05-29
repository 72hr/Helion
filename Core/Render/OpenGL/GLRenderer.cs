﻿using Helion.Render.OpenGL.Shared;
using NLog;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL
{
    public abstract class GLRenderer : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        protected readonly GLInfo info;

        public GLRenderer(GLInfo glInfo)
        {
            info = glInfo;

            SetGLStates();
            SetGLDebugger();
        }

        private void SetGLStates()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);

            if (info.Version.Supports(3, 2))
                GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        private void SetGLDebugger()
        {
            if (info.Version.Supports(4, 3))
            {
                GL.Enable(EnableCap.DebugOutput);
                GL.DebugMessageCallback((DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) =>
                {
                    string msg = Marshal.PtrToStringAnsi(message, length);

                    switch (severity)
                    {
                    case DebugSeverity.DebugSeverityHigh:
                    case DebugSeverity.DebugSeverityMedium:
                        log.Error("[GLDebug type={0}] {1}", type, msg);
                        break;
                    case DebugSeverity.DebugSeverityLow:
                        log.Warn("[GLDebug type={0}] {1}", type, msg);
                        break;
                    default:
                        break;
                    }
                }, IntPtr.Zero);
            }
        }

        public void Clear(Size windowDimension)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Viewport(windowDimension);
        }

        public abstract void Render();

        public abstract void Dispose();
    }
}
