﻿using ScottPlot.Drawing;
using System;
using System.Drawing;

namespace ScottPlot.Plottable
{
    public abstract class AxisLine : IDraggable, IPlottable, IHasLine, IHasColor
    {
        /// <summary>
        /// Location of the line (Y position if horizontal line, X position if vertical line)
        /// </summary>
        protected double Position;

        /// <summary>
        /// If True, the position will be labeled on the axis using the PositionFormatter
        /// </summary>
        public bool PositionLabel = false;

        /// <summary>
        /// Font to use for position labels (labels drawn over the axis)
        /// </summary>
        public Drawing.Font PositionLabelFont = new() { Color = Color.White, Bold = true };

        /// <summary>
        /// Color to use behind the position labels
        /// </summary>
        public Color PositionLabelBackground = Color.Black;

        /// <summary>
        /// If true the position label will be drawn on the right or top of the data area.
        /// </summary>
        public bool PositionLabelOppositeAxis = false;

        /// <summary>
        /// This method generates the position label text for numeric (non-DateTime) axes.
        /// For DateTime axes assign your own format string that uses DateTime.FromOADate(position).
        /// </summary>
        public Func<double, string> PositionFormatter = position => position.ToString("F2");

        /// <summary>
        /// Position of the axis line in DateTime (OADate) units.
        /// </summary>
        public DateTime DateTime
        {
            get => DateTime.FromOADate(Position);
            set => Position = value.ToOADate();
        }

        /// <summary>
        /// Indicates whether the line is horizontal (position in Y units) or vertical (position in X units)
        /// </summary>
        private readonly bool IsHorizontal;

        /// <summary>
        /// If true, AxisAuto() will ignore the position of this line when determining axis limits
        /// </summary>
        public bool IgnoreAxisAuto = false;

        public bool IsVisible { get; set; } = true;
        public int XAxisIndex { get; set; } = 0;
        public int YAxisIndex { get; set; } = 0;
        public LineStyle LineStyle { get; set; } = LineStyle.Solid;
        public double LineWidth { get; set; } = 1;
        public Color Color { get; set; } = Color.Black;
        public Color LineColor { get => Color; set { Color = value; } }

        /// <summary>
        /// Text that appears in the legend
        /// </summary>
        public string Label;

        /// <summary>
        /// Indicates whether this line is draggable in user controls.
        /// </summary>
        public bool DragEnabled { get; set; } = false;

        /// <summary>
        /// Cursor to display while hovering over this line if dragging is enabled.
        /// </summary>
        public Cursor DragCursor => IsHorizontal ? Cursor.NS : Cursor.WE;

        /// <summary>
        /// If dragging is enabled the line cannot be dragged more negative than this position
        /// </summary>
        public double DragLimitMin = double.NegativeInfinity;

        /// <summary>
        /// If dragging is enabled the line cannot be dragged more positive than this position
        /// </summary>
        public double DragLimitMax = double.PositiveInfinity;

        /// <summary>
        /// This event is invoked after the line is dragged
        /// </summary>
        public event EventHandler Dragged = delegate { };

        /// <summary>
        /// The lower bound of the axis line.
        /// </summary>
        public double Min = double.NegativeInfinity;

        /// <summary>
        /// The upper bound of the axis line.
        /// </summary>
        public double Max = double.PositiveInfinity;

        public AxisLine(bool isHorizontal)
        {
            IsHorizontal = isHorizontal;
        }

        public AxisLimits GetAxisLimits()
        {
            if (IgnoreAxisAuto)
                return new AxisLimits(double.NaN, double.NaN, double.NaN, double.NaN);

            if (IsHorizontal)
                return new AxisLimits(double.NaN, double.NaN, Position, Position);
            else
                return new AxisLimits(Position, Position, double.NaN, double.NaN);
        }

        public void ValidateData(bool deep = false)
        {
            if (double.IsNaN(Position) || double.IsInfinity(Position))
                throw new InvalidOperationException("position must be a valid number");

            if (PositionFormatter is null)
                throw new NullReferenceException(nameof(PositionFormatter));
        }

        public void Render(PlotDimensions dims, Bitmap bmp, bool lowQuality = false)
        {
            if (!IsVisible)
                return;

            RenderLine(dims, bmp, lowQuality);

            if (PositionLabel)
                RenderPositionLabel(dims, bmp, lowQuality);
        }

