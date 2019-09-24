using System.Numerics;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Subsectors;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities
{
    public class EntityRenderer
    {
        /// <summary>
        /// The rotation angle in diamond angle format. This is equal to 180
        /// degrees + 22.5 degrees. See <see cref="CalculateRotation"/> docs
        /// for more information.
        /// </summary>
        private const uint SpriteFrameRotationAngle = 9 * (uint.MaxValue / 16);

        private readonly LegacyGLTextureManager m_textureManager;
        private readonly RenderWorldDataManager m_worldDataManager;
        private readonly EntityDrawnTracker m_EntityDrawnTracker = new EntityDrawnTracker();
        private double m_tickFraction;
        private Entity? m_cameraEntity;

        public EntityRenderer(LegacyGLTextureManager textureManager, RenderWorldDataManager worldDataManager)
        {
            m_textureManager = textureManager;
            m_worldDataManager = worldDataManager;
        }

        public void UpdateTo(WorldBase world)
        {
            PreloadAllTextures(world);
        }

        public void Clear(WorldBase world, double tickFraction, Entity cameraEntity)
        {
            m_tickFraction = tickFraction;
            m_cameraEntity = cameraEntity;
            m_EntityDrawnTracker.Reset(world);
        }

        public void RenderSubsector(Subsector subsector, in Vec2D position, in Vec2D viewDirection)
        {
            foreach (Entity entity in subsector.Entities)
            {
                if (ShouldNotDraw(entity))
                    continue;

                RenderEntity(entity, position, viewDirection);
                m_EntityDrawnTracker.MarkDrawn(entity);
            }
        }

        private static uint CalculateRotation(uint viewAngle, uint entityAngle)
        {
            // This works as follows:
            //
            // First we find the angle that we have to the entity. Since
            // facing along with the actor (ex: looking at their back) wants to
            // give us the opposite rotation side, we add 180 degrees to our
            // angle delta.
            //
            // Then we add 22.5 degrees to that as well because we don't want
            // a transition when we hit 180 degrees... we'd rather have ranges
            // of [180 - 22.5, 180 + 22.5] be the angle rather than the range
            // [180 - 45, 180].
            //
            // Then we can do a bit shift trick which converts the higher order
            // three bits into the angle rotation between 0 - 7.
            return unchecked((viewAngle - entityAngle + SpriteFrameRotationAngle) >> 29);
        }
      
        private static short CalculateLightLevel(Entity entity, short sectorLightLevel)
        {
            if (entity.Flags.Bright || entity.Frame.Properties.Bright)
                return 255;
            return sectorLightLevel;
        }
        
        private void PreloadAllTextures(WorldBase world)
        {
            // TODO
        }

        private bool ShouldNotDraw(Entity entity)
        {
            return m_EntityDrawnTracker.HasDrawn(entity) || 
                   ReferenceEquals(m_cameraEntity, entity) ||
                   entity.Frame.Sprite == Constants.InvisibleSprite;
        }

        private void AddSpriteQuad(in Vec2D viewDirection, in Vec3D entityCenterBottom, Entity entity, 
            GLLegacyTexture texture, bool mirror)
        {
            // We need to find the perpendicular vector from the entity so we
            // know where to place the quad vertices.
            Vector2 rightNormal = viewDirection.OriginRightRotate90().Unit().ToFloat();

            Vector2 entityCenterXY = entityCenterBottom.To2D().ToFloat();
            Vector2 halfWidth = rightNormal * texture.Dimension.Width / 2;
            Vector2 left = entityCenterXY - halfWidth;
            Vector2 right = entityCenterXY + halfWidth;

            float bottomZ = (float)entityCenterBottom.Z;
            float topZ = bottomZ + texture.Height;
            float leftU = mirror ? 1.0f : 0.0f;
            float rightU = mirror ? 0.0f : 1.0f;
            short lightLevel = CalculateLightLevel(entity, entity.Sector.LightLevel);

            LegacyVertex topLeft = new LegacyVertex(left.X, left.Y, topZ, leftU, 0.0f, lightLevel);
            LegacyVertex topRight = new LegacyVertex(right.X, right.Y, topZ, rightU, 0.0f, lightLevel);
            LegacyVertex bottomLeft = new LegacyVertex(left.X, left.Y, bottomZ, leftU, 1.0f, lightLevel);
            LegacyVertex bottomRight = new LegacyVertex(right.X, right.Y, bottomZ, rightU, 1.0f, lightLevel);
                
            RenderWorldData renderWorldData = m_worldDataManager[texture];
            renderWorldData.Vbo.Add(topLeft);
            renderWorldData.Vbo.Add(bottomLeft);
            renderWorldData.Vbo.Add(topRight);
            renderWorldData.Vbo.Add(topRight);
            renderWorldData.Vbo.Add(bottomLeft);
            renderWorldData.Vbo.Add(bottomRight);
        }

        private void RenderEntity(Entity entity, in Vec2D position, in Vec2D viewDirection)
        {
            Vec3D centerBottom = entity.PrevPosition.Interpolate(entity.Position, m_tickFraction);
            Vec2D entityPos = centerBottom.To2D();

            var spriteDef = m_textureManager.GetSpriteDefinition(entity.Frame.Sprite);
            uint rotation;

            if (spriteDef != null && spriteDef.HasRotations)
            {
                uint viewAngle = ViewClipper.ToDiamondAngle(position, entityPos);
                uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
                rotation = CalculateRotation(viewAngle, entityAngle);
            }
            else
            {
                rotation = 0;
            }

            SpriteRotation spriteRotation;
            if (spriteDef != null)
                spriteRotation = m_textureManager.GetSpriteRotation(spriteDef, entity.Frame.Frame, rotation);
            else
                spriteRotation = m_textureManager.NullSpriteRotation;
            GLLegacyTexture texture = spriteRotation.Texture.RenderStore == null ? m_textureManager.NullTexture : (GLLegacyTexture)spriteRotation.Texture.RenderStore;

            AddSpriteQuad(viewDirection, centerBottom, entity, texture, spriteRotation.Mirror);
        }
    }
}