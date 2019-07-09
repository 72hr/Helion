﻿using Helion.Configuration;
using Helion.Input;
using Helion.Input.Adapter;
using Helion.Maps;
using Helion.Projects.Impl.Local;
using Helion.Render.OpenGL;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Time;
using Helion.Window;
using Helion.World.Impl.SinglePlayer;
using NLog;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System.Linq;

namespace Helion.Client
{
    public class Client : GameWindow
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly CommandLineArgs commandLineArgs;
        private readonly Config config;
        private readonly Console console = new Console();
        private readonly InputManager inputManager = new InputManager();
        private readonly OpenTKInputAdapter inputAdapter = new OpenTKInputAdapter();
        private readonly InputCollection frameCollection;
        private readonly InputCollection tickCollection;
        private readonly LocalProject project = new LocalProject();
        private readonly Ticker ticker = new Ticker(Constants.TicksPerSecond);
        private bool shouldExit = false;
        private GLRenderer renderer;
        private SinglePlayerWorld? world;

        public Client(CommandLineArgs args, Config configuration) : 
            base(configuration.Engine.Window.Width, configuration.Engine.Window.Height, 
                 CreateGraphicsMode(configuration), Constants.ApplicationName, GameWindowFlags.Default)
        {
            commandLineArgs = args;
            config = configuration;
            frameCollection = inputManager.RegisterCollection();
            tickCollection = inputManager.RegisterCollection();
            inputAdapter.InputEventEmitter += inputManager.HandleInputEvent;

            SetWindowProperties();
            LoadProject();

            GLInfo glInfo = new GLInfo();
            renderer = new GLRenderer(glInfo, config, project.Resources);
            PrintGLInfo(glInfo);

            // TODO: Very temporary!
            int levelNumber = (commandLineArgs.Warp != 0 ? commandLineArgs.Warp : 1);
            LoadMap("MAP" + levelNumber.ToString().PadLeft(2, '0'));
        }

        private void SetWindowProperties()
        {
            // TODO: Should register for updates for these!
            
            VSync = config.Engine.Window.VSync.Get().ToOpenTKVSync();
            WindowState = config.Engine.Window.State.Get().ToOpenTKWindowState();
            CursorVisible = false; // TODO: Should be configurable.
        }

        private void LoadMap(string mapName)
        {
            (Map? map, MapEntryCollection? MapEntryCollection) = project.GetMap(mapName);
            if (map != null)
            {
                log.Info($"LoadMap {mapName}");

                System.DateTime dtStart = System.DateTime.Now;

                renderer.ClearWorld();
                world = SinglePlayerWorld.Create(project, map, MapEntryCollection);

                log.Info($"Map Load Time: {System.DateTime.Now.Subtract(dtStart).TotalMilliseconds}");

                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();

                ticker.Start();
            }
        }
        
        private static GraphicsMode CreateGraphicsMode(Config config)
        {
            ColorFormat colorFormat = new ColorFormat(32);
            
            if (config.Engine.Render.Multisample.Enable)
                return new GraphicsMode(colorFormat, 24, 8, config.Engine.Render.Multisample.Value);
            return new GraphicsMode(colorFormat, 24, 8, 0);
        }

        private void LoadProject()
        {
            System.DateTime dtStart = System.DateTime.Now;
            if (!project.Load(commandLineArgs.Files))
            {
                log.Error("Unable to load files for the client");
                shouldExit = true;
            }
            log.Info($"Project Load Time: {System.DateTime.Now.Subtract(dtStart).TotalMilliseconds}");
        }

        private void PrintGLInfo(GLInfo glInfo)
        {
            log.Info("Loaded OpenGL v{0}", glInfo.Version);
            log.Info("OpenGL Shading Language: {0}", glInfo.ShadingVersion);
            log.Info("Vendor: {0}", glInfo.Vendor);
            log.Info("Hardware: {0}", glInfo.Renderer);
        }

        private void CheckForExit()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Key.AltLeft) && keyboardState.IsKeyDown(Key.F4))
                shouldExit = true;

            // We may not be the only place who sets `shouldExit = true`, so we
            // need to keep them separated.
            if (shouldExit)
                Exit();
        }

        private void PollInput()
        {
            MouseState state = Mouse.GetCursorState();
            Vec2I center = new Vec2I(Width / 2, Height / 2);
            Vec2I deltaPixels = new Vec2I(state.X, state.Y) - center;

            inputAdapter.HandleMouseMovement(deltaPixels);
        }

        private void RunLogic(TickerInfo tickerInfo)
        {
            ConsumableInput consumableTickInput = new ConsumableInput(tickCollection);
            tickCollection.Tick();

            if (world != null)
            {
                int ticksToRun = tickerInfo.Ticks;
                while (ticksToRun > 0)
                {
                    world.HandleTickInput(consumableTickInput);
                    world.Tick();
                    ticksToRun--;
                }
            }

            CheckForExit();
        }

        private void Render(TickerInfo tickerInfo)
        {
            ConsumableInput consumableFrameInput = new ConsumableInput(frameCollection);
            frameCollection.Tick();

            renderer.RenderStart(ClientRectangle);
            renderer.Clear(new System.Drawing.Size(Width, Height));

            if (world != null)
            {
                RenderInfo renderInfo = new RenderInfo(world.Camera, tickerInfo.Fraction, ClientRectangle);
                world.HandleFrameInput(consumableFrameInput);
                renderer.RenderWorld(world, renderInfo);
            }

            SwapBuffers();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleKeyDown(e);

            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleKeyPress(e);

            base.OnKeyPress(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleKeyUp(e);

            base.OnKeyDown(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (Focused)
            {
                PollInput();

                // Reset the mouse to the center of the screen. Unfortunately
                // we have to do this ourselves...
                Vec2I center = new Vec2I(Width / 2, Height / 2);
                Mouse.SetPosition(X + center.X, Y + center.Y);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (Focused)
                inputAdapter.HandleMouseWheelInput(e);

            base.OnMouseWheel(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            TickerInfo tickerInfo = ticker.GetTickerInfo();
            RunLogic(tickerInfo);
            Render(tickerInfo);

            base.OnRenderFrame(e);
        }

        protected override void OnUnload(System.EventArgs e)
        {
            // Do this here instead of OnClosing() because this is handled
            // before the OpenGL context is destroyed. This way we clean up
            // our side of the renderer first.
            inputAdapter.InputEventEmitter -= inputManager.HandleInputEvent;
            renderer.Dispose();
            console.Dispose();

            base.OnUnload(e);
        }

        public static void Main(string[] args)
        {
            CommandLineArgs cmdArgs = CommandLineArgs.Parse(args);

            Logging.Initialize(cmdArgs);
            log.Info("=========================================");
            log.Info($"{Constants.ApplicationName} v{Constants.ApplicationVersion}");
            log.Info("=========================================");
            
            if (cmdArgs.ErrorWhileParsing)
                log.Error("Bad command line arguments, unexpected results may follow");
            
            using (Config config = new Config())
            {
                using Client client = new Client(cmdArgs, config);
                client.Run();
            }

            LogManager.Shutdown();
        }
    }
}
