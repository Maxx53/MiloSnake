﻿// <copyright file="SnakeGame.cs" company="Maxx53">
// Copyright (c) 2015 All Rights Reserved
// </copyright>
// <author>Maxx53</author>
// <date>27/07/2015</date>
// <summary>Simple snake game, created for WinForms using GDI+</summary>

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace Maxx53.Games
{
    class SnakeGame
    {
        #region Игровые классы

        //Определяем набор возможных направлений движения нашей змейки
        enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        //Базовый класс - игровой объект
        //Любой объект на игровом поле имеет позицию (координаты) и картинку (текстуру)
        class GameObject
        {
            //Конструктор
            public GameObject(int x, int y, int imgN)
            {
                this.Pos = new Point(x, y);
                this.imgIndex = imgN;
            }

            //Свойства класса
            //Виртуальная позиция изображения, реальня позиция объекта
            private Point pos;
            public Point Pos
            {
                get
                {
                    return pos;
                }
                set
                {
                    pos = value;
                    //При изменении свойства, рассчитываем реальную позицию изображения на игровом поле (координаты с учетом размера пикселя)
                    CalculateImagePos();
                }
            }

            public void CalculateImagePos()
            {
                this.imgPos = new Point(pos.X * PixelLen, pos.Y * PixelLen);
            }

            public int imgIndex { set; get; }
            public Point imgPos { set; get; }
        }

        //Сегмент змейки, наследник базового класса GameObject
        class SnakeSegment : GameObject
        {
            public SnakeSegment(int x, int y, int imgN, Direction dir): base(x, y, imgN)
            {
                //Добавляем всего одной свойство - направление, в которое повернута текстура
                Dir = dir;
            }

            //Изрбражение (текстуру) достаем из списка, по номеру
            public void ChangeImg(int start)
            {
                //start - указывает на тип сегмента (позиция в списке)
                //0-3 голова, 4-7 тело, 8-11 хвост, 12-15 поворот
                switch (Dir)
                {
                    case Direction.Down:
                        imgIndex = start;
                        break;
                    case Direction.Right:
                        imgIndex = start + 1;
                        break;
                    case Direction.Left:
                        imgIndex = start + 2;
                        break;
                    case Direction.Up:
                        imgIndex = start + 3;
                        break;
                }
            }

            //Метод для удобного копирования сегмента
            public SnakeSegment Copy()
            {
                return new SnakeSegment(Pos.X, Pos.Y, imgIndex, Dir);
            }

            //Единственное свойство наследника - направление, в которое повернута текстура
            public Direction Dir { set; get; }
        }

        #endregion

        #region Объявление переменных и констант

        //Исходные картинки текстур, их изменять не будем
        private static Image appleScr = new Bitmap(texturePath + "apple.png");
        private static Image grassScr = new Bitmap(texturePath + "grass.jpg");
        private static Image rocks_texture = new Bitmap(texturePath + "rocks.png");
        private static Image snake_texture = new Bitmap(texturePath + "snake_texture.png");
        private static Image chicken_texture = new Bitmap(texturePath + "chicken.png");

        //С этими картинками работаем
        private static Image appleImg;
        private static Image grassImg;
        private static Image background;

        //Список рисунков разных камней, для разнообразия
        private static List<Image> rocksTexList = new List<Image>();

        //Список изображений, в котором текстуры змейки.
        //4 разных сегмента, по 4 копии для каждого направления + 4 копии головы для эффекта смерти
        private static List<Image> snakeTexList = new List<Image>();

        //Список изображений, в котором хранятся текстуры курицы, 4 копии для каждого направления
        private static List<Image> chickenTexList = new List<Image>();

        //Змейка состоит из списка сегментов, каждый сегмент должен иметь свои координаты.
        //Представим сегмент точкой, а саму змейку - списком точек.
        private List<SnakeSegment> snake = new List<SnakeSegment>();

        //Список объектов, камней, сюда загружаются координаты камней из левелов
        private List<GameObject> rocks = new List<GameObject>();

        //Еда для змейки, список яблок и куриц
        private List<GameObject> apples = new List<GameObject>();
        private List<GameObject> chickens = new List<GameObject>();

        //Список строк, тут храним левелы (расположение камней)
        private static List<string> levels = new List<string>();

        //Текущее направление движения, по умолчанию вниз
        private Direction direction = Direction.Down;

        //Таймер, по которому будет происходить обновление позиции (координат сегментов) нашей змейки
        private Timer gameTimer =  new Timer();

        //Логическое значение, отвечающее за смерть змейки.
        private bool gameOver = false;

        //Фиксированный размер игрового поля в пикселях, его не меняем.
        private static Size bounds = new Size(40, 30);

        //Реальный размер игрового поля, рассчитывается умножением размера игрового поля на размер пикселя
        private static Size realSize;

        //Точка по которой сдвигается игровое поле при отрисовке (нужно для установки области отрисовки по центру формы)
        private static Point transform = new Point(0, 0);

        //Очки
        private int gameScore = 0;

        //Формат строки для отрисовки текста
        private StringFormat sf = new StringFormat();

        //Шрифт, которым будет рисоваться текст
        private Font drawFont;

        //Центр игрового поля, для рисования текста
        private static Point screenCenter;

        //Рандомайзер
        private Random random = new Random();

        //Через плеер будем проигрывать wave-файлы
        private SoundPlayer appleCrunch = new SoundPlayer(soundsPath + "apple.wav");
        private SoundPlayer chickenScream = new SoundPlayer(soundsPath + "chicken.wav");
        private SoundPlayer hitSound = new SoundPlayer(soundsPath + "hit.wav");

        //Событие, которое будет вызываться по нажатию на клавишу Esc
        public event EscPressHandler PressEsc;
        public EventArgs e = null;
        public delegate void EscPressHandler(object sg, EventArgs e);

        //Константы, указывающие в папку с ресурсами
        private const string texturePath = "data/textures/";
        private const string levelPath = "data/levels/";
        private const string soundsPath = "data/sounds/";

        //Константы с текстом
        private const string overText = "Игра окончена!";
        private const string infoText = "Нажмите R, чтобы начать случайный уровень\r\nили Esc, чтобы вернуться к выбору уровня.";

        #endregion

        #region Свойства класса

        //Канвас - холст, на котором будем рисовать игру.
        private Control canvas { set; get; }

        //Показывается ли игровое поле, требуется для события изменения размера формы
        private bool GameShowed { set; get; }

        //Выключен звук или нет
        public bool Mute { get; set; }

        //Скорость игры
        private int gameSpeed = 10;
        public int GameSpeed
        {
            get
            {
                return gameSpeed;
            }
            //При назначении свойства меняем интервал игрового таймера
            set
            {
                gameSpeed = value;
                gameTimer.Interval = 1000 / gameSpeed;
            }
        }

        //Количество яблок на поле
        private int appleCount = 10;
        public int AppleCount
        {
            get
            {
                return appleCount;
            }
            //При изменение свойства делаем проверку, ограничиваем максимальное количество
            set
            {
                if (value < 50)
                    appleCount = value;
                else
                    appleCount = 50;
            }
        }

        //Количество куриц на поле
        private int chickCount = 5;
        public int ChickCount
        {
            get
            {
                return chickCount;
            }
            //При изменение свойства делаем проверку, ограничиваем максимальное количество
            set
            {
                if (value < 50)
                    chickCount = value;
                else
                    chickCount = 50;
            }
        }

        //Свойство, только для чтения, возвращает количество загруженных уровней
        public int LevelCount
        {
            get
            {
                return levels.Count();
            }
        }

        //Размер пикселя, из которых состоит игровое поле
        private static int pixelLen = 20;
 
        private static int PixelLen
        {
            get
            {
                return pixelLen;
            }
            set
            {
                //Во избежание проблем с мизерным скейлингом
                if (value == 0)
                    pixelLen = 1;
                else 
                    pixelLen = value;
            }
        }

        #endregion

        #region Загрузка игры

        //Инициализация класса
        public SnakeGame(Control control)
        {
            try
            {
                //Предаем форму в канвас, добавляем на него события отрисовки и нажатия клавишей
                canvas = control;
                canvas.Paint += new PaintEventHandler(canvas_Paint);
                canvas.KeyDown += new KeyEventHandler(canvas_KeyDown);
                canvas.KeyUp += new KeyEventHandler(canvas_KeyUp);
                canvas.Resize += new EventHandler(canvas_Resize);
                canvas.MouseDown +=new MouseEventHandler(canvas_MouseDown);
                canvas.MouseMove += new MouseEventHandler(canvas_MouseMove);
                canvas.MouseUp += new MouseEventHandler(canvas_MouseUp);

                InitMenuStrip();

                //Определяем интервал игрового таймера и событие на тик (обновление позиции змейки)
                //Интервал определяет скорость змейки. Чем он выше, тем змейка медленее двигается
                gameTimer.Tick += new EventHandler(gameTimer_Tick);
                gameTimer.Interval = 1000 / gameSpeed;

                //Задаем свойства формата для рисования выравнивания текста строго по центру
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                //Предзагрузка звуков
                chickenScream.Load();
                appleCrunch.Load();
                hitSound.Load();

                //Загружаем левелы в список строк
                OpenLevelFiles();
            }
            //Ловим ошибку, в случае, если потеряна папка с данными
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void InitMenuStrip()
        {
            MenuStrip ms = new MenuStrip();
            ms.Visible = false;
            ms.Name = "ms";
            canvas.Controls.Add(ms);

            var fileStrip = new ToolStripMenuItem("Файл");

            var saveAs = new ToolStripMenuItem("Сохранить как...");
            saveAs.ShortcutKeys = Keys.Control | Keys.S;
            saveAs.Click += new EventHandler(saveAs_Click);
            var backToScr = new ToolStripMenuItem("Выход");
            backToScr.ShortcutKeys = Keys.Control | Keys.B;
            backToScr.Click += new EventHandler(backToScr_Click);
            fileStrip.DropDownItems.Add(saveAs);
            fileStrip.DropDownItems.Add(backToScr);


            var editStrip = new ToolStripMenuItem("Правка");
            var clearField = new ToolStripMenuItem("Очистить поле");
            clearField.ShortcutKeys = Keys.Control | Keys.C;
            clearField.Click += new EventHandler(clearField_Click);
            editStrip.DropDownItems.Add(clearField);


            ms.Items.Add(fileStrip);
            ms.Items.Add(editStrip);

        }

        //Загружаем левелы из ресурсов в список строк
        private void OpenLevelFiles()
        {
            levels.Clear();

            //Ищем текстовые файлы в папке с левелами
            var txtFiles = Directory.EnumerateFiles(levelPath, "*.txt");

            //Пробегаемся по списку файлов, добавляем их содержимое в список levels
            foreach (var item in txtFiles)
            {
                levels.Add(File.ReadAllText(item));
            }           
        }

        //Тестовый метод, генерит пустой левел в текстовый файл
        private void CreateEmptyLevel()
        {
            string level = string.Empty;

            for (int i = 0; i < bounds.Height; i++)
            {
                for (int j = 0; j < bounds.Width; j++)
                {
                    level += "0";
                }
                level += Environment.NewLine;
            }
            System.IO.File.WriteAllText("empty_level.txt", level);
        }


        public void StartNewGame(int level)
        {
            //Свойство, показывающее, показывается ли игровое поле, требуется для события изменения размера формы
            GameShowed = true;

            //Инициализация игрового поля
            //Расчет размеров текстур, положения поля на форме
            InitGameField();

            //Запускаем игру
            StartGame(level, false);
        }

        //Инициализация игрового поля
        //Расчет размеров текстур, положения поля на форме
        private void InitGameField()
        {
            var canW = canvas.ClientSize.Width;
            var canH = canvas.ClientSize.Height;

            //Определяем размер пикселя, в зависимости от размера холста
            if (canW < canH)
            {
                PixelLen = canW / bounds.Width;
            }
            else
            {
                PixelLen = canH / bounds.Height;
            }

            //Реальный размер игрового поля, фиксированный размер поля, умножаем на размер пикселя
            realSize = new Size(bounds.Width * PixelLen, bounds.Height * PixelLen);

            //Рассчитываем точку для смещения игрового поля в центр формы (холста)
            transform = new Point(canW / 2 - realSize.Width / 2, canH / 2 - realSize.Height / 2);

            //Задаем размер шрифта, равный размеру пикселя
            drawFont = new Font("Arial", pixelLen);

            //Изменяем размеры текстур в зависимости от размера пикселя
            appleImg = Utils.ResizeImage(appleScr, PixelLen, PixelLen);
            grassImg = Utils.ResizeImage(grassScr, realSize.Width, realSize.Height);

            //Для удобства, заранее определяем центр игрового поля
            screenCenter = new Point(realSize.Width / 2, realSize.Height / 2);

            //Загружаем текстуру камней в список
            LoadRocksTex();

            //Загружаем текстуру змеи в список
            LoadSnakeTex();

            //Загружаем текстуру курицы в список
            LoadChickenTex();
        }

        //Дербаним текстуру на 5 частей
        private void LoadRocksTex()
        {
            rocksTexList.Clear();

            var sourceWidth = rocks_texture.Width;

            //В цикле, выполняем 5 раз
            for (int i = 0; i < 5; i++)
            {
                //Определяем область для вырезания, квадрат, в цикле меняется его позиция только по Y
                Rectangle cropArea = new Rectangle(0, i * sourceWidth, sourceWidth, sourceWidth);

                //Изменяем размер картинки в соответсивии с размером пикселя
                var source = Utils.ResizeImage(Utils.CropImage(rocks_texture, cropArea), PixelLen, PixelLen);
                //Добавляем в список
                rocksTexList.Add(source);
            }
        }


        //Дербаним картинку текстуры на куски, размером с пиксель, копируем каждый с поворотом
        //Всего 4 области (голова, тело, хвост, поворот), по 4 копии на каждое направление, всего в списке 16 изображений
        private void LoadSnakeTex()
        {
            snakeTexList.Clear();

            var sourceWidth = snake_texture.Width;

            //Пробегаеся по картинке текстуры, снизу вверх. Внизу голова, сверху поворот.
            for (int i = 3; i >= 0; i--)
            {
                //Определяем область для вырезания, квадрат, в цикле меняется его позиция только по Y
                Rectangle cropArea = new Rectangle(0, i * sourceWidth, sourceWidth, sourceWidth);

                //Первая картника, которая уже смотрит вниз, ее вращать не требуется
                var source = Utils.ResizeImage(Utils.CropImage(snake_texture, cropArea), PixelLen, PixelLen);

                //Делаем 3 копии первой картинки, их будем вращать
                var source270 = new Bitmap(source);
                var source90 = new Bitmap(source);
                var source180 = new Bitmap(source);

                //Вращаем по остальным направлениям
                //Смотри вправо
                source270.RotateFlip(RotateFlipType.Rotate270FlipNone);
                //Смотрит влево
                source90.RotateFlip(RotateFlipType.Rotate90FlipNone);
                //Смотрит вверх
                source180.RotateFlip(RotateFlipType.Rotate180FlipNone);

                //Добавляем в список 4 копии текстуры, каждая повернута в своем направлении
                snakeTexList.Add(source);
                snakeTexList.Add(source270);
                snakeTexList.Add(source90);
                snakeTexList.Add(source180);
            }

            //Пачка изображений для создания эффекта сплющенной от удара головы
            int halfPix = PixelLen / 2;

            //Во избежание проблем в мизерном скейлинге
            if (halfPix == 0)
                halfPix = 1;

            //Добавляем 4 копии головы (по 1й для каждого направления) для создания эффекта смерти
            snakeTexList.Add(new Bitmap(snakeTexList[0], new Size(PixelLen, halfPix)));
            snakeTexList.Add(new Bitmap(snakeTexList[1], new Size(halfPix, PixelLen)));
            snakeTexList.Add(new Bitmap(snakeTexList[2], new Size(halfPix, PixelLen)));
            snakeTexList.Add(new Bitmap(snakeTexList[3], new Size(PixelLen, halfPix)));
        }

        private void LoadChickenTex()
        {
            chickenTexList.Clear();

            var sourceWidth = chicken_texture.Width;

            //Пробегаеся по картинке текстуры, снизу вверх.
            //Всего 3 картинки. Снизу курица, смотрящая вниз, посередине курица смотрящая направо, сверху курица, смотрящая вверх.
            for (int i = 2; i >= 0; i--)
            {
                //Определяем область для вырезания, квадрат размером с ширину текстуры, в цикле меняется его позиция только по Y
                Rectangle cropArea = new Rectangle(0, i * sourceWidth, sourceWidth, sourceWidth);

                //Первая картника, которая уже смотрит вниз, ее вращать не требуется
                var source = Utils.ResizeImage(Utils.CropImage(chicken_texture, cropArea), PixelLen, PixelLen);
                chickenTexList.Add(source);

                //Если середина, попадаем на курицу, смотряющую вправо
                if (i == 1)
                {
                    //Копируем ее и отражаем по оси Х, получается курица, смотрящая влево
                    var sourceFlip = new Bitmap(source);
                    sourceFlip.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    chickenTexList.Add(sourceFlip);
                }
            }

            //В итоге, в список добавлены 4 картинки, с курицей, смотрящей во все 4 направления
        }

        //Запуск игры
        private void StartGame(int levNum, bool skipLoad)
        {
            //Т.к. данный метод вызывается и после смерти змейки, надо возвращать все значения в дефолт (по умолчанию).
            gameOver = false;
            direction = Direction.Down;
            gameScore = 0;

            if (!skipLoad)
            //Чистим список от точек
            snake.Clear();
            apples.Clear();
            chickens.Clear();

            if (!skipLoad)
            //Загружаем список камней из уровня, добавляем змейку на стартовую позицию
            ParseLevel(levNum);

            //Рисуем фон с камнями из левела
            DrawLevel();

            //Генерируем еду, точку в случайной позиции в пределах игрового поля 
            //Яблоки
            for (int i = 0; i < appleCount; i++)
            {
                 apples.Add(GenerateFood());
            }

            //Курочки
            for (int i = 0; i < ChickCount; i++)
            {
                chickens.Add(GenerateFood());
            }

            //Запускаем таймер обновления позиции змейки и курицы
            gameTimer.Start();
        }

        //Загружаем камни из уровня
        private void ParseLevel(int levNum)
        {
            //Для начала загрузим из левела список камней

            //Чистим список камней, т.к. этот метод будет вызываться повторно
            rocks.Clear();

            string current = string.Empty;

            //Если номер левела ноль или больше количества загруженных левелов
            if ((levNum == 0) | (levNum > levels.Count))
            {
                //Выбираем случайный левел
                current = levels[random.Next(levels.Count)];
            }
            else
            {
                //Выбираем левел по указанному номеру
                current = levels[levNum - 1];
            }

            //В этих переменных храним позицию элемента уровня
            int xPos = 0;
            int yPos = 0;

            //Пробегаемся по символам строки
            for (int i = 0; i < current.Length; i++)
            {
                //Если натыкаемся на символ, означающий камень, добавляем в список камней камень с текущими координатами
                if (current[i] == '#')
                {
                    rocks.Add(new GameObject(xPos, yPos, 0));

                    //Двигаем координату Х на единицу
                    xPos++;
                }
                else
                    //Если натыкаемся на символ, означающий место змейки
                    if (current[i] == '@')
                    {
                        //Добавляем голову змейки. Первая точка в списке всегда голова.
                        //Первое изображение в списке текстур - голова, повернутая вниз
                        snake.Add(new SnakeSegment(xPos, yPos, 0, direction));
                        //Восьмое изображение в списке - хвост, смотрящий назад
                        snake.Add(new SnakeSegment(xPos, yPos - 1, 8, direction));
                        
                        //Двигаем координату Х на единицу
                        xPos++;
                    }
                    else
                        //Если натыкаемся на символ, означающий пустое место, просто двигаем координату Х на единицу
                        if (current[i] == '0')
                        {
                            xPos++;
                        }
                        else
                            //Если натыкаемся на символ перехода на новую строку
                            if (current[i] == '\r')
                            {
                                //Двигаем координату Y на единицу, а координату X обнуляем
                                yPos++;
                                xPos = 0;
                            }
            }
            
            //Камни загружены из левела, теперь можно их нарисовать!
        }

        private void SaveLevel()
        {
            string levelstr = string.Empty;

            char[,] arr = new char[bounds.Width, bounds.Height];
            
            for (int i = 0; i < rocks.Count; i++)
            {
                arr[rocks[i].Pos.X, rocks[i].Pos.Y] = '#';
            }

            arr[snake[0].Pos.X, snake[0].Pos.Y] = '@';
            arr[snake[1].Pos.X, snake[1].Pos.Y] = '@';


            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for (int j = 0; j < arr.GetLength(1); j++)
                {

                    if (arr[i, j] == '\0')
                        levelstr += '0';

                
                }
                levelstr += Environment.NewLine;
            }

            File.WriteAllText("123.txt", levelstr);
        }

        private void DrawLevel()
        {
            //Берем копию нашего фона, на нем будем рисовать (на исходной картинке рисовать нельзя)
            background = Utils.ResizeImage(grassImg, realSize.Width, realSize.Height);
            //Используем этот класс для рисования по картинке
            Graphics g = Graphics.FromImage(background);

            //Пробегаемся по списку камней и рисуем каждый в своей позиции
            for (int i = 0; i < rocks.Count; i++)
            {
                g.DrawImage(rocksTexList[random.Next(5)], rocks[i].imgPos);
            }
        }

        #endregion

        #region Перемещение игровых объектов

        //Тик игрового таймера, по нему происходит передвижение змейки и курицы
        private void gameTimer_Tick(object sender, EventArgs e)
        {
            //Передвигаем змейку на шаг в один пиксель
            MoveSnake();

            //Пробегаемся по списку куриц
            for (int i = 0; i < chickens.Count; i++)
            {
                //Передвигаем курочку на шаг в один пиксель
                MoveChicken(chickens[i]);
            }

            //Принудительно перерисовываем холст
            canvas.Invalidate();
        }

        //Метод для воспроизведения звука из wave-файла
        private void PlaySound(SoundPlayer sp)
        {
            //Если звук включен
            if (!Mute)
            {
                sp.Play();
            }
        }

        private void MoveSnake()
        {
            //Запоминаем позицию и направление головы до следующего шага
            //Понадобится для определения определения направления следующего сегмента
            Point oldHeadPos = snake[0].Pos;
            Direction oldDir = snake[0].Dir;

            //Теперь меняем позицию головы на один пиксель в сторону, в зависимости от текущего направления
            switch (direction)
            {
                case Direction.Right:
                    snake[0].Pos = new Point(snake[0].Pos.X + 1, snake[0].Pos.Y);
                    break;
                case Direction.Left:
                    snake[0].Pos = new Point(snake[0].Pos.X - 1, snake[0].Pos.Y);
                    break;
                case Direction.Up:
                    snake[0].Pos = new Point(snake[0].Pos.X, snake[0].Pos.Y - 1);
                    break;
                case Direction.Down:
                    snake[0].Pos = new Point(snake[0].Pos.X, snake[0].Pos.Y + 1);
                    break;
            }

            //Задаем направление и изображение с учетом направления
            snake[0].Dir = direction;
            //В методе ChangeImg, 0 - указывает на текстуру головы
            snake[0].ChangeImg(0);

            //В обратном цикле (начинаем с последнего элемента списка) прогоняем все точки змейки, кроме головы
            for (int i = snake.Count - 1; i > 0; i--)
            {
                //Сдвигаем каждую точку тела на позицию предыдущей точки в списке. Получается эффект движения.
                //В итоге, вторая точка займет место первой точки (головы), голову двигаем отдельно

                //Если любой сегмент, кроме исключением головы и следующего после головы
                if (i != 1)
                {
                    //Двигаем сегмент на позицию предыдущего
                    snake[i] = snake[i - 1].Copy();

                    //Если попался хвост
                    if (i == snake.Count - 1)
                    {
                        //Определеяем направление хвоста (костыль)
                        if (snake.Count > 3)
                            snake[i].Dir = snake[snake.Count - 3].Dir;
                        else 
                            snake[i].Dir = oldDir;

                        //Меняем изображение сегмента на хвост с учетом направления
                        snake[i].ChangeImg(8);
                    }

                }
                else
                {
                    //Если первый после головы сегмент. C него копируются последующие сегменты

                    snake[1].Pos = oldHeadPos;
                    snake[1].Dir = oldDir;

                    //Если хвост
                    if (i == snake.Count - 1)
                    {
                        //Задаем ему направление головы
                        snake[1].Dir = snake[0].Dir;
                        //Меняем изображение на хвост, с учетом направления
                        //8 - указывает на позицию текстуры с хвостом в списке
                        snake[1].ChangeImg(8);
                    }
                    else
                    {
                        //Если другой сегмент, прямое тело или поворот

                        //Если направление головы не равно направлению первого после головы сегмента, значит змейка поворачивает!
                        //Определеям направление поворота, чтобы задать сегменту правильное изображение
                        if (snake[0].Dir != snake[1].Dir)
                        {
                            if (((oldDir == Direction.Left) && (snake[0].Dir == Direction.Down)) || ((oldDir == Direction.Up) && (snake[0].Dir == Direction.Right)))
                            {
                                //90 градусов
                                snake[1].imgIndex = 14;
                            }
                            else
                                if (((oldDir == Direction.Right) && (snake[0].Dir == Direction.Down)) || ((oldDir == Direction.Up) && (snake[0].Dir == Direction.Left)))
                                {
                                    //180 градусов
                                    snake[1].imgIndex = 15;
                                }
                                else
                                    if (((oldDir == Direction.Right) && (snake[0].Dir == Direction.Up)) || ((oldDir == Direction.Down) && (snake[0].Dir == Direction.Left)))
                                    {
                                        //270 градусов
                                        snake[1].imgIndex = 13;
                                    }
                                    else
                                        //0 градусов
                                        snake[1].imgIndex = 12;

                        }
                        else
                            //Если змейка не поворачивает, рисуем текстуру прямого тела с учетом направления
                            snake[1].ChangeImg(4);
                    }
                }
            }

            //Проверяем наличие столкновений головы змейки с игровыми объектами
            CheckCollisions();
        }


        private void CheckCollisions()
        {
            //Проверяем наличие столкновения головы с границами игрового поля
            //Игровое поле представляет собой прямоугольник: (0, 0, bounds.Width, bounds.Height);
            if (checkBounds(snake[0].Pos))
                //Воткнулись в стену и умерли
                Die();


            //Проверяем наличие столкновений головы змейки со своим телом
            for (int j = 1; j < snake.Count; j++)
            {
                if (snake[0].Pos == snake[j].Pos)
                {
                    //Воткнулись в себя, умерли
                    Die();
                }
            }

            //Проверяем наличие столкновения головы с камнем
            //Пробегаем по списку камней. Если координаты головы и камня совпадают - смерть.

            if (checkRocks(snake[0].Pos))
                Die();

            //Пробегаемся по списку яблок
            for (int i = 0; i < apples.Count; i++)
            {
                //Проверяем наличие столкновения с едой
                if (snake[0].Pos.X == apples[i].Pos.X && snake[0].Pos.Y == apples[i].Pos.Y)
                {
                    //Кушаем
                    Eat();

                    //Проигрываем звук съеденного яблока
                    PlaySound(appleCrunch);

                    //Создаем новый кусок еды
                    apples[i] = GenerateFood();
                }
                
            }

            //Пробегаемся по списку куриц
            for (int i = 0; i < chickens.Count; i++)
            {
                if (snake[0].Pos.X == chickens[i].Pos.X && snake[0].Pos.Y == chickens[i].Pos.Y)
                {
                    //Пищевая ценность курицы равняется 3м яблокам!
                    for (int j = 0; j < 3; j++)
                    {
                        Eat();
                    }

                    //Проигрываем звук умирающей курицы
                    PlaySound(chickenScream);

                    //Создаем новую курицу
                    chickens[i] = GenerateFood();
                }
            }

        }

        //Просто пачка переменных для хранения информации о гипотетическом курином шаге
        struct ChickInfo
        {
            //Расстояние до головы змеи
            public double Distance;
            //Позиция шага в списке
            public int index;
            //Координаты шага
            public Point pos;
        }
        
        //Двигаем курицу
        private void MoveChicken(GameObject chick)
        {
            //Объявляем список 
            var posPoints = new List<ChickInfo>();

            //Набиваем массив точек четярьмя возможными точками для новой позиции
            Point[] plist = new Point[4];
            plist[0] = new Point(chick.Pos.X, chick.Pos.Y + 1);
            plist[1] = new Point(chick.Pos.X - 1, chick.Pos.Y);
            plist[2] = new Point(chick.Pos.X + 1, chick.Pos.Y);
            plist[3] = new Point(chick.Pos.X, chick.Pos.Y - 1);

            //Проверяем в цикле, возможность "встать" в новую точку
            for (int i = 0; i < 4; i++)
            {
                if (checkBounds(plist[i]) || checkRocks(plist[i]) || checkSnake(plist[i]))
                {
                    //Если следующий шаг выходит за границы игрового поля, попадает на камень или на змейку
                    //Ничего не делаем
                }
                else
                {
                    //Если шаг выпадает на свободное место
                    ChickInfo chickInfo;
                    //Курица считает расстояние от головы змейки до своего следующего шага
                    chickInfo.Distance = Utils.GetDistance(plist[i], snake[0].Pos);
                    //Запоминаем ее номер в массиве
                    chickInfo.index = i;
                    //Запоминаем точку
                    chickInfo.pos = plist[i];
                    //Добавляем структуру в список
                    posPoints.Add(chickInfo);
                }
            }


            if (posPoints.Count != 0)
            {
                //Получим минимальную дистанцию

                //Linq, медленный, но простой способ. Сортируем список по минимальной дистанции, берем первый элемент.
                ChickInfo minItem = posPoints.OrderBy(p => p.Distance).First();


                //Если расстояние до головы меньше 10 пикселей, курица начинает сходить с ума, убегать
                if (minItem.Distance < 10)
                {
                    //В 97 случаях из 100, двигаемся! Даем змее небольшое преимущество в скорости
                    if (random.Next(100) > 3)
                    {
                        //Если шагов в списке больше чем 1
                        if (posPoints.Count != 1)
                        {
                            //Удаляем нежелательный шаг, что сократит расстояние до головы
                            posPoints.Remove(minItem);
                        }

                        //Переставляем курицу
                        ChickStep(posPoints, chick);
                        return;
                    }
                }
                else
                {
                    //Иначе, курица ведет себя спойно, клюет корм
                    //Изредка двигаемся
                    if (random.Next(100) < 10)
                    {
                        //Переставляем курицу
                        ChickStep(posPoints, chick);
                        return;
                    }
                }
            }

            //Если до этого момента дошло, значит курица стоит на месте, просто поворачиваем ее текстуру
            //Будто оглядывается по сторонам
            if (random.Next(100) < 20)
            {
                chick.imgIndex = random.Next(4);
            }
        }

        //Метод для перестановки курицы слуайную возможную позицию
        private void ChickStep(List<ChickInfo> posPoints, GameObject chick)
        {
            var rand = posPoints[random.Next(posPoints.Count)];
            //Определяем позицию объекта
            chick.Pos = rand.pos;
            //Присваиваем текстуру, соответствующую направлению движения
            chick.imgIndex = rand.index;
        }

        //Проверка, находится ли точка в пределах игрового поля
        public bool checkBounds(Point input)
        {
            return (input.X < 0 || input.Y < 0 || input.X >= bounds.Width || input.Y >= bounds.Height);
        }

        //Проверка, находится ли точка на камне
        public bool checkRocks(Point input)
        {
            for (int i = 0; i < rocks.Count; i++)
            {
                if (rocks[i].Pos == input)
                {
                    return true;
                }
            }
            return false;
        }

        //Проверка, содержит ли змейка точку
        private bool checkSnake(Point input)
        {
            for (int i = 0; i < snake.Count; i++)
            {
                //Если координата сегмента совпадает с точкой
                if (snake[i].Pos == input)
                {
                    return true;
                }
            }

            return false;
        }

        //Кушаем еду
        private void Eat()
        {
            //Добавляем новую точку, с координатами, равными последней точке в списке
            //В следующем шаге тело подрастет
            snake.Add(snake[snake.Count - 1].Copy());

            //Добавляем очки
            gameScore += 100;
        }


        private GameObject GenerateFood()
        {
            //Создаем точку в случайной позиции в пределах игрового поля
            Point randpoint = new Point(random.Next(0, bounds.Width), random.Next(0, bounds.Height));

            //Проверяем, содержит ли тело змейки нашу случайную точку, в таком случае еда появится на теле змейке
            //Пробегаемся по списку сегментов

            //Если содержит случайная коодината выпадает на змейку или камень, рекурсивно гененируем новую
            if (checkSnake(randpoint) | checkRocks(randpoint))
               return GenerateFood();
            else
                return new GameObject(randpoint.X, randpoint.Y, 0);

        }

        //Создаем эффект сплющенной головы
        private void FlatHead()
        {
            int halfPix = PixelLen / 2;

            //В зависимости от направления сдвигаем изображение на предыдущий сегмент
            switch (snake[0].Dir)
            {
                case Direction.Down:
                    snake[0].imgIndex = 16;
                    snake[0].imgPos = new Point(snake[0].imgPos.X, snake[0].imgPos.Y - halfPix);
                    break;
                case Direction.Right:
                    snake[0].imgIndex = 17;
                    snake[0].imgPos = new Point(snake[0].imgPos.X - halfPix, snake[0].imgPos.Y);
                    break;
                case Direction.Left:
                    snake[0].imgIndex = 18;
                    snake[0].imgPos = new Point(snake[0].imgPos.X + PixelLen, snake[0].imgPos.Y);
                    break;
                case Direction.Up:
                    snake[0].imgIndex = 19;
                    snake[0].imgPos = new Point(snake[0].imgPos.X, snake[0].imgPos.Y + PixelLen);
                    break;
            }
        }

        //Умерли
        private void Die()
        {
            //Плющим текстуру головы
            FlatHead();

            //Проигрываем звук удара
            PlaySound(hitSound);

            //Объявляем смерть
            gameOver = true;

            //Останавливаем игровой таймер
            gameTimer.Stop();
        }

        #endregion

        #region События формы, отрисовка, ресайз, нажатие клавиш

        //Отрисовка игры на холсте
        //В идеале, надо использовать CachedBitmap для ускорения отрисовки, но эта фишка GDI не доступна в System.Drawing
        //Если есть желание запарится, можно использовать враппер https://github.com/svejdo1/CachedBitmap
        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            if (!GameShowed) return;

            Graphics g = e.Graphics;

            //Сдвигаем начало координат для смещения области отрисовки в центр формы
            g.TranslateTransform(transform.X, transform.Y);

            if (isEditor)
            {
                g.DrawImage(background, 0, 0);

                //Пробегаемся по списку камней и рисуем каждый в своей позиции
                for (int i = 0; i < rocks.Count; i++)
                {
                    g.DrawImage(rocksTexList[0], rocks[i].imgPos);
                }

                if (snake.Count == 2)
                {
                    g.DrawImage(snakeTexList[snake[0].imgIndex], snake[0].imgPos);
                    g.DrawImage(snakeTexList[snake[1].imgIndex], snake[1].imgPos);
                }
            }
            else
            {
                //Включаем сглаживание шрифтов для рисования текста
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                //Рисуем фон (трава с камнями)
                g.DrawImage(background, 0, 0);

                //Рисуем текстуры сегментов змейки
                for (int i = snake.Count - 1; i >= 0; i--)
                {
                    //Изображение хватаем из списка, по номеру который хранится в поле класса imgIndex
                    g.DrawImage(snakeTexList[snake[i].imgIndex], snake[i].imgPos);
                }

                //Рисуем текстуры яблок
                for (int i = 0; i < apples.Count; i++)
                {
                    g.DrawImage(appleImg, apples[i].imgPos);
                }

                //Рисуем текстуры куриц
                for (int i = 0; i < chickens.Count; i++)
                {
                    //Изображение хватаем из списка 
                    g.DrawImage(chickenTexList[chickens[i].imgIndex], chickens[i].imgPos);
                }

                //Рисуем очки
                g.DrawString("Очки: " + gameScore.ToString(), drawFont, Brushes.White, screenCenter.X, PixelLen, sf);

                if (gameOver)
                {
                    //Если змейка мертва, надо оповестить об этом игрока
                    //Рисуем надпись
                    g.DrawString(overText + Environment.NewLine + infoText, drawFont, Brushes.White, screenCenter.X, screenCenter.Y, sf);
                }

            }

        }

        //Событие по изменению размера формы
        private void canvas_Resize(object sender, EventArgs e)
        {
            //Если показывается игры
            if (GameShowed)
            {
                //Перерасчет размера игрового поля, размеров текстур
                InitGameField();

                //Перерасчет позиции текстур камней
                for (int i = 0; i < rocks.Count; i++)
                {
                    rocks[i].CalculateImagePos();
                }

                //Перерасчет позиции текстур сегментов змеи
                for (int i = 0; i < snake.Count; i++)
                {
                    snake[i].CalculateImagePos();
                }

                //Если змея умерла, перерасчет позиции текстуры сплющенной головы
                if (gameOver)
                {
                    FlatHead();
                }

                //Перерасчет координат текстур яблок и куриц

                for (int i = 0; i < chickens.Count; i++)
                {
                    chickens[i].CalculateImagePos();
                }

                for (int i = 0; i < apples.Count; i++)
                {
                    apples[i].CalculateImagePos();
                }

                //Перерисовка камней на заднем фоне
                DrawLevel();

                //Принудительная перерисовка холста
                canvas.Invalidate();
            }
        }

        //Событие, вызывающееся по нажатию клавиш
        private void canvas_KeyDown(object sender, KeyEventArgs e)
        {
            //Определяем управление игрой
            switch (e.KeyCode)
            {
                //По стрелочкам или WSAD меняем текущее направление змейки
                case Keys.A:
                case Keys.Left:
                    //Во избежание смерти, от поворота "в себя"
                    if (snake[0].Dir != Direction.Right)
                        direction = Direction.Left;
                    break;

                case Keys.D:
                case Keys.Right:
                    if (snake[0].Dir != Direction.Left)
                        direction = Direction.Right;
                    break;

                case Keys.W:
                case Keys.Up:
                    if (snake[0].Dir != Direction.Down)
                        direction = Direction.Up;
                    break;

                case Keys.S:
                case Keys.Down:
                    if (snake[0].Dir != Direction.Up)
                        direction = Direction.Down;
                    break;

                //На R начинаем игру заново
                case Keys.R:
                    gameTimer.Stop();
                    StartGame(0,false);
                    break;

                //По пробелу ускоряем игру до скорости 20
                case Keys.Space:
                        gameTimer.Interval = 50;
                    break;

                case Keys.Enter:
                    if (isEditor)
                    {
                        isEditor = false;
                        StartGame(0, true);
                    }
                    break;

                //На Esc вызываем событие
                case Keys.Escape:
                    //Если умерли
                    if (gameOver)
                    {
                        if (PressEsc != null)
                        {
                            GameShowed = false;
                            PressEsc(this, e);
                        }
                    }
                    else 
                        //Пауза
                        gameTimer.Enabled = !gameTimer.Enabled;
                    break;
            }
        }

        //Событие отпускания клавиши
        private void canvas_KeyUp(object sender, KeyEventArgs e)
        {
            //Если пробел, возвращаем старую скорость игры
            if (e.KeyCode == Keys.Space)
                gameTimer.Interval = 1000 / gameSpeed;
        }

        #endregion

        private bool isEditor = false;
        private bool isZalip = false;

        private void backToScr_Click(object sender, EventArgs e)
        {
            if (PressEsc != null)
            {
                GameShowed = false;
                PressEsc(this, e);
            }
        }

        private void clearField_Click(object sender, EventArgs e)
        {
            if (PressEsc != null)
            {
                GameShowed = false;
                PressEsc(this, e);
            }
        }

        private void saveAs_Click(object sender, EventArgs e)
        {
            if (PressEsc != null)
            {
                GameShowed = false;
                PressEsc(this, e);
            }
        }

        public void RunEditor()
        {
            GameShowed = true;

            rocks.Clear();
            InitGameField();

            canvas.Controls["ms"].Visible = true;

            var msH = canvas.Controls["ms"].Height;
            
            canvas.ClientSize = new Size(canvas.ClientSize.Width, canvas.ClientSize.Height + msH);
            transform = new Point(transform.X, transform.Y + msH);
            //Меняем размер фона в соответствии с размером клиентской области формы
            background = Utils.ResizeImage(grassImg, realSize.Width, realSize.Height);

            //Рисуем вспомогательную сетку
            using (var g = Graphics.FromImage(background))
            {
                //Полупрозрачная красная ручка
                Pen p2 = new Pen(Color.FromArgb(100, Color.Red));

                //Пробегаемся по ширине и высоте, рисуем линии через пиксель
                for (int i = 1; i < bounds.Width; i++)
                {
                    g.DrawLine(p2, i * pixelLen, 0, i * pixelLen, realSize.Height);
                }

                for (int i = 1; i < bounds.Height; i++)
                {
                    g.DrawLine(p2, 0, i * pixelLen, realSize.Width, i * pixelLen);
                }
            }

            

            isEditor = true;
            canvas.Invalidate();
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isEditor)
            {
                if (isZalip)
                {
                    EditRocks(e);

                }
            
            }

        }

        private void canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (isEditor)
            {
                isZalip = false;
            }
        }

        private void EditRocks(MouseEventArgs e)
        {
            var virtPt = new Point(e.X / pixelLen, e.Y / pixelLen);

            if (e.Button == MouseButtons.Left)
            {
                if (!checkRocks(virtPt))
                {
                    rocks.Add(new GameObject(virtPt.X, virtPt.Y, 0));
                }
            }
            else
                if (e.Button == MouseButtons.Right)
                {
                    if (checkRocks(virtPt))
                    {
                        //Motherphuckin LINQ!!!
                        rocks.RemoveAll((x) => x.Pos == virtPt);
                    }
                }
                else if (e.Button == MouseButtons.Middle)
                {
                    snake.Clear();
                    snake.Add(new SnakeSegment(virtPt.X, virtPt.Y, 0, Direction.Down));
                    snake.Add(new SnakeSegment(virtPt.X, virtPt.Y - 1, 8, Direction.Down));
                }
            canvas.Invalidate();
         
        }

        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (isEditor)
            {
                EditRocks(e);
                isZalip = true;
   
            }
        }
    }

    class Utils
    {
        //Метод для вырезания прямоугольной области из картинки
        public static Bitmap CropImage(Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            Bitmap bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
            return bmpCrop;
        }

        //Метод для высококачественного изменения размера изображения
        public static Image ResizeImage(Image image, int width, int height)
        {
            //Если изменять нечего, возвращаем исходное изображение без изменений
            if ((image.Width == width && image.Height == height) || (width == 0 && height == 0))
                return new Bitmap(image);

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        //Простой метод для рассчета расстояния между 2мя точками
        public static double GetDistance(Point p1, Point p2)
        {
            double xDelta = p1.X - p2.X;
            double yDelta = p1.Y - p2.Y;

            //Теорема Пифагора
            return Math.Sqrt(Math.Pow(xDelta, 2) + Math.Pow(yDelta, 2));
        }
    }
}
