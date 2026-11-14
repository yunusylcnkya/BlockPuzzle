using System;
using System.Collections.Generic;

namespace BlockPuzzle.Selection
{
    // Values are serialized in the BlockSpawner Inspector. Do not reorder.
    public enum SmartSpawnProfile
    {
        Legacy = 0,
        ClassicBalanced = 1,
        AdventureAdaptive = 2
    }

    public enum SmartDifficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Expert = 3
    }

    /// <summary>
    /// Pure C# weighted selection. It never changes the game board, spends
    /// coins, or touches Unity state. The caller determines whether a shape
    /// currently fits and whether one placeable shape should be guaranteed.
    /// </summary>
    public static class SmartBlockSelector
    {
        private sealed class Candidate<T> where T : class
        {
            public T Item;
            public int Weight;
            public bool Fits;
        }

        public static IReadOnlyList<T> Select<T>(
            IReadOnlyList<T> definitions,
            int traySize,
            Random random,
            Func<T, int> getCellCount,
            Func<T, bool> canPlace,
            SmartDifficulty difficulty,
            bool guaranteePlaceable) where T : class
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (getCellCount == null)
                throw new ArgumentNullException(nameof(getCellCount));
            if (traySize <= 0)
                throw new ArgumentOutOfRangeException(nameof(traySize));
            if (guaranteePlaceable && canPlace == null)
                throw new ArgumentNullException(nameof(canPlace));
            if (!Enum.IsDefined(typeof(SmartDifficulty), difficulty))
                throw new ArgumentOutOfRangeException(nameof(difficulty));

            var candidates = new List<Candidate<T>>();
            foreach (T definition in definitions)
            {
                if (definition == null)
                    continue;

                bool exists = false;
                foreach (Candidate<T> candidate in candidates)
                {
                    if (EqualityComparer<T>.Default.Equals(candidate.Item, definition))
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists)
                    continue;

                int cellCount = Math.Max(1, getCellCount(definition));
                bool fits = canPlace != null && canPlace(definition);
                int weight = GetSizeWeight(cellCount, difficulty);

                // Prefer usable shapes, but never force survival unless explicitly
                // requested. Hard/Expert keep the challenge untouched.
                if (fits && (difficulty == SmartDifficulty.Easy ||
                             difficulty == SmartDifficulty.Medium))
                    weight *= 2;

                candidates.Add(new Candidate<T>
                {
                    Item = definition,
                    Weight = weight,
                    Fits = fits
                });
            }

            if (candidates.Count == 0)
                throw new ArgumentException("No valid candidates.", nameof(definitions));

            var selected = new List<T>(traySize);

            if (guaranteePlaceable)
            {
                var placeable = new List<Candidate<T>>();
                foreach (Candidate<T> c in candidates)
                    if (c.Fits) placeable.Add(c);

                // When the entire board is blocked, do not fabricate a move.
                if (placeable.Count > 0)
                    selected.Add(DrawWeighted(placeable, random).Item);
            }

            while (selected.Count < traySize)
            {
                var remaining = new List<Candidate<T>>();
                foreach (Candidate<T> c in candidates)
                    if (!selected.Contains(c.Item)) remaining.Add(c);

                // Repeats become possible only after every unique shape is used.
                if (remaining.Count == 0)
                    remaining.AddRange(candidates);

                selected.Add(DrawWeighted(remaining, random).Item);
            }

            // The assisted piece must not always occupy slot 0.
            for (int i = selected.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = selected[i];
                selected[i] = selected[j];
                selected[j] = temp;
            }

            return selected.AsReadOnly();
        }

        private static int GetSizeWeight(int cellCount, SmartDifficulty difficulty)
        {
            bool small = cellCount <= 2;
            bool large = cellCount >= 5;
            switch (difficulty)
            {
                case SmartDifficulty.Easy:
                    return small ? 6 : large ? 1 : 3;
                case SmartDifficulty.Medium:
                    return small ? 4 : large ? 2 : 4;
                case SmartDifficulty.Hard:
                    return small ? 2 : large ? 5 : 4;
                case SmartDifficulty.Expert:
                    return small ? 1 : large ? 7 : 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty));
            }
        }

        private static Candidate<T> DrawWeighted<T>(
            IReadOnlyList<Candidate<T>> pool, Random random) where T : class
        {
            int totalWeight = 0;
            foreach (Candidate<T> candidate in pool)
                totalWeight = checked(totalWeight + candidate.Weight);

            int roll = random.Next(totalWeight);
            foreach (Candidate<T> candidate in pool)
            {
                roll -= candidate.Weight;
                if (roll < 0) return candidate;
            }
            throw new InvalidOperationException("Weighted selection failed.");
        }
    }
}
