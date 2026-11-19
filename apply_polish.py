# coding: utf-8
"""BlockPuzzle dev5.1 source-only upgrade. Python 3.13 / Windows compatible.
Usage: py -3 apply_polish.py --check-only ; py -3 apply_polish.py
"""
import argparse
import pathlib
import shutil
import sys

ROOT = pathlib.Path(__file__).resolve().parent
# In the distributed zip this lives at the project root.
SRC = pathlib.Path('Assets/_Project/Scripts')

patches = {}
def add(path, old, new):
    patches.setdefault(path, []).append((old, new))

# 1: Separate the visual drag-follow from the grid-aligned placement preview.
P = SRC/'Blocks/BlockDragHandler.cs'
add(P,
'''        [SerializeField, Range(0.2f, 1f)]
        private float dragAlpha = 0.70f;''',
'''        [SerializeField, Range(0.2f, 1f)]
        private float dragAlpha = 0.70f;

        [Header("Visual Drag Follow - v2.0.0-dev.5.1")]
        [SerializeField, Range(0.01f, 0.12f)]
        private float dragSmoothTime = 0.035f;
        private Vector2 pointerTarget;
        private Vector2 pointerVelocity;''')
add(P,
'''        public void OnBeginDrag(PointerEventData eventData)
        {''',
'''        private void Update()
        {
            if (!isDragging) return;
            rect.anchoredPosition = Vector2.SmoothDamp(
                rect.anchoredPosition, pointerTarget, ref pointerVelocity,
                dragSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {''')
add(P,
'''            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)''',
'''            pointerVelocity = Vector2.zero;
            UpdateDragPosition(eventData);
            rect.anchoredPosition = pointerTarget;
        }

        public void OnDrag(PointerEventData eventData)''')
add(P,
'''            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, targetScreenPoint, uiCamera, out Vector2 fingerLocal))
                rect.anchoredPosition = fingerLocal;''',
'''            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, targetScreenPoint, uiCamera, out Vector2 fingerLocal))
                pointerTarget = fingerLocal;''')
add(P,
'''            Vector3 worldCenter = gridManager.GetPlacementCenterWorld(
                blockView.Definition, targetRow, targetColumn);
            Vector3 localCenter = dragLayer.InverseTransformPoint(worldCenter);
            rect.anchoredPosition = new Vector2(localCenter.x, localCenter.y);''',
'''            // Deliberately do NOT snap the dragged block to the grid.
            // The green/red cell preview is grid-aligned; the visual block
            // continuously tracks the finger. Placement uses row/column above.''')

# 2: A rewarded Game Over rescue must return three newly shuffled, placeable shapes.
P = SRC/'Blocks/BlockSpawner.cs'
add(P,
'''        private bool IsInTraySlot(BlockView block)
        {''',
'''        /// <summary>
        /// Rewarded Classic rescue only: replace the entire tray with 3 new
        /// shapes. Every generated shape has at least one placement on the
        /// restored board. Does not consume the five regular shuffle credits.
        /// </summary>
        public bool TryGenerateRescueTray()
        {
            if (!HasValidSpawnReferences() || IsAnyBlockDragging) return false;
            List<BlockDefinition> placeable = GetPlaceableDefinitions();
            if (placeable.Count == 0) return false;

            var pool = new List<BlockDefinition>(placeable);
            var replacement = new BlockDefinition[3];
            for (int i = 0; i < replacement.Length; i++)
            {
                if (pool.Count == 0) pool.AddRange(placeable);
                int pick = UnityEngine.Random.Range(0, pool.Count);
                replacement[i] = pool[pick];
                pool.RemoveAt(pick);
            }
            if (!RestoreTraySnapshot(replacement)) return false;
            RerollCompleted?.Invoke(); // Undo of the old terminal move is consumed.
            return true;
        }

        private bool IsInTraySlot(BlockView block)
        {''')

# 3: Continue = restore last turn + new three-shape tray. Normal Undo unchanged.
P = SRC/'Classic/ClassicRewardedTools.cs'
add(P,
'''                    success = RestorePreviousTurn();
                }
                Debug.Log("[ClassicAds] Rewarded action " + action + ": " + success);''',
'''                    success = RestorePreviousTurn(action == RewardAction.Continue);
                }
                Debug.Log("[ClassicAds] Rewarded action " + action + ": " + success);''')
add(P,
'''        private bool RestorePreviousTurn()
        {''',
'''        private bool RestorePreviousTurn(bool newThreeBlockTray)
        {''')
add(P,
'''            available = null;
            pending = null;
            if (classicGame.IsGameOver)
                classicGame.ResumeAfterRewardedUndo();
            classicRun.SaveAfterRewardedAction();
            return true;''',
'''            // Continue is a stronger one-time rescue than ordinary Undo.
            // The restored board is valid before we request any new blocks.
            if (newThreeBlockTray && !blockSpawner.TryGenerateRescueTray())
            {
                if (!ApplyTurn(before))
                    Debug.LogError("[ClassicAds] Could not roll back failed rescue shuffle.");
                return false;
            }

            available = null;
            pending = null;
            if (classicGame.IsGameOver)
                classicGame.ResumeAfterRewardedUndo();
            classicRun.SaveAfterRewardedAction();
            return true;''')
add(P,
'''                SetText(continueAdButton, "WATCH AD & CONTINUE");''',
'''                SetText(continueAdButton, "WATCH AD & CONTINUE + 3 BLOCKS");''')

