using System.Collections.Generic;
using System.Linq;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Players;
using Helion.World.Entities.Spawn;
using Helion.World.Geometry.Sectors;
using NLog;

namespace Helion.World.Entities
{
    public class EntityManager
    {
        public const int NoTid = 0;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();
        public readonly SpawnLocations SpawnLocations = new SpawnLocations();
        private readonly WorldBase m_world;
        private readonly EntityDefinitionComposer m_definitionComposer;
        private readonly AvailableIndexTracker m_entityIdTracker = new AvailableIndexTracker();
        private readonly Dictionary<int, ISet<Entity>> TidToEntity = new Dictionary<int, ISet<Entity>>();
        private readonly SkillLevel m_skill;

        public EntityManager(WorldBase world, ArchiveCollection archiveCollection, SkillLevel skill)
        {
            m_world = world;
            m_definitionComposer = new EntityDefinitionComposer(archiveCollection);
            m_skill = skill;
        }
        
        public IEnumerable<Entity> FindByTid(int tid)
        {
            return TidToEntity.TryGetValue(tid, out ISet<Entity> entities) ? entities : Enumerable.Empty<Entity>();
        }

        public Entity Create(EntityDefinition definition, Vec3D position, double angle, int tid)
        {
            int id = m_entityIdTracker.Next();
            Sector sector = m_world.BspTree.ToSector(position);
            Entity entity = new Entity(id, tid, definition, position, angle, sector, this);

            FinishCreatingEntity(entity);

            return entity;
        }

        private void FinishCreatingEntity(Entity entity)
        {
            LinkableNode<Entity> node = Entities.Add(entity);
            entity.EntityListNode = node;
            
            m_world.Link(entity);

            entity.ResetInterpolation();
        }

        public void Destroy(Entity entity)
        {
            // TODO: Remove from spawns if it is one.
            
            // To avoid more object allocation and deallocation, I'm going to
            // leave empty sets in the map in case they get populated again.
            // Most maps wouldn't even approach a number that high for us to
            // worry about. If it ever becomes an issue, then we can add a line
            // of code that removes empty sets here as well.
            if (TidToEntity.TryGetValue(entity.ThingId, out ISet<Entity> entities))
                entities.Remove(entity);

            entity.Dispose();
        }

        public Player CreatePlayer(int playerIndex)
        {
            EntityDefinition? playerDefinition = m_definitionComposer[Constants.PlayerClass];
            if (playerDefinition == null)
            {
                Log.Error("Missing player definition class {0}, cannot create player {1}", Constants.PlayerClass, playerIndex);
                throw new HelionException("Missing the default player class, should never happen");
            }

            Entity? spawnSpot = SpawnLocations.GetPlayerSpawn(playerIndex);
            if (spawnSpot == null)
            {
                Log.Warn("No player {0} spawns found, creating player at origin", playerIndex);
                return CreatePlayerEntity(playerIndex, playerDefinition, Vec3D.Zero, 0.0);
            }

            return CreatePlayerEntity(playerIndex, playerDefinition, spawnSpot.Position, spawnSpot.AngleRadians);
        }
        
        public void PopulateFrom(IMap map)
        {
            foreach (IThing mapThing in map.GetThings())
            {
                if (!ShouldSpawn(mapThing, m_skill))
                    continue;
                
                EntityDefinition? definition = m_definitionComposer[mapThing.EditorNumber];
                if (definition == null)
                {
                    Log.Warn("Cannot find entity by editor number {0} at {1}", mapThing.EditorNumber, mapThing.Position.To2D());
                    continue;
                }

                double angleRadians = MathHelper.ToRadians(mapThing.Angle);
                Entity entity = Create(definition, mapThing.Position.ToDouble(), angleRadians, mapThing.ThingId);

                PostProcessEntity(entity);
            }
        }
        
        private static bool ShouldSpawn(IThing mapThing, SkillLevel skill)
        {
            // TODO: These should be offloaded into SinglePlayerWorld...
            if (!mapThing.Flags.SinglePlayer)
                return false;
            
            switch (skill)
            {
            case SkillLevel.VeryEasy:
            case SkillLevel.Easy:
                return mapThing.Flags.Easy;
            case SkillLevel.Medium:
                return mapThing.Flags.Medium;
            case SkillLevel.Hard:
            case SkillLevel.Nightmare:
                return mapThing.Flags.Hard;
            default:
                return false;
            }
        }

        private void PostProcessEntity(Entity entity)
        {
            SpawnLocations.AddPossibleSpawnLocation(entity);

            if (entity.ThingId != NoTid)
            {
                if (TidToEntity.TryGetValue(entity.ThingId, out ISet<Entity> entities))
                    entities.Add(entity);
                else
                    TidToEntity.Add(entity.ThingId, new HashSet<Entity> { entity });
            }
        }
        
        private Player CreatePlayerEntity(int playerNumber, EntityDefinition definition, Vec3D position, double angle)
        {
            int id = m_entityIdTracker.Next();
            Sector sector = m_world.BspTree.ToSector(position);
            Player player = new Player(id, 0, definition, position, angle, sector, this, playerNumber);
            
            FinishCreatingEntity(player);
            
            return player;
        }
    }
}