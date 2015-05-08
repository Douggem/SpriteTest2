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
            return RotateVertexAroundPoint(p, angle, new Vector2(0, 0));
            /*float xnew = (float)(p.X * Math.Cos(angle) - p.Y * Math.Sin(angle));
            float ynew = (float)(p.X * Math.Sin(angle) + p.Y * Math.Cos(angle));
            Vector2 result = new Vector2(xnew, ynew);
            return result;*/
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

        public BoxWithPoints(BoxWithPoints toCopy)
        {
            P1 = toCopy.P1;
            P2 = toCopy.P2;
            P3 = toCopy.P3;
            P4 = toCopy.P4;
        }

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

       /* public BoundingBoxNonAligned(BoundingBoxNonAligned toCopy)
        {
            CenterX = toCopy.CenterX;
            CenterY = toCopy.CenterY;
            Width = toCopy.Width;
            Height = toCopy.Height;
            Theta = toCopy.Theta;
        }*/

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

            // Rotate so that box 1 is axis aligned
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

        public Entity(Entity toCopy)
        {
            Side = toCopy.Side;
            Type = toCopy.Type;
            Model = toCopy.Model;
            Position = new Vector2(toCopy.Position.X, toCopy.Position.Y);
            Elasticity = toCopy.Elasticity;
            Velocity = new Vector2(toCopy.Velocity.X, toCopy.Velocity.Y);
            Acceleration = new Vector2(toCopy.Acceleration.X, toCopy.Acceleration.Y);
            Rotation = toCopy.Rotation;
            Origin = toCopy.Origin;
        }


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

        public virtual bool KeepInBounds()
        {
            return false;
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

        public virtual void OnDestroy() {

        }

        public virtual void RotateToWorldVector(Vector2 dir)
        {

        }

        public virtual void RotateToScreenVector(Vector2 dir)
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
        public virtual void SetVelocity(float x, float y) { Velocity.X = x; Velocity.Y = y; }        
        public void SetAcceleration(float x, float y) { Acceleration.X = x; Acceleration.Y = y; }

        public void SetPosition(Vector2 p) { Position = p; }
        public void SetVelocity(Vector2 v) { Velocity = v; }
        public void SetCenterPosition(Vector2 p) { Position.X = p.X - Origin.X; Position.Y = p.Y - Origin.Y; }
    }

    public class Explosion : Entity
    {
        AnimatedTexture AnimTexture;
        float timeAlive;

        public Explosion(Explosion toCopy)
            : base(toCopy)
        {
            AnimTexture = toCopy.AnimTexture;
            timeAlive = toCopy.timeAlive;
        }

        public Explosion(AnimatedTexture anim, EntitySide side, Vector2 pos, float rot )
            : base(null, side, pos, rot)
        {
            AnimTexture = anim;
            timeAlive = 0;
        }

        public Explosion(ExplosionInfo info, Vector2 pos, float rot)
            : base (info, pos, rot)
        {
            AnimTexture = info.AnimTexture;
            Origin = new Vector2(AnimTexture.FrameWidth, AnimTexture.FrameHeight);
            timeAlive = 0;
        }

        public override bool Remove()
        {
            return AnimTexture.Complete(timeAlive);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 screenPos, float scale) 
        {
            // We want things drawn at the center of their position, not the top left
            
            AnimTexture.Rotation = Rotation;
            AnimTexture.DrawFrame(spriteBatch, screenPos, timeAlive, scale );            
        }

        public virtual void DoLogic(double deltaT) 
        {
            
        }

        public override void Update(double deltaT)
        {
            timeAlive += (float)deltaT;
            base.Update(deltaT);            
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
        protected float Hull                              // i.e. HP, health, life, etc.
            { get; set; }
        protected float MaxHull;
        public float Mass;                             // Used in collision calculations to calculate impulse
        public float MassInv;                          // Pre-calculated to avoid divide by zero
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
        public bool StretchOnVelocity = false;
        public bool AtWarp = false;
        public Vector2 WarpTarget;
        public EntityAI AIRoutine;

        // Animation for destruction
        public AnimatedTexture DestructionAnimation
            { get; set; }

        public Mobile(Mobile toCopy)
            : base(toCopy)
        {
            VelocityWanted = new Vector2(toCopy.VelocityWanted.X, toCopy.VelocityWanted.Y);
            PositionWanted = new Vector2(toCopy.PositionWanted.X, toCopy.PositionWanted.Y);
            RotationWanted = toCopy.RotationWanted;
            MaxSpeed = toCopy.MaxSpeed;
            ThrustAcceleration = toCopy.ThrustAcceleration;
            Hull = toCopy.Hull;
            MaxHull = toCopy.MaxHull;
            Mass = toCopy.Mass;
            MassInv = toCopy.MassInv;
            BoundingBox = new Rectangle(toCopy.BoundingBox.X, toCopy.BoundingBox.Y, toCopy.BoundingBox.Width, toCopy.BoundingBox.Height);
            AIRoutine = toCopy.AIRoutine.GetCopy();
            AIRoutine.SetParent(this);
            CanCollide = toCopy.CanCollide;
            CollideWithOwnSide = toCopy.CollideWithOwnSide;
            DampenInnertia = toCopy.DampenInnertia;
            IsDestroyed = false;
            Speed = toCopy.Speed;
            DestructionAnimation = toCopy.DestructionAnimation;

        }

        public Mobile(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass) 
            : base(model, side, pos, rot)
        {
            Type = EntityType.MOBILE;
            Hull = hull;
            MaxHull = Hull;
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
            MaxHull = Hull;
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
            DestructionAnimation = info.DestructionAnimation;
        }

        public void SetTexture(Texture2D model)
        {
            Model = model;
            BoundingBox = new Rectangle(model.Bounds.X, model.Bounds.Y, model.Bounds.Width, model.Bounds.Height);
        }

        public void Repair()
        {
            Hull = MaxHull;
        }

        public float GetHull()
        {
            return Hull;
        }

        virtual public Mobile GetCopy()
        {
            return new Mobile(this);
        }

        public override bool KeepInBounds()
        {
            return CanCollide;
        }

        public override void Update(double deltaT)         
        {            
            if (AtWarp)
            {
                Vector2 centerPosition = GetCenterPosition();
                Vector2 vecToTarget = WarpTarget - centerPosition;
                float dist = vecToTarget.Length();
                if (dist < 60)
                {
                    EngageWarp(false);
                }
                else if (dist > 5000)
                    dist = 5000;
                vecToTarget.Normalize();
                SetVelocity(vecToTarget * dist *5 );
            }
            else
            {
                base.Update(deltaT);
                SpeedUpToWanted(deltaT);
                AIRoutine.DoLogic(deltaT);
            }
            
        }

        // Force is in newtons
        public void ApplyForce(Vector2 force)
        {
            Vector2 velocityChange = force * MassInv * Elasticity;
            Vector2 newVelocity = GetVelocity() + velocityChange;
            SetVelocity(newVelocity);
        }

        public override void SetVelocity(float x, float y)
        {
            base.SetVelocity(x, y);
            
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

        public override void Draw(SpriteBatch spriteBatch, Vector2 screenPos, float scale)
        {
            Texture2D tex = Model;
            Vector2 vecScale = new Vector2(1, 1);
            // We want to stretch and keep the front of the ship at the same position
            Vector2 offset = new Vector2(tex.Width / 2, tex.Height / 2);
            if (AtWarp)
            {
                Speed = Velocity.Length();
                bool scaleY = Speed > 2400;                                
                if (scaleY)
                    vecScale.Y *= Speed / 2400;
            }
            spriteBatch.Draw(tex, screenPos, null, Color.White, -Rotation, GetOrigin(), scale * vecScale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1);           
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
            if ((Velocity.X == 0 && Velocity.Y == 0) || AtWarp)
                return;
            float speed = Velocity.Length();
            if (speed > MaxSpeed)
            {
                float ratio = MaxSpeed / speed;
                Velocity *= ratio;
            }
            Speed = speed;
        }

        // Used for vectors calculated in world space i.e. from ship to ship
        public override void RotateToWorldVector(Vector2 dir)
        {
            // Rotation is in screen space (because I'm stupid sometimes) so we need to translate from screen to world
            // Basically, we need to move the angle to the previous quadrant, i.e. subtract 90 degrees or pi/2
            dir.Normalize();
            float rotationWanted = (float)Math.Atan2(dir.X, dir.Y) + (float)(Math.PI / 2F);
            //rotationWanted += (float)Math.PI / 2;
            if (rotationWanted < 0)
                rotationWanted += (float)Math.PI * 2;
            RotationWanted = rotationWanted;
        }

        public void SetRotationWantedToWorldVector(Vector2 dir)
        {
            dir.Normalize();
            RotationWanted = (float)Math.Atan2(dir.X, dir.Y) + (float)(Math.PI / 2F);
        }

        // Used for joysticks
        public override void RotateToScreenVector(Vector2 dir)
        {
            dir.Normalize();
            float rotationWanted = (float)Math.Atan2(dir.Y, dir.X) - (float)(Math.PI / 2F);
            //rotationWanted += (float)Math.PI / 2;
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

        public void EngageWarp(bool warp)
        {
            AtWarp = warp;
            CanCollide = !warp;
            StretchOnVelocity = warp;
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
        public Pickup(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass)
            : base(model, side, pos, rot, hull, mass)
        {

        }

        public Pickup(Pickup toCopy)
            :base(toCopy)
        {
            
        }

        override public Mobile GetCopy()
        {
            return new Pickup(this);
        }

        public virtual void PickupLogic(Mobile collidedWith)
        {

        }

        public override void OnCollide(Mobile collidedWith)
        {
            if (collidedWith == Program.GGame.GetPlayerVessel())
            {
                PickupLogic(collidedWith);
            }
        }
    }

    public class Ancestor : Pickup
    {
        bool PickedUp = false;
        public Ancestor(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass)
            : base(model, side, pos, rot, hull, mass)
        {

        }

        public Ancestor(Pickup toCopy)
            :base(toCopy)
        {
            
        }

        override public Mobile GetCopy()
        {
            return new Ancestor(this);
        }

        public override void PickupLogic(Mobile collidedWidth)
        {
            if (!PickedUp)
            {
                int ancs = Program.GGame.AncestorsRetrieved;
                if (ancs == 0)
                {
                    // first time pickup stuff
                    Program.GGame.AncestorDialog();
                }
                Program.GGame.AncestorsRetrieved++;
                PickedUp = true;
            }
            IsDestroyed = true;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (!PickedUp)
                Program.GGame.RandomizeGraphics();
        }
    }

    /*
     * Projectile are bullets, missiles, etc.
     * */
    public class Projectile : Mobile
    {
        public float BaseDamage               // Base damage the projectile does to a target on collision
            { get; set; }
        public float ExplosionDamage;
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
        public bool Seeking = false;
        public Projectile(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass)
            : base(model, side, pos, rot, hull, mass)
        {
            Type = EntityType.PROJECTILE;
            DampenInnertia = false;
            TimeToLive = 6;
            InitTime = Environment.TickCount;
            DeflectionChance = 0.0F;
        }

        public Projectile(ProjectileInfo info, Vector2 pos, float rot)
            : base(info, pos, rot)
        {
            Type = EntityType.PROJECTILE;
            DampenInnertia = false;
            TimeToLive = info.TimeToLive;
            InitTime = Environment.TickCount;
            DeflectionChance = 0;
            BaseDamage = info.BaseDamage;
            ExplosionRadius = info.ExplosionRadius;
            ExplosionDamage = info.ExplosionDamage;
            Hull = info.Hull;
            Seeking = info.Seeking;
            Mass = info.Mass;
            MassInv = info.MassInv;
            Acceleration = info.Acceleration * new Vector2((float)Math.Cos(rot), -(float)Math.Sin(rot));
        }

        // Our base implementation of OnCollide is to do damage like a regular bullet and then disappear
        public override void OnCollide(Mobile collidedWith)
        {
            // Only destroy the object if it did not deflect
            if (collidedWith.Type != EntityType.PROJECTILE)
            {
                float randNum = (float)RNG.Generator.NextDouble();
                if (randNum > DeflectionChance)
                {
                    IsDestroyed = true;
                }
                else
                {
                    // Deflection should have already occurred, align our sprite with the new direction

                }
            }
            collidedWith.ApplyDamage(BaseDamage);
        }

        public override void OnDestroy() 
        {
            if (ExplosionRadius > 0 && ExplosionDamage > 0)
            {
                Vector2 centerPos = GetCenterPosition();
                List<Mobile> mobs = Program.GGame.GetMobsWithinRadius(centerPos, ExplosionRadius);
                foreach (Mobile mob in mobs)
                {
                    float distance = (centerPos - mob.GetCenterPosition()).Length();
                    float appliedDamage = distance / ExplosionRadius * ExplosionDamage;
                    if (CollideWithOwnSide || Side != mob.Side)
                    mob.ApplyDamage(appliedDamage);
                }
            }
        }

        public override void Update(double deltaT)
        {
            Velocity += Acceleration * 1000F * (float)deltaT;
            if (Seeking)
            {
                Vector2 pos = GetCenterPosition();
                Mobile closestMob = Program.GGame.GetClosestMob(pos, Side);

                if (closestMob != null)
                {
                    Vector2 dir = (closestMob.GetCenterPosition() - pos);
                    dir.Normalize();
                    VelocityWanted = dir * MaxSpeed;
                    SpeedUpToWanted(deltaT);
                }
            }
        }
    }

    class SeekingProjectile : Projectile
    {
        SeekingProjectile(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass)
            : base(model, side, pos, rot, hull, mass)
        {

        }

        public override void Update(double deltaT)
        {
            base.Update(deltaT);
            Vector2 pos = GetCenterPosition();
            List<Mobile> closeEntities = Program.GGame.GetMobsWithinRadius(pos, 1000);
            float closestDist = 1000;
            Mobile closestMob = null;
            foreach (Mobile mob in closeEntities)
            {
                if (mob.Type == EntityType.PROJECTILE || mob.Side == Side)
                    continue;
                float distance = (float)(mob.GetCenterPosition() - pos).Length();
                if (distance < closestDist)
                {
                    closestMob = mob;
                    closestDist = distance;
                }
            }
            if (closestMob != null)
            {
                Vector2 dir = (closestMob.GetCenterPosition() - pos);
                dir.Normalize();
                RotateToWorldVector(dir);
            }

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
        public float ShotTimer;                            // Set to CurrentWeapon.ShotDelay when shot, don't fire until reaches 0

        public bool SpawnAncestor = false;

        public Vessel(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass, float armor = 0)
            : base(model, side, pos, rot, hull, mass)
        {
            RotationSpeed = 6 * (float)Math.PI;
            Armor = armor;
            Type = EntityType.VESSEL;
        }

        public Vessel(Vessel toCopy)
            :base (toCopy)
        {
            RotationSpeed = toCopy.RotationSpeed;
            Armor = toCopy.Armor;
            Type = toCopy.Type;
            CurrentWeapon = toCopy.CurrentWeapon;
            SpawnAncestor = toCopy.SpawnAncestor;
        }

        override public Mobile GetCopy()
        {
            return new Vessel(this);
        }

        public Vessel() {  }

        public override void Update(double deltaT) 
        {
            base.Update(deltaT);
            RotateToWanted(deltaT);
            SpeedUpToWanted(deltaT);
            if (ShotTimer > 0)
                ShotTimer -= (float)deltaT;
            else ShotTimer = 0;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (SpawnAncestor)
            {
                Ancestor newAncestor = new Ancestor(Program.GGame.Ancestor);
                newAncestor.Rotation = Rotation;
                newAncestor.RotationWanted = RotationWanted;
                newAncestor.SetCenterPosition(GetCenterPosition());

                newAncestor.SetVelocity(GetVelocity());
                Program.GGame.AddToSimulated(newAncestor);
            }
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
            if (ShotTimer <= 0 && CurrentWeapon != null)
                return true;
            return false;
        }

        public virtual Projectile Fire()
        {
            //Program.GGame.FireSound.Play();
            float angle = Rotation + (float)Math.PI / 2;
            // Calculate the position the bullet should come from.  It should be our center position and correct for projectile width.
            // Along an axis perpendicular to our heading it should move half the width of the projectile width
            Vector2 centerPos = GetCenterPosition();
            Vector2 angleVector = new Vector2((float)Math.Cos(angle), -(float)Math.Sin(angle));
            Vector2 perpHeading = new Vector2(-angleVector.Y, angleVector.X);
            perpHeading.Normalize();
           
            Vector2 projPos = centerPos;

            
            Projectile bullet = new Projectile(CurrentWeapon.Munition, projPos, angle);
            bullet.SetCenterPosition(projPos);
            ShotTimer = CurrentWeapon.ShotDelay;
            bullet.SetVelocity(angleVector * 2 * CurrentWeapon.InitSpeed + GetVelocity());
            bullet.SetVelocityWanted(angleVector * 0);
            bullet.Side = Side;
            bullet.Rotation = Rotation;
             
            return bullet;
        }
    }

    public class SuicideVessel : Vessel
    {
        public float ExplosionDamage;
        public float ExplosionRadius;

        public SuicideVessel(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass, float armor, float expDamage, float expRad)
            : base(model, side, pos, rot, hull, mass, armor)
        {
            ExplosionDamage = expDamage;
            ExplosionRadius = expRad;
        }

        public SuicideVessel(SuicideVessel toCopy)
            :base (toCopy)
        {
            ExplosionDamage = toCopy.ExplosionDamage;
            ExplosionRadius = toCopy.ExplosionRadius;
        }

        override public Mobile GetCopy()
        {
            return new SuicideVessel(this);
        }

        public override void OnCollide(Mobile collidedWith)
        {
            collidedWith.ApplyDamage(ExplosionDamage);
            IsDestroyed = true;
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
        public float InitAcceleration;

        public Weapon(ProjectileInfo munition, float speed, float delay)
        {
            Name = munition.Name;
            Munition = munition;
            InitSpeed = speed;
            ShotDelay = delay;
            
        }
    }

    public class DialogBox : Entity
    {
        private string _dialog;
        public string Dialog
        {
            get { return _dialog; }
            set {
                _dialog = value;
                Vector2 dialogSize = Program.GGame.Font1.MeasureString(Dialog);
                rect = new Rectangle(0, 0, (int)dialogSize.X + 20 + Model.Width, (int)dialogSize.Y + 20);
                rect.X = (int)Position.X;
                rect.Y = (int)Position.Y;
            }
        }

        Rectangle rect;
        public DialogBox(Texture2D portrait, Vector2 pos, string dialog)
            : base(portrait, EntitySide.NEUTRAL, pos, 0)
        {
            Dialog = dialog;
            Vector2 dialogSize = Program.GGame.Font1.MeasureString(Dialog);            
            rect = new Rectangle(0, 0, (int)dialogSize.X + 20 + 3 * Model.Width, (int)dialogSize.Y + 20);
            rect.X = (int)Position.X;
            rect.Y = (int)Position.Y;
        }

        public DialogBox(DialogBox toCopy)
            : base(toCopy)
        {
            Dialog = toCopy.Dialog;
            rect = toCopy.rect;
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 screenPos, float scale)
        {
            scale = 1;
            // Draw our rectangle
            spriteBatch.Draw(Program.GGame.BoundingBoxTexture,
                                rect,
                                null,
                                Color.Black, //colour of line
                                0,     //angle of line (calulated above)
                                new Vector2(0, 0), // point in line about which to rotate
                                SpriteEffects.None,
                                0);
            // Draw our portrait
            Texture2D tex = Model;
            Vector2 textPos = Position;
            Vector2 imagePos = Position;
            imagePos.X += Model.Width / 2;
            textPos.X += Model.Width + 5;
            spriteBatch.Draw(tex, imagePos, null, Color.White, -Rotation, GetOrigin(), scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1);    
            // Draw our text
            spriteBatch.DrawString(Program.GGame.Font1, Dialog, textPos, Color.White);
        }
    }
}
