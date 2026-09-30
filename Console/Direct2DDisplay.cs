//=================================================================
// Direct2DDisplay.cs
// Hardware-presented compatibility bridge for the legacy display.
// This is the first migration stage toward the native Thetis renderer.
//=================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using D2DAlphaMode = SharpDX.Direct2D1.AlphaMode;
using D2DFactory = SharpDX.Direct2D1.Factory;
using GdiColor = System.Drawing.Color;
using GdiBitmap = System.Drawing.Bitmap;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;

namespace PowerSDR
{
    internal static class Direct2DDisplay
    {
        private static readonly object sync = new object();
        private static D2DFactory factory;
        private static WindowRenderTarget renderTarget;
        private static GdiInteropRenderTarget gdiInterop;
        private static Control host;
        private static int width, height;
        private static bool active;
        private static bool collectingFrame;
        private static bool hasPresented;
        internal static bool HasPresented { get { lock (sync) return active && hasPresented; } }
        private static string lastError = String.Empty;
        private static readonly List<NativeCommand> nativeCommands =
            new List<NativeCommand>();
        private static readonly List<RawVector2[]> pointBuffers = new List<RawVector2[]>();
        private static int pointBufferIndex;

        private static RawVector2[] SnapshotPoints(System.Drawing.Point[] source, int count, float offset)
        {
            count = Math.Min(count, source.Length);
            if (pointBufferIndex == pointBuffers.Count) pointBuffers.Add(null);
            RawVector2[] buffer = pointBuffers[pointBufferIndex];
            if (buffer == null || buffer.Length != count)
                pointBuffers[pointBufferIndex] = buffer = new RawVector2[count];
            pointBufferIndex++;
            for (int i = 0; i < count; i++)
                buffer[i] = new RawVector2(source[i].X + offset, source[i].Y + offset);
            return buffer;
        }
        private static readonly WaterfallCache[] waterfallCaches =
            { new WaterfallCache(), new WaterfallCache() };

        private sealed class WaterfallCache
        {
            internal GdiBitmap Source;
            internal D2DBitmap Bitmap;
            internal byte[] Bgra;
            internal byte[] BgrRow;
            internal int Width;
            internal int Height;
            internal int SourceX = -1;
        }

        private struct NativeCommand
        {
            internal int Kind;
            internal RawVector2[] Points;
            internal int Count;
            internal float Top, Bottom;
            internal RawColor4 Color;
            internal bool Gradient;
            internal float GradientAlpha;
            internal float Width;
            internal GdiBitmap Source;
            internal int SourceX;
            internal int DestinationY;
            internal int Receiver;
            internal bool Refresh;
            internal Vector2 Start, End;
        }

        private const int CommandWaterfall = 1;
        private const int CommandFill = 2;
        private const int CommandTrace = 3;
        private const int CommandLine = 4;

        internal static bool IsActive { get { lock (sync) return active; } }
        internal static string LastError { get { lock (sync) return lastError; } }

        internal static bool IsSupported()
        {
            try
            {
                using (D2DFactory probe = new D2DFactory(FactoryType.SingleThreaded)) return true;
            }
            catch { return false; }
        }

        internal static bool TryInitialize(Control value)
        {
            lock (sync)
            {
                ShutdownNoLock();
                if (value == null || value.IsDisposed || !value.IsHandleCreated) return false;
                try
                {
                    lastError = String.Empty;
                    factory = new D2DFactory(FactoryType.SingleThreaded);
                    host = value;
                    CreateSurfaces(Math.Max(1, value.ClientSize.Width),
                        Math.Max(1, value.ClientSize.Height));
                    active = true;
                    return true;
                }
                catch (Exception ex)
                {
                    lastError = ex.ToString();
                    ShutdownNoLock();
                    return false;
                }
            }
        }

