using System;
using System.Collections.Generic;
using System.Linq;
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
            int liveNeighbors = neighbors.Count(x => x.IsAlive);
            IsAliveNext = liveNeighbors == 3 || (IsAlive && liveNeighbors == 2);
        }

        public void Advance() => IsAlive = IsAliveNext;
    }

    public class Board
    {
        public Cell[,] Cells;
        public int CellSize;
        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);

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
            foreach (var c in liveCells)
                if (c.x >= 0 && c.x < Columns && c.y >= 0 && c.y < Rows)
                    Cells[c.x, c.y].IsAlive = true;
        }

        private Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells) cell.DetermineNextLiveState();
            foreach (var cell in Cells) cell.Advance();
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                {
                    int xL = x > 0 ? x - 1 : Columns - 1;
                    int xR = x < Columns - 1 ? x + 1 : 0;
                    int yT = y > 0 ? y - 1 : Rows - 1;
                    int yB = y < Rows - 1 ? y + 1 : 0;
                    var c = Cells[x, y];
                    c.neighbors.Add(Cells[xL, yT]);
                    c.neighbors.Add(Cells[x, yT]);
                    c.neighbors.Add(Cells[xR, yT]);
                    c.neighbors.Add(Cells[xL, y]);
                    c.neighbors.Add(Cells[xR, y]);
                    c.neighbors.Add(Cells[xL, yB]);
                    c.neighbors.Add(Cells[x, yB]);
                    c.neighbors.Add(Cells[xR, yB]);
                }
        }

        public int CountAlive()
        {
            int cnt = 0;
            foreach (var cell in Cells) if (cell.IsAlive) cnt++;
            return cnt;
        }

        public void SaveToFile(string filename)
        {
            var alive = new List<(int x, int y)>();
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive) alive.Add((x, y));
            string json = JsonSerializer.Serialize(new { Columns, Rows, CellSize, AliveCells = alive });
            File.WriteAllText(filename, json);
        }

        public static Board LoadFromFile(string filename)
        {
            string json = File.ReadAllText(filename);
            var data = JsonSerializer.Deserialize<BoardSaveData>(json);
            return new Board(data.Columns * data.CellSize, data.Rows * data.CellSize, data.CellSize, data.AliveCells);
        }

        public void LoadFigure(string filename, int offsetX, int offsetY)
        {
            if (!File.Exists(filename)) return;
            string[] lines = File.ReadAllLines(filename);
            for (int y = 0; y < lines.Length; y++)
                for (int x = 0; x < lines[y].Length; x++)
                {
                    int bx = offsetX + x;
                    int by = offsetY + y;
                    if (bx >= 0 && bx < Columns && by >= 0 && by < Rows)
                        Cells[bx, by].IsAlive = lines[y][x] == 'X' || lines[y][x] == '*';
                }
        }

        public void Render()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                    Console.Write(Cells[x, y].IsAlive ? '*' : ' ');
                Console.WriteLine();
            }
        }

        // ---------- Методы для исследования ----------
        public List<List<(int x, int y)>> GetClusters()
        {
            var visited = new bool[Columns, Rows];
            var clusters = new List<List<(int x, int y)>>();
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var cluster = new List<(int x, int y)>();
                        FloodFill(x, y, visited, cluster);
                        clusters.Add(cluster);
                    }
            return clusters;
        }

        private void FloodFill(int x, int y, bool[,] visited, List<(int x, int y)> cluster)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) return;
            if (visited[x, y] || !Cells[x, y].IsAlive) return;
            visited[x, y] = true;
            cluster.Add((x, y));
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        FloodFill(x + dx, y + dy, visited, cluster);
        }

        public string Classify(List<(int x, int y)> cluster)
        {
            int minX = cluster.Min(p => p.x);
            int minY = cluster.Min(p => p.y);
            var norm = cluster.Select(p => (x: p.x - minX, y: p.y - minY))
                              .OrderBy(p => p.x).ThenBy(p => p.y).ToList();

            if (norm.Count == 4 && norm.Contains((0,0)) && norm.Contains((1,0)) && norm.Contains((0,1)) && norm.Contains((1,1)))
                return "Блок";
            if (norm.Count == 6 && norm.Contains((1,0)) && norm.Contains((2,0)) && norm.Contains((0,1)) && norm.Contains((3,1)) && norm.Contains((1,2)) && norm.Contains((2,2)))
                return "Улей";
            if (norm.Count == 5 && norm.Contains((0,0)) && norm.Contains((1,0)) && norm.Contains((0,1)) && norm.Contains((2,1)) && norm.Contains((1,2)))
                return "Лодка";
            if (norm.Count == 3 && norm.Contains((0,0)) && norm.Contains((0,1)) && norm.Contains((0,2)))
                return "Мигалка";
            if (norm.Count == 5 && norm.Contains((0,1)) && norm.Contains((1,2)) && norm.Contains((2,0)) && norm.Contains((2,1)) && norm.Contains((2,2)))
                return "Планер";
            return "Другое";
        }

        public Dictionary<string, int> GetStatistics()
        {
            var clusters = GetClusters();
            var dict = new Dictionary<string, int>();
            foreach (var c in clusters)
            {
                string name = Classify(c);
                dict[name] = dict.GetValueOrDefault(name) + 1;
            }
            return dict;
        }
    }

    class BoardSaveData { public int Columns { get; set; } public int Rows { get; set; } public int CellSize { get; set; } public List<(int x, int y)> AliveCells { get; set; } }
    class Settings { public int Width { get; set; } = 80; public int Height { get; set; } = 40; public int CellSize { get; set; } = 1; public double LiveDensity { get; set; } = 0.3; public int UpdateIntervalMs { get; set; } = 200; }

    class Program
    {
        static Board board;
        static Settings settings = new Settings();

        static void LoadSettings()
        {
            string path = "Life/config.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                settings = JsonSerializer.Deserialize<Settings>(json);
            }
        }

        static void Reset() => board = new Board(settings.Width, settings.Height, settings.CellSize, settings.LiveDensity);
        static void Render() { Console.Clear(); board.Render(); Console.WriteLine($"\nЖивых клеток: {board.CountAlive()}"); }

        static void RunResearch()
        {
            Directory.CreateDirectory("Data");
            using (StreamWriter sw = new StreamWriter("Data/data.txt"))
            {
                for (double density = 0.1; density <= 0.9; density += 0.1)
                {
                    int totalGens = 0;
                    for (int trial = 0; trial < 5; trial++)
                    {
                        var test = new Board(settings.Width, settings.Height, settings.CellSize, density);
                        int prevAlive = test.CountAlive();
                        int stable = 0, gen = 0;
                        while (stable < 5 && gen < 500)
                        {
                            test.Advance();
                            int curAlive = test.CountAlive();
                            if (curAlive == prevAlive) stable++;
                            else stable = 0;
                            prevAlive = curAlive;
                            gen++;
                        }
                        totalGens += gen;
                    }
                    sw.WriteLine($"{density:F1} {totalGens / 5}");
                }
            }
            Console.WriteLine("Data/data.txt обновлён. Постройте график и сохраните как Data/plot.png");
        }

        static void Main(string[] args)
        {
            LoadSettings();
            Console.WriteLine("Нажмите R для исследования (создания data.txt), любую другую клавишу для игры.");
            if (Console.ReadKey().Key == ConsoleKey.R)
            {
                RunResearch();
                return;
            }
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
