/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.API.Maps.Processors.Scoring.Data;
using Quaver.Shared.Config;
using Quaver.Shared.Screens.Gameplay.Rulesets.Keys.HitObjects;
using Quaver.Shared.Skinning;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;

namespace Quaver.Shared.Screens.Gameplay.Rulesets.Keys.Playfield.Hits
{
    public class Hit
    {
        /// <summary>
        ///     The hit stat for this hit.
        /// </summary>
        private HitStat? HitStat { get; set; }

        /// <summary>
        /// </summary>
        private HitObjectManagerKeys Manager { get; }

        /// <summary>
        /// </summary>
        private IDictionary<Judgement, Color> JudgeColors { get; }

        /// <summary>
        /// </summary>
        private ScrollDirection ScrollDirection { get; }

        /// <summary>
        ///     Position for hitting without taking hit object height into account.
        ///     Use this to compute positions of line indicators (height = 1), such as the press and release position.
        /// </summary>
        private float LineHitPosition { get; }

        /// <summary>
        ///     The indicator line itself.
        /// </summary>
        private Sprite Indicator { get; }

        /// <summary>
        ///     Vertical line from the indicator to the perfect position.
        /// </summary>
        private Sprite LineToPerfect { get; }

        /// <summary>
        ///     The actual hit position.
        /// </summary>
        private long Position { get; set; }

        /// <summary>
        ///     Perfect position for this hit.
        /// </summary>
        private long PerfectPosition { get; set; }

        /// <summary>
        /// </summary>
        public bool Visible
        {
            set
            {
                Indicator.Visible = value;
                LineToPerfect.Visible = value;
            }
        }

        /// <summary>
        ///     Computes the hit position and the perfect hit position.
        /// </summary>
        /// <param name="hitStat"></param>
        private void ComputePositions()
        {
            var hitStat = HitStat.Value;
            var info = hitStat.HitObject;
            if (hitStat.KeyPressType == KeyPressType.Release
                || (hitStat.KeyPressType == KeyPressType.None && hitStat.Judgement == Judgement.Okay))
            {
                PerfectPosition = Manager.GetPositionFromTime(info.EndTime);

                if (hitStat.KeyPressType == KeyPressType.None)
                    Position = PerfectPosition;
                else
                    Position = Manager.GetPositionFromTime(info.EndTime - hitStat.HitDifference);
            }
            else
            {
                PerfectPosition = Manager.GetPositionFromTime(info.StartTime);

                if (hitStat.KeyPressType == KeyPressType.None)
                    Position = PerfectPosition;
                else
                    Position = Manager.GetPositionFromTime(info.StartTime - hitStat.HitDifference);
            }
        }

        /// <summary>
        ///     Creates a Hit and its sprites.
        /// </summary>
        public Hit(GameplayRulesetKeys ruleset, HitObjectManagerKeys manager, int lane)
        {
            Manager = manager;
            JudgeColors = SkinManager.Skin.Keys[ruleset.Mode].JudgeColors;

            var playfield = (GameplayPlayfieldKeys) ruleset.Playfield;
            ScrollDirection = playfield.ScrollDirections[lane];
            LineHitPosition = playfield.TimingLinePositionY[lane];

            var laneX = playfield.Stage.Receptors[lane].X;
            LineToPerfect = new Sprite
            {
                Alignment = Alignment.TopLeft,
                Position = new ScalableVector2(laneX + 0.5f * playfield.LaneSize, 0),
                Size = new ScalableVector2(2, 0),
                Alpha = Manager.ShowHits ? 1 : 0,
                Visible = false,
                Parent = playfield.Stage.HitContainer,
            };
            Indicator = new Sprite
            {
                Alignment = Alignment.TopLeft,
                Position = new ScalableVector2(laneX + 0.125f * playfield.LaneSize, 0),
                Size = new ScalableVector2(playfield.LaneSize * 0.75f, 2),
                Alpha = Manager.ShowHits ? 1 : 0,
                Visible = false,
                Parent = playfield.Stage.HitContainer,
            };
        }

        /// <summary>
        ///     Destroys the sprites.
        /// </summary>
        public void Destroy()
        {
            Indicator.Destroy();
            LineToPerfect.Destroy();
        }

        /// <summary>
        ///     Initializes the object with a new hit stat.
        /// </summary>
        /// <param name="hitStat"></param>
        public void InitializeWithHitStat(HitStat hitStat)
        {
            HitStat = hitStat;

            ComputePositions();

            var tint = JudgeColors[hitStat.Judgement];
            LineToPerfect.Tint = tint;
            Indicator.Tint = tint;
        }

        /// <summary>
        ///     Calculates the screen position offset from the initial track position and current track offset.
        /// </summary>
        /// <param name="initial"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private float GetPosition(long initial, long offset) =>
            (initial - offset) *
            (ScrollDirection.Equals(ScrollDirection.Down)
                ? -HitObjectManagerKeys.ScrollSpeed
                : HitObjectManagerKeys.ScrollSpeed) / HitObjectManagerKeys.TrackRounding;

        /// <summary>
        ///     Calculates the position of the indicator with a position offset.
        /// </summary>
        /// <returns></returns>
        private float GetIndicatorPosition(long offset) => LineHitPosition + GetPosition(Position, offset);

        /// <summary>
        ///     Calculates the position of the perfect hit with a position offset.
        /// </summary>
        /// <returns></returns>
        private float GetPerfectPosition(long offset) => LineHitPosition + GetPosition(PerfectPosition, offset);

        /// <summary>
        ///     Updates the sprite positions.
        /// </summary>
        /// <param name="offset"></param>
        public void UpdateSpritePositions(long offset)
        {
            if (HitStat == null)
                return;

            var indicatorPosition = GetIndicatorPosition(offset);
            var perfectPosition = GetPerfectPosition(offset);
            Indicator.Y = indicatorPosition;
            LineToPerfect.Height = Math.Abs(perfectPosition - indicatorPosition);
            if (indicatorPosition < perfectPosition)
                LineToPerfect.Y = indicatorPosition;
            else
                LineToPerfect.Y = perfectPosition;
        }
    }
}