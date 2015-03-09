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
    static class CollisionMath {
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

    class BoxWithPoints
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
    class BoundingBoxNonAligned
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
    class Entity
    {
        public enum EntitySide
        {
            PLAYER,
            ENEMY,
            NEUTRAL
        };

        public enum EntityType          // Save ourselves some processing power doing dynamic casts
        {
            ENTITY,
            MOBILE,
            PICKUP,
            PROJECTILE,
            VESSEL
        }
        public EntitySide Side;                // Lets AI know what to shoot at, and prevents enemies from colliding with one another
        public EntityType Type;                // We treat our base types differently, we can use this instead of dynamic casting
        public Texture2D Model                // The dimensions of the image make our bounding box, so cut off all the extra space
            { get; set;}
        protected Vector2 Position;  
        public float Elasticity                 // Coefficient applied to velocity when hit with a force.  Elasticity of 0 absorbs all impact, 1 bounces it all off                                    
            { get; set; }                       // So with 1, if you hit something at 100km/h you will bounce off at 100km/h in the opposite direction
        protected Vector2 Velocity;
        protected Vector2 Acceleration;
        private float _rotation;
        public float Rotation                 // In radians
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
            Origin = new Vector2(Model.Width / 2, Model.Height / 2);
            Elasticity = 0.8F;
        }

        public Entity()                 // Only here to prevent compiler errors for inherited classes, remove before production
        {

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

    class Mobile : Entity
    {
        public Vector2 VelocityWanted;         // Set VelocityWanted in the direction we want to go at max speed and handle smothing in the simulation
            
        public Vector2 PositionWanted          // During simulation this will be set to the future state before collision is done
            { get; set; }
        public float RotationWanted;
        
        public float MaxSpeed
            { get; set; }
        public float ThrustAcceleration        // Not only the "gas" if the entity wants to move, but also the "brake" in the event of stopping or inertial dampening.
            { get; set; } 
        float Hull                      // i.e. HP, health, life, etc.
            { get; set; } 
        float Mass;                     // Used in collision calculations to calculate impulse
        float MassInv;                  // Pre-calculated to avoid divide by zero
        Rectangle BoundingBox;          // Used for collision, obviously
        public bool CanCollide                 // If false, will not collide with anything
            { get; set; } 
        public bool CollideWithOwnSide         // If false, will not collide with entities of same side
            { get; set; } 
        public bool DampenInnertia             // If true, the object will automatically brake itself if not accelerating.
            { get; set; }
        public bool IsDestroyed 
            { get; set; }
        public float Speed;

        public Mobile(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass) 
            : base(model, side, pos, rot)
        {
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
            MaxSpeed = 400;
            ThrustAcceleration = 2000;
            VelocityWanted = new Vector2(0, 0);
            RotationWanted = 0;
            BoundingBox = new Rectangle(model.Bounds.X, model.Bounds.Y, model.Bounds.Width, model.Bounds.Height);
        }

        public Vector2 GetCenterPositionWanted()
        {
            Vector2 result = PositionWanted + Origin;
            return result;
        }

        public Rectangle GetBoundingBoxNonRotated()
        {
            // Return a copy so that it can't be edited 
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

        // Only until classes are filled out, do not leave in production code
        public Mobile()
        {

        }
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
        float TimeToLive;
        float InitTime;
        public Projectile(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass)
            : base(model, side, pos, rot, hull, mass)
        {
            Type = EntityType.PROJECTILE;
            DampenInnertia = false;
            TimeToLive = 6;
            InitTime = Environment.TickCount;
        }
    }

    class Vessel : Mobile
    {        
        float RotationSpeed;            // In rads/s
        float Armor;                    // Will dampen damage to the hull

        public Vessel(Texture2D model, EntitySide side, Vector2 pos, float rot, float hull, float mass, float armor = 0)
            : base(model, side, pos, rot, hull, mass)
        {
            RotationSpeed = 6 * (float)Math.PI;
            Armor = armor;
            Type = EntityType.VESSEL;
        }

        public Vessel() {  }

        public void RotateToWanted(double deltaT)
        {
            double radsToGo = Rotation - RotationWanted;
            int direction = -1;
            if (radsToGo < 0)
                direction = 1;
            radsToGo = Math.Abs(radsToGo);

            
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

        
    }

    class Shielded : Vessel
    {
        float MaxShields;               // Maximum shield amount, shields will recharge to this amount
        float Shields;                  // Current amount of shields.  Below 20%, damage bleeds through to hull
        float ShieldRechargeRate;       // Rate in shields/s of recharge
    }
}
