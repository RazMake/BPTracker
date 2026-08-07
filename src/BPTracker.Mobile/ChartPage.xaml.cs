using BPTracker.Presentation.Charts;

namespace BPTracker.Mobile;

public partial class ChartPage : ContentPage
{
    private readonly ChartViewModel _viewModel;

    private ChartGesture _gesture;
    private double _scrollOrigin;

    public ChartPage(ChartViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        BindingContext = _viewModel;
        Plot.Drawable = new ChartDrawable(_viewModel);
        _viewModel.FrameChanged += OnFrameChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _viewModel.RefreshAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("BPTracker", $"Could not read your history.\n\n{exception.Message}", "OK");
        }

        EmptyLabel.IsVisible = !_viewModel.HasData;
        Plot.Invalidate();
    }

    private void OnFrameChanged(object? sender, EventArgs e) => Plot.Invalidate();

    private void OnPlotSizeChanged(object? sender, EventArgs e) => _viewModel.Resize(
        Plot.Width - ChartDrawable.LeftGutter - ChartDrawable.RightPadding,
        Plot.Height - ChartDrawable.TopPadding - ChartDrawable.BottomGutter);

    private void OnTouchStart(object? sender, TouchEventArgs e)
    {
        if (InPlot(e) is not { } point)
        {
            return;
        }

        _gesture = _viewModel.GestureFor(point.Y);

        if (_gesture == ChartGesture.Scroll)
        {
            _scrollOrigin = point.X;
            _viewModel.BeginScroll();
        }
        else
        {
            _viewModel.Inspect(point.X);
        }
    }

    private void OnTouchDrag(object? sender, TouchEventArgs e)
    {
        if (InPlot(e) is not { } point)
        {
            return;
        }

        if (_gesture == ChartGesture.Scroll)
        {
            _viewModel.Scroll(point.X - _scrollOrigin);
        }
        else
        {
            _viewModel.Inspect(point.X);
        }
    }

    private void OnTouchEnd(object? sender, TouchEventArgs e) => _viewModel.ClearCursor();

    private void OnTouchCancel(object? sender, EventArgs e) => _viewModel.ClearCursor();

    // Touches arrive in view coordinates; the view model works in plot coordinates.
    private static Point? InPlot(TouchEventArgs e) => e.Touches.Length == 0
        ? null
        : new Point(e.Touches[0].X - ChartDrawable.LeftGutter, e.Touches[0].Y - ChartDrawable.TopPadding);
}
