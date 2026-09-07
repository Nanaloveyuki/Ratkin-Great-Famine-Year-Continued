using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MouseDisaster
{
    public static partial class MouseDisasterUtility
    {

        public static void ResetBeggarState(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            BegAttempts.Remove(pawnId);
            BeggedColonists.Remove(pawnId);
            BegSuccess.Remove(pawnId);
            RemoveSiegeBeggarState(pawnId);
            AirDropStayUntilTickByPawnId.Remove(pawnId);
        }

        public static void ClearBeggarTargetHistory(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            BegAttempts.Remove(pawnId);
            BeggedColonists.Remove(pawnId);
        }

        public static int GetBegAttempts(Pawn pawn)
        {
            return pawn != null && BegAttempts.TryGetValue(pawn.thingIDNumber, out int count) ? count : 0;
        }

        public static bool HasBeggedColonist(Pawn beggar, Pawn colonist)
        {
            return beggar != null &&
                   colonist != null &&
                   BeggedColonists.TryGetValue(beggar.thingIDNumber, out HashSet<int> targets) &&
                   targets.Contains(colonist.thingIDNumber);
        }

        public static void RecordBegAttempt(Pawn pawn, Pawn targetColonist, bool success)
        {
            if (pawn == null)
            {
                return;
            }

            BegAttempts[pawn.thingIDNumber] = GetBegAttempts(pawn) + 1;
            if (targetColonist != null)
            {
                if (!BeggedColonists.TryGetValue(pawn.thingIDNumber, out HashSet<int> targets))
                {
                    targets = new HashSet<int>();
                    BeggedColonists[pawn.thingIDNumber] = targets;
                }

                targets.Add(targetColonist.thingIDNumber);
            }

            if (success)
            {
                int pawnId = pawn.thingIDNumber;
                if (SiegeBeggarPawnIds.Contains(pawnId))
                {
                    return;
                }

                BegSuccess.Add(pawnId);
            }
        }

        public static bool TryConsumeBeggedFood(Pawn beggar, Pawn targetColonist)
        {
            if (beggar == null || targetColonist?.inventory?.innerContainer == null)
            {
                return false;
            }

            Thing food = targetColonist.inventory.innerContainer
                .Where(thing => thing.def.IsNutritionGivingIngestible && thing.IngestibleNow && thing.stackCount > 0)
                .OrderByDescending(thing => thing.GetStatValue(StatDefOf.Nutrition))
                .FirstOrDefault();
            if (food == null)
            {
                return false;
            }

            Thing beggedFood = food.SplitOff(1);
            if (beggedFood == null)
            {
                return false;
            }

            if (beggar.inventory?.innerContainer != null && beggar.inventory.innerContainer.TryAddOrTransfer(beggedFood))
            {
                return true;
            }

            if (beggar.MapHeld != null && beggar.PositionHeld.IsValid)
            {
                GenPlace.TryPlaceThing(beggedFood, beggar.PositionHeld, beggar.MapHeld, ThingPlaceMode.Near);
                return true;
            }

            beggedFood.Destroy();
            return false;
        }

        public static void RecordBeggarInteractionLog(Pawn beggar, Pawn targetColonist, bool success)
        {
            if (beggar == null || targetColonist == null)
            {
                return;
            }

            InteractionDef interaction = success ? InteractionDefOf.Chitchat : InteractionDefOf.Insult;
            Find.PlayLog?.Add(new PlayLogEntry_Interaction(interaction, beggar, targetColonist, null));
        }

        public static void ApplyBeggarWitnessThought(Pawn targetColonist)
        {
            if (targetColonist?.needs?.mood?.thoughts?.memories == null || targetColonist.story?.traits == null)
            {
                return;
            }

            if (targetColonist.story.traits.HasTrait(TraitDefOf.Psychopath))
            {
                targetColonist.needs.mood.thoughts.memories.TryGainMemory(MouseDisasterDefOf.MouseDisaster_SawBeggarRatkin_Psychopath);
                return;
            }

            if (targetColonist.story.traits.HasTrait(TraitDefOf.Kind))
            {
                targetColonist.needs.mood.thoughts.memories.TryGainMemory(MouseDisasterDefOf.MouseDisaster_SawBeggarRatkin_Kind);
            }
        }

        public static bool HasBeggarSucceeded(Pawn pawn)
        {
            return pawn != null && BegSuccess.Contains(pawn.thingIDNumber);
        }
    }
}
