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
using AnimatedSprite;

namespace SpriteTest
{
    /*
 * ***********************************************
 * Info holders for building predefined entities
 * ***********************************************
 * */

    // Contains information to build a predefined Entity from
    public class EntityInfo
    {
        public string Name
        { get; set; }
        public Texture2D Model
        { get; set; }
        public float Elasticity
        { get; set; }
        public Entity.EntitySide Side
        { get; set; }

        public EntityInfo(string name, Texture2D model)
        {
            Name = name;
            Model = model;
            Side = Entity.EntitySide.NEUTRAL;       // You need to be sure to set the side when you fire!
            Elasticity = 0.75F;
        }
    }

    public class ExplosionInfo : EntityInfo
    {
        public AnimatedTexture AnimTexture;
        public ExplosionInfo(string name, AnimatedTexture anim)
            : base(name, null)
        {
            AnimTexture = anim;
        }
    }

    // Contains information to build a predefined Mobile from
    public class MobileInfo : EntityInfo
    {
        public float MaxSpeed                      // If we are above max speed, we will DAMPEN down to max speed, so it's a 'soft' max not a 'hard' max        
            { get; set; }
        public float ThrustAcceleration            // Not only the "gas" if the entity wants to move, but also the "brake" in the event of stopping or inertial dampening.        
            { get; set; }
        public float Hull                                 // i.e. HP, health, life, etc.        
            { get; set; }
        public float Mass                                 // Used in collision calculations to calculate impulse
            { get; set; }
        public float MassInv                              // Pre-calculated to avoid divide by zero        
            { get; set; }
        public bool CanCollide                     // If false, will not collide with anything        
            { get; set; }
        public bool CollideWithOwnSide             // If false, will not collide with entities of same side        
            { get; set; }
        public bool DampenInnertia                 // If true, the object will automatically brake itself if not accelerating.
            { get; set; }
        public AnimatedTexture DestructionAnimation // Animation for destruction
            { get; set; }

        public MobileInfo(string name, Texture2D model, float hull, float mass)
            : base(name, model)
        {
            Hull = hull;
            Mass = mass;
            if (Mass != 0)
                MassInv = 1 / Mass;
            else
                MassInv = 0;
            CanCollide = true;
            CollideWithOwnSide = false;
            DampenInnertia = true;
            MaxSpeed = 400;
            ThrustAcceleration = 2000;
        }
    }

    public class ProjectileInfo : MobileInfo
    {
        public float BaseDamage               // Base damage the projectile does to a target on collision
        { get; set; }
        public float HullDamageCoef           // Coefficient if it hits only hull
        { get; set; }
        public float ShieldDamageCoef         // Coefficient if it hits shields
        { get; set; }
        public float ExplosionRadius          // If > 0, the projectile explodes on contact and damages entities within this radius.
        { get; set; }
        public float TimeToLive               // After TimeToLive seconds, this projectile should be removed from the simulation
        { get; set; }
        public float ExplosionDamage;
        public float Acceleration;
        public bool Seeking = false;
        public ProjectileInfo(string name, Texture2D model, float hull, float mass, float baseDamage)
            : base(name, model, hull, mass)
        {
            BaseDamage = baseDamage;
            HullDamageCoef = 1;
            ShieldDamageCoef = 1;
            ExplosionRadius = 0;
            TimeToLive = 6;

        }
    }

    public class VesselInfo : MobileInfo
    {
        float RotationSpeed;                        // In rads/s
        float Armor;                                // Will dampen damage to the hull

        public VesselInfo(string name, Texture2D model, float hull, float mass, float armor)
            : base(name, model, hull, mass)
        {
            Armor = armor;
            RotationSpeed = 4.0F;
        }
    }
}