        internal static bool Render(Control value, Action<Graphics> drawLegacyFrame)
        {
            lock (sync)
            {
                if (!active || value == null || drawLegacyFrame == null) return false;
                try
                {
                    int wantedWidth = Math.Max(1, value.ClientSize.Width);
                    int wantedHeight = Math.Max(1, value.ClientSize.Height);
                    if (value != host || wantedWidth != width || wantedHeight != height)
                    {
                        host = value;
                        CreateSurfaces(wantedWidth, wantedHeight);
                    }

                    renderTarget.BeginDraw();
                    IntPtr deviceContext = IntPtr.Zero;
                    Graphics directGraphics = null;
                    try
                    {
                        deviceContext = gdiInterop.GetDC(DeviceContextInitializeMode.Clear);
                        directGraphics = Graphics.FromHdc(deviceContext);
                        nativeCommands.Clear();
                        pointBufferIndex = 0;
                        collectingFrame = true;
                        directGraphics.Clear(GdiColor.Black);
                        drawLegacyFrame(directGraphics);
                    }
                    finally
                    {
                        collectingFrame = false;
                        if (directGraphics != null) directGraphics.Dispose();
                        if (deviceContext != IntPtr.Zero) gdiInterop.ReleaseDC();
                    }

                    DrawNativeCommands();
                    renderTarget.EndDraw();
                    hasPresented = true;
                    return true;
                }
                catch (Exception ex)
                {
                    lastError = ex.ToString();
                    ShutdownNoLock();
                    return false;
                }
            }
        }

        internal static void Shutdown()
        {
            lock (sync) ShutdownNoLock();
        }

        internal static bool QueuePanadapterTrace(System.Drawing.Point[] source,
            int count, GdiColor color, float strokeWidth)
        {
            lock (sync)
            {
                if (!active || !collectingFrame || source == null || count < 2)
                    return false;

                nativeCommands.Add(new NativeCommand
                {
                    Kind = CommandTrace,
                    Points = SnapshotPoints(source, count, 0.5f),
                    Count = Math.Min(count, source.Length),
                    Color = ToRawColor(color),
                    Width = Math.Max(0.5f, strokeWidth)
                });
                return true;
            }
        }

        internal static bool QueuePanadapterFill(System.Drawing.Point[] source,
            int count, float top, float bottom, GdiColor color,
            bool gradient, int gradientAlpha)
        {
            lock (sync)
            {
                if (!active || !collectingFrame || source == null || count < 2)
                    return false;

                nativeCommands.Add(new NativeCommand
                {
                    Kind = CommandFill,
                    Points = SnapshotPoints(source, count, 0.0f),
                    Count = Math.Min(count, source.Length),
                    Top = top,
                    Bottom = bottom,
                    Color = ToRawColor(color),
                    Gradient = gradient,
                    GradientAlpha = Math.Max(0, Math.Min(255, gradientAlpha)) / 255.0f
                });
                return true;
            }
        }

        internal static bool QueueWaterfallBitmap(GdiBitmap source, int sourceX,
            int visibleWidth, int destinationY, int receiver, bool refresh)
        {
            lock (sync)
            {
                if (!active || !collectingFrame || source == null || receiver < 1 || receiver > 2)
                    return false;

                int safeX = Math.Max(0, Math.Min(source.Width - 1, sourceX));
                int safeWidth = Math.Max(1, Math.Min(visibleWidth, source.Width - safeX));
                nativeCommands.Add(new NativeCommand
                {
                    Kind = CommandWaterfall,
                    Source = source,
                    SourceX = safeX,
                    Count = safeWidth,
                    DestinationY = destinationY,
                    Receiver = receiver,
                    Refresh = refresh
                });
                return true;
            }
        }

        internal static bool QueueLine(float x1, float y1, float x2, float y2,
            GdiColor color, float strokeWidth)
        {
            lock (sync)
            {
                if (!active || !collectingFrame) return false;
                nativeCommands.Add(new NativeCommand
                {
                    Kind = CommandLine,
                    Start = new Vector2(x1 + 0.5f, y1 + 0.5f),
                    End = new Vector2(x2 + 0.5f, y2 + 0.5f),
                    Color = ToRawColor(color),
                    Width = Math.Max(0.5f, strokeWidth)
                });
                return true;
            }
        }

