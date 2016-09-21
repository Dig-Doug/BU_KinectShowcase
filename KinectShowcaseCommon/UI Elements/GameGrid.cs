using KinectShowcaseCommon.Kinect_Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace KinectShowcaseCommon.UI_Elements
{
    public class GameGrid : UniformGrid, KinectHandManager.HandStateChangeListener, KinectHandManager.HandLocationListener
    {
        public delegate void ClickHandler(int row, int col);
        public ClickHandler Handler;

        private bool shouldAttach = true;
        public bool ButtonsShouldAttach
        {
            get
            {
                return shouldAttach;
            }
            set
            {
                if (shouldAttach != value)
                {
                    shouldAttach = value;
                    for (int row = 0; row < this.gridRows; row++)
                    {
                        for (int col = 0; col < this.gridColumns; col++)
                        {
                            gridElements[row, col].ShouldHandAttach = shouldAttach;
                        }
                    }
                }
            }
        }

        public Dictionary<int, Brush> stateFillColors = new Dictionary<int, Brush>();
        public Brush BorderColor = Brushes.Gray;
        public Brush HoverColor = Brushes.Blue;

        private int gridRows = 0;
        private int gridColumns = 0;
        private KinectButton[,] gridElements;
        private int[,] gridState;
        private Point lastHoverTile;

        public GameGrid() : base()
        {
            KinectManager.Default.HandManager.AddHandLocationListener(this);
            KinectManager.Default.HandManager.AddHandStateChangeListener(this);
            stateFillColors[0] = Brushes.White;
        }

        public void SetSize(int rows, int columns)
        {
            this.gridRows = rows;
            this.gridColumns = columns;
            InitGrid();
        }

        private void InitGrid()
        {
            //rows left->right
            gridElements = new KinectButton[this.gridRows, this.gridColumns];
            for (int row = 0; row < this.gridRows; row++)
            {
                for (int col = 0; col < this.gridColumns; col++)
                {
                    KinectButton button = new KinectButton();
                    button.ShouldHandAttach = this.ButtonsShouldAttach;
                    button.BorderThickness = new Thickness(3);
                    // Set button row & col here to capture value for event handler
                    int buttonRow = row;
                    int buttonCol = col;
                    button.Click += new RoutedEventHandler((sender, e) => {
                        if (Handler != null)
                        {
                            Handler(buttonRow, buttonCol); 
                        }
                    });
                    gridElements[row, col] = button;
                    this.Children.Add(gridElements[row, col]);
                }
            }

            gridState = new int[this.gridRows, this.gridColumns];
            ZeroGridArray();
        }

        public void ZeroGridArray()
        {
            for (int row = 0; row < this.gridRows; row++)
            {
                for (int col = 0; col < this.gridColumns; col++)
                {
                    SetGrid(row, col, 0);
                }
            }
        }

        public void SetGrid(int row, int col, int state)
        {
            if (GridCoordinatesWithingBounds(row, col))
            {
                gridState[row, col] = state;
                if (stateFillColors.ContainsKey(state))
                {
                    gridElements[row, col].Background = stateFillColors[state];
                }
                else
                {
                    // TODO(doug) - log
                }
            }
            else
            {
                Debug.WriteLine("Tried to set an invalid grid location X: " + row + " Y: " + col);
            }
        }

        public int GetGrid(int row, int col)
        {
            return gridState[row, col];
        }

        public void SetStateColor(int state, Brush brush)
        {
            stateFillColors[state] = brush;
        }

        public bool IsLocationInGrid(Point aLocation)
        {
            bool result = false;

            //get the grid's rect
            Rect gridRect = this.RenderTransform.TransformBounds(new Rect(this.RenderSize));// LayoutInformation.GetLayoutSlot(uniGrid);
            if (gridRect.Contains(aLocation))
            {
                result = true;
            }

            return result;
        }

        public Point GetGridLocationForPoint(Point aLocation)
        {
            //get the grid's rect
            Rect gridRect = this.RenderTransform.TransformBounds(new Rect(this.RenderSize)); //LayoutInformation.GetLayoutSlot(uniGrid);

            //calculate the point
            Point result = new Point(aLocation.X, aLocation.Y);
            //translate to origin of grid's rect
            result.X -= gridRect.Left;
            result.Y -= gridRect.Top;
            //divide by column/row size
            result.X /= gridRect.Width / this.gridColumns;
            result.Y /= gridRect.Height / this.gridRows;
            //floor
            result.X = (int)result.X;
            result.Y = (int)result.Y;

            return result;
        }


        public bool GridCoordinatesWithingBounds(int row, int col)
        {
            bool result = false;
            if (row >= 0 && row < this.gridRows && col >= 0 && col < this.gridColumns)
            {
                result = true;
            }
            return result;
        }

        private void hoverOverLocation(Point aLocation)
        {
            if (GridCoordinatesWithingBounds((int)lastHoverTile.X, (int)lastHoverTile.Y))
            {
                gridElements[(int)lastHoverTile.Y, (int)lastHoverTile.X].BorderBrush = this.BorderColor;
            }

            lastHoverTile = aLocation;
            if (GridCoordinatesWithingBounds((int)aLocation.X, (int)aLocation.Y))
            {
                gridElements[(int)lastHoverTile.Y, (int)lastHoverTile.X].BorderBrush = this.HoverColor;
            }

            InvalidateVisual();
        }

        #region Kinect Hand Manager Callbacks

        public bool KinectHandManagerDidGetHandLocation(KinectHandManager aManager, KinectHandManager.HandLocationEvent aEvent)
        {
            bool result = false;

            Point pagePoint = new Point(aEvent.HandPosition.X / aManager.HandCoordRangeX, aEvent.HandPosition.Y / aManager.HandCoordRangeY);


            Dispatcher.InvokeAsync((Action)delegate ()
            {
                if (IsLocationInGrid(pagePoint))
                {
                    hoverOverLocation(GetGridLocationForPoint(pagePoint));
                }
                else
                {
                    hoverOverLocation(new Point(-1, -1));
                }
            });

            return result;
        }

        public bool KinectHandManagerDidDetectHandStateChange(KinectHandManager aManager, KinectHandManager.HandStateChangeEvent aEvent)
        {
            bool result = false;
            return result;
        }

        public Point AttachLocation()
        {
            throw new NotImplementedException();
        }

        public bool HandShouldAttach()
        {
            return false;
        }

        #endregion
    }


}
