
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using WaveDudeNext.Engine;

namespace WaveDudeNext.Avalonia;


public partial class MainWindow : Window
{
    private const int WidthPixels = 256;
    private const int HeightPixels = 256;

    private readonly WaveSimulator _engine;
    private readonly WriteableBitmap _fieldBitmap;
    private readonly WriteableBitmap _intensityBitmap;
    private readonly DispatcherTimer _timer;

    private int _step;

    public MainWindow()
    {
        InitializeComponent();

        // Create a simple white-background scene in BGRA format.
        byte[] scenePixels = new byte[
            WidthPixels * HeightPixels * 4
        ];

        for (int i = 0; i < WidthPixels * HeightPixels; i++)
        {
            scenePixels[i * 4 + 2] = 100; // Refractive index 1.0
            scenePixels[i * 4 + 3] = 255; // Alpha
        }

        // Green-channel pixel = wave source.
        int source = (
            (HeightPixels / 2) * WidthPixels +
            WidthPixels / 2
        ) * 4;

        scenePixels[source + 1] = 180;

        var scene = SceneData.FromBgra(
            scenePixels,
            WidthPixels,
            HeightPixels,
            WidthPixels * 4,
            useSources: true,
            dampingEdge: 16
        );

        var options = new EngineOptions(
            SourceFrequencyScale: 1.0,
            SourceAmplitude: 1.0,
            TrackIntensity: true,
            FourthOrderLaplacian: false,
            TimeDecay: 1.0,
            IntensityScale: 1.0
        );

        _engine = new WaveSimulator(scene, options);

        var size = new PixelSize(WidthPixels, HeightPixels);
        var dpi = new Vector(96, 96);

        _fieldBitmap = new WriteableBitmap(
            size, dpi, PixelFormats.Bgra8888, AlphaFormat.Opaque
        );

        _intensityBitmap = new WriteableBitmap(
            size, dpi, PixelFormats.Bgra8888, AlphaFormat.Opaque
        );

        FieldImage.Source = _fieldBitmap;
        IntensityImage.Source = _intensityBitmap;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };

        _timer.Tick += (_, _) => UpdateSimulation();

        RenderFrame();
    }

    private void Start_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _timer.Start();
    }

    private void Pause_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _timer.Stop();
    }

    private void Reset_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _timer.Stop();
        _engine.Reset();
        _step = 0;
        RenderFrame();
    }

    private void UpdateSimulation()
    {
        // Advance several simulation steps per display update.
        for (int i = 0; i < 4; i++)
        {
            _engine.AdvanceOneStep();
            _step++;
        }

        RenderFrame();
    }
    
    
    
    private void RenderFrame()
	{
	    _engine.PrepareFrame(true, true);

	    DrawPlane(_fieldBitmap, _engine.FieldData, false);

	    if (_engine.IntensityData is { } intensity)
	    {
		DrawPlane(_intensityBitmap, intensity, true);
	    }

	    // Tell Avalonia the images have changed.
	    FieldImage.InvalidateVisual();
	    IntensityImage.InvalidateVisual();

	    StatusText.Text = $"CPU simulation - Step {_step}";
	}



    private void DrawPlane(
        WriteableBitmap bitmap,
        float[] values,
        bool intensity)
    {
        int width = WidthPixels;
        int height = HeightPixels;
        int stride = _engine.Stride;
        int rim = _engine.Rim;

        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int src = (y + rim + 1) * stride
                          + x + rim + 1;

                float value = values[src];

                float normalized = intensity
                    ? MathF.Sqrt(MathF.Max(0, value)) * 8f
                    : value * 8f;

                normalized = Math.Clamp(normalized, -1f, 1f);

                int dst = (y * width + x) * 4;

                if (intensity)
                {
                    byte v = (byte)(
                        Math.Clamp(normalized, 0f, 1f) * 255f
                    );

                    // Grayscale intensity
                    pixels[dst] = v;
                    pixels[dst + 1] = v;
                    pixels[dst + 2] = v;
                }
                else
                {
                    // Negative waves blue, positive waves red.
                    byte magnitude = (byte)(
                        MathF.Abs(normalized) * 255f
                    );

                    pixels[dst] = normalized < 0 ? magnitude : (byte)0;
                    pixels[dst + 1] = 0;
                    pixels[dst + 2] = normalized > 0 ? magnitude : (byte)0;
                }

                pixels[dst + 3] = 255;
            }
        }

        using var framebuffer = bitmap.Lock();

        for (int y = 0; y < height; y++)
        {
            Marshal.Copy(
                pixels,
                y * width * 4,
                IntPtr.Add(framebuffer.Address, y * framebuffer.RowBytes),
                width * 4
            );
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _fieldBitmap.Dispose();
        _intensityBitmap.Dispose();
        _engine.Dispose();
        base.OnClosed(e);
    }
}

