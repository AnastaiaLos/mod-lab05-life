using cli_life;

namespace Tests
{
    [TestClass]
    public class BoardTests
    {
        private Board CreateEmptyBoard(int w = 5, int h = 5)
        {
            return new Board(w, h, 1, 0);
        }

        [TestMethod]
        public void Board_Creates_Correct_Columns()
        {
            var board = new Board(10, 5, 1, 0);
            Assert.AreEqual(10, board.Columns);
        }

        [TestMethod]
        public void Board_Creates_Correct_Rows()
        {
            var board = new Board(10, 5, 1, 0);
            Assert.AreEqual(5, board.Rows);
        }

        [TestMethod]
        public void All_Cells_Are_Created()
        {
            var board = new Board(4, 4, 1, 0);
            Assert.AreEqual(16, board.Cells.Cast<Cell>().Count());
        }

        [TestMethod]
        public void Randomize_ZeroDensity_Makes_All_Dead()
        {
            var board = new Board(5, 5, 1, 0);
            Assert.AreEqual(0, board.Cells.Cast<Cell>().Count(c => c.IsAlive));
        }

        [TestMethod]
        public void Randomize_OneDensity_Makes_All_Alive()
        {
            var board = new Board(5, 5, 1, 1);
            Assert.AreEqual(25, board.Cells.Cast<Cell>().Count(c => c.IsAlive));
        }

        [TestMethod]
        public void Every_Cell_Has_8_Neighbors()
        {
            var board = CreateEmptyBoard();
            Assert.AreEqual(8, board.Cells[0, 0].neighbors.Count);
        }

        [TestMethod]
        public void Corner_Cell_Has_Wrapped_Neighbor()
        {
            var board = CreateEmptyBoard();
            Assert.IsTrue(board.Cells[0, 0].neighbors.Contains(board.Cells[4, 4]));
        }

        [TestMethod]
        public void Single_Alive_Cell_Dies()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 2].IsAlive = true;
            board.Advance();
            Assert.IsFalse(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Alive_Cell_With_Two_Neighbors_Survives()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Alive_Cell_With_Three_Neighbors_Survives()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Alive_Cell_With_Four_Neighbors_Dies()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[3, 2].IsAlive = true;
            board.Advance();
            Assert.IsFalse(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Dead_Cell_With_Three_Neighbors_Becomes_Alive()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Dead_Cell_With_Two_Neighbors_Stays_Dead()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Advance();
            Assert.IsFalse(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Block_StillLife_Remains_Unchanged()
        {
            var board = CreateEmptyBoard();
            board.Cells[1, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 1].IsAlive);
            Assert.IsTrue(board.Cells[1, 2].IsAlive);
            Assert.IsTrue(board.Cells[2, 1].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
        }

        [TestMethod]
        public void Blinker_Changes_To_Horizontal()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Advance();
            Assert.IsTrue(board.Cells[1, 2].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
            Assert.IsTrue(board.Cells[3, 2].IsAlive);
        }

        [TestMethod]
        public void Blinker_Returns_After_Two_Steps()
        {
            var board = CreateEmptyBoard();
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            board.Cells[2, 3].IsAlive = true;
            board.Advance();
            board.Advance();
            Assert.IsTrue(board.Cells[2, 1].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
            Assert.IsTrue(board.Cells[2, 3].IsAlive);
        }

        [TestMethod]
        public void Advance_Does_Not_Change_Cell_Count_For_Block()
        {
            var board = CreateEmptyBoard();
            board.Cells[1, 1].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            int before = board.Cells.Cast<Cell>().Count(c => c.IsAlive);
            board.Advance();
            int after = board.Cells.Cast<Cell>().Count(c => c.IsAlive);
            Assert.AreEqual(before, after);
        }

        [TestMethod]
        public void Empty_Board_Remains_Empty()
        {
            var board = CreateEmptyBoard();
            board.Advance();
            Assert.AreEqual(0, board.Cells.Cast<Cell>().Count(c => c.IsAlive));
        }
    }
}
