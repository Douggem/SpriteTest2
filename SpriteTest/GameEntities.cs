/* 
 * TODO:
 * Add Collidable interface or something
 * Fix collision detection
 * Add impulse handling
 * */

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

    public static class RNG
    {
        public static Random Generator = new Random();
    }

    public static class CollisionMath
    {
        // Rotates a vertex around the origin with 'angle' radians
        public static Vector2 RotateVertexAroundOrigin(Vector2 p, float angle)
        {
            float xnew = (float)(p.X * Math.Cos(angle) - p.Y * Math.Sin(angle));
            float ynew = (float)(p.X * Math.Sin(angle) + p.Y * Math.Cos(angle));
            Vector2 result = new Vector2(xnew, ynew);
            return result;
        }
        public static Vector2 RotateVertexAroundPoint(Vector2 p, float angle, Vector2 o)
        {
            float xnew = o.X + (float)((p.X - o.X)* Math.Cos(angle) + (p.Y - o.Y) * Math.Sin(angle));
            float ynew = o.Y + (float)((p.X - o.X) * -Math.Sin(angle) + (p.Y - o.Y) * Math.Cos(angle));
            Vector2 result = new Vector2(xnew, ynew);
            return result;
        }
    }

    public class BoxWithPoints
    {
        public Vector2 P1;                         // Use vectors instead of points so we can use floats and have sub-points
        public Vector2 P2;
        public Vector2 P3;
        public Vector2 P4;

        // Until we add sanity checks, P1 should be upper left, P2 lower left, P3 lower right, P4 upper right corner
        public BoxWithPoints(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
        }

        public void RotateAroundOrigin(float theta) 
        {
            P1 = CollisionMath.RotateVertexAroundOrigin(P1, theta);
            P2 = CollisionMath.RotateVertexAroundOrigin(P2, theta);
            P3 = CollisionMath.RotateVertexAroundOrigin(P3, theta);
            P4 = CollisionMath.RotateVertexAroundOrigin(P4, theta);
        }

        public void RotateAroundPoint(float theta, Vector2 o)
        {   
            P1 = CollisionMath.RotateVertexAroundPoint(P1, theta, o);
            P2 = CollisionMath.RotateVertexAroundPoint(P2, theta, o);
            P3 = CollisionMath.RotateVertexAroundPoint(P3, theta, o);
            P4 = CollisionMath.RotateVertexAroundPoint(P4, theta, o);
        }

        // Assumes axis alignment for the time being
        public bool PointIsInside(Vector2 point)
        {
            if (point.X < P1.X)             // Point is to the LEFT of the box
                return false;
            if (point.X > P3.X)             // Point is to the RIGHT of the box
                return false;
            if (point.Y > P1.Y)             // Point is ABOVE the box
                return false;
            if (point.Y < P2.Y)             // Point is BELOW the box
                return false;
            return true;                    // If it's not any of the above, it's in the box
        }
    }

    /*
     * Holds four vertices representing a bounding box that can be rotated and not just aligned along the x/y axes
     * */
    public class BoundingBoxNonAligned
    {
        float CenterX;
        float CenterY;
        float Width;
        float Height;
        float Theta;
        public BoundingBoxNonAligned(float centerX, float centerY, float width, float height, float theta) {
            CenterX = centerX;
            CenterY = centerY;
            Width = width;
            Height = height;
            Theta = theta;
        }

        public BoundingBoxNonAligned(BoundingBoxNonAligned cpy)
        {
            CenterX = cpy.CenterX;
            CenterY = cpy.CenterY;
            Width = cpy.Width;
            Height = cpy.Height;
            Theta = cpy.Theta;
        }

        public BoundingBoxNonAligned(Rectangle rect, float theta)
        {
            Width = rect.Width;
            Height = rect.Height;
            CenterX = rect.X + Width / 2;
            CenterY = rect.Y + Height / 2;            
            Theta = theta;
        }

        public BoxWithPoints GetPoints()
        {
            Vector2 p1, p2, p3, p4;
            p1 = new Vector2(CenterX - Width / 2, CenterY + Height / 2);
            p2 = new Vector2(CenterX - Width / 2, CenterY - Height / 2);
            p3 = new Vector2(CenterX + Width / 2, CenterY - Height / 2);
            p4 = new Vector2(CenterX + Width / 2, CenterY + Height / 2);

            BoxWithPoints result = new BoxWithPoints(p1, p2, p3, p4);
            result.RotateAroundPoint(Theta, new Vector2(CenterX, CenterY));
            return result;
        }

        public bool Intersects(BoundingBoxNonAligned box)
        {
            // Work with copies of the boxes instead of modifying the originals
            BoundingBoxNonAligned thisCopy = new BoundingBoxNonAligned(this);
            BoundingBoxNonAligned target = new BoundingBoxNonAligned(box);
            
            // Use thisCopy's point as the origin
            Vector2 origin = new Vector2(thisCopy.CenterX, thisCopy.CenterY);
            thisCopy.CenterX -= origin.X;
            thisCopy.CenterY -= origin.Y;
            target.CenterX -= origin.X;
            target.CenterY -= origin.Y;

            // Rotate so that box 2 is axis aligned
            BoxWithPoints thisPoints = thisCopy.GetPoints();
            BoxWithPoints targetPoints = target.GetPoints();
            float angle = -target.Theta;
            thisPoints.RotateAroundOrigin(angle);
            targetPoints.RotateAroundOrigin(angle);

            // Check if any points in box 1 are contained within box 2
            // At this point, assume points are from top left and go clockwise
            // TODO: Add some kind of checking to the box with points so we don't have to assume
            if (targetPoints.PointIsInside(thisPoints.P1))
                return true;
            if (targetPoints.PointIsInside(thisPoints.P2))
                return true;
            if (targetPoints.PointIsInside(thisPoints.P3))
                return true;
            if (targetPoints.PointIsInside(thisPoints.P4))
                return true;
            return false;
        }
    }

    /*
     * The base class used by all game entities, gives the basic framework for any game object
     * Can not collide with anything, use mobile for collision
     * */
    public class Entity
    {
        public enum EntitySide              // Subclasses of entity can be configured to not collide with objects of the same side
        {
            PLAYER,
            ENEMY,
            NEUTRAL
        };

        public enum EntityType              // Save ourselves some processing power doing dynamic casts
        {
            ENTITY,
            MOBILE,
            PICKUP,
            PROJECTILE,
            VESSEL
        };

        public EntitySide Side;                 // Lets AI know what to shoot at, and prevents enemies from colliding with one another
        public EntityType Type;                 // We treat our base types differently, we can use this instead of dynamic casting
        public Texture2D Model                  // The dimensions of the image make our bounding box, so cut off all the extra space
            { get; set;}
        protected Vector2 Position;             // Duh, position in game space.  Represents top left 
        public float Elasticity                 // Coefficient applied to velocity when hit with a force.  Elasticity of 0 absorbs all impact, 1 bounces it all off                                    
            { get; set; }                       // So with 1, if you hit something at 100km/h you will bounce off at 100km/h in the opposite direction
        protected Vector2 Velocity;             // Current velocity
        protected Vector2 Acceleration;         // Current acceleration, will be applied to velocity every tick
        private float _rotation;        
        public float Rotation                   // In radians, use setter so we can constrain it between 0 and 2pi
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
                if (_rotation < 0)
                    _rotation += 2 * (float)Math.PI;
                else if (_rotation > 2 * (float)Math.PI)
                    _rotation -= 2 * (float)Math.PI;
            }
        }
        protected Vector2 Origin;
        

        public Entity(Texture2D model, EntitySide side, Vector2 pos, float rot) 
        {
            Type = EntityType.ENTITY;
            Model = model;
            Side = side;
            Position = pos;
            Rotation = rot;
            Velocity = new Vector2(0, 0);
            Acceleration = new Vector2(0, 0);
            if (Model != null)
                Origin = new Vector2(Model.Width / 2, Model.Height / 2);
            else
                Origin = new Vector2(0, 0);
            Elasticity = 0.8F;
        }

        public Entity(EntityInfo info, Vector2 pos, float rot)
        {
            Type = EntityType.ENTITY;
            Model = info.Model;
            Elasticity = info.Elasticity;
            Side = info.Side;
            Position = pos;
            Rotation = rot;
            Acceleration = new Vector2(0, 0);
            if (Model != null)
                Origin = new Vector2(Model.Width / 2, Model.Height / 2);
            else
                Origin = new Vector2(0, 0);
        }

        public Entity()                 // Only here to prevent compiler errors for inherited classes, remove before production
        {

        }

        public virtual bool Remove()
        {
            return false;
        }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 screenPos, float scale)
        {
            Texture2D tex = Model;
            
            // We want things drawn at the center of their position, not the top left
            Vector2 offset = new Vector2(tex.Width / 2, tex.Height / 2);
            spriteBatch.Draw(tex, screenPos, null, Color.White, -Rotation, GetOrigin(), scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1);            
        }

        public virtual void Update(double deltaT) 
        {
            // Do nothing by default
        }

        public virtual bool SpawnOnDestroy()
        {
            return false;
        }

        public virtual void DoSpawnOnDestroy()
        {
            // This should not be called by default
            return;
        }

        public virtual void RotateToVector(Vector2 dir)
        {
            dir.Normalize();
            float rotationWanted = (float)Math.Atan2(dir.Y, dir.X) - (float)(Math.PI / 2F);
            if (rotationWanted < 0)
                rotationWanted += (float)Math.PI * 2;
            Rotation = rotationWanted;
        }
                
        public Vector2 GetPosition() { return Position; }
        public Vector2 GetCenterPosition()
        {
            Vector2 result = Position + Origin;
            return result;
        }
                
        public Vector2 GetVelocity() { return Velocity; }
        public Vector2 GetAcceleration() { return Acceleration; }
        public Vector2 GetOrigin() { return Origin; }

        public void SetPosition(float x, float y) { Position.X = x; Position.Y = y; }
        public void SetCenterPosition(float x, float y) { Position.X = x - Origin.X; Position.Y = y - Origin.Y; }
        public void SetVelocity(float x, float y) { Velocity.X = x; Velocity.Y = y; }        
        public void SetAcceleration(float x, float y) { Acceleration.X = x; Acceleration.Y = y; }

        public void SetPosition(Vector2 p) { Position = p; }
        public void SetVelocity(Vector2 v) { Velocity = v; }
        public void SetCenterPosition(Vector2 p) { Position.X = p.X - Origin.X; Position.Y = p.Y - Origin.Y; }
    }

    public class Explosion : Entity
    {
        AnimatedTexture AnimTexture;

        public Explosion(AnimatedTexture anim, EntitySide side, Vector2 pos, float rot )
            : base(null, side, pos, rot)
        {
            AnimTexture = anim;
        }

        public Explosion(ExplosionInfo info, Vector2 pos, float rot)
            : base (info, pos, rot)
        {
            AnimTexture = info.AnimTexture;
            Origin = new Vector2(AnimTexture.FrameWidth, AnimTexture.FrameHeight);
        }

        public override bool Remove()
        {
            return AnimTexture.Complete();
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 screenPos, float scale) 
        {
            // We want things drawn at the center of their position, not the top left
            
            AnimTexture.Rotation = Rotation;
            AnimTexture.DrawFrame(spriteBatch, Position, scale);
            
        }

        public virtual void DoLogic(double deltaT) 
        {
            
        }

        public override void Update(double deltaT)
        {
            base.Update(deltaT);
            AnimTexture.UpdateFrame((float)deltaT);
            DoLogic(deltaT);
        }

        
    }

    public class Mobile : Entity
    {
        public Vector2 VelocityWanted;          // Set VelocityWanted in the direction we want to go at max speed and handle smoothing in the simulation            
        public Vector2 PositionWanted           // During simulation this will be set to the future state before collision is done
            { get; set; }
        public float RotationWanted;            // Set the direction we WANT to face, so we can do smooth transitions when turning        
        public float MaxSpeed                   // If we are above max speed, we will DAMPEN down to max speed, so it's a 'soft' max not a 'hard' max
            { get; set; }
        public float ThrustAcceleration         // Not only the "gas" if the entity wants to move, but also the "brake" in the event of stopping or inertial dampening.
            { get; set; } 
        float Hull                              // i.e. HP, health, life, etc.
            { get; set; } 
        float Mass;                             // Used in collision calculations to calculate impulse
        float MassInv;                          // Pre-calculated to avoid divide by zero
        Rectangle BoundingBox;                  // Used for collision, obviously
        public bool CanCollide                  // If false, will not collide with anything
            { get; set; } 
        public bool CollideWithOwnSide          // If false, will not collide with entities of same side
            { get; set; } 
        public bool DampenInnertia              // If true, the object will automatically brake itself if not accelerating.
            { get; set; }
        public bool IsDestroyed 
            { get; set; }
        public float Speed;
        public EntityAI AIRoutine;

        // Animation for destruction
        public AnimatedTexture DestructionAnimation
            { get; set; }

        public Mobile(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass) 
            : base(model, side, pos, rot)
        {
            Type = EntityType.MOBILE;
            Hull = hull;
            Mass = mass;
            if (Mass != 0)
                MassInv = 1 / Mass;
            else
                MassInv = 0;
            CanCollide = true;
            CollideWithOwnSide = false;
            IsDestroyed = false;
            DampenInnertia = true;
            RotationWanted = 0;
            MaxSpeed = 401;
            ThrustAcceleration = 2000;
            VelocityWanted = new Vector2(0, 0);            
            BoundingBox = new Rectangle(model.Bounds.X, model.Bounds.Y, model.Bounds.Width, model.Bounds.Height);
            AIRoutine = new EntityAI(this);
        }

        public Mobile(MobileInfo info, Vector2 pos, float rot)
            : base(info, pos, rot)
        {
            Type = EntityType.MOBILE;
            Hull = info.Hull;
            Mass = info.Mass;
            if (Mass != 0)
                MassInv = 1 / Mass;
            else
                MassInv = 0;
            CanCollide = info.CanCollide;
            CollideWithOwnSide = info.CollideWithOwnSide;
            IsDestroyed = false;
            DampenInnertia = info.DampenInnertia;
            RotationWanted = 0;
            MaxSpeed = info.MaxSpeed;
            ThrustAcceleration = info.ThrustAcceleration;
            VelocityWanted = new Vector2(0, 0);
            BoundingBox = new Rectangle(Model.Bounds.X, Model.Bounds.Y, Model.Bounds.Width, Model.Bounds.Height);
            AIRoutine = new EntityAI(this);
        }        

        public override void Update(double deltaT)         
        {
            base.Update(deltaT);
            SpeedUpToWanted(deltaT);
            AIRoutine.DoLogic(deltaT);
        }

        public virtual void OnCollide(Mobile collidedWith) 
        {

        }

        public virtual void ApplyDamage(float dmg)
        {
            Hull -= dmg;
            if (Hull < 0)
                IsDestroyed = true;
        }

        public void AddImpulse(Vector2 impulse)
        {
            // impulse is in newtons
            // Handle as instantaneous change in velocity, each newton will accelerate by 1m/s * massinv
            impulse *= MassInv;
            Velocity += impulse;
        }

        public Vector2 GetForce()
        {
            Vector2 force = MassInv * Velocity;
            return force;
        }

        public Vector2 GetCenterPositionWanted()
        {
            Vector2 result = PositionWanted + Origin;
            return result;
        }

        public Rectangle GetBoundingBoxNonRotated()
        {
            // Return a copy so that it can't be edited -  maybe not necessary?
            Vector2 position = GetPosition();
            return new Rectangle((int)(BoundingBox.X + position.X), (int)(BoundingBox.Y + position.Y), BoundingBox.Width, BoundingBox.Height);
        }

        public BoundingBoxNonAligned GetBoundingBoxNonAligned() 
        {
            BoundingBoxNonAligned box = new BoundingBoxNonAligned(GetBoundingBoxNonRotated(), Rotation);
            return box;
        }

        public BoxWithPoints GetBoundingBoxPointsRotated()
        {

            BoundingBoxNonAligned box = new BoundingBoxNonAligned(GetBoundingBoxNonRotated(), Rotation);
            BoxWithPoints result = box.GetPoints();
            return result;
        }

        /*
         * If we're over max speed, dampen down to max speed gradually
         * */
        public void CheckSpeed()
        {
            if (Velocity.X == 0 && Velocity.Y == 0)
                return;
            float speed = Velocity.Length();
            if (speed > MaxSpeed)
            {
                float ratio = MaxSpeed / speed;
                Velocity *= ratio;
            }
        }

        public override void RotateToVector(Vector2 dir)
        {
            dir.Normalize();
            float rotationWanted = (float)Math.Atan2(dir.Y, dir.X) - (float)(Math.PI / 2F);
            if (rotationWanted < 0)
                rotationWanted += (float)Math.PI * 2;
            RotationWanted = rotationWanted;
        }

        /*
         * Speed up to SpeedWanted gradually
         * */
        public void SpeedUpToWanted(double deltaT)
        {
            if (VelocityWanted.X == 0 && VelocityWanted.Y == 0 && !DampenInnertia)
                return;
            Vector2 velDiff = VelocityWanted - Velocity;
            float speedDiff = velDiff.Length();
            if (speedDiff == 0)
                return;
            // If we can meet max speed in this tick, do so
            if (speedDiff < ThrustAcceleration * (float)deltaT)
            {
                Velocity = VelocityWanted;
                return;
            }

            // Otherwise, add as much speed as we can
            float accelThisFrame = (float)deltaT * ThrustAcceleration;
            Vector2 copy = new Vector2(velDiff.X, velDiff.Y);
            copy.Normalize();
            Vector2 offset = copy * accelThisFrame;
            Velocity += offset;
        }

        public void SetVelocityWanted(Vector2 vel)
        {
            VelocityWanted = vel;
        }

        public void SetVelocityWanted(float x, float y)
        {
            VelocityWanted.X = x;
            VelocityWanted.Y = y;
        }

        public bool IsCollidingWith(Mobile target)
        {
            return GetBoundingBoxNonAligned().Intersects(target.GetBoundingBoxNonAligned());
        }

        public override bool SpawnOnDestroy()
        {
            return (DestructionAnimation != null);
        }

        public Mobile()
        {

        }

        public override void DoSpawnOnDestroy()
        {
            // If you called this without checking SpawnOnDestroy first, you done goofed
            Vector2 pos = GetCenterPosition();
            Explosion exp = new Explosion(DestructionAnimation, Side, pos, Rotation);
            exp.SetCenterPosition(pos);
            Program.GGame.AddToNonSimulated(exp);            
        }
    }

    public class Pickup : Mobile
    {
        float PickupRadius;             // If within this distance to pickup, you pick it up

    }

    /*
     * Projectile are bullets, missiles, etc.
     * */
    public class Projectile : Mobile
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
        public float InitTime                 // The ticktime the projectile was created
            { get; set; }
        public float DeflectionChance
            { get; set; }

        public Projectile(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass)
            : base(model, side, pos, rot, hull, mass)
        {
            Type = EntityType.PROJECTILE;
            DampenInnertia = false;
            TimeToLive = 6;
            InitTime = Environment.TickCount;
            DeflectionChance = 0.25F;
        }

        public Projectile(ProjectileInfo info, Vector2 pos, float rot)
            : base(info, pos, rot)
        {
            Type = EntityType.PROJECTILE;
            DampenInnertia = false;
            TimeToLive = info.TimeToLive;
            InitTime = Environment.TickCount;
            DeflectionChance = 0.25F;
            BaseDamage = info.BaseDamage;
        }

        // Our base implementation of OnCollide is to do damage like a regular bullet and then disappear
        public override void OnCollide(Mobile collidedWith)
        {
            // Only destroy the object if it did not deflect
            float randNum = (float)RNG.Generator.NextDouble();
            if (randNum > DeflectionChance)            
            {
                IsDestroyed = true;
            }
            collidedWith.ApplyDamage(BaseDamage);

        }
    }

    /*
     * Vessels are mobile entities that can fire projectiles 
     * */
    public class Vessel : Mobile
    {        
        public float RotationSpeed;            // In rads/s
        float Armor;                    // Will dampen damage to the hull
        public Weapon CurrentWeapon
        { get; set; }

        public Vessel(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass, float armor = 0)
            : base(model, side, pos, rot, hull, mass)
        {
            RotationSpeed = 6 * (float)Math.PI;
            Armor = armor;
            Type = EntityType.VESSEL;
        }

        public Vessel() {  }

        public override void Update(double deltaT) 
        {
            base.Update(deltaT);
            RotateToWanted(deltaT);
            SpeedUpToWanted(deltaT);
            if (CurrentWeapon.ShotTimer > 0)
                CurrentWeapon.ShotTimer -= (float)deltaT;
            else CurrentWeapon.ShotTimer = 0;
        }

        /*
         * Rotate as much as we can (RotationSpeed rads/s) toward our target rotation.
         * */
        public void RotateToWanted(double deltaT)
        {
            double radsToGo = Rotation - RotationWanted;
            int direction = -1;
            if (radsToGo < 0)
                direction = 1;
            radsToGo = Math.Abs(radsToGo);

            // Don't go the wrong direction, silly
            if (RotationWanted < 0)
                RotationWanted += 2 * (float)Math.PI;
            else if (RotationWanted > 2 * (float)Math.PI)
                RotationWanted -= 2 * (float)Math.PI;
            
            if (radsToGo > Math.PI)
            {                        // Means we need to go backwards
                radsToGo = 2 * (float)Math.PI - radsToGo;
                direction *= -1;
            }

            // If we can complete the rotation in this tick, do so
            if (radsToGo <= RotationSpeed * deltaT)
            {
                Rotation = RotationWanted;
                return;
            }
            // If we can not, rotate as much as we can toward the target rotation
            Rotation += direction * RotationSpeed * (float)deltaT;
        }

        public virtual bool CanFire()
        {
            if (CurrentWeapon.ShotTimer <= 0)
                return true;
            return false;
        }

        public virtual Projectile Fire()
        {
            float angle = Rotation + (float)Math.PI / 2;
            Projectile bullet = new Projectile(CurrentWeapon.Munition, GetCenterPosition(), angle);
            Vector2 angleVector = new Vector2((float)Math.Cos(angle), -(float)Math.Sin(angle));
            CurrentWeapon.ShotTimer = CurrentWeapon.ShotDelay;
            bullet.SetVelocity(angleVector * 2 * CurrentWeapon.InitSpeed + GetVelocity());
            bullet.SetVelocityWanted(angleVector * 0);
            bullet.Side = Side;
            bullet.Rotation = Rotation;
             
            return bullet;
        }
    }

    public class Shielded : Vessel
    {
        public float MaxShields;                           // Maximum shield amount, shields will recharge to this amount
        public float Shields;                              // Current amount of shields.  Below 20%, damage bleeds through to hull
        public float ShieldRechargeRate;                   // Rate in shields/s of recharge
    }

    public class Weapon
    {
        public string Name
            {get; set;}
        public ProjectileInfo Munition;
        public float InitSpeed;                            // Velocity (relative to vessel in direction of heading) the projectile will be launched at
        public float ShotDelay;                            // Time between shots, i.e. rate of fire
        public float ShotTimer;                            // Set to ShotDelay when shot, don't fire until reaches 0

        public Weapon(ProjectileInfo munition, float speed, float delay)
        {
            Name = munition.Name;
            Munition = munition;
            InitSpeed = speed;
            ShotDelay = delay;
            ShotTimer = 0;
        }
    }
}
