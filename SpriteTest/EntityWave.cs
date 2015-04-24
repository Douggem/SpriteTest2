using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace SpriteTest
{
    public class VesselEntry
    {
        public VesselInfo VInfo;
        public float EntryDirection;            // In radians in screen space
        public float EntryDirectionAngleRand;   // In radians, give a little randomness to entry direction

        public  class WaveEntry 
        {
            //virtual void 
        }

        public VesselEntry(VesselInfo vinfo, float direction, float randomness = 0.10F)
        {
            VInfo = vinfo;
            EntryDirection = direction;
            EntryDirectionAngleRand = randomness;
        }
    }

    public class EntityWave
    {
        public List<VesselEntry> Vessels;
    }
}
