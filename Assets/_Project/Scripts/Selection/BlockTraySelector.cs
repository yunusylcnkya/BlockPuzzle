using System;
using System.Collections.Generic;

namespace BlockPuzzle.Selection
{
    /// <summary>
    /// Pure C# tray selection logic. Not connected to the game in dev.1.
    /// The caller supplies a deterministic Random instance and, optionally,
    /// a callback that checks whether a candidate fits on the current board.
    /// </summary>
    public static class BlockTraySelector
    {
        public static IReadOnlyList<T> Select<T>(
            IReadOnlyList<T> candidates,
            int traySize,
            Random random,
            Func<T, bool> canPlace = null,
            bool guaranteePlaceable = false) where T : class
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (traySize <= 0)
                throw new ArgumentOutOfRangeException(nameof(traySize));
            if (guaranteePlaceable && canPlace == null)
                throw new ArgumentNullException(nameof(canPlace));

            // Keep candidate order stable. Eliminate nulls and duplicates.
            var pool = new List<T>();
            foreach (T candidate in candidates)
            {
                if (candidate != null && !pool.Contains(candidate))
                    pool.Add(candidate);
            }

            if (pool.Count == 0)
                throw new ArgumentException(
                    "At least one non-null candidate is required.",
                    nameof(candidates));

            var selected = new List<T>(traySize);

            // Opt-in only. Future dev.2 will decide this separately for
            // Classic, Adventure and the current difficulty.
            if (guaranteePlaceable)
            {
                var placeable = new List<T>();
                foreach (T candidate in pool)
                {
                    if (canPlace(candidate))
                        placeable.Add(candidate);
                }

                if (placeable.Count > 0)
                    selected.Add(placeable[random.Next(placeable.Count)]);
            }

            while (selected.Count < traySize)
            {
                var unused = new List<T>();
                foreach (T candidate in pool)
                {
                    if (!selected.Contains(candidate))
                        unused.Add(candidate);
                }

                // Repeat a block only after distinct candidates run out.
                IReadOnlyList<T> choices = unused.Count > 0 ? unused : pool;
                selected.Add(choices[random.Next(choices.Count)]);
            }

            // Do not reserve the placeable block for a fixed tray slot.
            for (int i = selected.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temporary = selected[i];
                selected[i] = selected[j];
                selected[j] = temporary;
            }

            return selected.AsReadOnly();
        }
    }
}
