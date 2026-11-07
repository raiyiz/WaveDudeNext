// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Alexander Graef <alex@graef.cc>
//
// This file is part of WaveDude Next, a 2D wave simulator.
//
// Based on the work of
//   Jeroen Vleggaar (Huygens Optics), author of Wave Dude 2D, Copyright (C) 2026
//     <https://www.youtube.com/@HuygensOptics>, <https://www.huygensoptics.com>
//   0x23 (Diffraction Limited), author of WaveSimulator2D
//     <https://www.youtube.com/@DiffractionLimited>, <https://github.com/0x23/WaveSimulator2D>
//
// WaveDude Next is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version. It is distributed WITHOUT ANY WARRANTY. See the LICENSE.md file or
// <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace WaveDudeNext.Avalonia.Controls;

/// <summary>A frame ruler with an availability band, playhead and draggable export brackets.</summary>
public sealed class TimelineBar : RangeBase
{
    public static readonly StyledProperty<int> AvailableProperty = AvaloniaProperty.Register<TimelineBar, int>(nameof(Available));
    public static readonly StyledProperty<int> RangeStartProperty = AvaloniaProperty.Register<TimelineBar, int>(nameof(RangeStart));
    public static readonly StyledProperty<int> RangeEndProperty = AvaloniaProperty.Register<TimelineBar, int>(nameof(RangeEnd), -1);
    public int Available { get => GetValue(AvailableProperty); set => SetValue(AvailableProperty, value); }
    public int RangeStart { get => GetValue(RangeStartProperty); set => SetValue(RangeStartProperty, value); }
    public int RangeEnd { get => GetValue(RangeEndProperty); set => SetValue(RangeEndProperty, value); }
    public event Action<int, int>? RangeEdited;
    public bool IsDragging => _drag != Drag.None;
    private enum Drag { None, Playhead, In, Out }
    private Drag _drag;
    private IPointer? _pointer;
    private (double Value, int Start, int End) _beforeDrag;
    private int? _hover;
    private const double Gutter = 10;
    private const double TrackTop = 22;
    private const double TrackBottom = 46;
    static TimelineBar() => AffectsRender<TimelineBar>(MinimumProperty, MaximumProperty, ValueProperty, AvailableProperty, RangeStartProperty, RangeEndProperty);
    public TimelineBar()
    {
        Focusable = true; Height = 51; MinWidth = 120; ClipToBounds = true;
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }
    public int LastFrame => Math.Max(0, (int)Maximum);
    public int LastAvailable => Math.Clamp(Available, 0, LastFrame);
    public double FrameX(int frame) => Gutter + Math.Clamp(frame, 0, LastFrame) * Math.Max(1, Bounds.Width - Gutter * 2) / Math.Max(1, Maximum);
    public int FrameAt(double x) => Math.Clamp((int)Math.Round((x - Gutter) * Maximum / Math.Max(1, Bounds.Width - Gutter * 2)), 0, LastFrame);
    public static int TickInterval(int maximum, double width)
    {
        double needed = Math.Max(1, maximum) * 72 / Math.Max(1, width - Gutter * 2);
        double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(1, needed))));
        foreach (int factor in new[] { 1, 2, 5, 10 })
            if (scale * factor >= needed) return (int)Math.Clamp(scale * factor, 1, int.MaxValue);
        return 1;
    }
    private IBrush Brush(string key, IBrush fallback) => this.TryFindResource(key, out var value) && value is IBrush brush ? brush : fallback;
    private FormattedText Text(string text, IBrush brush) => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 11, brush);
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= Gutter * 2) return;
        var foreground = Brush("WorkspaceForeground", Brushes.White);
        var muted = Brush("WorkspaceMuted", Brushes.Gray);
        var border = Brush("WorkspaceBorder", Brushes.Gray);
        var surface = Brush("WorkspaceHeader", Brushes.DimGray);
        var accent = Brushes.DeepSkyBlue;
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        var track = new Rect(Gutter, TrackTop, Math.Max(1, Bounds.Width - Gutter * 2), TrackBottom - TrackTop);
        context.DrawRectangle(surface, new Pen(border), track);
        double availableX = FrameX(LastAvailable);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(40, 170, 185, 200)), new Rect(Gutter, TrackTop, Math.Max(1, availableX - Gutter), track.Height));
        if (LastAvailable < LastFrame)
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(65, 0, 0, 0)), new Rect(availableX, TrackTop, Math.Max(0, track.Right - availableX), track.Height));
        int start = Math.Clamp(RangeStart, 0, LastFrame), end = Math.Clamp(RangeEnd < 0 ? LastFrame : RangeEnd, start, LastFrame);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(25, 0, 160, 240)), new Rect(FrameX(start), TrackTop, Math.Max(1, FrameX(end) - FrameX(start)), track.Height));
        int interval = TickInterval(LastFrame, Bounds.Width);
        for (long frame = 0; frame <= LastFrame; frame += interval)
        {
            double x = FrameX((int)frame);
            context.DrawLine(new Pen(border), new Point(x, TrackTop - 3), new Point(x, TrackTop));
            var label = Text(frame.ToString(CultureInfo.CurrentCulture), muted);
            if (frame == 0 || x + label.Width / 2 < Bounds.Width - 48)
                context.DrawText(label, new Point(Math.Clamp(x - label.Width / 2, 0, Math.Max(0, Bounds.Width - label.Width)), 2));
        }
        if (LastFrame > 0)
        {
            var last = Text(LastFrame.ToString(CultureInfo.CurrentCulture), muted);
            context.DrawText(last, new Point(Math.Max(0, Bounds.Width - last.Width - 2), 2));
        }
        void Bracket(int frame, bool opening)
        {
            double x = FrameX(frame), foot = opening ? 6 : -6;
            var pen = new Pen(accent, 2);
            context.DrawLine(pen, new Point(x, TrackTop + 2), new Point(x, TrackBottom - 2));
            context.DrawLine(pen, new Point(x, TrackTop + 2), new Point(x + foot, TrackTop + 2));
            context.DrawLine(pen, new Point(x, TrackBottom - 2), new Point(x + foot, TrackBottom - 2));
        }
        Bracket(start, true); Bracket(end, false);
        if (_hover is { } hover && hover != (int)Value)
        {
            double x = FrameX(hover);
            context.DrawLine(new Pen(muted), new Point(x, TrackTop), new Point(x, TrackBottom));
            Label(hover, muted);
        }
        double head = FrameX((int)Value);
        context.DrawLine(new Pen(foreground, 1.5), new Point(head, TrackTop - 2), new Point(head, TrackBottom));
        var triangle = new StreamGeometry();
        using (var geometry = triangle.Open())
        { geometry.BeginFigure(new Point(head - 4, TrackTop - 6), true); geometry.LineTo(new Point(head + 4, TrackTop - 6)); geometry.LineTo(new Point(head, TrackTop - 1)); geometry.EndFigure(true); }
        context.DrawGeometry(foreground, null, triangle);
        if (_hover is null || _hover == (int)Value || Math.Abs(FrameX(_hover.Value) - head) > 50) Label((int)Value, foreground);
        if (IsKeyboardFocusWithin) context.DrawRectangle(null, new Pen(accent), track.Inflate(2));
        void Label(int frame, IBrush color)
        {
            var label = Text(frame.ToString(CultureInfo.CurrentCulture), color);
            double x = Math.Clamp(FrameX(frame) - label.Width / 2, 0, Math.Max(0, Bounds.Width - label.Width - 6));
            context.DrawRectangle(surface, null, new Rect(Math.Max(0, x - 4), 0, label.Width + 14, 16));
            context.DrawText(label, new Point(x + 3, 1));
        }
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        Focus(); var point = e.GetPosition(this);
        _beforeDrag = (Value, RangeStart, RangeEnd);
        double start = FrameX(RangeStart), end = FrameX(RangeEnd < 0 ? LastFrame : RangeEnd);
        _drag = point.Y >= TrackTop && Math.Abs(point.X - start) <= 7 ? Drag.In
            : point.Y >= TrackTop && Math.Abs(point.X - end) <= 7 ? Drag.Out : Drag.Playhead;
        _pointer = e.Pointer; e.Pointer.Capture(this);
        ApplyPointer(point.X); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e); var point = e.GetPosition(this);
        _hover = FrameAt(point.X);
        if (_drag != Drag.None) ApplyPointer(point.X);
        InvalidateVisual();
    }
    private void ApplyPointer(double x)
    {
        int frame = FrameAt(x);
        if (_drag == Drag.Playhead) Value = Math.Min(frame, LastAvailable);
        else if (_drag == Drag.In) { RangeStart = Math.Min(frame, RangeEnd < 0 ? LastFrame : RangeEnd); RangeEdited?.Invoke(RangeStart, RangeEnd); }
        else if (_drag == Drag.Out) { RangeEnd = Math.Max(frame, RangeStart); RangeEdited?.Invoke(RangeStart, RangeEnd); }
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    { base.OnPointerReleased(e); _drag = Drag.None; _pointer = null; e.Pointer.Capture(null); }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    { base.OnPointerCaptureLost(e); _drag = Drag.None; _pointer = null; }
    protected override void OnPointerExited(PointerEventArgs e)
    { base.OnPointerExited(e); _hover = null; InvalidateVisual(); }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    { base.OnPointerWheelChanged(e); Value = Math.Clamp((int)Value + Math.Sign(e.Delta.Y), 0, LastAvailable); e.Handled = true; }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && _drag != Drag.None)
        {
            _drag = Drag.None; Value = _beforeDrag.Value; RangeStart = _beforeDrag.Start; RangeEnd = _beforeDrag.End;
            RangeEdited?.Invoke(RangeStart, RangeEnd); var pointer = _pointer; _pointer = null; pointer?.Capture(null); e.Handled = true; return;
        }
        int? target = e.Key switch
        {
            Key.Left => (int)Value - 1, Key.Right => (int)Value + 1,
            Key.PageUp => (int)Value - 10, Key.PageDown => (int)Value + 10,
            Key.Home => RangeStart, Key.End => RangeEnd < 0 ? LastFrame : RangeEnd, _ => null
        };
        if (target is { } frame) { Value = Math.Clamp(frame, 0, LastAvailable); e.Handled = true; }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new TimelinePeer(this);
    private sealed class TimelinePeer : ControlAutomationPeer, global::Avalonia.Automation.Provider.IRangeValueProvider
    {
        private readonly TimelineBar _bar;
        public TimelinePeer(TimelineBar bar) : base(bar) { _bar = bar; }
        public bool IsReadOnly => !_bar.IsEnabled;
        public double Minimum => 0;
        public double Maximum => _bar.LastAvailable;
        public double Value => _bar.Value;
        public double SmallChange => 1;
        public double LargeChange => 10;
        public void SetValue(double value)
        {
            if (IsReadOnly) throw new InvalidOperationException("The timeline is disabled.");
            _bar.Value = Math.Clamp(Math.Round(value), 0, _bar.LastAvailable);
        }
        protected override string GetNameCore() => UiText.Text("Frame timeline");
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;
        protected override string GetItemStatusCore() => $"{(int)_bar.Value} / {_bar.LastFrame}; available through {_bar.LastAvailable}; in {_bar.RangeStart}, out {(_bar.RangeEnd < 0 ? _bar.LastFrame : _bar.RangeEnd)}";
        protected override string GetHelpTextCore() => _bar.TryFindResource("TipPreviewTimeline", out var help) && help is string text ? text : "Drag to scrub or adjust the in/out brackets.";
    }
}
