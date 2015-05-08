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
    public interface WaveStackInterface
    {
        bool IsComplete();
        void Execute(float dir = -1);
    }

    public class WaveStackItem : WaveStackInterface
    {
        protected bool Complete;
        protected bool ExecutionComplete;

        public WaveStackItem()
        {
            
        }

        public WaveStackItem(WaveStackItem toCopy)
        {
            Complete = toCopy.Complete;
            ExecutionComplete = toCopy.ExecutionComplete;
        }

        virtual public bool IsComplete()
        {
            return Complete;
        }

        virtual public void Execute(float dir = -1)
        {
            // Do nothing by default
            // Set complete to true so we don't hang up the stack executor
            Complete = true;
        }
    }

    public class WaveStackClearEntities : WaveStackItem
    {
        public WaveStackClearEntities()
        {

        }

        public override void Execute(float dir = -1)
        {
            Complete = true;
            Program.GGame.ClearEntities();
        }
    }

    public class WaveStackFadeOut : WaveStackItem
    {
        public WaveStackFadeOut()
            : base()
        {
            
        }
        public override void Execute(float dir = -1)
        {
            base.Execute(dir);
            Program.GGame.FadeOut = true;            
            Program.GGame.FadeAmount = 0;
        }
    }

    public class WaveStackFadeIn : WaveStackItem
    {
        public WaveStackFadeIn()
            : base()
        {
            
        }

        public override void Execute(float dir = -1)
        {
            base.Execute(dir);
            Program.GGame.FadeOut = false;            
            Program.GGame.FadeAmount = 1;
        }
    }

    public class WaveStackPlaySong : WaveStackItem
    {
        Song SongToPlay;
        public WaveStackPlaySong(Song song)            
        {
            SongToPlay = song;        
        }

        public override void Execute(float dir = -1)
        {
            MediaPlayer.Play(SongToPlay);
            MediaPlayer.IsRepeating = true;
            Complete = true;
        }
    }

    public class WaveStackStopSong : WaveStackItem
    {
        
        public WaveStackStopSong()
        {
           
        }

        public override void Execute(float dir = -1)
        {
            MediaPlayer.Volume -= 0.01F;
            if (MediaPlayer.Volume <= 0)
            {
                MediaPlayer.Stop();
                MediaPlayer.Volume = 100;
                Complete = true;
            }
        }
    }

    public class WaveStackEnding : WaveStackItem
    {
        public WaveStackEnding()
        {

        }

        public override void Execute(float dir = -1)
        {
            Complete = true;
            Program.GGame.DoEnding();
        }
    }

    public class WaveStackChangeBackground : WaveStackItem
    {
        Texture2D Background;
        Vector2 Position;
        public WaveStackChangeBackground(Texture2D bg, Vector2 pos)
            :base()
        {
            Background = bg;
            Position = pos;
        }

        override public void Execute(float dir = -1)
        {
            Program.GGame.BGNeon = Background;
            Program.GGame.BGPos = Position;
            Complete = true;
        }
    }

    public class WaveStackAllowUserInput : WaveStackItem
    {
        bool AllowInput;
        public WaveStackAllowUserInput(bool allowInput)
        {
            AllowInput = allowInput;
        }

        override public void Execute(float dir = -1)
        {
            Program.GGame.AcceptPlayerInput = AllowInput;
            Complete = true;
        }
    }

    public class WaveStackSleep : WaveStackItem
    {
        int InitTime;
        int TimeTowait;
        public WaveStackSleep(int sleepTime)
        {
            InitTime = 0;
            TimeTowait = sleepTime;
        }

        public override void Execute(float dir = -1)
        {
            if (InitTime == 0)
                InitTime = InitTime = Environment.TickCount;
            if (Environment.TickCount - InitTime > TimeTowait)
                Complete = true;
        }
    }

    public class WaveStackDialog : WaveStackItem
    {
        DialogBox Dialog;
        int InitTime;
        int TimeToWait;
        public WaveStackDialog(DialogBox dialog, int time, string text = "") 
        {
            Dialog = new DialogBox(dialog);
            TimeToWait = time;
            InitTime = 0;
            if (text != "")
                Dialog.Dialog = text;
        }

        public override void Execute(float dir = -1)
        {
            if (InitTime == 0)
            {
                InitTime = InitTime = Environment.TickCount;
                Program.GGame.AddToNonSimulated(Dialog);
            }
            if (Environment.TickCount - InitTime > TimeToWait)
            {
                Program.GGame.RemoveFromNonSimulated(Dialog);
                Complete = true;
            }
        }
    }

    public class WaveStackWaitForEnemiesDestroyed : WaveStackItem
    {
        public WaveStackWaitForEnemiesDestroyed()
        {

        }

        public override bool IsComplete()
        {
            return Program.GGame.NumberOfSide(Entity.EntitySide.ENEMY) < 1;
        }
    }

    public class WaveStackEntity : WaveStackItem
    {
        Entity EntityToSpawn;
        public WaveStackEntity(Entity ent)
            :base()
        {
            EntityToSpawn = ent;
        }

        public override void Execute(float dir = -1)
        {
            Program.GGame.AddToNonSimulated(EntityToSpawn);
            Complete = true;
        }
    }

    public class WaveStackEnemy : WaveStackItem
    {
        Mobile EnemyToSpawn;
        float DirectionToSpawnFrom;

        public WaveStackEnemy(Vessel enemy, float direction)
        {
            EnemyToSpawn = enemy;
            DirectionToSpawnFrom = direction;
        }

        public WaveStackEnemy(WaveStackEnemy toCopy) 
        : base(toCopy)
        {
            EnemyToSpawn = toCopy.EnemyToSpawn;
            DirectionToSpawnFrom = toCopy.DirectionToSpawnFrom;
        }

        public override void Execute(float dir = -1)
        {
            float spawnDir = dir == -1 ? DirectionToSpawnFrom : dir;
            float direction;


            if (spawnDir == 0)
            {
                float randDir = (float)RNG.Generator.NextDouble() * 2 * (float)Math.PI;                
                direction = randDir;
            }
            else
            {
                direction = spawnDir;
            }

            Mobile vessel = EnemyToSpawn.GetCopy();
            // Set the position as twice the width of the play space
            Vector2 center = new Vector2(Program.GGame.Boundaries.Width / 2, Program.GGame.Boundaries.Height / 2);
            Vector2 directionVector = new Vector2((float)Math.Cos(direction), (float)Math.Sin(direction));
            float rand2 = (float)RNG.Generator.NextDouble();
            // Min distance should be twice the width
            float min = Program.GGame.Boundaries.Width * 2;
            float distance = min + rand2 * min * 3;
            Vector2 position = center + distance * directionVector;
            vessel.SetCenterPosition(position);
            vessel.Rotation = -direction + (float)Math.PI / 2;
            vessel.RotationWanted = vessel.Rotation;
            vessel.EngageWarp(true);
            vessel.WarpTarget = center + directionVector * Program.GGame.Boundaries.Width / 4;
            
            Program.GGame.AddToSimulated(vessel);
            Complete = true;
        }
    }

    public class WaveStackGraphicsChange : WaveStackItem
    {
        public WaveStackGraphicsChange()
            : base()
        {
            
        }

        public override void Execute(float dir = -1)
        {
            Program.GGame.ChangeGraphics();
            Complete = true;
        }
    }

    public class WaveStackEnemyCollection : WaveStackItem
    {
        List<WaveStackEnemy> Enemies = new List<WaveStackEnemy>();
        public float Direction;

        public WaveStackEnemyCollection()
            : base()
        {
            Direction = -1;
        }

        public WaveStackEnemyCollection(List<WaveStackEnemy> enemies)
        {
            Enemies = enemies;
        }

        public WaveStackEnemyCollection(WaveStackEnemyCollection toCopy)
            : base(toCopy)
        {
            Enemies = toCopy.Enemies;
            Direction = toCopy.Direction;
        }

        public void AddEnemy(WaveStackEnemy enemy)
        {
            Enemies.Add(enemy);

        }

        public override void Execute(float dir = -1)
        {
            foreach (WaveStackEnemy enemy in Enemies)
            {
                enemy.Execute(Direction);
            }
            Complete = true;
        }
    }

    



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
