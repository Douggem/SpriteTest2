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
        GamePadState   OldGamepadState,                 // We will compare the new and old keyboard states so we know if a key was freshly pressed, held, or released
                        CurrentGamepadState;
        MouseState CurrentMouseState;
        KeyboardState CurrentKeyboardState;
        Queue<WaveStackItem> WaveStack;
        WaveStackItem LastItem;
        List<Entity> NonSimulated;                      // We shouldn't need random access to our game entities, so a list will suffice and be more efficient than an array.
        List<Mobile> Simulated;
        List<Mobile> ToAddSimulated;
        Vessel Player;
        public Rectangle Boundaries;                           // Game boundaries, top left should typically be 0,0
        float Scale = .5F;
        Vector2 ScreenOffset = new Vector2(0, 0);
        public Texture2D BoundingBoxTexture;
        Texture2D TracerTexture;
        Random RNG;
        public bool AcceptPlayerInput = true;

        public float FadeAmount = 0;
        float FadeSpeed = 3;
        public Rectangle FadeRectangle;
        public bool FadeOut = false;
        Texture2D FadeOutTexture;
        public SpriteFont Font1;
        public SpriteFont SmallFont;

        public SoundEffect FireSound;
        public SoundEffect HitSound;

        public int AncestorsRetrieved = 0;

        Dictionary<string, EntityInfo> EntityDic = new Dictionary<string, EntityInfo>();
        Dictionary<string, MobileInfo> MobileDic = new Dictionary<string, MobileInfo>();
        Dictionary<string, VesselInfo> VesselDic = new Dictionary<string, VesselInfo>();
        Dictionary<string, ProjectileInfo> ProjectileDic = new Dictionary<string, ProjectileInfo>();
        Dictionary<string, Weapon> WeaponDic = new Dictionary<string, Weapon>();
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = false;            
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            // For now, use screen edges as our boundary
            //Boundaries = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            FadeRectangle = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            Boundaries = new Rectangle(0, 0, 3000, 3000);
            Content.RootDirectory = "Content";

            Simulated = new List<Mobile>();
            NonSimulated = new List<Entity>();
            ToAddSimulated = new List<Mobile>();
            WaveStack = new Queue<WaveStackItem>();

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

        ProjectileInfo GatlingBullet;
        ProjectileInfo Plasma;
        ExplosionInfo LargeExplosion;
        ExplosionInfo TinyExplosion;
        ExplosionInfo MidExplosion;
        ExplosionInfo SmallExplosion;
        Weapon PlayerAutoCannon;
        Weapon PlayerGatlingGun;
        Weapon PlayerRocket;
        Weapon PlayerPlasma;
        Weapon EnemyAutoCannon;
        Weapon EnemyPlasma;
        Weapon EnemyRocket;


        AnimatedTexture LargeExplosionTexture;
        AnimatedTexture TinyExplosionTexture;
        AnimatedTexture SmallExplosionTexture;
        AnimatedTexture MidExplosionTexture;

        Texture2D RocketTexture;
        Texture2D ThinTracer;
        Texture2D PlasmaBomb;

        

        // Set this to the CURRENT background!
        public Texture2D BGNeon;
        public Vector2 BGPos = new Vector2(-1000, -500);
        Texture2D BG1;
        Texture2D BG2;
        Texture2D BG3;
        Texture2D BG4;
        Texture2D BG5;
        Texture2D BG6;
        

        List<Weapon> WeaponArray = new List<Weapon>();
        int WeaponIndex = 0;
        void InitializeWeapons()
        {
            PlayerAutoCannon = new Weapon(BasicBullet, 1600, .2F);
            ProjectileInfo BasicRocket = new ProjectileInfo("rocket", RocketTexture, 1, 500, 10);
            BasicRocket.ExplosionRadius = SmallExplosionTexture.FrameWidth;
            BasicRocket.ExplosionDamage = 10;
            BasicRocket.Acceleration = 5;
            BasicRocket.DestructionAnimation = SmallExplosionTexture;
            PlayerRocket = new Weapon(BasicRocket, 10, .33F);

            GatlingBullet = new ProjectileInfo("GatlingBullet", ThinTracer, 1, 100, 2);
            GatlingBullet.DestructionAnimation = TinyExplosionTexture;
            PlayerGatlingGun = new Weapon(GatlingBullet, 3000, 0.1F);

            Plasma = new ProjectileInfo("Plasma", PlasmaBomb, 30, 100, 1F);
            Plasma.DestructionAnimation = SmallExplosionTexture;
            PlayerPlasma = new Weapon(Plasma, 600, 0.1F);            
            //Plasma.ExplosionRadius = (float)MidExplosionTexture.FrameWidth;
            Plasma.ExplosionDamage = 0;
            Plasma.Seeking = true;
            Plasma.ThrustAcceleration = 800;
            Plasma.MaxSpeed = 1000;

            EnemyPlasma = new Weapon(Plasma, 600, 0.5F);
            EnemyAutoCannon = new Weapon(BasicBullet, 600, 0.5F);
            EnemyRocket = new Weapon(BasicRocket, 10, 0.75F);

            WeaponArray.Add(PlayerPlasma);
            WeaponArray.Add(PlayerAutoCannon);
            WeaponArray.Add(PlayerGatlingGun);
            WeaponArray.Add(PlayerRocket);
            WeaponArray.Add(PlayerRocket);            
        }

        void InitializeExplosions()
        {
            // Load the large ship explosion animation
            AnimatedTexture largeExplosionTexture = new AnimatedTexture(Vector2.Zero, 0, 3, 0.5f, false);
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
            largeExplosionTexture.Load(Content, "Exp_type_AL", 32, 60, 2);
            ExplosionInfo explosionInfo = new ExplosionInfo("Exp_1", largeExplosionTexture);

            // Load the mid ship explosion animation
            MidExplosionTexture = new AnimatedTexture(Vector2.Zero, 0, 2, 0.5f, false);
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
            MidExplosionTexture.Load(Content, "Exp_type_AL", 32, 60, 2);
            MidExplosion = new ExplosionInfo("Exp_1", MidExplosionTexture);

            // Load the small ship explosion animation
            AnimatedTexture smallExplosionTexture = new AnimatedTexture(Vector2.Zero, 0, 1, 0.5f, false);
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
            smallExplosionTexture.Load(Content, "Exp_type_AL", 32, 60, 2);
            ExplosionInfo smallExplosionInfo = new ExplosionInfo("Exp_1", smallExplosionTexture);

            // Load the tiny explosion animation (i.e. for rockets hitting)
            AnimatedTexture tinyExplosionTexture = new AnimatedTexture(Vector2.Zero, 0, 0.5F, 0.5f, false);
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
            tinyExplosionTexture.Load(Content, "Exp_type_AL", 32, 60, 2);
            ExplosionInfo tinyExplosionInfo = new ExplosionInfo("Exp_1", tinyExplosionTexture);
            
            TinyExplosion = tinyExplosionInfo;
            TinyExplosionTexture = tinyExplosionTexture;
            SmallExplosion = smallExplosionInfo;
            SmallExplosionTexture = smallExplosionTexture;
            LargeExplosion = explosionInfo;
            LargeExplosionTexture = largeExplosionTexture;
        }

        // This is a texture we can render.
        Texture2D myTexture;

        // Set the coordinates to draw the sprite at.
        Vector2 spritePosition = Vector2.Zero;

        // Store some information about the sprite's motion.
        Vector2 spriteSpeed = new Vector2(50.0f, 50.0f);
        ProjectileInfo BasicBullet;

        Vessel LargeDumbShooter;
        Vessel MedBasic;
        Vessel MedStraferPlasma;
        Vessel MedStraferCannon;
        Vessel MedStraferRocket;
        SuicideVessel SmallRammer;
        Vessel SmallRammerCannon;
        Vessel Carrier;

        public Ancestor Ancestor;

        Texture2D MedRed1;        
        Texture2D MedBlue;
        Texture2D RedSmall1;
        Texture2D RedSmall2;
        Texture2D BlueSmall;

        Texture2D PinkLarge;
        Texture2D MedPink1;
        Texture2D PinkSmall1;
        Texture2D PinkSmall2;
        Texture2D CarrierTexture;

        Texture2D Alien3;
        Texture2D Alien4;

        List<Texture2D> ShipTextures;
        
        protected void InitializeEnemies()
        {
            ShipTextures = new List<Texture2D>();
            ShipTextures.Add(MedRed1);
            ShipTextures.Add(MedBlue);
            ShipTextures.Add(RedSmall1);
            ShipTextures.Add(RedSmall2);
            ShipTextures.Add(BlueSmall);
            ShipTextures.Add(PinkLarge);
            ShipTextures.Add(MedPink1);
            ShipTextures.Add(PinkSmall1);
            ShipTextures.Add(PinkSmall2);
            ShipTextures.Add(CarrierTexture);
            ShipTextures.Add(myTexture);
            ShipTextures.Add(Alien3);
            ShipTextures.Add(Alien4);

            Ancestor = new Ancestor(BlueSmall, Entity.EntitySide.NEUTRAL, new Vector2(0, 0), 0, 30, 20);
            Ancestor.DestructionAnimation = MidExplosionTexture;

            Carrier = new Vessel(CarrierTexture, Entity.EntitySide.ENEMY, new Vector2(0, 0), 0, 200F, 1000F);
            Carrier.DestructionAnimation = MidExplosionTexture;
            Carrier.CurrentWeapon = EnemyRocket;
            Carrier.AIRoutine = new DumbShooter(Carrier, 0.11F);
            Carrier.SpawnAncestor = true;

            MedBasic = new Vessel(MedRed1, Entity.EntitySide.ENEMY, new Vector2(0, 0), 0, 100, 100, 0);

            MedStraferCannon = new Vessel(MedRed1, Entity.EntitySide.ENEMY, new Vector2(0, 0), 0, 6, 100);
            MedStraferCannon.DestructionAnimation = MidExplosionTexture;
            MedStraferCannon.CurrentWeapon = EnemyAutoCannon;
            //enemy.AIRoutine = new Rammer(enemy, 0.1);
            MedStraferCannon.AIRoutine = new StrafingShooter(MedStraferCannon, .1, 1000);
            MedStraferCannon.MaxSpeed = 2000;
            MedStraferCannon.ThrustAcceleration = 1200;
            MedStraferCannon.RotationSpeed = 5F * (float)Math.PI;
            MedStraferCannon.Elasticity = 0.1F;

            MedStraferPlasma = new Vessel(MedStraferCannon);
            MedStraferPlasma.CurrentWeapon = EnemyPlasma;
            
            MedStraferRocket = new Vessel(MedStraferCannon);
            MedStraferRocket.CurrentWeapon = EnemyRocket;

            MedStraferCannon.MaxSpeed = 1000;

            SmallRammer = new SuicideVessel(RedSmall1, Entity.EntitySide.ENEMY, new Vector2(0, 0), 0, 1, 100, 0, 6, 0);
            SmallRammer.AIRoutine = new Rammer(SmallRammer, .1);
            SmallRammer.DestructionAnimation = SmallExplosionTexture;
            SmallRammer.MaxSpeed = 1500;
            SmallRammer.ThrustAcceleration = 400;
            SmallRammer.RotationSpeed = (float)Math.PI;
            SmallRammer.CurrentWeapon = null;



            SmallRammerCannon = new SuicideVessel(SmallRammer);
            SmallRammerCannon.CurrentWeapon = EnemyAutoCannon;

            LargeDumbShooter = new Vessel(myTexture, Entity.EntitySide.ENEMY, new Vector2(0, 0), 0, 16, 500);
            LargeDumbShooter.DampenInnertia = true;
            LargeDumbShooter.DestructionAnimation = MidExplosionTexture;
            LargeDumbShooter.CurrentWeapon = EnemyAutoCannon;
            LargeDumbShooter.AIRoutine = new DumbShooter(LargeDumbShooter, 0.1);
            LargeDumbShooter.RotationSpeed = (float)Math.PI;


        }

        WaveStackEnemyCollection Rammers5;
        WaveStackEnemyCollection Rammers5r;
        WaveStackEnemyCollection Rammers5n;
        WaveStackEnemyCollection Rammers5s;
        WaveStackEnemyCollection Rammers5w;
        WaveStackEnemyCollection RammersCannon5;
        WaveStackEnemyCollection DumbShooters4;
        WaveStackEnemyCollection StraferCannons4;
        WaveStackEnemyCollection StraferPlasma4;
        WaveStackEnemyCollection LargeDumbStraferCannon;
        WaveStackEnemyCollection LargeDumbShooters4;
        WaveStackEnemyCollection CarrierCollection;

        WaveStackEnemy MidDumbshooters;
        WaveStackEnemy LargeDumbShooters;
        WaveStackEnemy SmallRammers;
        WaveStackEnemy SmallCannonrammers;
        WaveStackEnemy SmallStraferCannons;
        WaveStackEnemy SmallStraferPlasmas;
        WaveStackEnemy AncestorCarrier;
        
        protected void InitializeWaves()
        {
            LargeDumbShooters = new WaveStackEnemy(LargeDumbShooter, 0);
            SmallStraferCannons = new WaveStackEnemy(MedStraferCannon, 0);
            SmallStraferPlasmas = new WaveStackEnemy(MedStraferPlasma, 0);
            SmallRammers = new WaveStackEnemy(SmallRammer, 0.20F);
            SmallCannonrammers = new WaveStackEnemy(SmallRammerCannon, 0);
            AncestorCarrier = new WaveStackEnemy(Carrier, 0);

            DumbShooters4 = new WaveStackEnemyCollection();
            DumbShooters4.AddEnemy(LargeDumbShooters);
            DumbShooters4.AddEnemy(LargeDumbShooters);
            DumbShooters4.AddEnemy(LargeDumbShooters);
            DumbShooters4.AddEnemy(LargeDumbShooters);

            LargeDumbStraferCannon = new WaveStackEnemyCollection();
            LargeDumbStraferCannon.AddEnemy(LargeDumbShooters);
            LargeDumbStraferCannon.AddEnemy(SmallStraferPlasmas);
            LargeDumbStraferCannon.AddEnemy(LargeDumbShooters);
            LargeDumbStraferCannon.AddEnemy(SmallStraferPlasmas);

            StraferCannons4 = new WaveStackEnemyCollection();
            StraferCannons4.AddEnemy(SmallStraferCannons);
            StraferCannons4.AddEnemy(SmallStraferCannons);
            StraferCannons4.AddEnemy(SmallStraferCannons);
            StraferCannons4.AddEnemy(SmallStraferCannons);

            LargeDumbShooters4 = new WaveStackEnemyCollection();
            LargeDumbShooters4.AddEnemy(LargeDumbShooters);
            LargeDumbShooters4.AddEnemy(LargeDumbShooters);
            LargeDumbShooters4.AddEnemy(LargeDumbShooters);
            LargeDumbShooters4.AddEnemy(LargeDumbShooters);

            Rammers5 = new WaveStackEnemyCollection();
            Rammers5.AddEnemy(SmallRammers);
            Rammers5.AddEnemy(SmallRammers);
            Rammers5.AddEnemy(SmallRammers);
            Rammers5.AddEnemy(SmallRammers);
            Rammers5.AddEnemy(SmallRammers);

            Rammers5n = new WaveStackEnemyCollection(Rammers5);
            Rammers5.Direction = 0.01F;
            Rammers5w = new WaveStackEnemyCollection(Rammers5);
            Rammers5.Direction = (float)Math.PI;
            Rammers5s = new WaveStackEnemyCollection(Rammers5);
            Rammers5.Direction = (float)Math.PI * 0.5F;

            Rammers5r = new WaveStackEnemyCollection();
            Rammers5r.AddEnemy(SmallRammers);
            Rammers5r.AddEnemy(SmallRammers);
            Rammers5r.AddEnemy(SmallRammers);
            Rammers5r.AddEnemy(SmallRammers);
            Rammers5r.AddEnemy(SmallRammers);
            Rammers5r.Direction = 0;

            RammersCannon5 = new WaveStackEnemyCollection();
            RammersCannon5.AddEnemy(SmallCannonrammers);
            RammersCannon5.AddEnemy(SmallCannonrammers);
            RammersCannon5.AddEnemy(SmallCannonrammers);
            RammersCannon5.AddEnemy(SmallCannonrammers);
            RammersCannon5.AddEnemy(SmallCannonrammers);

            StraferPlasma4 = new WaveStackEnemyCollection();
            StraferPlasma4.AddEnemy(SmallStraferPlasmas);
            StraferPlasma4.AddEnemy(SmallStraferPlasmas);
            StraferPlasma4.AddEnemy(SmallStraferPlasmas);
            StraferPlasma4.AddEnemy(SmallStraferPlasmas);

            CarrierCollection = new WaveStackEnemyCollection();
            CarrierCollection.AddEnemy(AncestorCarrier);
        }

        Texture2D JohnSmith;
        Texture2D JohnSmithFuture;
        Texture2D Rolfe;
        Texture2D Ratcliffe;
        Texture2D Nakoma;

        DialogBox SmithDialog;
        DialogBox RolfeDialog;
        DialogBox RatcliffeDialog;
        DialogBox NakomaDialog;

        Texture2D Asteroid;
        void InitializeDialogTemplates()
        {
            SmithDialog = new DialogBox(JohnSmithFuture, new Vector2(20, 600), "template");
            RolfeDialog = new DialogBox(Rolfe, new Vector2(20, 600), "template");
            RatcliffeDialog = new DialogBox(Ratcliffe, new Vector2(20, 600), "template");
            NakomaDialog = new DialogBox(Nakoma, new Vector2(20, 600), "template");
        }

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
            RocketTexture = Content.Load<Texture2D>("rocket");
            ThinTracer = Content.Load<Texture2D>("TracerThin");
            PlasmaBomb = Content.Load<Texture2D>("plasma");
            BG1 = Content.Load<Texture2D>("BGNeon");
            BGNeon = BG1;
            MedRed1 = Content.Load<Texture2D>("medRed1");            
            MedBlue = Content.Load<Texture2D>("medBlue");
            RedSmall1 = Content.Load<Texture2D>("redSmall1");
            RedSmall2 = Content.Load<Texture2D>("redSmall2");
            BlueSmall = Content.Load<Texture2D>("smallBlue");
            JohnSmith = Content.Load<Texture2D>("johnsmith");
            JohnSmithFuture = Content.Load<Texture2D>("JohnSmithFuture");
            Rolfe = Content.Load<Texture2D>("Rolfe");
            Ratcliffe = Content.Load<Texture2D>("ratcliffe");
            Nakoma = Content.Load<Texture2D>("nakoma");
            Asteroid = Content.Load<Texture2D>("asteroid");

            PinkSmall1 = Content.Load<Texture2D>("aliensprite2");
            MedPink1 = Content.Load<Texture2D>("alien4");
            PinkSmall2 = Content.Load<Texture2D>("aliensprite2");
            PinkLarge = Content.Load<Texture2D>("alien2");
            CarrierTexture = Content.Load<Texture2D>("alienspaceship");
            Alien3 = Content.Load<Texture2D>("alien3");
            Alien4 = Content.Load<Texture2D>("alien1");

            BG2 = Content.Load<Texture2D>("bg2");
            BG3 = Content.Load<Texture2D>("bg3");
            BG4 = Content.Load<Texture2D>("bg4");
            BG5 = Content.Load<Texture2D>("bg5");
            BG6 = Content.Load<Texture2D>("bg6");

            HitSound = Content.Load<SoundEffect>("mmHit");
            FireSound = Content.Load<SoundEffect>("mmFire");

            Font1 = Content.Load<SpriteFont>("SpriteFont1");
            SmallFont = Content.Load<SpriteFont>("SpriteFont2");

            // TODO: use this.Content to load your game content here            

            InitializeExplosions();
            BasicBullet = new ProjectileInfo("BasicBullet", TracerTexture, 0, 100, 6);
            BasicBullet.DestructionAnimation = TinyExplosionTexture;
            
            InitializeWeapons();
            InitializeEnemies();
            InitializeWaves();
            InitializeDialogTemplates();
            Vessel player = new Vessel(MedBlue, Entity.EntitySide.PLAYER, new Vector2(600, 600), 0, 100, 100);
            player.SetVelocity(50, 50);
            Simulated.Add(player);
            Player = player;
            Player.MaxSpeed = 600;
            Player.DestructionAnimation = LargeExplosionTexture;
            
            Vessel enemy = new Vessel(myTexture, Entity.EntitySide.ENEMY, new Vector2(200, 200), 0, 16, 100);
            enemy.DampenInnertia = false;
            enemy.DestructionAnimation = LargeExplosionTexture;
            //Simulated.Add(enemy);
            // For hitbox drawing            
            BoundingBoxTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            BoundingBoxTexture.SetData(new[] { Color.White }); // so that we can draw whatever color we want on top of it

            FadeOutTexture = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            FadeOutTexture.SetData(new[] { Color.White });

            ProjectileDic.Add(BasicBullet.Name, BasicBullet);
            Weapon wep = new Weapon(BasicBullet, 800, .25F);
            //Weapon enemyWep = new Weapon(BasicBullet, 800, 1);
            Weapon enemyWep = EnemyAutoCannon;
            WeaponDic.Add(wep.Name, wep);
            Player.CurrentWeapon = WeaponArray[WeaponIndex];
            enemy.CurrentWeapon = enemyWep;
            //enemy.AIRoutine = new Rammer(enemy, 0.1);
            enemy.AIRoutine = new DumbShooter(enemy, .1);
            enemy.MaxSpeed = 2000;
            enemy.ThrustAcceleration = 1200;
            enemy.RotationSpeed = 5F * (float)Math.PI;
            enemy.Elasticity = 0.1F;
            Vessel enemy2 = new Vessel(enemy);
            enemy2.SetCenterPosition(400, 400);
            //Simulated.Add(enemy2);
            List<WaveStackEnemy> wave = new List<WaveStackEnemy>();
            WaveStackEnemy en1 = new WaveStackEnemy(MedStraferCannon, 0F);
            WaveStackEnemy en2 = new WaveStackEnemy(MedStraferPlasma, (float)Math.PI);
            wave.Add(LargeDumbShooters);
            wave.Add(LargeDumbShooters);
            wave.Add(LargeDumbShooters);
            wave.Add(LargeDumbShooters);
            wave.Add(LargeDumbShooters);

            Entity asteroid = new Entity(Asteroid, Entity.EntitySide.NEUTRAL, new Vector2(900, 2500), 0);

            // Set up waves for level 1
            WaveStack.Enqueue(new WaveStackItem());
            WaveStack.Enqueue(new WaveStackEntity(asteroid));
            WaveStack.Enqueue(new WaveStackPlaySong(Content.Load<Song>("rollermobster")));
            
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "We're approaching the source of the signal, Captain."));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "It's coming from that asteroid sir."));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Thank you chief.  Any ideas on who made it?"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 2000, "It's not a technology our systems recognize..."));
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "I'm reading incoming warp signatures, sir!"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "The incoming ships match the signature of the signal beacon"));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 2000, "Battlestations!  Red alert!"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "They're coming in hot sir, I their weapons are armed."));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 2000, "Ratcliffe, get us moving,"));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "I don't want us to be standing still when they get here."));
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 1000, "Aye sir!"));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Rolfe, ready all weapons and standby to engage..."));
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "I know what to do, this isn't my time in combat, Captain"));
            
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 2000, "Ten seconds to contact, Captain!"));// 39
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 2000, "Equal power to shields, weapons...")); // 41
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 2000, "Six seconds!"));// 43
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 2000, "and engines, shields equal distribution on all octants!")); // 45            
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 2000, "Contact imminent!"));// 47*/
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());            
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackSleep(3000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "The material in their ships is thousands of years old..."));
            WaveStack.Enqueue(new WaveStackSleep(2000));
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "This doesn't make any sense, it's like they came out of a hibernation?"));
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "These cheeky bastards don't know who they're messing with!"));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "I can't wait to tell Command about this great victory!"));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "I bet I get a promotion! And a commission!"));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbStraferCannon);
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "What ship do you think I'll get?  The Zenith?  Maybe the McCormick?"));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbStraferCannon);
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "We're all very impressed, Commander, but please focus on your station.")); 
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(LargeDumbStraferCannon);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5s);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5w);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Sir I have no explanation for where these things came from"));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "It's like they were just hiding in an asteroid belt or something."));
            WaveStack.Enqueue(new WaveStackSleep(6000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackSleep(6000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackSleep(2000));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Sir, the beacon's energy output is rising..."));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Sir!  The beacon's locked on to us!"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Hull integrity is critical, our matter is phasing into another plane?"));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Ratcliffe get us out of here!"));
            WaveStack.Enqueue(new WaveStackStopSong());
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "I can't sir the helm's not responding!"));
            WaveStack.Enqueue(new WaveStackFadeOut());            
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(new WaveStackClearEntities());
            
            // Level 2 - 2 ancestors possible            
            WaveStack.Enqueue(new WaveStackPlaySong(Content.Load<Song>("futureclub")));
            WaveStack.Enqueue(new WaveStackGraphicsChange());
            WaveStack.Enqueue(new WaveStackChangeBackground(BG5, new Vector2(-3000, -1000)));            
            WaveStack.Enqueue(new WaveStackFadeIn());            
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 2000, "What happened?  Report!"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Hull integrity is back to normal, we made it out alright."));
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "I'm not sure where we are yet, the system is still calibrating."));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Alright, standby and once we know where we are we'll plot a course home."));
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "Long range sensors are busted up, Nakoma can you get someone on it?"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 1500, "Aye sir."));            
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackSleep(2000));
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "Well god damn it."));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 2000, "What, they're pink now?"));            
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "These aren't vessels, they're organic sir."));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());            
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Nakoma that doesn't make any sense."));
            WaveStack.Enqueue(new WaveStackSleep(6000));
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "I don't know what else to say Captain, they're alive."));            
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "Sir, cartography has no match for this region."));
            WaveStack.Enqueue(new WaveStackSleep(2000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "I think we've gone through some temporal rift, sir"));            
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Astrometrics shows that we could be in the same region but"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "about 6,000 years in the past."));            
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "This just keeps getting better and better."));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbStraferCannon);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Sir, I'm reading a human signature onboard a nearby alien lifeform!"));
            WaveStack.Enqueue(AncestorCarrier);
            WaveStack.Enqueue(new WaveStackSleep(3000));
            WaveStack.Enqueue(StraferPlasma4);            
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(2000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());            
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);            
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "The human was an ancestor of a member of the crew"));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "He says the aliens are trying to wipe out our ancestors"));            
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "In order to stop us from finding their beacon in the future."));
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "I failed temporal systems at the Academy so I don't know how that works."));
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "If we don't save all of our ancestors, it could change the timeline"));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "So try not to shoot the escape pods, OK Rolfe?"));
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 1500, "..."));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());            
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5s);            
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(Rammers5w);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(Rammers5r);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(AncestorCarrier);
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "Sir, another carrier."));
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Ratcliffe, take us to wherever these alien things are coming from."));
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "Aye sir, preparing for warp..."));
            WaveStack.Enqueue(new WaveStackStopSong());
            WaveStack.Enqueue(new WaveStackFadeOut());

            // Level 3 - 4 Ancestors possible
            WaveStack.Enqueue(new WaveStackPlaySong(Content.Load<Song>("perv")));
            WaveStack.Enqueue(new WaveStackChangeBackground(BG6, new Vector2(-3000, -4000)));
            WaveStack.Enqueue(new WaveStackFadeIn());
            WaveStack.Enqueue(new WaveStackSleep(3000));
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "This is where the enemy have been coming from, sir."));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Well I don't have any other ideas so let's blow them up I guess."));
            WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "I was hoping you'd say that!"));
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Incoming!"));
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(AncestorCarrier);
            WaveStack.Enqueue(new WaveStackSleep(8000));
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5s);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(Rammers5w);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));            
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(1000));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackSleep(3000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(2000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(AncestorCarrier);
            WaveStack.Enqueue(Rammers5);
            WaveStack.Enqueue(new WaveStackSleep(500));
            WaveStack.Enqueue(Rammers5s);
            WaveStack.Enqueue(new WaveStackSleep(500));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(Rammers5w);
            WaveStack.Enqueue(new WaveStackSleep(500));
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(new WaveStackSleep(3000));
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(RammersCannon5);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(LargeDumbShooters4);
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(StraferCannons4);
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(StraferPlasma4);
            WaveStack.Enqueue(new WaveStackSleep(4000));
            WaveStack.Enqueue(AncestorCarrier);
            WaveStack.Enqueue(Rammers5n);
            WaveStack.Enqueue(new WaveStackWaitForEnemiesDestroyed());
            WaveStack.Enqueue(new WaveStackStopSong());
            WaveStack.Enqueue(new WaveStackSleep(8000));
            WaveStack.Enqueue(new WaveStackEnding());
        }

        public void ChangeGraphics()
        {
            MedBasic.SetTexture(MedPink1);
            MedStraferCannon.SetTexture(MedPink1);
            MedStraferPlasma.SetTexture(MedPink1);
            MedStraferRocket.SetTexture(MedPink1);
            SmallRammer.SetTexture(PinkSmall1);
            SmallRammerCannon.SetTexture(PinkSmall2);
            LargeDumbShooter.SetTexture(PinkLarge);
        }

        public void RandomizeGraphics()
        {            
            MedBasic.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
            MedStraferCannon.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
            MedStraferPlasma.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
            MedStraferRocket.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
            SmallRammer.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
            SmallRammerCannon.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
            LargeDumbShooter.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
            //Carrier.SetTexture(ShipTextures[RNG.Next(ShipTextures.Count)]);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected void HandleWaveQueue()
        {
            if (WaveStack.Count > 0)
            {
                WaveStackItem currentStackItem = WaveStack.Peek();
                currentStackItem.Execute();
                if (currentStackItem.IsComplete())
                {
                    LastItem = WaveStack.Dequeue();                    
                }
            }
        }

        public void AncestorDialog()
        {
            var oldQueue = WaveStack.ToArray();
            WaveStack.Clear();
            // Don't interrupt the currently running item
            WaveStack.Enqueue(oldQueue[0]);
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Ratcliffe!  Why did you run into them?!"));
            WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "I...I thought that's how we picked things up?"));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Luckily we beamed them aboard before you rammed them."));
            WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "But if that's how you want to do it, then go ahead, Ratcliffe."));
            
            
            for (int i = 1; i < oldQueue.Length; i++ )
            {
                WaveStack.Enqueue(oldQueue[i]);
            }
        }

        public int NumberOfSide(Entity.EntitySide side)
        {
            int num = 0;
            foreach (Mobile mob in Simulated)
            {
                if (mob.Side == side && mob.Type != Entity.EntityType.PROJECTILE && mob.Type != Entity.EntityType.PICKUP)
                    num++;
            }
            return num;
        }
        

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        bool ChangeWeaponDown = false;
        Vessel NewPlayer;
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
            bool zoomOut = CurrentGamepadState.IsButtonDown(Buttons.RightTrigger);
            bool zoomIn = CurrentGamepadState.IsButtonDown(Buttons.LeftTrigger);
            bool debugWeaponChange = CurrentGamepadState.IsButtonDown(Buttons.LeftShoulder);
            if (!debugWeaponChange)
                ChangeWeaponDown = false;

            // Only accept input if we...are accepting input
            if (AcceptPlayerInput && Player != null && !Player.AtWarp)
            {
                if (!(rightStick.X == 0 && rightStick.Y == 0))
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

                if (zoomIn)
                    Scale /= 0.99F;
                if (zoomOut)
                    Scale *= 0.99F;
                if (Scale < 0.33)
                    Scale = 0.33F;
                

                if (fireButton)
                {
                    if (Player.CanFire())
                    {
                        Projectile bullet = Player.Fire();
                        Simulated.Add(bullet);
                    }                    
                }

                if (debugWeaponChange && !ChangeWeaponDown) 
                {
                    Player.CurrentWeapon = WeaponArray[WeaponIndex++ % (WeaponArray.Count - 1)];
                    ChangeWeaponDown = true;
                }
            }

            // Add queued entities to the simulation
            foreach (Mobile ent in ToAddSimulated)
            {
                Simulated.Add(ent);
            }

            if (Player == null || Player.IsDestroyed)
            {
                // We need to respawn
                Vessel player = new Vessel(MedBlue, Entity.EntitySide.PLAYER, new Vector2(600, 600), 0, 100, 100);
                player.SetVelocity(50, 50);
                Simulated.Add(player);

                player.MaxSpeed = 600;
                player.DestructionAnimation = LargeExplosionTexture;                
                player.CurrentWeapon = PlayerAutoCannon;
                player.SetCenterPosition(-20000, -20000);
                player.EngageWarp(true);
                player.WarpTarget = Player.GetCenterPosition();
                Vector2 dir = player.WarpTarget - player.GetCenterPosition();
                dir.Normalize();
                player.RotateToWorldVector(dir);
                player.RotationWanted += (float)Math.PI / 2;

                player.Rotation = player.RotationWanted;
                player.CurrentWeapon = Player.CurrentWeapon;
                Player = player;
                AddToSimulated(Player);
                Deaths++;
                /*NewPlayer = new Vessel(Player);
                NewPlayer.Repair();
                NewPlayer.IsDestroyed = false;
                NewPlayer.Rotation = 0;
                NewPlayer.SetPosition(new Vector2(400, 400));
                NewPlayer.PositionWanted = NewPlayer.GetCenterPosition();
                //NewPlayer.EngageWarp(true);
                NewPlayer.WarpTarget = Player.GetCenterPosition();
                Player = NewPlayer;
                AddToSimulated(Player);*/
            }

            ToAddSimulated.Clear();

            Simulate(gameTime);
            HandleWaveQueue();
            base.Update(gameTime);
        }

        public Vector2 WorldToScreen(Vector2 pos)
        {
            // ScreenOffset is in screenspace, pos is in worldspace.  Translate pos with scale to put in ScreenSpace
            // then apply ScreenOffset
            // Do scale transform
            Vector2 translatedPos = pos * Scale;
            // Do offset transform
            translatedPos -= ScreenOffset;
            return translatedPos;
        }

        public Vector2 ScreenToWorld(Vector2 pos)
        {
            pos += ScreenOffset;
            Vector2 translatedPos = pos / Scale;
            return translatedPos;
        }

        void SimulateEntity(Entity a, Entity b)
        {

        }

        public void AddToNonSimulated(Entity ent)
        {
            NonSimulated.Add(ent);
        }

        public void ClearEntities()
        {
            NonSimulated.Clear();
        }

        public void RemoveFromNonSimulated(Entity ent)
        {
            NonSimulated.Remove(ent);
        }

        public void DoEnding()
        {
            if (AncestorsRetrieved >= 5)
            {
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Hm.  Well now what?"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "The ancestors we've retrieved have an idea."));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "They say they're familiar with the aliens' hibernation technology"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "They can put us in stasis and we can awaken back in our own time,"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "after the other us gets sent back in time."));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Do you really think that will work?"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Do we really have another choice?"));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "I suppose not.  Well then, make it so."));
                WaveStack.Enqueue(new WaveStackFadeOut());
                WaveStack.Enqueue(new WaveStackSleep(4000));
                WaveStack.Enqueue(new WaveStackChangeBackground(BG1, new Vector2(-3000, -1000)));                
                WaveStack.Enqueue(new WaveStackSleep(4000));
                WaveStack.Enqueue(new WaveStackFadeIn());
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Ugh...everybody awake?"));
                WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "I feel like I fell off a cliff into a cactus."));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "Well you DID just sleep 6,000 years."));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Did it work?  Are we in the right time?"));
                WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "Astrometrics shows us in the correct place and time, sir."));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Well, I'm looking forward to getting home, then."));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Lay in course for earth, Ratcliffe."));
                WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "Aye sir"));
                WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "I can't wait for my commission.  You think I'll get a carrier?"));
                WaveStack.Enqueue(new WaveStackFadeOut());
            }
            else
            {
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Hm.  Well now what?"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "The ancestors say the aliens have a hibernation technology,"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "but the only person who knew how to use it"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "was taken by the aliens and hasn't been seen since."));                
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Do you think he was that pod that got destroyed?"));
                WaveStack.Enqueue(new WaveStackDialog(NakomaDialog, 3000, "There's no way to know, Captain."));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "I suppose we're stuck here, then."));
                WaveStack.Enqueue(new WaveStackDialog(RolfeDialog, 3000, "Surely there is a way home!"));
                WaveStack.Enqueue(new WaveStackDialog(RatcliffeDialog, 3000, "We can just wait, it's only about 6,000 years, right?"));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Now's not the time for snark, Ratcliffe."));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "Let's start looking for our missing man."));
                WaveStack.Enqueue(new WaveStackDialog(SmithDialog, 3000, "I hope we can find him because, if not, we're stuck here forever."));
                WaveStack.Enqueue(new WaveStackFadeOut());                
            }
        }

        public void AddToSimulated(Mobile ent)
        {
            ToAddSimulated.Add(ent);
        }

        public Mobile GetClosestMob(Vector2 position, Entity.EntitySide excludeSide)
        {
            float closestDist = 8000000;
            Mobile closestMob = null;
            foreach (Mobile mob in Simulated)
            {
                if (mob.Type == Entity.EntityType.PROJECTILE || mob.Side == excludeSide)
                    continue;
                float distance = (float)(mob.GetCenterPosition() - position).Length();
                if (distance < closestDist)
                {
                    closestMob = mob;
                    closestDist = distance;
                }
            }
            return closestMob;
        }

        public List<Mobile> GetMobsWithinRadius(Vector2 position, float radius)
        {
            List<Mobile> mobs = new List<Mobile>();
            foreach (Mobile mob in Simulated)
            {
                if ((mob.GetCenterPosition() - position).Length() <= radius)
                    mobs.Add(mob);
            }
            return mobs;
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
            // deflectionAngle.Normalize();
            //netVel.Normalize();            
            // a will bounce on deflectionAngle, b will bounce on aToB
            // For testing, assume equal mass

            // Calculate the force for each object and apply to the other
            a.ApplyForce(-deflectionAngle * b.Mass);
            b.ApplyForce(deflectionAngle * a.Mass);

            /*a.SetVelocity(deflectionAngle * netVelLen / 2);
            b.SetVelocity(aToB * netVelLen / 2);*/            
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
                
                if (ent.Type != Entity.EntityType.PROJECTILE)
                    ent.CheckSpeed();
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
                bool keepInBounds = ent.KeepInBounds();
                
                                
                // Make sure we aren't outside of the boundaries
                if (keepInBounds)
                {
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
                    ent.OnDestroy();
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

        void DrawRectangle(SpriteBatch sb, Rectangle rect)
        {
            // DrawLine needs a start and end vector, so do this
            int left = rect.Center.X - rect.Width / 2;
            int right = rect.Center.X + rect.Width / 2;
            int top = rect.Center.Y + rect.Height / 2;
            int bottom = rect.Center.Y - rect.Height / 2;

            Vector2 topLeft = new Vector2(left, top);
            Vector2 topRight = new Vector2(right, top);
            Vector2 bottomLeft = new Vector2(left, bottom);
            Vector2 bottomRight = new Vector2(right, bottom);
            DrawLine(sb, topLeft, topRight);
            DrawLine(sb, topLeft, bottomLeft);
            DrawLine(sb, bottomLeft, bottomRight);
            DrawLine(sb, topRight, bottomRight);
        }

        void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end)
        {
            start = WorldToScreen(start);
            end = WorldToScreen(end);
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
        float FadeTimer;
        public int Deaths = 0;
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            // Draw background
            spriteBatch.Draw(BGNeon, WorldToScreen(BGPos), null, Color.White, 0, new Vector2(0, 0), 5 * Scale, SpriteEffects.None, 1);
            // Draw the scene boundaries
            DrawRectangle(spriteBatch, Boundaries);
            // Center the screen on our player
            if (Player != null && !Player.IsDestroyed && !Player.AtWarp)
            {
                Vector2 playerScreenPos = Player.GetCenterPosition();
                Vector2 newCamOffset = new Vector2(0, 0);
                newCamOffset.X = playerScreenPos.X * Scale - (graphics.PreferredBackBufferWidth / 2);               
                newCamOffset.Y = playerScreenPos.Y * Scale - (graphics.PreferredBackBufferHeight / 2);
                
                // Sanity check the cam position
                // Top left boundary is just screen to world of the camera position

                float slack = 500;
                float top = Boundaries.Y - slack * Scale;
                float bottom = Boundaries.Y + Boundaries.Height + slack;
                float left = Boundaries.X - slack * Scale;
                float right = Boundaries.X + Boundaries.Width + slack;

                Vector2 topLeft = newCamOffset;
                Vector2 bottomRight = new Vector2(newCamOffset.X + graphics.PreferredBackBufferWidth, newCamOffset.Y + graphics.PreferredBackBufferHeight) / Scale;
                if (topLeft.Y < top)
                    newCamOffset.Y = top;
                else if (bottomRight.Y > bottom)
                    newCamOffset.Y = bottom * Scale - graphics.PreferredBackBufferHeight;
                if (topLeft.X < left)
                    newCamOffset.X = left;
                else if (bottomRight.X > right)
                    newCamOffset.X = right * Scale - graphics.PreferredBackBufferWidth;

                int no = 0;
                ScreenOffset = newCamOffset;
            }
            
            // For debugging, draw hit boxes
            
            // TODO: Add your drawing code here
            // Draw the sprite.
            
            foreach (Entity ent in NonSimulated)
            {
                Vector2 screenPos = WorldToScreen(ent.GetCenterPosition());
                ent.Draw(spriteBatch, screenPos, Scale);
            }

            foreach (Mobile ent in Simulated) {                
                Vector2 screenPos = WorldToScreen(ent.GetCenterPosition());
                ent.Draw(spriteBatch, screenPos, Scale);

                // Hitbox drawing
                /*BoxWithPoints hitBox = ent.GetBoundingBoxPointsRotated();
                DrawLine(spriteBatch, hitBox.P1, hitBox.P2);
                DrawLine(spriteBatch, hitBox.P2, hitBox.P3);
                DrawLine(spriteBatch, hitBox.P3, hitBox.P4);
                DrawLine(spriteBatch, hitBox.P1, hitBox.P4);*/
            }
            

            

            // Draw the fade out box if applicable
            if (FadeAmount != 0 || FadeOut )
            {
                if (FadeOut && FadeAmount < 1)
                    FadeAmount += 0.01F;
                else if (!FadeOut)
                    FadeAmount -= 0.01F;

                spriteBatch.Draw(BoundingBoxTexture,
                                FadeRectangle,
                                null,
                                Color.Black * FadeAmount, //colour of line
                                0,     //angle of line (calulated above)
                                new Vector2(0, 0), // point in line about which to rotate
                                SpriteEffects.None,
                                0);
            }
            
            // Draw our hud
            Vector2 pos = new Vector2(0,0);
            string ancestorsRescued = "";
            string health = "";
            string deaths = "Deaths: " + Deaths;
            if (AncestorsRetrieved > 0)
            {
                ancestorsRescued = "Ancestors: " + AncestorsRetrieved.ToString();
            }
            if (Player != null)
            {
                health = "Hull: " + Player.GetHull().ToString();
            }
            spriteBatch.DrawString(Program.GGame.SmallFont, health, pos , Color.White);
            pos.Y += Program.GGame.SmallFont.MeasureString(health).Y;
            spriteBatch.DrawString(Program.GGame.SmallFont, ancestorsRescued, pos, Color.White);
            pos.Y += Program.GGame.SmallFont.MeasureString(ancestorsRescued).Y;
            spriteBatch.DrawString(Program.GGame.SmallFont, deaths, pos, Color.White);
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public Vessel GetPlayerVessel()
        {
            return Player;
        }
    }
}
