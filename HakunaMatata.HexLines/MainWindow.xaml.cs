﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Alba.Framework.Wpf;

namespace HakunaMatata.HexLines
{
    public partial class MainWindow
    {
        private const double AnimMovingBallStep = 0.05;

        private readonly Table _table = new Table();
        private Storyboard _animMovingBall;

        public MainWindow ()
        {
            _table.Resize(20, 10);
            _table.GenerateBallColors(8);
            _table.GenerateBalls(60);
            _table.NewGame();

            DataContext = _table;
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            FuckOffDearWpfTrace();
        }

        private static void FuckOffDearWpfTrace ()
        {
            typeof(PresentationTraceSources).GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(prop => prop.PropertyType == typeof(TraceSource) && prop.Name != "DataBindingSource")
                .Select(prop => (TraceSource)prop.GetValue(null))
                .ForEach(trace => trace.Listeners.Clear());
        }

        private void CurrentDomain_UnhandledException (object sender, UnhandledExceptionEventArgs args)
        {
            MessageBox.Show(this, args.ExceptionObject + "\n\nApplication will terminate now.", "Unhandled exception :(", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(0);
        }

        protected override void OnKeyDown (KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.F2:
                    var rnd = new Random();
                    _table.Resize(rnd.Next(8, 30), rnd.Next(8, 16));
                    _table.GenerateBallColors(8);
                    _table.GenerateBalls(rnd.Next(10, _table.Cells.Count - 10));
                    _table.NewGame(GameMode.Groups);
                    break;
            }
            base.OnKeyDown(e);
        }

        private void FigCell_OnMouseDown (object sender, MouseButtonEventArgs e)
        {
            var cell = e.GetSourceDataContext<Cell>();
            if (_animMovingBall != null || e.ChangedButton != MouseButton.Left)
                return;
            if (cell.IsAvailable) {
                Cell selectedCell = _table.SelectedCell;
                Ball selectedBall = selectedCell.Ball;
                _table.SelectedCell = null;
                AnimateMoveBall(selectedBall, selectedCell, cell);
            }
            else if (cell.Ball != null && !cell.Ball.IsNew) {
                _table.SelectedCell = cell;
            }
        }

        private void AnimateMoveBall (Ball ball, Cell fromCell, Cell toCell)
        {
            List<Point> path = _table.FindPath(fromCell, toCell).Select(c => c.BallPoint).ToList();
            _table.StartMoveBallTo(ball, toCell);

            TimeSpan duration = TimeSpan.FromSeconds(AnimMovingBallStep * path.Count);
            DoubleAnimationUsingPath animX;
            _animMovingBall = new Storyboard {
                Children = {
                    (animX = new DoubleAnimationUsingPath {
                        PathGeometry = new PathGeometry {
                            Figures = new PathFigureCollection {
                                new PathFigure(fromCell.BallPoint, path.Select(p => new LineSegment(p, false)), false)
                            }
                        },
                        Duration = new Duration(duration),
                        AccelerationRatio = .1, DecelerationRatio = .7, Source = PathAnimationSource.X,
                    }.SetTarget(Canvas.LeftProperty.ToPath())),
                    animX.CloneForY().SetTarget(Canvas.TopProperty.ToPath()),
                }
            };
            _animMovingBall.AddCompleted(AnimMove_OnCompleted).Begin(lstBalls.GetItemContainer(toCell.Ball));
        }

        private void AnimMove_OnCompleted (object sender, EventArgs e)
        {
            _animMovingBall.Remove(lstBalls.GetItemContainer(_table.MovingBall));
            _animMovingBall = null;
            _table.EndMoveBallTo();
        }

        private void LstTable_OnMouseDown (object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
                _table.SelectedCell = null;
        }
    }
}