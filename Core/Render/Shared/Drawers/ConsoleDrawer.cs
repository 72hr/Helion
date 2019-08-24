using Helion.Render.Commands;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Render.Shared.Drawers
{
    /// <summary>
    /// Performs console drawing by issuing rendering commands.
    /// </summary>
    public static class ConsoleDrawer
    {
        public static void Draw(HelionConsole console, Dimension viewport, RenderCommands renderCommands)
        {
            renderCommands.ClearDepth();
            
            // TODO
            renderCommands.DrawImage("TITLEPIC", 0, 0, viewport.Width, viewport.Height);
        }
    }
}