using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using System.IO;
using System;
using System.Collections.Generic;

namespace Sudoku
{
    // стейт уровень
    class SudokuState
    {
        // текущая таблица с вводимимыми данными
        int[,] state = new int[9, 9];
        protected void SetState(int[,] newState)
        {
            int[,] result = new int[9, 9];
            Array.Copy(newState, result, 81);
            state = result;
        }
        protected int[,] GetState()
        {
            return state;
        }
        // изначальный уровень
        int[,] save = new int[9, 9];
        protected void SetSave(int[,] new_save)
        {
            int[,] result = new int[9, 9];
            Array.Copy(new_save, result, 81);
            save = result;
        }
        protected int[,] GetSave()
        {
            return save;
        }
        // решение
        int[,] winner = new int[9, 9];
        protected void SetWinner(int[,] new_winner)
        {
            int[,] result = new int[9, 9];
            Array.Copy(new_winner, result, 81);
            winner = result;
        }
        protected int[,] GetWinner()
        {
            return winner;
        }
    }
    // бэкенд уровень
    class SudokuMap : SudokuState
    {
        const int n = 3; // размероность подмассива массива 9х9 
        private string path = "./records.txt"; // путь к файлу со списком лидеров
        private int level = 0; // значение текущего уровня сложности 0 - дефолт - Лёгкий
        private bool is_active = false; // индикатор существует ли игра которую можно продолжить
        private Stopwatch stopwatch = Stopwatch.StartNew(); // таймер
        private string elapsed_time = ""; // значение таймера
        // метод запуска таймера
        protected void TimerStart()
        {
            stopwatch.Start();
        }
        // метод остановки таймера
        protected string TimerStop()
        {
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            elapsed_time = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            return elapsed_time;
        }
        // запись в сипок лидеров новых игроков
        protected void RecordWrite(string name)
        {
            StreamWriter SW = new StreamWriter(path, true, System.Text.Encoding.Default);
            SW.WriteLine(name + " " + elapsed_time);
            SW.Close();
        }
        // считывание списка лидеров
        protected List<string> RecordRead()
        {
            List<string> result = new List<string>();
            using (StreamReader sr = new StreamReader(path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    result.Add(line);
                }
            }
            return result;
        }
        // метод получения текущей позиции вводимых чисел
        protected int[,] Room()
        {
            return GetState();
        }

        private int[,] Transposition(int[,] map)
        {
            int[,] transposition_map = new int[n * n, n * n];

            for (int i = 0; i < n * n; i++)
            {
                for (int j = 0; j < n * n; j++)
                {
                    transposition_map[i, j] = map[j, i];
                }
            }
            map = transposition_map;
            return (map);
        }

        private int[,] Line(int[,] map, Random rnd)
        {
            int block = rnd.Next(0, n - 1);
            int line_one = rnd.Next(0, n);
            int line_two = rnd.Next(0, n);
            int lo = block * n + line_one;
            int lt = block * n + line_two;
            for (int j = 0; j < n * n; j++)
                (map[lo, j], map[lt, j]) = (map[lt, j], map[lo, j]);
            return (map);
        }

        private int[,] Column(int[,] map, Random rnd)
        {
            int block = rnd.Next(0, n - 1);
            int column_one = rnd.Next(0, n);
            int column_two = rnd.Next(0, n);
            int co = block * n + column_one;
            int ct = block * n + column_two;
            for (int i = 0; i < n * n; i++)
                (map[i, co], map[i, ct]) = (map[i, co], map[i, ct]);

            return (map);
        }
        // метод - генератор уровня на основе рандома
        private void Hide(ref int[,] map, Random rnd)
        {
            int chance = 0, null_check = 0, removed = 0;

            if (level == 0)
            {
                removed = 40;
            }
            if (level == 1)
            {
                removed = 54;
            }
            if (level == 2)
            {
                removed = 61;
            }

            while (removed != 0)
            {
                for (int i = 0; i < n * n; i++)
                {
                    for (int j = 0; j < n * n; j++)
                    {
                        chance = rnd.Next(0, 3);
                        if (chance == 0 && map[i, j] != 0 && removed > 0)
                        {
                            null_check = NullCheck(map, i, j);
                            if (null_check == 0)
                            {
                                map[i, j] = 0;
                                removed--;
                            }

                        }
                    }
                }
            }
        }

