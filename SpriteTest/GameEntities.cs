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
    class Entity
    {
        Vector2 Position;
        Vector2 Velocity;
        Vector2 Acceleration;
        float Rotation;
    }

    class Mobile : Entity
    {
        float ThrustAcceleration;       // Not only the "gas" if the entity wants to move, but also the "brake" in the event of stopping or inertial dampening.
        float Hull;                     // i.e. HP, health, life, etc.
        bool DampenInnertia;            // If true, the object will automatically brake itself if not accelerating.
    }

    class Pickup : Mobile
    {
        float PickupRadius;             // If within this distance to pickup, you pick it up
    }

    class Projectile : Mobile
    {
        float BaseDamage;               // Base damage the projectile does to a target on collision
        float HullDamageCoef;           // Coefficient if it hits only hull
        float ShieldDamageCoef;         // Coefficient if it hits shields
        float ExplosionRadius;          // If > 0, the projectile explodes on contact and damages entities within this radius.
    }

    class Vessel : Mobile
    {        
        float RotationSpeed;            // In rads/s
        float Armor;                    // Will dampen damage to the hull
    }

    class Shielded : Vessel
    {
        float MaxShields;               // Maximum shield amount, shields will recharge to this amount
        float Shields;                  // Current amount of shields.  Below 20%, damage bleeds through to hull
        float ShieldRechargeRate;       // Rate in shields/s of recharge
    }
}
