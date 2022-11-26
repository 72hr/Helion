using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Renderers.Legacy.World.Automap;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Entities;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Configs;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics.Blockmap;
using SixLabors.Primitives;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class LegacyWorldRenderer : WorldRenderer
{
    public static readonly VertexArrayAttributes Attributes = new(
        new VertexPointerFloatAttribute("pos", 0, 3),
        new VertexPointerFloatAttribute("uv", 1, 2),
        new VertexPointerFloatAttribute("lightLevel", 2, 1),
        new VertexPointerFloatAttribute("alpha", 3, 1),
        new VertexPointerFloatAttribute("colorMul", 4, 3),
        new VertexPointerFloatAttribute("fuzz", 5, 1));

    private readonly IConfig m_config;
    private readonly IGLFunctions gl;
    private readonly GeometryRenderer m_geometryRenderer;
    private readonly EntityRenderer m_entityRenderer;
    private readonly LegacyShader m_shaderProgram;
    private readonly RenderWorldDataManager m_worldDataManager;
    private readonly LegacyAutomapRenderer m_automapRenderer;
    private readonly ViewClipper m_viewClipper;
    private readonly List<IRenderObject> m_alphaEntities = new();
    private int m_renderCount;
    private Sector m_viewSector;
    private Vec2D m_occludeViewPos;
    private bool m_occlude;

    public LegacyWorldRenderer(IConfig config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
        IGLFunctions functions, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        gl = functions;
        m_automapRenderer = new LegacyAutomapRenderer(capabilities, gl, archiveCollection);
        m_worldDataManager = new RenderWorldDataManager(capabilities, gl, archiveCollection.DataCache);
        m_entityRenderer = new EntityRenderer(config, textureManager, m_worldDataManager);
        m_viewClipper = new(archiveCollection.DataCache);
        m_viewSector = Sector.CreateDefault();

        using (ShaderBuilder shaderBuilder = LegacyShader.MakeBuilder(functions))
            m_shaderProgram = new LegacyShader(functions, shaderBuilder, Attributes);

        m_geometryRenderer = new GeometryRenderer(config, archiveCollection, capabilities, functions,
            textureManager, m_viewClipper, m_worldDataManager, Attributes);
    }

    ~LegacyWorldRenderer()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public override void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected override void UpdateToNewWorld(IWorld world)
    {
        m_geometryRenderer.UpdateTo(world);
        m_entityRenderer.UpdateTo(world);
    }

    protected override void PerformAutomapRender(IWorld world, RenderInfo renderInfo)
    {
        Clear(world, renderInfo);
        TraverseBsp(world, renderInfo);

        m_automapRenderer.Render(world, renderInfo);
    }

    private void IterateBlockmap(IWorld world, RenderInfo renderInfo)
    {
        Vec2D viewPos = renderInfo.Camera.Position.XY.Double;
        Vec3D position3D = renderInfo.Camera.Position.Double;
        Vec2D viewDirection = renderInfo.Camera.Direction.XY.Double;
        m_viewSector = world.BspTree.ToSector(position3D);

        m_geometryRenderer.Clear(renderInfo.TickFraction);
        m_entityRenderer.SetViewDirection(viewDirection);
        m_renderCount++;

        int maxDistance = world.Config.Render.MaxDistance;
        if (maxDistance <= 0)
            maxDistance = 6000;
        int maxScrollDistance = world.Config.Render.MaxScrollDistance;
        if (maxScrollDistance <= 0)
            maxDistance = 512;

        Vec2D? occludePos = m_occlude ? m_occludeViewPos : null;
        Box2D box = new(viewPos, maxDistance);
        world.BlockmapTraverser.RenderTraverse(box, viewPos, occludePos, viewDirection, maxDistance, maxScrollDistance,
            RenderEntity, RenderSector, RenderScrollingSector);

        void RenderEntity(Entity entity)
        {
            if (m_entityRenderer.ShouldNotDraw(entity))
                return;

            // Not in front 180 FOV
            if (occludePos.HasValue)
            {
                Vec2D entityToTarget = entity.Position.XY - occludePos.Value;
                if (entityToTarget.Dot(viewDirection) < 0)
                    return;
            }

            double dx = Math.Max(entity.Position.X - viewPos.X, Math.Max(0, viewPos.X - entity.Position.X));
            double dy = Math.Max(entity.Position.Y - viewPos.Y, Math.Max(0, viewPos.Y - entity.Position.Y));
            if (dx * dx + dy * dy > maxDistance * maxDistance)
                return;

            if (entity.Definition.Properties.Alpha < 1)
            {
                entity.RenderDistance = entity.Position.XY.Distance(viewPos);
                m_alphaEntities.Add(entity);
                return;
            }

            m_entityRenderer.RenderEntity(m_viewSector, entity, position3D);
        }

        void RenderSector(Sector sector)
        {
            if (sector.RenderCount == m_renderCount)
                return;

            m_geometryRenderer.RenderSector(m_viewSector, sector, position3D);
            sector.RenderCount = m_renderCount;
        }

        void RenderScrollingSector(Sector sector)
        {
            if (sector.RenderCount == m_renderCount)
                return;

            m_geometryRenderer.RenderSector(m_viewSector, sector, position3D);
            sector.RenderCount = m_renderCount;
        }

        RenderAlphaObjects(viewPos, position3D, m_alphaEntities);
        m_alphaEntities.Clear();
    }

    protected override void PerformRender(IWorld world, RenderInfo renderInfo)
    {
        Clear(world, renderInfo);

        SetPosition(renderInfo);
        if (m_config.Render.Blockmap)
            IterateBlockmap(world, renderInfo);
        else
            TraverseBsp(world, renderInfo);

        m_shaderProgram.Bind();
        gl.ActiveTexture(TextureUnitType.Zero);
        SetUniforms(renderInfo);
        m_worldDataManager.DrawNonAlpha();
        m_geometryRenderer.RenderStaticGeometry();
        m_shaderProgram.Unbind();

        // Does shader bindings, which has to come outside of the above shader bindings
        // to avoid clobbering GL state.
        m_geometryRenderer.Render(renderInfo);

        if (m_config.Render.TextureTransparency)
        {
            m_shaderProgram.Bind();
            m_worldDataManager.DrawAlpha();
            m_shaderProgram.Unbind();
        }
    }

    private void SetPosition(RenderInfo renderInfo)
    {
        // This is a hack until frustum culling exists.
        // Push the position back to stop occluding things that are straight up/down
        if (Math.Abs(renderInfo.Camera.PitchRadians) > MathHelper.QuarterPi)
        {
            m_occlude = false;
            return;
        }

        m_occlude = true;
        Vec2D unit = Vec2D.UnitCircle(renderInfo.ViewerEntity.AngleRadians + MathHelper.Pi);
        m_occludeViewPos = renderInfo.Camera.Position.XY.Double + (unit * 32);
    }

    private void Clear(IWorld world, RenderInfo renderInfo)
    {
        m_viewClipper.Clear();
        m_worldDataManager.Clear();

        m_geometryRenderer.Clear(renderInfo.TickFraction);
        m_entityRenderer.Clear(world, renderInfo.TickFraction, renderInfo.ViewerEntity);       
    }

    private void TraverseBsp(IWorld world, RenderInfo renderInfo)
    {
        Vec2D position = renderInfo.Camera.Position.XY.Double;
        Vec3D position3D = renderInfo.Camera.Position.Double;
        Vec2D viewDirection = renderInfo.Camera.Direction.XY.Double;
        m_viewSector = world.BspTree.ToSector(position3D);

        m_entityRenderer.SetViewDirection(viewDirection);
        m_viewClipper.Center = position;
        m_renderCount++;
        RecursivelyRenderBsp((uint)world.BspTree.Nodes.Length - 1, position3D, viewDirection, world);
        RenderAlphaObjects(position, position3D, m_entityRenderer.AlphaEntities);
    }

    private void RenderAlphaObjects(Vec2D position, Vec3D position3D, List<IRenderObject> alphaEntities)
    {
        // This will just render based on distance from their center point.
        // Not really correct, but mostly correct enough for now.
        List<IRenderObject> alphaObjects = alphaEntities;
        alphaObjects.AddRange(m_geometryRenderer.AlphaSides);
        alphaObjects.Sort((i1, i2) => i2.RenderDistance.CompareTo(i1.RenderDistance));
        for (int i = 0; i < alphaObjects.Count; i++)
        {
            IRenderObject renderObject = alphaObjects[i];
            if (renderObject.Type == RenderObjectType.Entity)
            {
                Entity entity = (Entity)renderObject;
                m_entityRenderer.RenderEntity(m_viewSector, entity, position3D);
            }
            else if (renderObject.Type == RenderObjectType.Side)
            {
                Side side = (Side)renderObject;
                m_geometryRenderer.RenderAlphaSide(side, side.Line.Segment.OnRight(position));
            }
        }
    }

    private bool Occluded(in Box2D box, in Vec2D position, in Vec2D viewDirection)
    {
        if (box.Contains(position))
            return false;

        if (m_config.Render.MaxDistance > 0)
        {
            int max = m_config.Render.MaxDistance;
            double dx = Math.Max(box.Min.X - position.X, Math.Max(0, position.X - box.Max.X));
            double dy = Math.Max(box.Min.Y - position.Y, Math.Max(0, position.Y - box.Max.Y));
            if (dx * dx + dy * dy > max * max)
                return true;
        }

        if (m_occlude && !box.InView(m_occludeViewPos, viewDirection))
            return true;

        (Vec2D first, Vec2D second) = box.GetSpanningEdge(position);
        return m_viewClipper.InsideAnyRange(first, second);
    }

    private unsafe void RecursivelyRenderBsp(uint nodeIndex, in Vec3D position, in Vec2D viewDirection, IWorld world)
    {
        Vec2D pos2D = position.XY;
        while ((nodeIndex & BspNodeCompact.IsSubsectorBit) == 0)
        {
            fixed (BspNodeCompact* node = &world.BspTree.Nodes[nodeIndex])
            {
                if (Occluded(node->BoundingBox, pos2D, viewDirection))
                    return;

                int front = Convert.ToInt32(node->Splitter.PerpDot(pos2D) < 0);
                int back = front ^ 1;

                RecursivelyRenderBsp(node->Children[front], position, viewDirection, world);
                nodeIndex = node->Children[back];
            }
        }

        Subsector subsector = world.BspTree.Subsectors[nodeIndex & BspNodeCompact.SubsectorMask];
        if (Occluded(subsector.BoundingBox, pos2D, viewDirection))
            return;

        bool hasRenderedSector = subsector.Sector.RenderCount == m_renderCount;
        m_geometryRenderer.RenderSubsector(m_viewSector, subsector, position, hasRenderedSector);

        // Entities are rendered by the sector
        if (hasRenderedSector)
            return;
        subsector.Sector.RenderCount = m_renderCount;
        m_entityRenderer.RenderSubsector(m_viewSector, subsector, position);
    }

    private void SetUniforms(RenderInfo renderInfo)
    {
        int drawInvulnerability = 0;

        // We divide by 4 to make it so the noise changes every four ticks.
        // We then mod by 8 so that the number stays small (or else when it
        // is multiplied in the shader it will overflow very quickly if we
        // don't do this). This could be any number, I just arbitrarily
        // chose 8. This means there are 8 different versions that are to
        // be rendered if the person stares at an unmoving body long enough.
        // Then we add 1 because if the value is 0, then the noise formula
        // outputs zero uniformly which makes it look invisible.
        const int ticksPerFrame = 4;
        const int differentFrames = 8;
        float timeFrac = ((renderInfo.ViewerEntity.World.Gametick / ticksPerFrame) % differentFrames) + 1;
        int extraLight = 0;
        float mix = 0.0f;

        if (renderInfo.ViewerEntity.PlayerObj != null)
        {
            if (renderInfo.ViewerEntity.PlayerObj.DrawFullBright())
                mix = 1.0f;
            if (renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap())
                drawInvulnerability = 1;

            extraLight = renderInfo.ViewerEntity.PlayerObj.GetExtraLightRender();
        }

        m_shaderProgram.BoundTexture.Set(gl, 0);
        m_shaderProgram.HasInvulnerability.Set(gl, drawInvulnerability);
        m_shaderProgram.LightDropoff.Set(gl, m_config.Render.LightDropoff ? 1 : 0);
        m_shaderProgram.Mvp.Set(gl, GLRenderer.CalculateMvpMatrix(renderInfo));
        m_shaderProgram.MvpNoPitch.Set(gl, GLRenderer.CalculateMvpMatrix(renderInfo, true));
        m_shaderProgram.TimeFrac.Set(gl, timeFrac);
        m_shaderProgram.LightLevelMix.Set(gl, mix);
        m_shaderProgram.ExtraLight.Set(gl, extraLight);
    }

    private void ReleaseUnmanagedResources()
    {
        m_shaderProgram.Dispose();
        m_geometryRenderer.Dispose();
        m_worldDataManager.Dispose();
        m_automapRenderer.Dispose();
    }
}