# Confirmed Classic audio bug: gameEnded remains true after rewarded Game Over rescue.
P = SRC/'Gameplay/GameFeedbackController.cs'
add(P,
'''        public void PlayGameOver()
        {''',
'''        public void ResumeAfterRewardedContinue()
        {
            gameEnded = false;
            consecutiveClearMoves = 0;
        }

        public void PlayGameOver()
        {''')
P = SRC/'Classic/ClassicGameController.cs'
add(P,
'''            isGameOver = false;
            if (gameOverPanel != null) gameOverPanel.SetActive(false);''',
'''            isGameOver = false;
            if (feedbackController != null)
                feedbackController.ResumeAfterRewardedContinue();
            if (gameOverPanel != null) gameOverPanel.SetActive(false);''')

# 4: Recover background music not only after Android pause but also focus/audio reconfiguration.
P = SRC/'Gameplay/BackgroundMusicController.cs'
add(P,
'''        private void OnApplicationPause(bool paused)
        {''',
'''        private void OnEnable()
        {
            AudioSettings.OnAudioConfigurationChanged += OnAudioChanged;
        }

        private void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioChanged;
        }

        private void OnAudioChanged(bool deviceWasChanged)
        {
            RefreshSettings();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus) RefreshSettings();
        }

        private void OnApplicationPause(bool paused)
        {''')

# 5: On an ad close restore only audio state that was not intentionally paused
# before the ad; always re-apply the stored music volume / enabled state.
P = SRC/'Monetization/AdMobManager.cs'
add(P,
'''        private bool showingRewarded;
        private bool rewardGrantedForCurrentAd;''',
'''        private bool showingRewarded;
        private bool rewardGrantedForCurrentAd;
        private bool audioPausedBeforeFullscreen;''')
add(P,
'''            showingRewarded = true;
            rewardGrantedForCurrentAd = false;''',
'''            audioPausedBeforeFullscreen = AudioListener.pause;
            showingRewarded = true;
            rewardGrantedForCurrentAd = false;''')
add(P,
'''        private void FinishRewardedAd()
        {
            if (!showingRewarded) return;
            showingRewarded = false;''',
'''        private void FinishRewardedAd()
        {
            if (!showingRewarded) return;
            showingRewarded = false;
            RestoreAudioAfterFullscreen();''')
add(P,
'''        private void FinishAdAndContinue()
        {
            if (!showingAd) return;
            showingAd = false;''',
'''        private void FinishAdAndContinue()
        {
            if (!showingAd) return;
            showingAd = false;
            RestoreAudioAfterFullscreen();''')
add(P,
'''            showingAd = true;
            pendingContinuation = continuation;''',
'''            audioPausedBeforeFullscreen = AudioListener.pause;
            showingAd = true;
            pendingContinuation = continuation;''')
add(P,
'''        private static void InvokeContinuation(Action continuation)
        {''',
'''        private void RestoreAudioAfterFullscreen()
        {
            if (!audioPausedBeforeFullscreen && AudioListener.pause)
                AudioListener.pause = false;
            BlockPuzzle.Gameplay.BackgroundMusicController.Instance?.RefreshSettings();
        }

        private static void InvokeContinuation(Action continuation)
        {''')


def run():
    parser = argparse.ArgumentParser(description='BlockPuzzle dev5.1 gameplay polish')
    parser.add_argument('--check-only', action='store_true')
    args = parser.parse_args()
    operations = []
    errors = []
    for relative, edits in patches.items():
        path = ROOT / relative
        if not path.exists():
            errors.append(f'MISSING: {relative}')
            continue
        old_bytes = path.read_bytes()
        raw = old_bytes.decode('utf-8-sig')
        newline = '\r\n' if '\r\n' in raw else '\n'
        content = raw.replace('\r\n', '\n')
        for before, after in edits:
            count = content.count(before)
            if count == 1:
                content = content.replace(before, after, 1)
            elif count == 2 and before == (
                '            showingRewarded = true;\n'
                '            rewardGrantedForCurrentAd = false;'):
                # Both coin and gameplay reward pathways must restore audio.
                content = content.replace(before, after)
            elif count == 0 and after in content:
                # Idempotence: previously-applied patch.
                pass
            else:
                errors.append(f'ANCHOR MISMATCH ({count}): {relative} -> {before[:74]!r}')
        if content != raw.replace('\r\n','\n'):
            operations.append((path, content.replace('\n', newline).encode('utf-8')))
    if errors:
        print('PREFLIGHT FAILED - NO FILES WRITTEN', file=sys.stderr)
        for e in errors: print('  '+e, file=sys.stderr)
        return 1
    print(f'Preflight OK: {len(operations)} modified scripts, {len(patches)-len(operations)} already current.')
    if args.check_only:
        print('No files written.')
        return 0
    backup = ROOT/'Library'/'BlockPuzzleDev51Backups'
    backup.mkdir(parents=True, exist_ok=True)
    for path, content in operations:
        target = backup/path.relative_to(ROOT)
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists(): shutil.copy2(path, target)
    for path, content in operations:
        path.write_bytes(content)
        print('Patched:', path.relative_to(ROOT))
    print('Dev5.1 source integration completed. Restart Unity and run Editor installer.')
    return 0

if __name__ == '__main__':
    sys.exit(run())
