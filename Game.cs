﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleBattleships
{
    class Game
    {
        public readonly bool Multiplayer;
        private readonly int _boardSize;
        private int[] _selected = new int[2] { 0, 0 };

        public readonly struct Ship
        {
            public readonly string Name;
            public readonly int Length;
            public Ship(string name, int length)
            {
                Name = name;
                Length = length;
            }
        }
        private readonly Ship[] _ships = new Ship[]
        {
            new Ship("Aircraft Carrier", 5),
            new Ship("Battleship", 4),
            new Ship("Destroyer", 3),
            new Ship("Submarine", 3),
            new Ship("Cruiser", 2)
        };

        // Input options to pass back out of the input method idk
        // Thought it would be fun
        private enum Inputs
        {
            None,
            Select,
            Chat,
            Rotate
        }

        // Board and where both players have attempted a hit 
        private bool[,] _playerBoard;
        private bool[,]? _enemyBoard;
        private List<string> _playerHits = new List<string>();
        private List<string> _enemyHits = new List<string>();


        public Game(int boardSize = 10, bool multiplayer = false)
        {
            Multiplayer = multiplayer;
            _boardSize = boardSize > 0 ? boardSize : 1;
            _playerBoard = new bool[boardSize, boardSize];
            _enemyBoard = new bool[boardSize, boardSize];
        }
        // Optional premade/loaded board input idk (ignore for now)
        public Game(bool[,] playerBoard, bool[,] enemyBoard)
        {
            Multiplayer = false;
            _playerBoard = playerBoard;
            _enemyBoard = enemyBoard;
            _boardSize = playerBoard.Length;
        }

        // Run after the creation of a game to make the player choose ship locations
        public void SetUp()
        {
            // Filling an empty board
            char[,] board = new char[_boardSize, _boardSize];
            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    board[i, j] = '.';
                    _playerBoard[i, j] = false;
                }
            }

            // Making player choose a location for each ship
            bool rotation;
            foreach (Ship ship in _ships)
            {
                rotation = true;
                _selected = new int[] { 0, 0 };
                while (true)
                {
                    char[,] tempBoard = (char[,])board.Clone();

                    int[] selection = (int[])_selected.Clone();
                    int num = rotation ? 0 : 1;
                    for (int i = 0; i < ship.Length; i++)
                    {
                        if (selection[num] >= _boardSize) { break; }
                        tempBoard[selection[0], selection[1]] = 'X';
                        selection[num]++;
                    }

                    Draw(tempBoard);

                    Inputs input = Input(Console.ReadKey());
                    if (input == Inputs.Rotate) { rotation = rotation ? false : true; }
                    if (input == Inputs.Select)
                    {
                        if (CheckValid(_playerBoard, _selected, ship, rotation))
                        {
                            selection = (int[])_selected.Clone();
                            for (int i = 0; i < ship.Length; i++)
                            {
                                board[selection[0], selection[1]] = 'x';
                                _playerBoard[selection[0], selection[1]] = true;
                                selection[num]++;
                            }
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Invalid Placement");
                            Thread.Sleep(1500);
                        }
                    }
                }
            }

            if (!Multiplayer) { _enemyBoard = GenerateBoard(); }
            else
            {
                // TODO: Put code to get opponent's board over network here
            }

            _selected = new int[] { 0, 0 };
        }

        // Method to generate a battleship board, usually for the COM player
        private bool[,] GenerateBoard()
        {
            bool[,] output = new bool[_boardSize, _boardSize];

            // This is so fucking fucked holy shit
            // All of this sucks in so many ways but it's also fucking hilarious
            // Edit: Fucked it less by not making the 'CheckValid' parameter inputs on seperate lines
            Random random = new Random();
            foreach (Ship ship in _ships)
            {
                while (true)
                {
                    int[] selection = new int[] { random.Next(0, _boardSize), random.Next(0, _boardSize) };
                    bool rotation = random.Next(2) == 0;
                    if (CheckValid(output, selection, ship, rotation))
                    {
                        int num = rotation ? 0 : 1;
                        for (int i = 0; i < ship.Length; i++)
                        {
                            output[selection[0], selection[1]] = true;
                            selection[num]++;
                        }
                        break;
                    }
                }
            }

            return output;
        }

        // Checking if a given ship placement is valid
        private bool CheckValid(bool[,] board, int[] selection, Ship ship, bool rotation)
        {
            int num = rotation ? 0 : 1;
            int[] tempSelect = (int[])selection.Clone();
            if (tempSelect[num] + ship.Length >= _boardSize) { return false; }
            for (int i = 0; i < ship.Length; i++)
            {
                if (board[tempSelect[0], tempSelect[1]]) { return false; }
                tempSelect[num]++;
            }

            return true;
        }


        private bool PlayerTurn()
        {
            char[,] board = new char[_boardSize, _boardSize];
            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    // Checks for an existing hit or miss to display from the enemy board
                    if (_playerHits.Contains($"{i}{j}")) { board[i, j] = _enemyBoard[i, j] ? '*' : 'o'; }
                    else { board[i, j] = '.'; }
                }
            }

            _selected = new int[] { 0, 0 };
            while (true)
            {
                Draw(board);
                Inputs input = Input(Console.ReadKey());
                if (input == Inputs.Select)
                {
                    if (_playerHits.Contains($"{_selected[0]}{_selected[1]}")) { continue; }
                    
                    _playerHits.Add($"{_selected[0]}{_selected[1]}");
                    Console.WriteLine(_enemyBoard[_selected[0], _selected[1]] ? "Hit!!" : "Miss");
                    Thread.Sleep(1000);
                    return CheckWin(_enemyBoard, _playerHits);
                }
                if (input == Inputs.Chat && Multiplayer)
                {
                    // TODO: Multiplayer chat bullshit here
                }
            }
        }

        // TODO: Merge 'ComTurn' and 'EnemyTurn' as the latter half can be reused for both
        private bool ComTurn()
        {
            // TODO: Maybe make it target adjacent to succesful hits?
            Random random = new Random();
            int x;
            int y;
            while (true)
            {
                x = random.Next(0, _boardSize);
                y = random.Next(0, _boardSize);
                if (_enemyHits.Contains($"{x}{y}")) { continue; }
                _enemyHits.Add($"{x}{y}");
                break;
            }

            char[,] board = new char[_boardSize, _boardSize];
            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    // Checks for hits or misses the COM player has made
                    if (x == i && y == j) { board[i, j] = '+'; }
                    else if (_enemyHits.Contains($"{i}{j}")) { board[i, j] = _playerBoard[i, j] ? '*' : 'o'; }
                    else { board[i, j] = _playerBoard[i, j] ? 'x' : '.'; }
                }
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Draw(board, false);
            Console.ResetColor();
            Thread.Sleep(2000);

            return CheckWin(_playerBoard, _enemyHits);
        }

        private bool EnemyTurn()
        {
            // TODO: Multiplayer stuff
            return false; // Temporary
        }

        private bool CheckWin(bool[,] board, List<string> hits)
        {
            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    if (board[i, j] && !hits.Contains($"{i}{j}")) { return false; }
                }
            }

            return true;
        }

        public bool GameLoop()
        {
            SetUp();

            while (true)
            {
                if (Multiplayer)
                {
                    if (PlayerTurn()) { return true; }
                    if (EnemyTurn()) { return false; }
                }
                else
                {
                    if (PlayerTurn()) { return true; }
                    if (ComTurn()) { return false; }
                }
            }
        }

        private Inputs Input(ConsoleKeyInfo keyInfo)
        {
            // Directional inputs with a clamp to prevent going out of bounds
            if (keyInfo.Key == ConsoleKey.UpArrow) { _selected[0]--; }
            if (keyInfo.Key == ConsoleKey.DownArrow) { _selected[0]++; }
            if (keyInfo.Key == ConsoleKey.LeftArrow) { _selected[1]--; }
            if (keyInfo.Key == ConsoleKey.RightArrow) { _selected[1]++; }
            _selected[0] = Math.Clamp(_selected[0], 0, _boardSize - 1);
            _selected[1] = Math.Clamp(_selected[1], 0, _boardSize - 1);

            // Non-movement inputs (selection, rotation for placing ships, opening a chat feature for multiplayer)
            if (keyInfo.Key == ConsoleKey.Enter) { return Inputs.Select; }
            if (keyInfo.Key == ConsoleKey.R) { return Inputs.Rotate; }
            if (keyInfo.Key == ConsoleKey.T) { return Inputs.Chat; }
            return Inputs.None;
        }

        private void Draw(char[,] board, bool selectShow = true)
        {
            // Clears screen, resets cursor position, hides cursor
            string output = "\x1b[2J\x1b[H\x1b[?25l";

            output += "╔" + new string('═', _boardSize) + "╗\n";
            for (int i = 0; i < _boardSize; i++)
            {
                output += "║";
                for (int j = 0; j < _boardSize; j++)
                {
                    if (_selected[0] == i && _selected[1] == j && selectShow) { output += $"\x1b[4m{board[i, j]}\x1b[0m"; }
                    else { output += board[i, j]; }
                }
                output += "║\n";
            }
            output += "╚" + new string('═', _boardSize) + "╝";

            Console.WriteLine(output);
            Console.Write(
                "\x1b[0;14HKey:" +
                "\x1b[1;14H" + ". - Empty" +
                "\x1b[2;14H" + "x - Your Ship" +
                "\x1b[3;14H" + "* - Hit" +
                "\x1b[4;14H" + "o - Miss" +
                "\x1b[5;14H" + "+ - Last Enemy Selection" +
                "\x1b[13;0H");
        }
        
        private void DrawMultiplayer(char[,] playerBoard, char[,] enemyBoard, string message = "", bool selectShow = true)
        {
            int boardOffset = 50; //How many spaces away the second board is.
            string offset = new string(' ', boardOffset);
            // Clears screen, resets cursor position, hides cursor
            string output = "\x1b[2J\x1b[H\x1b[?25l";

            if (message.Length > (14 + boardOffset)) message = message.Substring(0, 11 + boardOffset) + "..."; // Truncate string if too long
            
            string messageString = ""; // String to add between two boards with message
            messageString += new string(' ', 13 + (boardOffset / 2) - (int)Math.Floor((float)message.Length / 2)) + message; // Line with message with appropriate spacing.
            messageString += new string(' ', 13 + (boardOffset / 2) - (int)Math.Ceiling((float)message.Length / 2));

            output += "╔" + new string('═', _boardSize) + "╗" + "                            " + offset + "╔" + new string('═', _boardSize) + "╗\n";
            for (int i = 0; i < _boardSize; i++)
            {
                output += "║";
                for (int j = 0; j < _boardSize; j++)
                {
                    if (_selected[0] == i && _selected[1] == j && selectShow) { output += $"\x1b[4m{playerBoard[i, j]}\x1b[0m"; }
                    else { output += playerBoard[i, j]; }
                }
                output += "║  ";
                switch (i){
                    case 0:
                        output += " . - Empty                " + offset;
                        break;
                    case 1:
                        output += " x - Your Ship            " + offset;
                        break;
                    case 2:
                        output += " * - Hit                  " + offset;
                        break;
                    case 3:
                        output += " o - Miss                 " + offset;
                        break;
                    case 4:
                        output += " + - Last Enemy Selection " + offset;
                        break;
                    case 6:
                        output += messageString;
                        break;
                    default:
                        output += "                          " + offset;
                        break;
                }
                output += "║";
                for (int j = 0; j < _boardSize; j++)
                {
                    if (_selected[0] == i && _selected[1] == j && selectShow) { output += $"\x1b[4m{enemyBoard[i, j]}\x1b[0m"; }
                    else { output += enemyBoard[i, j]; }
                }
                output += "║\n";
            }
            output += "╚" + new string('═', _boardSize) + "╝" + "                            " + offset + "╚" + new string('═', _boardSize) + "╝";

            Console.WriteLine(output);
        }
    }
}