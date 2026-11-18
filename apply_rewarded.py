#!/usr/bin/env python3
"""Safe, preflight-first source patcher for BlockPuzzle 2.0.0 hotfix Classic rewards.
Run from Unity project root. Does not touch .meta, Adventure saves, or Build settings.
"""
import argparse, datetime, pathlib, shutil, sys

ROOT=pathlib.Path(__file__).resolve().parent
FILES={
 'admob':'Assets/_Project/Scripts/Monetization/AdMobManager.cs',
 'classic':'Assets/_Project/Scripts/Classic/ClassicGameController.cs',
 'run':'Assets/_Project/Scripts/Classic/ClassicRunController.cs',
 'snapshot':'Assets/_Project/Scripts/ClassicPersistence/ClassicRunSnapshot.cs',
 'validator':'Assets/_Project/Scripts/ClassicPersistence/ClassicRunValidator.cs'
}

def transform(path, source):
    content=source
    count=0
    def patch(old,new,name):
        nonlocal content,count
        if new in content:
            count+=1;return
        hits=content.count(old)
        if hits!=1:
            raise ValueError(f'{path}: expected exactly one anchor for {name}; found {hits}. Files unchanged.')
        content=content.replace(old,new,1);count+=1
    if path=='admob':
        patch('''        private void FinishRewardedAd()
''','''        /// <summary>
        /// Plays an ordinary rewarded ad for a *gameplay* action rather than
        /// awarding Adventure coins. The action executes only after the SDK
        /// reports a verified earned reward; ad dismissal grants nothing.
        /// The old ShowRewardedForCoins() path is unchanged.
        /// </summary>
        public bool ShowRewardedForGameplay(Action onEarned)
        {
#if UNITY_EDITOR
            Debug.Log("[AdMob Rewarded] Android device required.");
            return false;
#else
            if (onEarned == null || !IsRewardedReady) return false;
            RewardedAd currentAd = rewardedAd;
            showingRewarded = true;
            rewardGrantedForCurrentAd = false;
            RewardedStateChanged?.Invoke();
            int rewardProcessed = 0;
            try
            {
                currentAd.Show(reward =>
                {
                    if (System.Threading.Interlocked.Exchange(ref rewardProcessed, 1) != 0)
                        return;
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null) return;
                        rewardGrantedForCurrentAd = true;
                        try { onEarned(); }
                        catch (Exception exception)
                        {
                            Debug.LogError("[AdMob Rewarded] Gameplay grant failed: " + exception);
                        }
                    });
                });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("[AdMob Rewarded] Gameplay show exception: " + exception.Message);
                FinishRewardedAd();
                return false;
            }
#endif
        }

        private void FinishRewardedAd()
''','Rewarded gameplay API')
    elif path=='classic':
        patch('''using BlockPuzzle.Monetization;
''','''using BlockPuzzle.Monetization;
using BlockPuzzle.SaveSystem;
''','Classic save import')
        patch('''        private bool isGameOver;
''','''        private bool isGameOver;
        public bool IsGameOver => isGameOver;
''','Classic GameOver accessor')
        patch('''        private void RestartGameImmediate()
''','''        /// <summary>Called only after a completed rewarded Undo restored the board.</summary>
        public void ResumeAfterRewardedUndo()
        {
            if (!isGameOver) return;
            isGameOver = false;
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            foreach (BlockView block in blockSpawner.ActiveBlocks)
            {
                if (block == null) continue;
                BlockDragHandler drag = block.GetComponent<BlockDragHandler>();
                if (drag != null) drag.enabled = true;
            }
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);
            RequestMoveCheck();
        }

        private void RestartGameImmediate()
''','Classic rewarded resume')
        patch('''        private void RestartGameImmediate()
        {
''','''        private void RestartGameImmediate()
        {
            // A reward-offer Game Over may still have a resumable snapshot.
            // Explicit Retry always starts a clean run.
            ClassicRunSaveService.CloseRun();
''','Retry close Classic run')
        patch('''        private void ReturnToMainMenuImmediate()
        {
''','''        private void ReturnToMainMenuImmediate()
        {
            // Close only after Game Over; leaving active Classic play still
            // preserves Classic Continue.
            if (isGameOver) ClassicRunSaveService.CloseRun();
''','Game Over home close')
    elif path=='run':
        patch('''        private bool runClosed;
''','''        private bool runClosed;
        private int rewardedRerollsUsed;
        public int RewardedRerollsUsed => rewardedRerollsUsed;
        public bool IsReady => ready && !restoring && !protectExistingSave && !runClosed;

        /// <summary>Called after an ad-earned reshuffle, at most five per run.</summary>
        public void RegisterRewardedReroll()
        {
            if (!IsReady || rewardedRerollsUsed >= 5) return;
            rewardedRerollsUsed++;
            SaveCommittedState();
        }

        /// <summary>Save the board/tray after a successful rewarded Undo.</summary>
        public void SaveAfterRewardedAction()
        {
            if (IsReady) SaveCommittedState();
        }
''','Run reward counter API')
        patch('''                else
                {
                    Debug.Log("[ClassicContinue] Restored Classic run; score=" + snapshot.score +
''','''                else
                {
                    rewardedRerollsUsed = snapshot.rewardedRerollsUsed;
                    Debug.Log("[ClassicContinue] Restored Classic run; score=" + snapshot.score +
''','Restore reward counter')
        patch('''                occupiedCells = cells.ToArray(),
                trayBlockIds = new string[3]
''','''                occupiedCells = cells.ToArray(),
                rewardedRerollsUsed = rewardedRerollsUsed,
                trayBlockIds = new string[3]
''','Store reward counter')
        patch('''        private void Update()
        {
            if (ready && !runClosed && !protectExistingSave &&
                GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.GameOver)
                EndRun();
        }
''','''        private void Update()
        {
            if (ready && !runClosed && !protectExistingSave &&
                GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.GameOver)
            {
                ClassicRewardedTools tools = GetComponent<ClassicRewardedTools>();
                if (tools == null || !tools.CanRescue) EndRun();
            }
        }
''','Keep run open when last move can be rescued')
        patch('''        private void OnDisable()
        {
            BlockDragHandler.AfterPlacement -= HandleAfterPlacement;
            if (ready && !protectExistingSave && !runClosed &&
                GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.GameOver)
                EndRun();
        }
''','''        private void OnDisable()
        {
            BlockDragHandler.AfterPlacement -= HandleAfterPlacement;
            if (ready && !protectExistingSave && !runClosed &&
                GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.GameOver)
            {
                ClassicRewardedTools tools = GetComponent<ClassicRewardedTools>();
                if (tools == null || !tools.CanRescue) EndRun();
            }
        }
''','Keep run open on disable if rescue eligible')
    elif path=='snapshot':
        patch('''        public int lastClearedLines;
''','''        public int lastClearedLines;
        // Dev.4 hotfix: additive V2 field. Old V2 saves deserialize as zero.
        public int rewardedRerollsUsed;
''','Add rewarded reroll count')
    elif path=='validator':
        patch('''            if (run.trayBlockIds == null || run.trayBlockIds.Length != 3)
''','''            if (run.rewardedRerollsUsed < 0 || run.rewardedRerollsUsed > 5)
                return Fail("Invalid rewarded shuffle usage", out reason);
            if (run.trayBlockIds == null || run.trayBlockIds.Length != 3)
''','Validate reroll limit')
    return content,count

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--check-only',action='store_true')
    a=parser.parse_args()
    modified=[]
    try:
        for key,rel in FILES.items():
            path=ROOT/rel
            if not path.exists(): raise ValueError(f'Missing expected project source: {rel}')
            raw=path.read_bytes()
            src=raw.decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n')
            after,num=transform(key,src)
            linebreak='\r\n' if b'\r\n' in raw else '\n'
            bom=b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b''
            modified.append((path,raw,bom+after.replace('\n',linebreak).encode('utf-8'),num,rel))
        for rel in [
          'Assets/_Project/Scripts/Classic/ClassicRewardedTools.cs',
          'Assets/_Project/Scripts/Editor/ClassicRewardedSceneInstaller.cs',
          'Assets/_Project/Tests/EditModeClassic/ClassicRewardedValidationTests.cs']:
            if not (ROOT/rel).exists(): raise ValueError('Missing new file from ZIP: '+rel)
    except (ValueError,UnicodeDecodeError,OSError) as e:
        print('PREFLIGHT FAILED:',e)
        print('No files changed. Ensure installed revision matches GitHub f74bcbe3 and the ZIP contents were extracted.')
        return 1
    changed=[item for item in modified if item[1] != item[2]]
    print('Preflight OK:',sum(x[3] for x in modified),'patch anchors verified,',len(changed),'source files pending.')
    if a.check_only:
        print('Check-only completed; no files modified.');return 0
    stamp=datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
    back=ROOT/'Library'/('BlockPuzzleRewardedBackups_'+stamp)
    try:
        for path,original,revised,num,rel in changed:
            dest=back/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(original)
        written=[]
        try:
            for path,old,revised,num,rel in changed:
                path.write_bytes(revised);written.append((path,old))
        except Exception:
            for path,old in reversed(written):path.write_bytes(old)
            raise
    except OSError as e:
        print('Write failed:',e,'; originals restored where possible.');return 2
    print('Classic rewarded code patched successfully. Backup:',back)
    print('NEXT: Unity > Tools > Block Puzzle > v2.0 > Install Rewarded Classic + Fix Adventure Button')
    return 0

if __name__=='__main__':sys.exit(main())
