﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Helion.Audio;
using Helion.Maps;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.Util.RandomGenerators;
using Helion.Util.Time;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Sound;
using Helion.World.Special;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.World
{
    public abstract class WorldBase : IWorld
    {
        public event EventHandler<LevelChangeEvent>? LevelExit;

        public readonly long CreationTimeNanos;
        public readonly CIString MapName;
        public readonly BlockMap Blockmap;
        public readonly PhysicsManager PhysicsManager;
        public readonly SoundManager SoundManager;
        public WorldState WorldState { get; protected set; } = WorldState.Normal;
        public int Gametick { get; private set; }
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly Config Config;
        protected readonly MapGeometry Geometry;
        protected readonly EntityManager EntityManager;
        protected readonly SpecialManager SpecialManager;

        private int m_exitTicks = 0;
        private LevelChangeType m_levelChangeType = LevelChangeType.Next;

        public IList<Line> Lines => Geometry.Lines;
        public IList<Side> Sides => Geometry.Sides;
        public IList<Wall> Walls => Geometry.Walls;
        public IList<Sector> Sectors => Geometry.Sectors;
        public BspTree BspTree => Geometry.BspTree;
        public LinkableList<Entity> Entities => EntityManager.Entities;
        public abstract Vec3D ListenerPosition { get; }
        public abstract double ListenerAngle { get; }
        public abstract double ListenerPitch { get; }

        private readonly DoomRandom m_random = new DoomRandom();
        
        protected WorldBase(Config config, ArchiveCollection archiveCollection, IAudioSystem audioSystem, 
            MapGeometry geometry, IMap map)
        {
            CreationTimeNanos = Ticker.NanoTime();
            ArchiveCollection = archiveCollection;
            Config = config;
            MapName = map.Name;
            Geometry = geometry;
            Blockmap = new BlockMap(Lines);
            SoundManager = new SoundManager(this, audioSystem);            
            EntityManager = new EntityManager(this, archiveCollection, SoundManager, config.Engine.Game.Skill);
            PhysicsManager = new PhysicsManager(this, BspTree, Blockmap, SoundManager, EntityManager, m_random);
            SpecialManager = new SpecialManager(this, archiveCollection.Definitions, m_random);
        }

        ~WorldBase()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Link(Entity entity)
        {
            Precondition(entity.SectorNodes.Empty() && entity.BlockmapNodes.Empty(), "Forgot to unlink entity before linking");
            
            PhysicsManager.LinkToWorld(entity);
        }

        public void Tick()
        {
            if (WorldState == WorldState.Exit)
            {
                m_exitTicks--;
                if (m_exitTicks == 0)
                    LevelExit?.Invoke(this, new LevelChangeEvent(m_levelChangeType));
            }
            else if (WorldState == WorldState.Normal)
            {
                // We need to do this (for now) because MoveZ and PreviouslyClipped
                // run into issues if this is not updated properly. If we can do a
                // resolution to the sector moving up/down with clipping monsters
                // issue, then this might be able to be handled better later on.
                EntityManager.Entities.ForEach(entity => entity.PrevPosition = entity.Box.Position);

                EntityManager.Entities.ForEach(entity =>
                {
                    entity.Tick();

                    // Entities can be disposed after Tick() (rocket explosion, blood spatter etc.)
                    if (!entity.IsDisposed)
                        PhysicsManager.Move(entity);
                });

                foreach (Player player in EntityManager.Players)
                {
                    if (player.Sector.SectorDamageSpecial != null)
                        player.Sector.SectorDamageSpecial.Tick(player);
                }

                SoundManager.Tick();
                TextureManager.Instance.Tick();
            }

            SpecialManager.Tick();

            Gametick++;
        }
        
        public IEnumerable<Sector> FindBySectorTag(int tag)
        {
            return Geometry.FindBySectorTag(tag);
        }

        public IEnumerable<Entity> FindByTid(int tid)
        {
            return EntityManager.FindByTid(tid);
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        public void ExitLevel(LevelChangeType type)
        {
            m_levelChangeType = type;
            WorldState = WorldState.Exit;
            m_exitTicks = 15;

            foreach (Player player in EntityManager.Players)
                player.ResetInterpolation();
        }

        protected void ChangeToLevel(int number)
        {
            LevelExit?.Invoke(this, new LevelChangeEvent(number));
        }

        protected virtual void PerformDispose()
        {
            EntityManager.Dispose();
            SoundManager.Dispose();
        }
    }
}