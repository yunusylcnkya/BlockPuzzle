using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BlockPuzzle.QA.PlayMode
{
    /// <summary>
    /// Runtime smoke tests for the project's actual GridManager and BlockDefinition.
    /// No production scenes, saved games, PlayerPrefs, AdMob or rewards are touched.
    /// Reflection is intentional: Unity test asmdefs cannot depend directly on
    /// predefined Assembly-CSharp without moving game code into a new asmdef.
    /// </summary>
    public sealed class GridRegressionPlayModeTests
    {
        private Type gridType;
        private Type definitionType;
        private Type cellViewType;
        private Type specialKindType;
        private GameObject canvasObject;
        private GameObject managerObject;
        private GameObject cellPrefabObject;
        private Component grid;
        private readonly List<ScriptableObject> definitions = new List<ScriptableObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            gridType = FindGameType("BlockPuzzle.Grid.GridManager");
            definitionType = FindGameType("BlockPuzzle.Blocks.BlockDefinition");
            cellViewType = FindGameType("BlockPuzzle.Grid.GridCellView");
            specialKindType = FindGameType("BlockPuzzle.Grid.SpecialCellKind");

            canvasObject = new GameObject("QA_Only_Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var rootObject = new GameObject("QA_Only_GridRoot", typeof(RectTransform), typeof(GridLayoutGroup));
            var gridRoot = rootObject.GetComponent<RectTransform>();
            gridRoot.SetParent(canvasObject.transform, false);
            gridRoot.sizeDelta = new Vector2(800f, 800f);

            cellPrefabObject = new GameObject("QA_Only_CellPrefab", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            var prefab = cellPrefabObject.AddComponent(cellViewType);

            managerObject = new GameObject("QA_Only_GridManager");
            managerObject.SetActive(false); // Give Awake its serialized dependencies first.
            grid = managerObject.AddComponent(gridType);
            SetField(grid, "gridRoot", gridRoot);
            SetField(grid, "cellPrefab", prefab);
            SetField(grid, "rows", 8);
            SetField(grid, "columns", 8);
            managerObject.SetActive(true);
            yield return null;
            Assert.That(Cell(0, 0), Is.Not.Null, "QA grid failed to initialize.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var definition in definitions)
                if (definition != null) UnityEngine.Object.Destroy(definition);
            definitions.Clear();
            if (managerObject != null) UnityEngine.Object.Destroy(managerObject);
            if (canvasObject != null) UnityEngine.Object.Destroy(canvasObject);
            if (cellPrefabObject != null) UnityEngine.Object.Destroy(cellPrefabObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GridInitializesEightByEightAndEmpty()
        {
            Assert.That(GetProperty<int>(grid, "Rows"), Is.EqualTo(8));
            Assert.That(GetProperty<int>(grid, "Columns"), Is.EqualTo(8));
            for (int row = 0; row < 8; row++)
            for (int col = 0; col < 8; col++)
                Assert.That(IsOccupied(row, col), Is.False, "Unexpected occupied cell " + row + "," + col);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SingleBlockCanBePlacedOnEmptyBoard()
        {
            var single = Block(new Vector2Int(0, 0));
            Assert.That(Call<bool>(grid, "CanPlaceBlock", single, 2, 3), Is.True);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", single, 2, 3, 1001), Is.True);
            Assert.That(IsOccupied(2, 3), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverlapIsRejectedWithoutChangingOccupiedCell()
        {
            var single = Block(new Vector2Int(0, 0));
            Assert.That(Call<bool>(grid, "TryPlaceBlock", single, 2, 3, 1001), Is.True);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", single, 2, 3, 1002), Is.False);
            Assert.That(GetProperty<int>(Cell(2, 3), "BlockId"), Is.EqualTo(1001));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OutOfBoundsPlacementIsRejected()
        {
            var twoWide = Block(new Vector2Int(0, 0), new Vector2Int(1, 0));
            Assert.That(Call<bool>(grid, "CanPlaceBlock", twoWide, 0, 7), Is.False);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", twoWide, 0, 7, 1003), Is.False);
            Assert.That(IsOccupied(0, 7), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullHorizontalLineClearsEightCells()
        {
            for (int col = 0; col < 7; col++) Occupy(2, col);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", Block(new Vector2Int(0, 0)), 2, 7, 1010), Is.True);
            for (int col = 0; col < 8; col++) Assert.That(IsOccupied(2, col), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullVerticalLineClearsEightCells()
        {
            for (int row = 0; row < 7; row++) Occupy(row, 4);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", Block(new Vector2Int(0, 0)), 7, 4, 1011), Is.True);
            for (int row = 0; row < 8; row++) Assert.That(IsOccupied(row, 4), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrossClearRemovesUniqueFifteenCells()
        {
            for (int col = 0; col < 8; col++) if (col != 4) Occupy(3, col);
            for (int row = 0; row < 8; row++) if (row != 3) Occupy(row, 4);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", Block(new Vector2Int(0, 0)), 3, 4, 1012), Is.True);
            for (int col = 0; col < 8; col++) Assert.That(IsOccupied(3, col), Is.False);
            for (int row = 0; row < 8; row++) Assert.That(IsOccupied(row, 4), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedCellRejectsPlacement()
        {
            Special(1, 2, "Locked");
            Assert.That(Call<bool>(grid, "CanPlaceBlock", Block(new Vector2Int(0, 0)), 1, 2), Is.False);
            Assert.That(IsOccupied(1, 2), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PermanentLockSurvivesLineClear()
        {
            Special(2, 0, "Locked");
            for (int col = 1; col < 8; col++) Occupy(2, col);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", Block(new Vector2Int(0, 0)), 7, 7, 1013), Is.True);
            Assert.That(IsOccupied(2, 0), Is.True);
            Assert.That(GetProperty<object>(Cell(2, 0), "SpecialKind").ToString(), Is.EqualTo("Locked"));
            for (int col = 1; col < 8; col++) Assert.That(IsOccupied(2, col), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BreakableObstaclesDisappearOnCompletedRow()
        {
            Special(2, 0, "BreakableObstacle");
            Special(2, 1, "BreakableObstacle");
            for (int col = 2; col < 8; col++) Occupy(2, col);
            Assert.That(Call<bool>(grid, "TryPlaceBlock", Block(new Vector2Int(0, 0)), 7, 7, 1014), Is.True);
            for (int col = 0; col < 8; col++)
            {
                Assert.That(IsOccupied(2, col), Is.False);
                Assert.That(GetProperty<object>(Cell(2, col), "SpecialKind").ToString(), Is.EqualTo("None"));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GreenPreviewMatchesValidPlacement()
        {
            var single = Block(new Vector2Int(0, 0));
            Call<object>(grid, "ShowPlacementPreview", single, 0, 0);
            Color shown = ViewColor(0, 0);
            Assert.That(shown.g, Is.GreaterThan(shown.r));
            Call<object>(grid, "ClearPreview");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RedPreviewMatchesBlockedPlacement()
        {
            var single = Block(new Vector2Int(0, 0));
            Occupy(0, 0);
            Call<object>(grid, "ShowPlacementPreview", single, 0, 0);
            Color shown = ViewColor(0, 0);
            Assert.That(shown.r, Is.GreaterThan(shown.g));
            Call<object>(grid, "ClearPreview");
            yield return null;
        }

        private void Occupy(int row, int col)
        {
            Assert.That(Call<bool>(grid, "TryOccupyCell", row, col, 5000 + row * 8 + col), Is.True);
        }

        private void Special(int row, int col, string kind)
        {
            object value = Enum.Parse(specialKindType, kind);
            Assert.That(Call<bool>(grid, "TrySetSpecialCell", row, col, value), Is.True);
        }

        private bool IsOccupied(int row, int col) => GetProperty<bool>(Cell(row, col), "IsOccupied");
        private object Cell(int row, int col) => Call<object>(grid, "GetCell", row, col);

        private Color ViewColor(int row, int col)
        {
            var views = (Array)GetField(grid, "cellViews");
            var view = (Component)views.GetValue(row, col);
            return view.GetComponent<Image>().color;
        }

        private ScriptableObject Block(params Vector2Int[] cells)
        {
            var definition = ScriptableObject.CreateInstance(definitionType);
            SetField(definition, "blockId", "QA_Only_" + definitions.Count);
            SetField(definition, "cells", new List<Vector2Int>(cells));
            definitions.Add(definition);
            Assert.That(Call<bool>(definition, "IsValid"), Is.True);
            return definition;
        }

        private static Type FindGameType(string name)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(x => x.GetName().Name == "Assembly-CSharp");
            Assert.That(assembly, Is.Not.Null, "Assembly-CSharp not found: " + name);
            var type = assembly.GetType(name, false);
            Assert.That(type, Is.Not.Null, "Game runtime type missing: " + name);
            return type;
        }

        private static BindingFlags Flags => BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static void SetField(object obj, string name, object value)
        {
            var field = obj.GetType().GetField(name, Flags);
            Assert.That(field, Is.Not.Null, "Field not found: " + name);
            field.SetValue(obj, value);
        }

        private static object GetField(object obj, string name)
        {
            var field = obj.GetType().GetField(name, Flags);
            Assert.That(field, Is.Not.Null, "Field not found: " + name);
            return field.GetValue(obj);
        }

        private static T GetProperty<T>(object obj, string name)
        {
            Assert.That(obj, Is.Not.Null);
            var property = obj.GetType().GetProperty(name, Flags);
            Assert.That(property, Is.Not.Null, "Property not found: " + name);
            return (T)property.GetValue(obj);
        }

        private static T Call<T>(object obj, string methodName, params object[] args)
        {
            Assert.That(obj, Is.Not.Null);
            var method = obj.GetType().GetMethods(Flags).FirstOrDefault(m =>
            {
                if (m.Name != methodName) return false;
                var parameters = m.GetParameters();
                if (parameters.Length != args.Length) return false;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i]))
                        return false;
                }
                return true;
            });
            Assert.That(method, Is.Not.Null, "Method not found: " + methodName + "(" + args.Length + ")");
            return (T)method.Invoke(obj, args);
        }
    }
}
