// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Catch.Difficulty.Evaluators;
using osu.Game.Rulesets.Catch.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;
using osu.Game.Rulesets.Taiko.Difficulty.Preprocessing;
using osu.Game.Rulesets.Taiko.Objects;
using osuTK;

namespace PerformanceCalculatorGUI.Screens.ObjectInspection
{
    public partial class ObjectDifficultyValuesContainer : Container
    {
        [Resolved]
        private Bindable<IReadOnlyList<Mod>> appliedMods { get; set; } = null!;

        [Resolved]
        private Track track { get; set; } = null!;

        private SpriteText hitObjectTypeText = null!;

        private FillFlowContainer flowContainer = null!;

        public Bindable<DifficultyHitObject?> CurrentDifficultyHitObject { get; } = new Bindable<DifficultyHitObject?>();

        private const int hit_object_type_container_height = 50;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colors)
        {
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colors.Background5
                },
                new OsuScrollContainer
                {
                    Padding = new MarginPadding(10) { Top = hit_object_type_container_height + 10 },
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarAnchor = Anchor.TopLeft,
                    Child = flowContainer = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(10)
                    },
                },
                new Container
                {
                    Name = "Hit object type name container",
                    RelativeSizeAxes = Axes.X,
                    Height = hit_object_type_container_height,
                    Margin = new MarginPadding { Bottom = 10 },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = colors.Background6,
                            RelativeSizeAxes = Axes.Both
                        },
                        hitObjectTypeText = new OsuSpriteText
                        {
                            Font = new FontUsage(size: 30),
                            Padding = new MarginPadding(10)
                        }
                    }
                }
            };

            CurrentDifficultyHitObject.ValueChanged += h => updateValues(h.NewValue);
        }

        private void updateValues(DifficultyHitObject? hitObject)
        {
            flowContainer.Clear();

            if (hitObject == null)
            {
                hitObjectTypeText.Text = "";
                return;
            }

            hitObjectTypeText.Text = hitObject.BaseObject.GetType().Name;

            switch (hitObject)
            {
                case OsuDifficultyHitObject osuDifficultyHitObject:
                {
                    drawOsuValues(osuDifficultyHitObject);
                    break;
                }

                case TaikoDifficultyHitObject taikoDifficultyHitObject:
                {
                    drawTaikoValues(taikoDifficultyHitObject);
                    break;
                }

                case CatchDifficultyHitObject catchDifficultyHitObject:
                {
                    drawCatchValues(catchDifficultyHitObject);
                    break;
                }
            }
        }

        private void drawOsuValues(OsuDifficultyHitObject hitObject)
        {
            bool hidden = appliedMods.Value.Any(x => x is ModHidden);
            var hitObjectLast = (OsuDifficultyHitObject)hitObject.Previous(0);
            var hitObjectLastLast = (OsuDifficultyHitObject)hitObject.Previous(1);
            flowContainer.AddRange(new[]
            {
                new ObjectInspectorDifficultyValue("Position", (hitObject.BaseObject as OsuHitObject)!.StackedPosition),
                new ObjectInspectorDifficultyValue("Index", hitObject.Index),
                new ObjectInspectorDifficultyValue("Hit Window Great", hitObject.HitWindowGreat),
                new ObjectInspectorDifficultyValue("Delta Time", hitObject.DeltaTime),
                new ObjectInspectorDifficultyValue("Adjusted Delta Time", hitObject.AdjustedDeltaTime),
                new ObjectInspectorDifficultyValue("Doubletapness", hitObject.GetDoubletapness((OsuDifficultyHitObject)hitObject.Next(0))),
                new ObjectInspectorDifficultyValue("Lazy Jump Dist", hitObject.LazyJumpDistance),
                new ObjectInspectorDifficultyValue("Min Jump Dist", hitObject.MinimumJumpDistance),
                new ObjectInspectorDifficultyValue("Min Jump Time", hitObject.MinimumJumpTime),
                new ObjectInspectorDifficultyValue("Speed Difficulty", SpeedEvaluator.EvaluateDifficultyOf(hitObject, appliedMods.Value)),
                new ObjectInspectorDifficultyValue("Rhythm Diff", osu.Game.Rulesets.Osu.Difficulty.Evaluators.RhythmEvaluator.EvaluateDifficultyOf(hitObject)),
                new ObjectInspectorDifficultyValue(hidden ? "FLHD Difficulty" : "Flashlight Diff", FlashlightEvaluator.EvaluateDifficultyOf(hitObject, hidden)),
                new ObjectInspectorDifficultyValue("Velocity", hitObject.Index > 1 ? AimEvaluator.VelocityEvaluator(hitObject, hitObjectLast, true):0),
                new ObjectInspectorDifficultyValue("Velocity Change Bonus", hitObject.Index > 1 ? AimEvaluator.VelocityChangeBonus(hitObject, hitObjectLast, hitObjectLastLast)*0.75:0),
                new ObjectInspectorDifficultyValue("Aim Difficulty", AimEvaluator.EvaluateDifficultyOf(hitObject, true)),
                new ObjectInspectorDifficultyValue("Aim Difficulty (w/o sliders)", AimEvaluator.EvaluateDifficultyOf(hitObject, false)),
            });
            if (hitObject.Angle != null && hitObjectLast.Angle != null)
            {
                double angleBonus = Math.Min(AimEvaluator.VelocityEvaluator(hitObject, hitObjectLast, true), AimEvaluator.VelocityEvaluator(hitObjectLast, hitObjectLastLast, true));
                flowContainer.AddRange(new Drawable[]
                {
                    new ObjectInspectorDifficultyValue("Wide Angle Bonus", AimEvaluator.WideAngleBonus(hitObject.Angle.Value, hitObjectLast.Angle.Value, angleBonus, hitObject)*1.5),
                    new ObjectInspectorDifficultyValue("Acute Angle Bonus",AimEvaluator.AcuteAngleBonus(hitObject.Angle.Value, hitObjectLast.Angle.Value, angleBonus, hitObject)*2.55),
                    new ObjectInspectorDifficultyValue("Angle Repeat Penalty%", 0.08 + 0.92 * (1 - Math.Min(AimEvaluator.CalcAcuteAngleBonus(hitObject.Angle.Value), Math.Pow(AimEvaluator.CalcAcuteAngleBonus(hitObjectLast.Angle.Value), 3)))),
                    new ObjectInspectorDifficultyValue("Wide Repeat Penalty%", 1 - Math.Min(AimEvaluator.CalcWideAngleBonus(hitObject.Angle.Value), Math.Pow(AimEvaluator.CalcWideAngleBonus(hitObjectLast.Angle.Value), 3))),
                    new ObjectInspectorDifficultyValue("Angle Bonus",angleBonus),
                });
            }

            if (hitObject.Angle is not null)
            {
                flowContainer.AddRange(new Drawable[]
                {
                    new ObjectInspectorDifficultyValue("Angle", double.RadiansToDegrees(hitObject.Angle.Value)),
                    new ObjectInspectorDifficultyValue("Wide Angle", AimEvaluator.CalcWideAngleBonus(hitObject.Angle.Value)),
                    new ObjectInspectorDifficultyValue("Acute Angle", AimEvaluator.CalcAcuteAngleBonus(hitObject.Angle.Value)),
                });
            }
            if (hitObject.BaseObject is Slider)
            {
                flowContainer.AddRange(new Drawable[]
                {
                    new Box
                    {
                        Name = "Separator",
                        Height = 1,
                        RelativeSizeAxes = Axes.X,
                        Alpha = 0.5f
                    },
                    new ObjectInspectorDifficultyValue("Travel Time", hitObject.TravelTime),
                    new ObjectInspectorDifficultyValue("Lazy Travel Time", hitObject.LazyTravelTime),
                    new ObjectInspectorDifficultyValue("Travel Distance", hitObject.TravelDistance),
                    new ObjectInspectorDifficultyValue("Lazy Travel Distance", hitObject.LazyTravelDistance)
                });

                if (hitObject.LazyEndPosition != null)
                    flowContainer.Add(new ObjectInspectorDifficultyValue("Lazy End Position", hitObject.LazyEndPosition!.Value));
            }
            if (hitObjectLast?.BaseObject is Slider && hitObjectLast is not null)
            {
                flowContainer.AddRange(new Drawable[]
                {
                    new Box
                    {
                        Name = "Separator",
                        Height = 1,
                        RelativeSizeAxes = Axes.X,
                        Alpha = 0.5f
                    },
                    new ObjectInspectorDifficultyValue("Travel Time Previous", hitObjectLast.TravelTime),
                    new ObjectInspectorDifficultyValue("Travel Distance Previous", hitObjectLast.TravelDistance),
                });
            }
        }

        private void drawTaikoValues(TaikoDifficultyHitObject hitObject)
        {
            double rhythmDifficulty =
                osu.Game.Rulesets.Taiko.Difficulty.Evaluators.RhythmEvaluator.EvaluateDifficultyOf(hitObject, 2 * hitObject.BaseObject.HitWindows.WindowFor(HitResult.Great) / track.Rate);

            flowContainer.AddRange(new[]
            {
                new ObjectInspectorDifficultyValue("Delta Time", hitObject.DeltaTime),
                new ObjectInspectorDifficultyValue("Effective BPM", hitObject.EffectiveBPM),
                new ObjectInspectorDifficultyValue("Rhythm Ratio", hitObject.RhythmData.Ratio),
                new ObjectInspectorDifficultyValue("Colour Difficulty", ColourEvaluator.EvaluateDifficultyOf(hitObject)),
                new ObjectInspectorDifficultyValue("Stamina Difficulty", StaminaEvaluator.EvaluateDifficultyOf(hitObject)),
                new ObjectInspectorDifficultyValue("Rhythm Difficulty", rhythmDifficulty),
            });

            if (hitObject.BaseObject is Hit hit)
            {
                flowContainer.AddRange(new[]
                {
                    new ObjectInspectorDifficultyValue($"Mono ({hit.Type}) Index", hitObject.MonoIndex),
                    new ObjectInspectorDifficultyValue("Note Index", hitObject.NoteIndex),
                });
            }
        }

        private void drawCatchValues(CatchDifficultyHitObject hitObject)
        {
            flowContainer.AddRange(new[]
            {
                new ObjectInspectorDifficultyValue("Strain Time", hitObject.StrainTime),
                new ObjectInspectorDifficultyValue("Normalized Position", hitObject.NormalizedPosition),
                new ObjectInspectorDifficultyValue("Last Normalized Position", hitObject.LastNormalizedPosition),
                new ObjectInspectorDifficultyValue("Player Position", hitObject.PlayerPosition),
                new ObjectInspectorDifficultyValue("Last Player Position", hitObject.LastPlayerPosition),
                new ObjectInspectorDifficultyValue("Distance Moved", hitObject.DistanceMoved),
                new ObjectInspectorDifficultyValue("Exact Distance Moved", hitObject.ExactDistanceMoved),

                // see https://github.com/ppy/osu/blob/a08f7327b11977f1de57b8a177bf26918ebfacda/osu.Game.Rulesets.Catch/Difficulty/Skills/Movement.cs#L36
                new ObjectInspectorDifficultyValue("Movement Difficulty", MovementEvaluator.EvaluateDifficultyOf(hitObject, track.Rate)),
            });
        }
    }
}