        private int NullCheck(int[,] map, int i, int j)
        {
            int line = 0, column = 0;
            for (int k = 0; k < n * n; k++)
            {
                if (map[i, k] == 0)
                {
                    line++;
                }
                if (map[k, j] == 0)
                {
                    column++;
                }
            }
            if (line == n * n - 1)
                return line;
            else if (column == n * n - 1)
                return column;
            else return 0;
        }
        // метод смены уровня сложности, автоматически сбрасывает предыдущую игру
        protected void LevelChoosing(int new_level)
        {
            level = new_level;
            is_active = false;

        }
        // метод отдающий выбранный уровень сложности
        protected int GetLevel()
        {
            return level;
        }
        // метод отдающий информацию о том есть ли игра для продолжнения
        protected bool GetIsActive()
        {
            return is_active;
        }
        // функция создания новой игры
        protected void CreateRoom()
        {
            stopwatch = Stopwatch.StartNew();
            is_active = true;
            int[,] map = Room();
            for (int i = 0; i < n * n; i++)
            {
                for (int j = 0; j < n * n; j++)
                {
                    map[i, j] = ((i * n + i / n + j) % (n * n) + 1);
                }
            }
            Random rnd = new Random();

            int create = rnd.Next(n * n * n * n, n * n * n * n * n * n * n * n);
            for (int i = 0; i < create; i++)
            {
                map = Transposition(map);
                map = Line(map, rnd);
                map = Column(map, rnd);
            }
            SetWinner(map);
            Hide(ref map, rnd);
            SetSave(map);
            SetState(map);
            elapsed_time = "";
        }
        protected void GetCell(int x, int y, int num)
        {
            int[,] transfer_cell = GetState();
            transfer_cell[x - 1, y - 1] = num;
            SetState(transfer_cell);
        }
        protected bool InputValidation(int x, int y)
        {
            int[,] validation_check = GetSave();

            bool Validation;
            if (validation_check[x - 1, y - 1] != 0)
            {

                Validation = false;
                return (Validation);

            }
            else
            {
                Validation = true;
                return (Validation);
            }
        }
        // метод проверки результата, сравнивает текущий массив с победными массивом
        protected bool WinCheck()
        {
            int[,] win_pretendent = GetState();
            int[,] win_variant = GetWinner();
            bool wincheck = true;
            for (int i = 0; i < win_pretendent.GetLength(0); i++)
            {
                for (int j = 0; j < win_pretendent.GetLength(1); j++)
                {
                    if (win_pretendent[i, j] != win_variant[i, j])
                    {
                        wincheck = false;
                        break;
                    }
                }
            }
            if (wincheck) is_active = false;
            return wincheck;
        }
        // дев метод для автозаполнения верными данными
        protected int[,] DeveloperWin()
        {
            int[,] devel = GetWinner();
            SetState(devel);
            return devel;
        }
        protected enum Error
        {
            no_key,
            same_elems_str,
            same_elems_col,

        }
        protected int ErrorCheck()
        {
            int error = 0;
            int[,] map = GetState();
            for (int X = 0; X < n * n; X++)
            {
                for (int Y = 0; Y < n * n; Y++)
                {
                    if (map[X, Y] != 0)
                    {
                        for (int j = 0; j < n * n; j++)
                        {
                            if ((map[X, j] == map[X, Y]) && (j != Y))
                            {
                                error = 1;
                                break;
                            }
                        }
                        for (int i = 0; i < n * n; i++)
                        {
                            if ((map[i, Y] == map[X, Y]) && (i != X))
                            {
                                error = 2;
                                break;
                            }
                        }
                    }
                }
            }
            return error;
        }
    }
    // UI-уровень
    class SudokuUi : SudokuMap
    {
        // вывод меню
        private void StartMenu()
        {
            bool start_menu_active = true;
            bool app_active = true;
            bool you_win_active = false;
            do
            {
                do
                {
                    try
                    {
                        Console.WriteLine("----МЕНЮ----");
                        if (GetIsActive()) Console.WriteLine("0. Продолжить игру");
                        Console.WriteLine("1. Начать игру");
                        Console.WriteLine("2. Режим сложности");
                        Console.WriteLine("3. Список лидеров");
                        Console.WriteLine("4. Выход");
                        int input = Convert.ToInt32(Console.ReadLine());
                        if (input < 0 || input > 4)
                        {
                            throw new Exception("Ошибка");
                        }
                        else
                        {
                            if (input == 0)
                            {
                                TimerStart();
                                start_menu_active = false;
                                Console.Clear();
                                break;
                            }
                            if (input == 1)
                            {
                                CreateRoom();
                                start_menu_active = false;
                                TimerStart();
                                Console.Clear();
                                break;
                            }
                            if (input == 2)
                            {
                                ChangeDifficulty();
                                Console.Clear();
                            }
                            if (input == 3)
                            {
                                Console.Clear();
                                foreach (string record in RecordRead())
                                {
                                    Console.WriteLine(record);
                                }
                                Console.WriteLine();
                            }
                            if (input == 4)
                            {
                                start_menu_active = false;
                                app_active = false;
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Clear();
                        Console.WriteLine("Неверный формат ввода.");
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                }
                while (start_menu_active);
                while (app_active)
                {
                    try
                    {
                        PrintSudoku();
                        Console.WriteLine("1. Ввести число");
                        Console.WriteLine("2. Завершить игру");
                        Console.WriteLine("3. В главное меню");
                        int input = Convert.ToInt32(Console.ReadLine());
                        if (input < 1 || (input > 4))
                        {
                            throw new Exception("Ошибка");
                        }
                        else
                        {
                            if (input == 1)
                            {
                                InputCell();
                                Console.Clear();
                            }
                            if (input == 2)
                            {
                                if (WinCheck())
                                {
                                    Console.Clear();
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Поздравляю, вы победили");
                                    Console.WriteLine("Ваше время: {0}", TimerStop());
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    you_win_active = true;
                                    break;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Clear();
                                    Console.WriteLine("Решение неверное, проверьте заполнены ли все клетки, нет ли одинаковых значений в каждом ряду и колонке");
                                    Console.ForegroundColor = ConsoleColor.Black;
                                }

                            }
                            if (input == 3)
                            {
                                TimerStop();
                                start_menu_active = true;
                                Console.Clear();
                                break;
                            }
                            if (input == 4)
                            {
                                Console.Clear();
                                DeveloperWin();
                                Console.WriteLine("DEVELOPMENTMODE");
                                int[,] a = GetWinner();

                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Clear();
                        Console.WriteLine("Неверный формат ввода.");
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                }
                while (you_win_active)
                {

                    Console.WriteLine("Желаете ли вы отправить данные и занять место в сипке лидеров?");
                    Console.WriteLine("1. Да!");
                    Console.WriteLine("2. Нет, спасибо");
                    int input = Convert.ToInt32(Console.ReadLine());
                    if (input < 1 || (input > 2))
                    {
                        throw new Exception("Ошибка");
                    }
                    else
                    {
                        if (input == 1)
                        {
                            Console.WriteLine("Введите ваше имя");
                            string name = Console.ReadLine();
                            RecordWrite(name);
                            you_win_active = false;
                            Console.Clear();
                            start_menu_active = true;
                            break;

                        }
                        if (input == 2)
                        {
                            Console.Clear();
                            you_win_active = false;
                            start_menu_active = true;
                            break;
                        }
                    }


                }
            }
            while (app_active);
        }
        // Вывод в консоль поля судоку
        private void PrintSudoku()
        {
            int n = 9;
            int[,] mas = Room();
            Console.Write(String.Format("{0,3}", '|'));
            for (int j = 1; j <= n; j++)
                Console.Write(String.Format("{0,2}", j));
            Console.WriteLine();
            Console.Write("   --------------------");
            Console.WriteLine();
            for (int i = 0; i < n; i++)
            {
                Console.Write(String.Format("{0,0}{1,0} ", i + 1, '}'));
                for (int j = 0; j < n; j++)
                {
                    if (j != n - 1)
                    {
                        Console.Write(String.Format("{0,1}{1,1}", '|', mas[i, j] == 0 ? ' ' : mas[i, j].ToString()));
                    }
                    else
                    {
                        Console.Write(String.Format("{0,1}{1,1}{2,1}", '|', mas[i, j] == 0 ? ' ' : mas[i, j].ToString(), '|'));
                    }
                }
                Console.WriteLine();
            }
            Console.Write("   -------------------");
            Console.WriteLine();
            Error error = (Error)ErrorCheck();
            Console.ForegroundColor = ConsoleColor.Blue;
            switch (error)
            {
                case Error.no_key:
                    break;
                case Error.same_elems_str:
                    Console.WriteLine("\nПодсказка: Одинаковые элементы в строке\n");
                    break;
                case Error.same_elems_col:
                    Console.WriteLine("\nПодсказка: Одинаковые элементы в столбце\n");
                    break;
            }
            Console.ForegroundColor = ConsoleColor.Black;
        }
        // Метод ввода значение в клетку
        private void InputCell()
        {
            int x_coord;
            int y_coord;
            int value;
            do
            {
                try
                {
                    Console.WriteLine("Введите строку:");
                    int x = Convert.ToInt32(Console.ReadLine());
                    if (x < 1 || x > 9)
                    {
                        throw new Exception("Ошибка");
                    }
                    else
                    {
                        x_coord = x;
                        break;
                    }
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Неверный формат ввода.");
                    Console.ForegroundColor = ConsoleColor.Black;
                }
            }
            while (true);
            do
            {
                try
                {
                    Console.WriteLine("Введите столбец:");
                    int y = Convert.ToInt32(Console.ReadLine());
                    if (y < 1 || y > 9)
                    {
                        throw new Exception("Ошибка");
                    }
                    else
                    {
                        y_coord = y;
                        break;
                    }
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Неверный формат ввода.");
                    Console.ForegroundColor = ConsoleColor.Black;
                }
            }
            while (true);
            do
            {
                try
                {
                    Console.WriteLine("Введите число для выбранной клетки:");
                    int num = Convert.ToInt32(Console.ReadLine());
                    if (num < 1 || num > 9)
                    {
                        throw new Exception("Ошибка");
                    }
                    else
                    {
                        value = num;
                        break;
                    }
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Неверный формат ввода.");
                    Console.ForegroundColor = ConsoleColor.Black;
                }
            }
            while (true);
            try
            {
                if (InputValidation(x_coord, y_coord))
                {
                    GetCell(x_coord, y_coord, value);
                }
                else
                {
                    throw new Exception("Ошибка");
                }
            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Вы ввели занятую ячейку\nНачните сначала");
                Console.ForegroundColor = ConsoleColor.Black;
                InputCell();

            }
        }
        // метод смены уровня сложности
        private void ChangeDifficulty()
        {
            string[] difficulty_arr = new string[3] { "Легкий", "Средниий", "Сложный" };
            int active_difficulty = GetLevel();
            Console.WriteLine(String.Format("Текущий уровень сложности: {0,0}", difficulty_arr[active_difficulty]));
            do
            {
                try
                {
                    Console.WriteLine("Выберите уровень сложности:");
                    Console.WriteLine("1. Легкий");
                    Console.WriteLine("2. Средний");
                    Console.WriteLine("3. Сложный");
                    int x = Convert.ToInt32(Console.ReadLine());
                    if (x < 1 || x > 3)
                    {
                        throw new Exception("Ошибка");
                    }
                    else
                    {
                        if (active_difficulty != x - 1)
                        {
                            LevelChoosing(x - 1);
                        }
                        Console.WriteLine("Сохранено!");
                        break;
                    }
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Неверный формат ввода.");
                    Console.ForegroundColor = ConsoleColor.Black;
                }
            }
            while (true);
        }

        static void Main()
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Clear();
            SudokuUi map = new SudokuUi();
            map.StartMenu();
        }
    }
}