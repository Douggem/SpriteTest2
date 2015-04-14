using System;

namespace SpriteTest
{
#if WINDOWS || XBOX
    static class Program
    {
        public static Game1 GGame;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (GGame = new Game1())
            {
                GGame.Run();
            }
        }
    }
#endif
}

