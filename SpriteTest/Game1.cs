using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        GamePadState   OldGamepadState,           // We will compare the new and old keyboard states so we know if a key was freshly pressed, held, or released
                        CurrentGamepadState;
        MouseState CurrentMouseState;
        KeyboardState CurrentKeyboardState;
        List<Entity> NonSimulated;                      // We shouldn't need random access to our game entities, so a list will suffice and be more efficient than an array.
        List<Mobile> Simulated;
        List<Mobile> ToAddSimulated;
        Vessel Player;
        Rectangle Boundaries;                       // Game boundaries, top left should typically be 0,0
        float Scale = 1.0F;
        Texture2D BoundingBoxTexture;
        Texture2D TracerTexture;
        Random RNG;

        Dictionary<string, EntityInfo> EntityDic = new Dictionary<string, EntityInfo>();
        Dictionary<string, MobileInfo> MobileDic = new Dictionary<string, MobileInfo>();
        Dictionary<string, VesselInfo> VesselDic = new Dictionary<string, VesselInfo>();
        Dictionary<string, ProjectileInfo> ProjectileDic = new Dictionary<string, ProjectileInfo>();
        Dictionary<string, Weapon> WeaponDic = new Dictionary<string, Weapon>();
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = false;            
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;
            // For now, use screen edges as our boundary
            Boundaries = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            Content.RootDirectory = "Content";

            Simulated = new List<Mobile>();
            NonSimulated = new List<Entity>();
            ToAddSimulated = new List<Mobile>();
            RNG = new Random(Environment.TickCount);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }


        // This is a texture we can render.
        Texture2D myTexture;

        // Set the coordinates to draw the sprite at.
        Vector2 spritePosition = Vector2.Zero;

        // Store some information about the sprite's motion.
        Vector2 spriteSpeed = new Vector2(50.0f, 50.0f);
        ProjectileInfo BasicBullet;
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            myTexture = Content.Load<Texture2D>("Redbig");
            TracerTexture = Content.Load<Texture2D>("Tracer");
            // TODO: use this.Content to load your game content here
            // Load the explosion animation
            AnimatedTexture explosionTexture = new AnimatedTexture(Vector2.Zero, 0, 3, 0.5f, false);
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
            explosionTexture.Load(Content, "Exp_type_AL", 32, 60, 2);
            ExplosionInfo explosionInfo = new ExplosionInfo("Exp_1", explosionTexture);

            Vessel player = new Vessel(myTexture, Entity.EntitySide.PLAYER, new Vector2(600, 600), 0, 100, 100);
            player.SetVelocity(50, 50);
            Simulated.Add(player);
            Player = player;
            
            Vessel enemy = new Vessel(myTexture, Entity.EntitySide.ENEMY, new Vector2(200, 200), 0, 5, 100);
            enemy.DampenInnertia = false;
            enemy.DestructionAnimation = explosionTexture;
            Simulated.Add(enemy);
            // For hitbox drawing            
            BoundingBoxTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            BoundingBoxTexture.SetData(new[] { Color.White }); // so that we can draw whatever color we want on top of it
            BasicBullet = new ProjectileInfo("BasicBullet", TracerTexture, 0, 100, 6);
            ProjectileDic.Add(BasicBullet.Name, BasicBullet);
            Weapon wep = new Weapon(BasicBullet, 800, .25F);
            Weapon enemyWep = new Weapon(BasicBullet, 800, 1);
            WeaponDic.Add(wep.Name, wep);
            Player.CurrentWeapon = wep;
            enemy.CurrentWeapon = enemyWep;
            enemy.AIRoutine = new DumbShooter(enemy, .1);
            enemy.MaxSpeed = 2000;
            enemy.ThrustAcceleration = 400;
            enemy.RotationSpeed = 0.5F * (float)Math.PI;
            enemy.Elasticity = 0.1F;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Get the state of the controller
            CurrentGamepadState = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.Circular);
            
            CurrentMouseState = Mouse.GetState();
            if (CurrentGamepadState.Buttons.Back == ButtonState.Pressed)
                this.Exit();
            // Get analog stick input
            Vector2 leftStick = CurrentGamepadState.ThumbSticks.Left;
            Vector2 rightStick = CurrentGamepadState.ThumbSticks.Right;
            bool fireButton = CurrentGamepadState.IsButtonDown(Buttons.RightShoulder);
            if (!(rightStick.X == 0 && rightStick.Y == 0) )
            {
                Player.RotateToScreenVector(rightStick);
            }
            if (!(leftStick.X == 0 && leftStick.Y == 0))
            {
                float mag = leftStick.Length();
                leftStick.Normalize();
                leftStick.Y *= -1;
                Player.SetVelocityWanted(Player.MaxSpeed * mag * leftStick);
            }
            else
            {
                Player.SetVelocityWanted(0, 0);
            }

            if (fireButton)
            {
                if (Player.CanFire())
                {
                    Projectile bullet = Player.Fire();
                    Simulated.Add(bullet);
                }
                /*Projectile bullet = new Projectile(TracerTexture, Entity.EntitySide.PLAYER, Player.GetCenterPosition(), Player.Rotation, 0, 1000);
                float angle = Player.Rotation + (float)Math.PI / 2;
                bullet.MaxSpeed = 400;
                Vector2 angleVector = new Vector2((float)Math.Cos(angle), -(float)Math.Sin(angle));
                bullet.SetVelocity(angleVector * 2*bullet.MaxSpeed + Player.GetVelocity());
                bullet.SetVelocityWanted(angleVector * 0);
                Simulated.Add(bullet);*/
            }

            // Add queued entities to the simulation
            foreach (Mobile ent in ToAddSimulated)
            {
                Simulated.Add(ent);
            }

            ToAddSimulated.Clear();

            Simulate(gameTime);
            base.Update(gameTime);
        }

        Vector2 WorldToScreen(Vector2 pos)
        {
            return pos;
        }

        void SimulateEntity(Entity a, Entity b)
        {

        }

        public void AddToNonSimulated(Entity ent)
        {
            NonSimulated.Add(ent);
        }

        public void AddToSimulated(Mobile ent)
        {
            ToAddSimulated.Add(ent);
        }

        Vector2 Projection(Vector2 a, Vector2 b)
        {
            return (Vector2.Dot(a, b)/ (Vector2.Dot(b, b)) * b);
        }

        void HandleCollision(Mobile a, Mobile b)
        {
            Vector2 aToB = b.GetCenterPosition() - a.GetCenterPosition();
            aToB.Normalize();               // Unit vector representing angle from A to B            
            Vector2 netVel = a.GetVelocity() - b.GetVelocity();
            if (netVel.X == 0 && netVel.Y == 0)
            {
                netVel = a.GetCenterPosition() - b.GetCenterPosition();
                netVel *= 5;
            }
            float netVelLen = netVel.Length();

            Vector2 deflectionAngle = Projection(netVel, new Vector2(-aToB.Y, aToB.X) );
            deflectionAngle.Normalize();
            //netVel.Normalize();            
            // a will bounce on deflectionAngle, b will bounce on aToB
            // For testing, assume equal mass
            a.SetVelocity(deflectionAngle * netVelLen / 2);
            b.SetVelocity(aToB * netVelLen / 2);

            a.OnCollide(b);
            b.OnCollide(a);

        }

        void SimulateCollision(double deltaT)
        {
            List<Mobile> toRemove = new List<Mobile>();
            foreach (Mobile ent in Simulated)
            {
                ent.Update(deltaT);

                /*if (ent.Type == Entity.EntityType.VESSEL)
                {
                    Vessel ves = (Vessel)ent;
                    if (ves != null)
                    {
                        ves.RotateToWanted(deltaT);
                        // Calculate collision and adjust velocity                        
                    }
                }*/

                // Update our velocity with the acceleration
                //ent.SetVelocity(ent.GetVelocity() + ent.GetAcceleration() * (float)deltaT);
                
                //ent.CheckSpeed();
                Vector2 oldPos = ent.GetCenterPosition();
                Vector2 posBack = new Vector2(oldPos.X, oldPos.Y);
                // Calculate the wanted position by adding the velocity vector times deltaT
                ent.PositionWanted = ent.GetCenterPosition() + (ent.GetVelocity() * (float)deltaT);
                // If we are a vessel, do our rotationwanted calculations here
                ent.SetCenterPosition(ent.PositionWanted);

                foreach (Mobile otherEnt in Simulated)
                {
                    // TODO: Add collision!
                    if (!ent.CanCollide)              // If collision is not enabled for the entity, do not collide
                        break;
                    if ((!ent.CollideWithOwnSide && !otherEnt.CollideWithOwnSide) && ent.Side == otherEnt.Side)
                        continue;                     // If they're on the same side and they aren't supposed to collide with same side, do not collide
                    if (ent == otherEnt)
                        continue;
                    
                    if (ent.IsCollidingWith(otherEnt))
                    {
                        HandleCollision(ent, otherEnt);
                        ent.SetCenterPosition(posBack);
                        ent.PositionWanted = posBack;
                    }
                }

                float halfWidth = ent.GetBoundingBoxNonRotated().Width / 2;
                float halfHeight = ent.GetBoundingBoxNonRotated().Height / 2;
                bool OutOfBounds = false;
                // Make sure we aren't outside of the boundaries
                if (ent.PositionWanted.X - halfWidth < Boundaries.X)            // Going through left wall
                {
                    ent.PositionWanted = new Vector2(Boundaries.X + halfWidth, ent.PositionWanted.Y);
                    ent.SetVelocity(-ent.GetVelocity().X * ent.Elasticity, ent.GetVelocity().Y);
                    OutOfBounds = true;
                }
                else if (ent.PositionWanted.X + halfWidth > Boundaries.Width)   // Going through right wall
                {
                    ent.PositionWanted = new Vector2(Boundaries.Width - halfWidth, ent.PositionWanted.Y);
                    ent.SetVelocity(-ent.GetVelocity().X * ent.Elasticity, ent.GetVelocity().Y);
                    OutOfBounds = true;
                }
                if (ent.PositionWanted.Y - halfHeight < Boundaries.Y)           // Going through top wall
                {
                    ent.PositionWanted = new Vector2(ent.PositionWanted.X, Boundaries.Y + halfHeight);
                    ent.SetVelocity(ent.GetVelocity().X, -ent.GetVelocity().Y * ent.Elasticity);
                    OutOfBounds = true;
                }
                else if (ent.PositionWanted.Y + halfHeight > Boundaries.Height) // Going through bottom wall
                {
                    ent.PositionWanted = new Vector2(ent.PositionWanted.X, Boundaries.Height - halfHeight);
                    ent.SetVelocity(ent.GetVelocity().X, -ent.GetVelocity().Y * ent.Elasticity);
                    OutOfBounds = true;
                }
                               

                // All should be well at this point, set our new position
                ent.SetCenterPosition(ent.PositionWanted);
                // Remove projectiles that have gone out of bounds
                if (ent.Type == Entity.EntityType.PROJECTILE)                
                {
                    Projectile proj = (Projectile)ent;
                    if (proj != null)
                    {
                        if (OutOfBounds)
                            toRemove.Add(proj);
                        
                        else if (Environment.TickCount - proj.InitTime > proj.TimeToLive * 1000)
                            toRemove.Add(proj);
                    }
                }

                if (ent.IsDestroyed)
                {
                    toRemove.Add(ent);
                    if (ent.SpawnOnDestroy())
                        ent.DoSpawnOnDestroy();
                }
                
            }
            foreach (Mobile ent in toRemove)
            {
                Simulated.Remove(ent);
            }
            
        }

        void Simulate(GameTime gameTime)
        {
            double deltaT = gameTime.ElapsedGameTime.TotalSeconds;           // We'll use seconds for the sake of simplicity
            List<Entity> toRemove = new List<Entity>();
            // For non-simulated entities, just move them, no collision detection needed
            foreach (Entity ent in NonSimulated)
            {
                ent.Update(deltaT);
                if (ent.Remove())
                    toRemove.Add(ent);
            }

            foreach (Entity ent in toRemove)
            {
                NonSimulated.Remove(ent);
            }
            // For each simulated entity, calculate the wanted position then do collision detection as well as draw
            
            SimulateCollision(deltaT);
        }

        void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end)
        {
            Vector2 edge = end - start;
            // calculate angle to rotate line
            float angle =
                (float)Math.Atan2(edge.Y, edge.X);


            sb.Draw(BoundingBoxTexture,
                new Rectangle(// rectangle defines shape of line and position of start of line
                    (int)start.X,
                    (int)start.Y,
                    (int)edge.Length(), //sb will strech the texture to fill this rectangle
                    1), //width of line, change this to make thicker line
                null,
                Color.Red, //colour of line
                angle,     //angle of line (calulated above)
                new Vector2(0, 0), // point in line about which to rotate
                SpriteEffects.None,
                0);

        }
        

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            // For debugging, draw hit boxes
            
            // TODO: Add your drawing code here
            // Draw the sprite.
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            foreach (Entity ent in NonSimulated)
            {
                Vector2 screenPos = WorldToScreen(ent.GetCenterPosition());
                ent.Draw(spriteBatch, screenPos, Scale);
            }

            foreach (Mobile ent in Simulated) {                
                Vector2 screenPos = WorldToScreen(ent.GetCenterPosition());
                ent.Draw(spriteBatch, screenPos, Scale);

                // Hitbox drawing
                BoxWithPoints hitBox = ent.GetBoundingBoxPointsRotated();
                DrawLine(spriteBatch, hitBox.P1, hitBox.P2);
                DrawLine(spriteBatch, hitBox.P2, hitBox.P3);
                DrawLine(spriteBatch, hitBox.P3, hitBox.P4);
                DrawLine(spriteBatch, hitBox.P1, hitBox.P4);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        public Vessel GetPlayerVessel()
        {
            return Player;
        }
    }
}
