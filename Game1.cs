using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LandViewer
{

    public struct Node
    {
        public int x; //x coordinates in draw
        public int y; //y coordinates in draw
        public int height; //the height of the point
        public int row; //Row position of this node in _nodes
        public int collumn; //Collumn position of this node in _nodes
        public List<Tuple<int, int>> neighboursIndexes; //Nodes which are neighbours of current node. Stocking the indexes in _nodes object.

        public Node()
        {
            neighboursIndexes = new List<Tuple<int, int>>();
        }
    }

    public class Game1 : Game
    {
        //Engine
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private double _timeSinceLastClick = 0;
        private double ClickInterval = 0.1;
        private int _previousScrollValue;
        private double _fpsCounter;
        private int _fps;
        private string _fpsText = "";
        private bool _showFps = true;
        private bool _showCoordinates = false;
        private bool _showNodes = false;

        //Graphics
        private Texture2D _canvas;
        private UInt32[] _pixels;
        private int _windowWidth = 1300;
        private int _windowHeight = 1300;
        private int _numberOfPixels = 0;
        private int _horizontalDistanceBetweenTwoPoints = 0;
        private int _verticalDistanceBetweenTwoPoints = 0;
        private int _scale = 1;
        private float _zoom = 1;
        private int _horizontalMove = 0;
        private int _verticalMove = 0;

        //Maps
        private string[] _mapFiles;
        private int _mapMaxNumberOfPointsByLine = 0;
        private int _mapNumberOfLines = 0;
        private int _mapHighestPoint = 0;
        private int _mapLowestPoint = 0;
        private string _mapPath = @"Maps/";
        private List<List<Node>> _nodes = new List<List<Node>>();
        private int _mapIndex = 0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = _windowWidth;
            _graphics.PreferredBackBufferHeight = _windowHeight;
            _graphics.SynchronizeWithVerticalRetrace = false;
            IsFixedTimeStep = false;
            _graphics.ApplyChanges();
            _canvas = new Texture2D(GraphicsDevice, _windowWidth, _windowHeight, false, SurfaceFormat.Color);
            _pixels = new UInt32[_windowWidth * _windowHeight];
            _numberOfPixels = _windowWidth * _windowHeight;
            _mapFiles = Directory.GetFiles(_mapPath);
            _previousScrollValue = Mouse.GetState().ScrollWheelValue;

            base.Initialize();
        }

        private void printText(int x, int y, string message, Color color)
        {
            _spriteBatch.DrawString(_spriteFont, message, new Vector2(x, y), color);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteFont = Content.Load<SpriteFont>("Arial_16_Regular");

            LoadMap(); //Retriving map from file
            CalculateNeighbours(); //Calculates neighbours for each nodes
            CalculateNodesCoordinates(); //Calculate the nodes coordinates
            DrawLines(); //Calculate the lines between each points
            _canvas.SetData(_pixels, 0, _windowWidth * _windowHeight);
        }

        protected override void Update(GameTime gameTime)
        {
            //check for keyboard and mouse inputs
            int recalculate = HandleKeyboardInput(gameTime);

            //display FPS if needed
            if (_showFps)
                FPSCounter(gameTime);

            //recalculate and draw
            if (recalculate > 0)
            {
                CalculateNodesCoordinates();
                DrawLines();
                _canvas.SetData(_pixels, 0, _windowWidth * _windowHeight);
                base.Update(gameTime);
            }
        }

        private void FPSCounter(GameTime gameTime)
        {
            _fpsCounter += gameTime.ElapsedGameTime.TotalSeconds;
            if (_fpsCounter >= 1.0)
            {
                _fps = (int)(1.0 / gameTime.ElapsedGameTime.TotalSeconds);
                _fpsText = "FPS: " + _fps.ToString();
                _fpsCounter = 0;
            }
        }

        private void ResetView()
        {
            _scale = 1;
            _zoom = 1;
            _horizontalMove = 0;
            _verticalMove = 0;
        }

        private int HandleKeyboardInput(GameTime gameTime)
        {
            _timeSinceLastClick += gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kState = Keyboard.GetState();
            MouseState mState = Mouse.GetState();

            if (kState.GetPressedKeyCount() == 0 && mState.ScrollWheelValue == _previousScrollValue) return 0;
            if (kState.IsKeyDown(Keys.Escape)) Exit();
            if (kState.IsKeyDown(Keys.Up)) _verticalMove--;
            if (kState.IsKeyDown(Keys.Down)) _verticalMove++;
            if (kState.IsKeyDown(Keys.Left)) _horizontalMove--;
            if (kState.IsKeyDown(Keys.Right)) _horizontalMove++;
            if (kState.IsKeyDown(Keys.S)) _scale++;
            if (kState.IsKeyDown(Keys.X)) _scale--;
            if (kState.IsKeyDown(Keys.R)) ResetView();
            if (kState.IsKeyDown(Keys.F) && _timeSinceLastClick >= ClickInterval) _showFps = _showFps == true ? false : true;
            if (kState.IsKeyDown(Keys.G) && _timeSinceLastClick >= ClickInterval) _showCoordinates = _showCoordinates == true ? false : true;
            if (kState.IsKeyDown(Keys.H) && _timeSinceLastClick >= ClickInterval) _showNodes = _showNodes == true ? false : true;
            if (kState.IsKeyDown(Keys.D1) && _timeSinceLastClick >= ClickInterval)
            {
                _mapIndex = _mapIndex - 1 >= 0 ? _mapIndex - 1 : _mapFiles.Length - 1;
                ResetView();
                LoadMap();
                CalculateNeighbours();
            }
            if (kState.IsKeyDown(Keys.D2) && _timeSinceLastClick >= ClickInterval)
            {
                _mapIndex = _mapIndex < _mapFiles.Length - 1 ? _mapIndex + 1 : 0;
                ResetView();
                LoadMap();
                CalculateNeighbours();
            }
            if (mState.ScrollWheelValue < _previousScrollValue)
            {
                _zoom = Math.Clamp(_zoom - 0.1f, 0.1f, 5f);
                _horizontalMove = (int)Math.Floor(mState.Position.X * (1 - _zoom));
                _verticalMove = (int)Math.Floor((mState.Position.Y - (_windowHeight / 2)) * (1 - _zoom));
            }
            if (mState.ScrollWheelValue > _previousScrollValue)
            {
                _zoom = Math.Clamp(_zoom + 0.1f, 0.1f, 5f);
                _horizontalMove = (int)Math.Floor(mState.Position.X * (1 - _zoom));
                _verticalMove = (int)Math.Floor((mState.Position.Y - (_windowHeight / 2)) * (1 - _zoom));
            }

            _previousScrollValue = mState.ScrollWheelValue;
            _timeSinceLastClick = 0;
            return 1;
        }

        protected void LoadMap()
        {
            //reset map
            _nodes.Clear();
            _mapNumberOfLines = 0;
            _mapMaxNumberOfPointsByLine = 0;

            var path = _mapFiles[_mapIndex];
            var lines = File.ReadLines(path).ToList();

            System.Diagnostics.Debug.WriteLine(path);

            for (int i = 0; i < lines.Count; i++)
            {
                //removing useless characters.
                lines[i] = lines[i].Trim();
                lines[i] = Regex.Replace(lines[i].Trim(), @"\s+", " ");
                //splitting to get each number
                var numbersAsString = lines[i].Split(' ');

                var row = new List<Node>();
                for (int j = 0; j < numbersAsString.Length; j++)
                {
                    var node = new Node() { row = i, collumn = j, height = Int32.Parse(numbersAsString[j]) };
                    row.Add(node);
                }

                _nodes.Add(row);

                _mapMaxNumberOfPointsByLine = _nodes[i].Count > _mapMaxNumberOfPointsByLine ? _nodes[i].Count : _mapMaxNumberOfPointsByLine;
            }
            _mapNumberOfLines = _nodes.Count;
            _horizontalDistanceBetweenTwoPoints = (int)Math.Floor((_windowWidth / 2) / (float)(_mapMaxNumberOfPointsByLine - 1));
            _verticalDistanceBetweenTwoPoints = (int)Math.Floor((_windowHeight / 2) / (float)(_mapNumberOfLines - 1));
        }

        private uint MyColorToUInt(Color color)
        {
            return (uint)((color.A << 24) | (color.B << 16) | (color.G << 8) | (color.R << 0));
        }

        private void PrintNodesCoordinates(Color color)
        {
            foreach (var nodesList in _nodes)
                foreach (var node in nodesList)
                    _spriteBatch.DrawString(_spriteFont, "[" + node.x + "," + node.y + "]", new Vector2(node.x, node.y), color);
        }

        private static double CalculateDistance(int x1, int y1, int x2, int y2)
        {
            int deltaX = x2 - x1;
            int deltaY = y2 - y1;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        private bool IsOutsideOfWindow(int x, int y)
        {
            if (x < 0 || x > _windowWidth || y < 0 || y > _windowHeight)
                return true;
            return false;
        }

        private Color GetColorForHeight(float height)
        {
            float middle = (_mapHighestPoint + _mapLowestPoint) / 2;

            if (_mapHighestPoint - _mapLowestPoint <= 1) //not enough gradiant, cancel calculations
                return Color.Gray;
            if (height >= middle)
                return Color.Lerp(Color.Gray, Color.DeepPink, (float)(height - middle) / (float)(_mapHighestPoint - middle)); //0 gray => 1 pink
            else
                return Color.Lerp(Color.YellowGreen, Color.Gray, (float)(height - _mapLowestPoint) / (float)(middle - _mapLowestPoint)); //0 yellowGreen => 1 pink
        }

        public void DrawLineUnfixedColors(int x1, int y1, int height1, int x2, int y2, int height2)
        {
            if (IsOutsideOfWindow(x1, y1) && !IsOutsideOfWindow(x2, y2)) //We want to draw from the other node since startNode is outside of the window
            {
                DrawLineUnfixedColors(x2, y2, height2, x1, y1, height1);
                return;
            }

            //line
            HashSet<int> drawnPixels = new HashSet<int>();
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;
            int index = 0;

            //calculate height across the line
            float distance = (int)CalculateDistance(x1, y1, x2, y2);
            float heightOffset = Math.Abs((height1 - height2) / distance);
            float initialHeigth = height1;

            while (x1 != x2 || y1 != y2)
            {
                index = y1 * _windowHeight + x1;
                initialHeigth += height1 > height2 ? -heightOffset : heightOffset;

                if (!drawnPixels.Contains(index) && index >= 0 && index < _numberOfPixels)
                    if ((y1 * _windowHeight) + x1 < (y1 + 1) * _windowHeight && (y1 * _windowHeight) + x1 > y1 * _windowHeight)
                    {
                        _pixels[(y1 * _windowHeight) + x1] = MyColorToUInt(GetColorForHeight(initialHeigth));
                        drawnPixels.Add(index);
                    }
                if (x1 > _windowWidth || x1 < 0 || y1 > _windowHeight || y1 < 0)
                    return;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }

        //isometric view 
        //initialX = (nodesDistance * i);
        //initialY = (_windowHeight / 2) + (nodesDistance * i);
        //node.y = initialY - (nodesDistance * j) - (_nodes[i][j].height * _scale) + _verticalMove;
        //node.x = initialX + (nodesDistance * j) + _horizontalMove;
        //Si on modifie "l'angle" actuel (45°), on pourai par exemple utiliser 80°
        protected void CalculateNodesCoordinates()
        {
            int initialX = 0;
            int initialY = 0;
            int nodesDistance = (int)Math.Floor(Math.Min(_horizontalDistanceBetweenTwoPoints, _verticalDistanceBetweenTwoPoints) * _zoom);

            _mapHighestPoint = 0;
            _mapLowestPoint = 0;
            Array.Clear(_pixels, 0, _numberOfPixels);

            for (int i = 0; i < _nodes.Count; i++)
            {
                initialX = (nodesDistance * i) + _horizontalMove;
                initialY = (_windowHeight / 2) + (nodesDistance * i) + _verticalMove;

                for (int j = 0; j < _nodes[i].Count; j++)
                {
                    var node = _nodes[i][j];
                    node.y = initialY - (nodesDistance * j) - (_nodes[i][j].height * _scale);
                    node.x = initialX + (nodesDistance * j);
                    _nodes[i][j] = node;
                    if (node.height * _scale > _mapHighestPoint)
                        _mapHighestPoint = node.height * _scale;
                    if (node.height * _scale < _mapLowestPoint)
                        _mapLowestPoint = node.height * _scale;
                }
            }

        }

        protected void DrawLines()
        {
            foreach (var nodesLine in _nodes)
                foreach (var node in nodesLine)
                    foreach (var neighbour in node.neighboursIndexes)
                        DrawLineUnfixedColors(node.x, node.y, node.height * _scale, _nodes[neighbour.Item1][neighbour.Item2].x, _nodes[neighbour.Item1][neighbour.Item2].y, _nodes[neighbour.Item1][neighbour.Item2].height * _scale);
        }

        protected void CalculateNeighbours()
        {
            //adding horizontale neighbours
            for (int i = 0; i < _nodes.Count; i++)
                for (int j = 0; j < _nodes[i].Count - 1; j++)
                    _nodes[i][j].neighboursIndexes.Add(new Tuple<int, int>(i, j + 1));

            //adding verticale neighbours
            for (int i = 0; i < _nodes.Count; i++)
                for (int j = 0; j < _nodes[i].Count; j++)
                {
                    if (i < _nodes.Count - 1)
                    {
                        if (_nodes[i + 1].Count > j) //if next vertical point exist
                            _nodes[i][j].neighboursIndexes.Add(new Tuple<int, int>(i + 1, j));
                        else //no next vertical point, linking to closest in front
                            _nodes[i][j].neighboursIndexes.Add(new Tuple<int, int>(i + 1, _nodes[i + 1].Count - 1));
                    }
                    if (i != 0 && _nodes[i - 1].Count - 1 < j) //no aligned previous vertical point, linking to closest in the back
                        _nodes[i][j].neighboursIndexes.Add(new Tuple<int, int>(i - 1, _nodes[i - 1].Count - 1));
                }
        }

        private void Debug()
        {
            if (_showFps)
                printText(10, 10, _fpsText, Color.White);
            if (_showCoordinates)
                PrintNodesCoordinates(Color.White);
        }

        protected override void Draw(GameTime gameTime)
        {
            //cleaning
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();
            Debug();
            _spriteBatch.Draw(_canvas, new Vector2(0, 0), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
