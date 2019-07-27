﻿using System;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Maps.Geometry
{
    public class SectorFlat
    {
        public readonly int Id;
        public readonly PlaneD Plane;
        public readonly SectorFlatFace Facing;
        public double Z;
        public CIString Texture;
        public byte LightLevel;
        private Sector? m_sector;

        public Sector Sector => m_sector ?? throw new NullReferenceException("Forgot to set sector for sector flat");

        public SectorFlat(int id, CIString texture, double z, byte lightLevel, SectorFlatFace facing)
        {
            Id = id;
            Facing = facing;
            Texture = texture;
            Z = z;
            Plane = new PlaneD(z);
            LightLevel = lightLevel;
        }

        public void SetSector(Sector sector)
        {
            m_sector = sector;
        }
    }
}