using FirstPuzzle.Editor;
using NUnit.Framework;
using UnityEngine;

namespace FirstPuzzleTests
{
    public class PuzzleImageLayoutTests
    {
        [TestCase(648, 648, 3, 3)]
        [TestCase(850, 850, 3, 3)]
        [TestCase(1000, 950, 3, 3)]
        [TestCase(850, 1243, 3, 4)]
        [TestCase(600, 1000, 3, 5)]
        [TestCase(500, 900, 3, 5)]
        [TestCase(850, 1870, 3, 7)]
        [TestCase(850, 661, 4, 3)]
        [TestCase(1000, 547, 6, 3)]
        [TestCase(100, 1000, 3, 8)]
        [TestCase(1000, 100, 6, 3)]
        public void GridMatchesSourceAspect(int width, int height, int columns, int rows)
        {
            Assert.AreEqual(new Vector2Int(columns, rows), PuzzleImageLayout.ChooseGrid(width, height));
            Assert.AreEqual(PuzzleImageLayout.ChooseGrid(width, height),
                PuzzleImageLayout.ChooseGrid(width * 2, height * 2), "Pixel density must not change puzzle difficulty.");
        }

        [TestCase(0, 100)]
        [TestCase(100, 0)]
        public void InvalidDimensionsAreRejected(int width, int height)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => PuzzleImageLayout.ChooseGrid(width, height));
        }
    }
}
