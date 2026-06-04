using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buscaminas.Gameplay
{
    public class Board
    {
        public int Width { get; }
        public int Height { get; }

        public int MineCount { get; }

        public int Flags { get; private set; }

        public bool IsGameOver { get; private set; }

        public bool IsWin { get; private set; }

        public event Action OnWin;

        public event Action OnLose;

        private struct CellData
        {
            public bool isMine;
            public bool isRevealed;
            public bool isFlagged;
            public int adjacentMines;
        }

        private CellData[,] cells;
        private bool minesPlaced;
        public Board(int width, int height, int mineCount)
        {
            Width = width;
            Height = height;
            MineCount = mineCount;
            Flags = mineCount;
            cells = new CellData[width, height];
        }


        public bool IsMine(int x, int y) => cells[x, y].isMine;


        public bool IsRevealed(int x, int y) => cells[x, y].isRevealed;


        public bool IsFlagged(int x, int y) => cells[x, y].isFlagged;


        public int AdjacentMines(int x, int y) => cells[x, y].adjacentMines;


        public List<List<Vector2Int>> Reveal(int x, int y)
        {
            if (IsGameOver || cells[x, y].isRevealed || cells[x, y].isFlagged)
                return null;

            if (!minesPlaced)
            {
                minesPlaced = true;
                PlaceMines(x, y);
                CalculateNeighbors();
            }

            if (cells[x, y].isMine)
            {
                cells[x, y].isRevealed = true;
                GameOver(false);
                return null;
            }

            var levels = new List<List<Vector2Int>>();
            var visited = new bool[Width, Height];
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(x, y));
            visited[x, y] = true;

            while (queue.Count > 0)
            {
                int levelSize = queue.Count;
                var level = new List<Vector2Int>();

                for (int i = 0; i < levelSize; i++)
                {
                    Vector2Int current = queue.Dequeue();
                    int cx = current.x;
                    int cy = current.y;

                    if (cells[cx, cy].isRevealed || cells[cx, cy].isFlagged)
                        continue;

                    cells[cx, cy].isRevealed = true;
                    level.Add(current);

                    if (cells[cx, cy].adjacentMines == 0)
                    {
                        AddNeighborsToQueue(cx, cy, queue, visited);
                    }
                }

                if (level.Count > 0)
                    levels.Add(level);
            }

            CheckWinCondition();
            return levels;
        }

        public void ToggleFlag(int x, int y)
        {
            if (IsGameOver || cells[x, y].isRevealed) return;

            if (cells[x, y].isFlagged)
            {
                cells[x, y].isFlagged = false;
                Flags++;
            }
            else if (Flags > 0)
            {
                cells[x, y].isFlagged = true;
                Flags--;
            }
        }

        public void RevealAllMines()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (cells[x, y].isMine)
                    {
                        cells[x, y].isRevealed = true;
                    }
                }
            }
        }

        private void PlaceMines(int avoidX, int avoidY)
        {
            var candidates = new List<Vector2Int>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    bool inAvoidArea = (x >= avoidX - 1 && x <= avoidX + 1 &&
                                        y >= avoidY - 1 && y <= avoidY + 1);
                    if (!inAvoidArea)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            System.Random rng = new System.Random();
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                Vector2Int temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }

            int minesToPlace = Math.Min(MineCount, candidates.Count);
            for (int i = 0; i < minesToPlace; i++)
            {
                cells[candidates[i].x, candidates[i].y].isMine = true;
            }
        }

        private void CalculateNeighbors()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (cells[x, y].isMine) continue;

                    int mines = 0;
                    for (int xi = -1; xi <= 1; xi++)
                    {
                        for (int yi = -1; yi <= 1; yi++)
                        {
                            if (xi == 0 && yi == 0) continue;

                            int nx = x + xi;
                            int ny = y + yi;

                            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                            {
                                if (cells[nx, ny].isMine) mines++;
                            }
                        }
                    }
                    cells[x, y].adjacentMines = mines;
                }
            }
        }

        private void AddNeighborsToQueue(int x, int y, Queue<Vector2Int> queue, bool[,] visited)
        {
            for (int xi = -1; xi <= 1; xi++)
            {
                for (int yi = -1; yi <= 1; yi++)
                {
                    if (xi == 0 && yi == 0) continue;

                    int nx = x + xi;
                    int ny = y + yi;

                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        private void CheckWinCondition()
        {
            int revealedCount = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (cells[x, y].isRevealed && !cells[x, y].isMine) revealedCount++;
                }
            }

            if (revealedCount == (Width * Height) - MineCount)
            {
                GameOver(true);
            }
        }

        private void GameOver(bool win)
        {
            IsGameOver = true;
            if (win)
            {
                IsWin = true;
                OnWin?.Invoke();
            }
            else
            {
                IsWin = false;
                OnLose?.Invoke();
            }
        }
    }
}
