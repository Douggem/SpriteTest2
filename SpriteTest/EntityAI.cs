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
    // We want different enemy types to have different AI
    // We will achieve this by making AI classes for different behaviors
    // And letting enemy types be given AI Types to use
    public class EntityAI
    {
        protected Mobile Parent;
        protected double LogicFrequency;     // How often, in seconds, we perform logic
        protected double TimeLogicLastPerformed;

        public EntityAI(Mobile parent, double logicFrequency = 1)
        {
            Parent = parent;
            LogicFrequency = logicFrequency;
            TimeLogicLastPerformed = 0;
        }

        public virtual void PerformLogic(double deltaT)
        {

        }

        // Check if we need to perform logic and, if so, call PerformLogic
        public virtual void DoLogic(double deltaT)
        {
            TimeLogicLastPerformed += deltaT;
            if (TimeLogicLastPerformed > LogicFrequency)
            {
                PerformLogic(deltaT);
                TimeLogicLastPerformed = 0;
            }
        }
    }

    public class DumbShooter : EntityAI
    {
        protected Mobile Target;          // Typically use the player      

        public DumbShooter(Mobile parent, double logicFrequency)
            : base(parent, logicFrequency)
        {
            
        }

        // We want to try to ram the target
        // We will add velocity on a vector toward the target, without correcting for our current heading
        // This will make us miss an actively moving target, but hit a stationary one
        // It will give us a 'swarm' effect
        public override void PerformLogic(double deltaT)
        {
            if (Target != null && !Target.IsDestroyed)
            {
                // Set velocity wanted as the vector to the target
                Vector2 parentToTarget = Target.GetCenterPosition() - Parent.GetCenterPosition();
                Vector2 rot = new Vector2(parentToTarget.Y, -parentToTarget.X);
                parentToTarget.Normalize();
                Parent.RotateToWorldVector(rot);
                if (Parent.Type == Entity.EntityType.VESSEL)
                {
                    Vessel v = (Vessel)Parent;
                    if (v.CanFire())
                    {
                        Program.GGame.AddToSimulated(v.Fire());
                    }
                }
                
            }
            else
            {
                Target = Program.GGame.GetPlayerVessel();
            }
        }
    }

    public class Rammer : EntityAI
    {
        protected Mobile Target;          // Typically use the player      

        public Rammer(Mobile parent, double logicFrequency)
            : base(parent, logicFrequency)
        {
            
        }

        // We want to try to ram the target
        // We will add velocity on a vector toward the target, without correcting for our current heading
        // This will make us miss an actively moving target, but hit a stationary one
        // It will give us a 'swarm' effect
        public override void PerformLogic(double deltaT)
        {
            if (Target != null && !Target.IsDestroyed)
            {
                // Set velocity wanted as the vector to the target
                Vector2 parentToTarget = Target.GetCenterPosition() - Parent.GetCenterPosition();
                parentToTarget.Normalize();
                Vector2 rot = new Vector2(parentToTarget.Y, -parentToTarget.X);
                Parent.RotateToWorldVector(rot);
                parentToTarget *= Parent.MaxSpeed;
                Parent.SetVelocityWanted(parentToTarget);
            }
            else
            {
                Target = Program.GGame.GetPlayerVessel();
            }
        }
    }
}
