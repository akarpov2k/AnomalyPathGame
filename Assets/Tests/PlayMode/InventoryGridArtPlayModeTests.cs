#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Проверяет шаблон сетки инвентаря: клетки заполняют область окна без остатка.</summary>
    public sealed class InventoryGridArtPlayModeTests
    {
        /// <summary>Сетка занимает область целиком и по ширине, и по высоте: пустой полосы не остаётся.</summary>
        [Test]
        public void CellSize_FillsAreaBothWays()
        {
            var area = new Vector2(736f, 688f);

            var cell = InventoryGridArt.CellSize(area, 8, 6, 4f);
            var grid = InventoryGridArt.GridSize(8, 6, cell, 4f);

            Assert.AreEqual(area.x, grid.x, 1e-3f, "Сетка должна занимать ширину области целиком");
            Assert.AreEqual(area.y, grid.y, 1e-3f, "Сетка должна занимать высоту области целиком");
            Assert.Greater(cell.y, cell.x, "У высокого окна клетка выше, чем шире");
        }

        /// <summary>Квадратная клетка считается от ширины: сетка занимает ширину области без остатка.</summary>
        [Test]
        public void CellSide_FitsWidthAndStaysSquare()
        {
            var area = new Vector2(736f, 688f);

            var side = InventoryGridArt.CellSide(area.x, 8, 4f);
            var grid = InventoryGridArt.GridSize(8, 1, new Vector2(side, side), 4f);

            Assert.AreEqual(area.x, grid.x, 1e-3f, "Сетка из восьми клеток занимает ширину области");
            Assert.Greater(side, 0f, "Сторона клетки положительная");
        }

        /// <summary>Число строк округляется вниз: лишняя строка ушла бы в прокрутку.</summary>
        [Test]
        public void FitRows_RoundsDownAndKeepsAtLeastOne()
        {
            Assert.AreEqual(11, InventoryGridArt.FitRows(856f, 68f, 4f), "11 полных строк");
            Assert.AreEqual(1, InventoryGridArt.FitRows(10f, 100f, 4f), "Меньше строки — одна");
        }

        /// <summary>Предмет и клетка считаются одной раскладкой: габарит включает зазоры между клетками.</summary>
        [Test]
        public void ItemSize_AndOffset_UseSameGridLayout()
        {
            var cell = new Vector2(60f, 90f);

            var size = InventoryGridArt.ItemSize(4, 2, cell, 4f);
            var offset = InventoryGridArt.Offset(2, 3, cell, 4f);

            Assert.AreEqual((4f * 60f) + (3f * 4f), size.x, 1e-3f, "Ширина предмета 4×2");
            Assert.AreEqual((2f * 90f) + (1f * 4f), size.y, 1e-3f, "Высота предмета 4×2");
            Assert.AreEqual(2f * (60f + 4f), offset.x, 1e-3f, "Смещение по колонке");
            Assert.AreEqual(3f * (90f + 4f), offset.y, 1e-3f, "Смещение по строке");
        }

        /// <summary>Разметка тонкая и полупрозрачная: текстура окна остаётся главной.</summary>
        [Test]
        public void LineStyle_IsThinAndDim()
        {
            Assert.AreEqual(1f, InventoryGridArt.LineThickness, 1e-3f, "Линия сетки — 1 px");
            Assert.Less(InventoryGridArt.LineColor.a, 0.5f, "Линия должна быть полупрозрачной");
        }
    }
}