        private static void DrawNativeCommands()
        {
            foreach (NativeCommand command in nativeCommands)
            {
                switch (command.Kind)
                {
                    case CommandWaterfall: DrawNativeWaterfall(command); break;
                    case CommandFill: DrawNativePanadapterFill(command); break;
                    case CommandTrace: DrawNativePanadapterTrace(command); break;
                    case CommandLine: DrawNativeLine(command); break;
                }
            }
        }

        private static void DrawNativeWaterfall(NativeCommand overlay)
        {
            WaterfallCache cache = waterfallCaches[overlay.Receiver - 1];
            bool recreate = cache.Bitmap == null || cache.Source != overlay.Source ||
                cache.Width != overlay.Count || cache.Height != overlay.Source.Height;
            if (recreate)
            {
                DisposeWaterfallCache(cache);
                cache.Source = overlay.Source;
                cache.Width = overlay.Count;
                cache.Height = overlay.Source.Height;
                cache.Bgra = new byte[cache.Width * cache.Height * 4];
                cache.BgrRow = new byte[cache.Width * 3];
                cache.Bitmap = new D2DBitmap(renderTarget,
                    new Size2(cache.Width, cache.Height),
                    new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm,
                        D2DAlphaMode.Ignore)));
            }

            if (recreate || overlay.Refresh || cache.SourceX != overlay.SourceX)
                UploadWaterfall(cache, overlay.SourceX);

