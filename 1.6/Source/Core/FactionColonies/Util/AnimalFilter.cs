using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies
{
    public class AnimalFilter : IExposable
    {
        private HashSet<PawnKindDef> allowedAnimals = new HashSet<PawnKindDef>();
        private bool _initialized;

        private List<PawnKindDef> _cachedAllowed;
        private List<PawnKindDef> _cachedAllowedCombat;
        private List<PawnKindDef> _cachedAllowedPack;

        public bool IsInitialized => _initialized;
        public int AllowedCount => allowedAnimals.Count;

        public List<PawnKindDef> AllowedAnimals
        {
            get
            {
                return _cachedAllowed ?? (_cachedAllowed =
                    FactionCache.AllAnimalKindDefs.Where(k => allowedAnimals.Contains(k)).ToList());
            }
        }

        public List<PawnKindDef> AllowedCombatAnimals
        {
            get
            {
                return _cachedAllowedCombat ?? (_cachedAllowedCombat =
                    FactionCache.AllCombatAnimalKindDefs.Where(k => allowedAnimals.Contains(k)).ToList());
            }
        }

        public List<PawnKindDef> AllowedPackAnimals
        {
            get
            {
                return _cachedAllowedPack ?? (_cachedAllowedPack =
                    FactionCache.AllPackAnimalKinds.Where(k => allowedAnimals.Contains(k)).ToList());
            }
        }

        public bool IsAllowed(PawnKindDef kind)
        {
            return allowedAnimals.Contains(kind);
        }

        public void SetAllowed(PawnKindDef kind, bool allowed)
        {
            if (allowed)
                allowedAnimals.Add(kind);
            else
                allowedAnimals.Remove(kind);
            InvalidateCache();
        }

        public void AllowAll()
        {
            allowedAnimals.Clear();
            allowedAnimals.AddRange(FactionCache.AllAnimalKindDefs);
            InvalidateCache();
        }

        public void DisallowAll()
        {
            allowedAnimals.Clear();
            InvalidateCache();
        }

        public void FinalizeInit()
        {
            if (allowedAnimals.Count == 0)
            {
                AllowAll();
            }
            _initialized = true;
            InvalidateCache();
        }

        /// <summary>
        /// Validates the filter state. If no animals are allowed, re-enables all.
        /// </summary>
        public void Validate()
        {
            if (allowedAnimals.Count == 0)
            {
                LogUtil.Warning("AnimalFilter has no allowed animals. Re-enabling all");
                AllowAll();
            }
        }

        public void InvalidateCache()
        {
            _cachedAllowed = null;
            _cachedAllowedCombat = null;
            _cachedAllowedPack = null;
            FactionCache.FactionComp?.xenotypeFilter?.InvalidateGuardAnimalCache();
        }

        public void ExposeData()
        {
            List<PawnKindDef> allowedList = allowedAnimals?.ToList() ?? new List<PawnKindDef>();
            Scribe_Collections.Look(ref allowedList, "allowedAnimals", LookMode.Def);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                allowedAnimals = allowedList is object
                    ? new HashSet<PawnKindDef>(allowedList.Where(d => d is object))
                    : new HashSet<PawnKindDef>();
                InvalidateCache();
            }
        }
    }
}
