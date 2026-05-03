using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(int width, int height, int cellSize, List<(int x, int y)> liveCells)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            
            foreach (var cell in liveCells)
            {
                if (cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows)
                    Cells[cell.x, cell.y].IsAlive = true;
            }
        }

        private readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public int CountAlive()
        {
            int count = 0;
            foreach (var cell in Cells)
                if (cell.IsAlive) count++;
            return count;
        }

        public void SaveToFile(string filename)
        {
            var aliveCells = new List<(int x, int y)>();
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive)
                        aliveCells.Add((x, y));
            
            string json = JsonSerializer.Serialize(new { Columns, Rows, CellSize, AliveCells = aliveCells });
            File.WriteAllText(filename, json);
        }

        public static Board LoadFromFile(string filename)
        {
            string json = File.ReadAllText(filename);
            var data = JsonSerializer.Deserialize<BoardSaveData>(json);
            return new Board(data.Columns * data.CellSize, data.Rows * data.CellSize, data.CellSize, data.AliveCells);
        }

        public void Render()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    Console.Write(Cells[col, row].IsAlive ? '*' : ' ');
                }
                Console.Write('\n');
            }
        }
    }

    class BoardSaveData
    {
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int CellSize { get; set; }
        public List<(int x, int y)> AliveCells { get; set; }
    }

    class Settings
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.3;
        public int UpdateIntervalMs { get; set; } = 500;
    }

    class Program
    {
        static Board board;
        static Settings settings = new Settings();

        static void LoadSettings()
        {
            string settingsPath = "Life/settings.json";
            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize<Settings>(json);
            }
        }

        static void Reset()
        {
            board = new Board(
                width: settings.Width,
                height: settings.Height,
                cellSize: settings.CellSize,
                liveDensity: settings.LiveDensity);
        }

        static void Render()
        {
            Console.Clear();
            board.Render();
            Console.WriteLine($"\nЖивых клеток: {board.CountAlive()}");
        }

        static void Main(string[] args)
        {
            LoadSettings();
            Reset();

            while (true)
            {
                Render();
                board.Advance();
                Thread.Sleep(settings.UpdateIntervalMs);
            }
        }
    }
}