        public void RenderLine(PlotDimensions dims, Bitmap bmp, bool lowQuality)
        {
            using var gfx = GDI.Graphics(bmp, dims, lowQuality);
            using var pen = GDI.Pen(Color, LineWidth, LineStyle, true);

            if (IsHorizontal)
            {
                float pixelY = dims.GetPixelY(Position);

                double xMin = Math.Max(Min, dims.XMin);
                double xMax = Math.Min(Max, dims.XMax);
                float pixelX1 = dims.GetPixelX(xMin);
                float pixelX2 = dims.GetPixelX(xMax);

                gfx.DrawLine(pen, pixelX1, pixelY, pixelX2, pixelY);
            }
            else
            {
                float pixelX = dims.GetPixelX(Position);

                double yMin = Math.Max(Min, dims.YMin);
                double yMax = Math.Min(Max, dims.YMax);
                float pixelY1 = dims.GetPixelY(yMin);
                float pixelY2 = dims.GetPixelY(yMax);

                gfx.DrawLine(pen, pixelX, pixelY1, pixelX, pixelY2);
            }
        }

        private void RenderPositionLabel(PlotDimensions dims, Bitmap bmp, bool lowQuality)
        {
            using var gfx = GDI.Graphics(bmp, dims, lowQuality, clipToDataArea: false);
            using var pen = GDI.Pen(Color, LineWidth, LineStyle, true);

            using var fnt = GDI.Font(PositionLabelFont);
            using var fillBrush = GDI.Brush(PositionLabelBackground);
            using var fontBrush = GDI.Brush(PositionLabelFont.Color);

            if (IsHorizontal)
            {
                if (Position > dims.YMax || Position < dims.YMin)
                    return;

                float pixelY = dims.GetPixelY(Position);
                string yLabel = PositionFormatter(Position);
                SizeF yLabelSize = GDI.MeasureString(yLabel, PositionLabelFont);
                float xPos = PositionLabelOppositeAxis ? dims.DataOffsetX + dims.DataWidth : dims.DataOffsetX - yLabelSize.Width;
                float yPos = pixelY - yLabelSize.Height / 2;
                RectangleF xLabelRect = new(xPos, yPos, yLabelSize.Width, yLabelSize.Height);
                gfx.FillRectangle(fillBrush, xLabelRect);
                var sf = GDI.StringFormat(HorizontalAlignment.Left, VerticalAlignment.Middle);
                gfx.DrawString(yLabel, fnt, fontBrush, xPos, pixelY, sf);
            }
            else
            {
                if (Position > dims.XMax || Position < dims.XMin)
                    return;

                float pixelX = dims.GetPixelX(Position);
                string xLabel = PositionFormatter(Position);
                SizeF xLabelSize = GDI.MeasureString(xLabel, PositionLabelFont);
                float xPos = pixelX - xLabelSize.Width / 2;
                float yPos = PositionLabelOppositeAxis ? dims.DataOffsetY - xLabelSize.Height : dims.DataOffsetY + dims.DataHeight;
                RectangleF xLabelRect = new(xPos, yPos, xLabelSize.Width, xLabelSize.Height);
                gfx.FillRectangle(fillBrush, xLabelRect);
                var sf = GDI.StringFormat(HorizontalAlignment.Center, VerticalAlignment.Upper);
                gfx.DrawString(xLabel, fnt, fontBrush, pixelX, yPos, sf);
            }
        }

        /// <summary>
        /// Move the line to a new coordinate in plot space.
        /// </summary>
        /// <param name="coordinateX">new X position</param>
        /// <param name="coordinateY">new Y position</param>
        /// <param name="fixedSize">This argument is ignored</param>
        public void DragTo(double coordinateX, double coordinateY, bool fixedSize)
        {
            if (!DragEnabled)
                return;

            if (IsHorizontal)
            {
                if (coordinateY < DragLimitMin) coordinateY = DragLimitMin;
                if (coordinateY > DragLimitMax) coordinateY = DragLimitMax;
                Position = coordinateY;
            }
            else
            {
                if (coordinateX < DragLimitMin) coordinateX = DragLimitMin;
                if (coordinateX > DragLimitMax) coordinateX = DragLimitMax;
                Position = coordinateX;
            }

            Dragged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Return True if the line is within a certain number of pixels (snap) to the mouse
        /// </summary>
        /// <param name="coordinateX">mouse position (coordinate space)</param>
        /// <param name="coordinateY">mouse position (coordinate space)</param>
        /// <param name="snapX">snap distance (pixels)</param>
        /// <param name="snapY">snap distance (pixels)</param>
        /// <returns></returns>
        public bool IsUnderMouse(double coordinateX, double coordinateY, double snapX, double snapY) =>
            IsHorizontal ?
            Math.Abs(Position - coordinateY) <= snapY :
            Math.Abs(Position - coordinateX) <= snapX;

        public LegendItem[] GetLegendItems()
        {
            var singleItem = new LegendItem(this)
            {
                label = Label,
                color = Color,
                lineStyle = LineStyle,
                lineWidth = LineWidth,
                markerShape = MarkerShape.none
            };
            return new LegendItem[] { singleItem };
        }
    }
}