            renderTarget.DrawBitmap(cache.Bitmap,
                new SharpDX.RectangleF(0, overlay.DestinationY,
                    cache.Width, cache.Height),
                1.0f, BitmapInterpolationMode.NearestNeighbor);
        }

        private static void UploadWaterfall(WaterfallCache cache, int sourceX)
        {
            System.Drawing.Rectangle area = new System.Drawing.Rectangle(
                sourceX, 0, cache.Width, cache.Height);
            BitmapData bits = cache.Source.LockBits(area, ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try
            {
                int destination = 0;
                for (int y = 0; y < cache.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(bits.Scan0, y * bits.Stride),
                        cache.BgrRow, 0, cache.BgrRow.Length);
                    int source = 0;
                    for (int x = 0; x < cache.Width; x++)
                    {
                        cache.Bgra[destination++] = cache.BgrRow[source++];
                        cache.Bgra[destination++] = cache.BgrRow[source++];
                        cache.Bgra[destination++] = cache.BgrRow[source++];
                        cache.Bgra[destination++] = 255;
                    }
                }

                GCHandle pinned = GCHandle.Alloc(cache.Bgra, GCHandleType.Pinned);
                try { cache.Bitmap.CopyFromMemory(pinned.AddrOfPinnedObject(), cache.Width * 4); }
                finally { pinned.Free(); }
                cache.SourceX = sourceX;
            }
            finally { cache.Source.UnlockBits(bits); }
        }

        private static void DrawNativeLine(NativeCommand line)
        {
            using (SolidColorBrush brush = new SolidColorBrush(renderTarget, line.Color))
                renderTarget.DrawLine(line.Start, line.End, brush, line.Width);
        }

        private static void DrawNativePanadapterFill(NativeCommand fill)
        {
            using (PathGeometry geometry = new PathGeometry(factory))
            {
                using (GeometrySink sink = geometry.Open())
                {
                    sink.BeginFigure(new Vector2(fill.Points[0].X, fill.Points[0].Y),
                        FigureBegin.Filled);
                    sink.AddLines(fill.Points);
                    sink.AddLine(new Vector2(fill.Points[fill.Count - 1].X, fill.Bottom));
                    sink.AddLine(new Vector2(fill.Points[0].X, fill.Bottom));
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                if (fill.Gradient)
                {
                    GradientStop[] stops = CreatePanadapterGradientStops(fill.GradientAlpha);
                    using (GradientStopCollection collection =
                        new GradientStopCollection(renderTarget, stops))
                    using (LinearGradientBrush brush = new LinearGradientBrush(renderTarget,
                        new LinearGradientBrushProperties
                        {
                            StartPoint = new Vector2(0, fill.Bottom),
                            EndPoint = new Vector2(0, fill.Top + 10)
                        }, collection))
                        renderTarget.FillGeometry(geometry, brush);
                }
                else
                {
                    using (SolidColorBrush brush = new SolidColorBrush(renderTarget, fill.Color))
                        renderTarget.FillGeometry(geometry, brush);
                }
            }
        }

        private static GradientStop[] CreatePanadapterGradientStops(float alpha)
        {
            float[] positions = { 0.0f, 0.05f, 0.10f, 0.15f, 0.20f, 0.25f,
                0.30f, 0.35f, 0.45f, 0.55f, 0.65f, 0.80f, 1.0f };
            GdiColor[] colors = { GdiColor.Black, GdiColor.Blue,
                GdiColor.FromArgb(0, 127, 255), GdiColor.Cyan,
                GdiColor.FromArgb(0, 255, 127), GdiColor.FromArgb(127, 255, 0),
                GdiColor.Yellow, GdiColor.Orange, GdiColor.Red,
                GdiColor.FromArgb(255, 0, 127), GdiColor.Magenta,
                GdiColor.FromArgb(127, 0, 255), GdiColor.FromArgb(127, 0, 255) };
            GradientStop[] stops = new GradientStop[positions.Length];
            for (int i = 0; i < stops.Length; i++)
            {
                RawColor4 value = ToRawColor(colors[i]);
                value.A = alpha;
                stops[i] = new GradientStop { Position = positions[i], Color = value };
            }
            return stops;
        }

        private static RawColor4 ToRawColor(GdiColor color)
        {
            return new RawColor4(color.R / 255.0f, color.G / 255.0f,
                color.B / 255.0f, color.A / 255.0f);
        }

        private static void DrawNativePanadapterTrace(NativeCommand trace)
        {
            using (PathGeometry geometry = new PathGeometry(factory))
            {
                using (GeometrySink sink = geometry.Open())
                {
                    sink.BeginFigure(trace.Points[0], FigureBegin.Hollow);
                    sink.AddLines(trace.Points);
                    sink.EndFigure(FigureEnd.Open);
                    sink.Close();
                }
                using (SolidColorBrush brush = new SolidColorBrush(renderTarget, trace.Color))
                    renderTarget.DrawGeometry(geometry, brush, trace.Width);
            }
        }

        private static void CreateSurfaces(int newWidth, int newHeight)
        {
            if (factory == null || host == null || !host.IsHandleCreated)
                throw new InvalidOperationException("Direct2D display host is unavailable.");

            DisposeSurfaces();
            width = newWidth;
            height = newHeight;
            RenderTargetProperties properties = new RenderTargetProperties(
                RenderTargetType.Hardware,
                new PixelFormat(Format.B8G8R8A8_UNorm, D2DAlphaMode.Ignore),
                96.0f, 96.0f, RenderTargetUsage.GdiCompatible, FeatureLevel.Level_DEFAULT);
            HwndRenderTargetProperties window = new HwndRenderTargetProperties
            {
                Hwnd = host.Handle,
                PixelSize = new Size2(width, height),
                PresentOptions = PresentOptions.Immediately
            };
            renderTarget = new WindowRenderTarget(factory, properties, window);
            gdiInterop = renderTarget.QueryInterface<GdiInteropRenderTarget>();
        }

        private static void DisposeSurfaces()
        {
            for (int i = 0; i < waterfallCaches.Length; i++)
                DisposeWaterfallCache(waterfallCaches[i]);
            if (gdiInterop != null) gdiInterop.Dispose();
            if (renderTarget != null) renderTarget.Dispose();
            gdiInterop = null;
            renderTarget = null;
        }

        private static void DisposeWaterfallCache(WaterfallCache cache)
        {
            if (cache.Bitmap != null) cache.Bitmap.Dispose();
            cache.Source = null;
            cache.Bitmap = null;
            cache.Bgra = null;
            cache.BgrRow = null;
            cache.Width = cache.Height = 0;
            cache.SourceX = -1;
        }

        private static void ShutdownNoLock()
        {
            active = false;
            hasPresented = false;
            collectingFrame = false;
            nativeCommands.Clear();
            pointBuffers.Clear();
            pointBufferIndex = 0;
            DisposeSurfaces();
            if (factory != null) factory.Dispose();
            factory = null;
            host = null;
            width = height = 0;
        }
    }
}
