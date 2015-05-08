#region File Description
//-----------------------------------------------------------------------------
// AnimatedTexture.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
// Modified by Doug Confere 2015 holla
#endregion

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AnimatedSprite
{
    public class AnimatedTexture
    {
        private int framecount;
        private Texture2D myTexture;
        private float TimePerFrame;
        private int Frame;
        private float TotalElapsed;
        private bool Paused;
        
        public bool Looping;
        public float Rotation, Scale, Depth;
        public Vector2 Origin;

        public int FrameHeight;
        public int FrameWidth;
        public int FrameRows;
        public int FrameColumns;
        public AnimatedTexture(Vector2 origin, float rotation,
            float scale, float depth, bool looping = true)
        {
            this.Origin = origin;
            this.Rotation = rotation;
            this.Scale = scale;
            this.Depth = depth;
            FrameHeight = 0;
            FrameWidth = 0;
            Looping = looping;
        }
        public void Load(ContentManager content, string asset,
            int frameCount, int framesPerSec, int frameRows = 1)
        {
            framecount = frameCount;
            myTexture = content.Load<Texture2D>(asset);
            TimePerFrame = (float)1 / framesPerSec;
            Frame = 0;
            TotalElapsed = 0;
            Paused = false;
            FrameRows = frameRows;
            FrameColumns = frameCount / FrameRows;
            FrameWidth = myTexture.Width / FrameColumns;
            FrameHeight = myTexture.Height / FrameRows;
            Origin = new Vector2(FrameWidth / 2, FrameHeight / 2);
        }

        // class AnimatedTexture
        public void UpdateFrame(float elapsed)
        {
            if (Paused)
                return;
            TotalElapsed += elapsed;
            if (TotalElapsed > TimePerFrame)
            {
                Frame++;
                // Keep the Frame between 0 and the total frames, minus one.
                if (!Looping && Frame >= framecount)
                {
                    Paused = true;
                    return;
                }
                Frame = Frame % framecount;
                TotalElapsed -= TimePerFrame;
            }
        }

        public int GetFrameNumber(float totalElapsed)
        {
            int frameNum = (int)(totalElapsed / TimePerFrame);                
            // Keep the Frame between 0 and the total frames, minus one.
            if (!Looping && Frame >= framecount)
            {
                Paused = true;
                return framecount;
            }
            return frameNum;
        }

        public bool Complete(float totalElapsed)
        {
            return (!Looping && GetFrameNumber(totalElapsed) >= framecount);
        }

        public void DrawFrame(SpriteBatch batch, Vector2 screenPos, float totalTimeAlive, float scale = 1)
        {
            DrawFrame(batch, GetFrameNumber(totalTimeAlive), screenPos, scale);
        }

        // class AnimatedTexture
        public void DrawFrame(SpriteBatch batch, Vector2 screenPos, float scale = 1)
        {
            DrawFrame(batch, Frame, screenPos, scale);
        }

        public void DrawFrame(SpriteBatch batch, int frame, Vector2 screenPos, float scale = 1)
        {
            int frameWidth = myTexture.Width / framecount;
            int frameColumn = frame % FrameColumns;
            int frameRow = frame / FrameColumns;
            
            Rectangle sourcerect = new Rectangle(FrameWidth * frameColumn, frameRow * FrameHeight,
                FrameWidth, FrameHeight);
            batch.Draw(myTexture, screenPos, sourcerect, Color.White,
                Rotation, Origin, Scale * scale, SpriteEffects.None, Depth);
        }

        public bool IsPaused
        {
            get { return Paused; }
        }

        public void Reset()
        {
            Frame = 0;
            TotalElapsed = 0f;
        }
        public void Stop()
        {
            Pause();
            Reset();
        }
        public void Play()
        {
            Paused = false;
        }
        public void Pause()
        {
            Paused = true;
        }

    }
}
