using System.Drawing;

namespace Pic.Recorder;

public class ScreenRecorder : IDisposable
{
    private bool _isRecording;
    private CancellationTokenSource? _cts;
    private Task? _recordingTask;
    private readonly string _outputPath;
    private readonly int _frameRate;
    private readonly Rectangle _captureRegion;

    public event EventHandler<string>? RecordingStarted;
    public event EventHandler<string>? RecordingStopped;
    public event EventHandler<string>? RecordingError;

    public bool IsRecording => _isRecording;

    public ScreenRecorder(Rectangle captureRegion, string outputPath, int frameRate = 30)
    {
        _captureRegion = captureRegion;
        _outputPath = outputPath;
        _frameRate = frameRate;
    }

    public void Start()
    {
        if (_isRecording) return;
        _isRecording = true;
        _cts = new CancellationTokenSource();
        _recordingTask = Task.Run(() => RecordLoop(_cts.Token));
        RecordingStarted?.Invoke(this, _outputPath);
    }

    public void Stop()
    {
        if (!_isRecording) return;
        _isRecording = false;
        _cts?.Cancel();
        try { _recordingTask?.Wait(5000); } catch { }
    }

    private void RecordLoop(CancellationToken token)
    {
        var interval = (int)(1000.0 / _frameRate);
        var frameDir = Path.Combine(Path.GetTempPath(), $"pic_frames_{Guid.NewGuid():N}");
        Directory.CreateDirectory(frameDir);

        try
        {
            var frameIndex = 0;
            while (!token.IsCancellationRequested)
            {
                var framePath = Path.Combine(frameDir, $"frame_{frameIndex:D8}.png");
                using var bitmap = CaptureFrame();
                if (bitmap != null)
                {
                    bitmap.Save(framePath, System.Drawing.Imaging.ImageFormat.Png);
                }
                frameIndex++;
                token.WaitHandle.WaitOne(interval);
            }
        }
        catch (Exception ex)
        {
            RecordingError?.Invoke(this, ex.Message);
        }
        finally
        {
            try
            {
                EncodeToMp4(frameDir, _outputPath, _frameRate);
            }
            catch (Exception ex)
            {
                RecordingError?.Invoke(this, $"Encoding failed: {ex.Message}");
            }
            RecordingStopped?.Invoke(this, _outputPath);
        }
    }

    private System.Drawing.Bitmap? CaptureFrame()
    {
        try
        {
            var hdcSrc = GetDC(IntPtr.Zero);
            try
            {
                var bitmap = new System.Drawing.Bitmap(_captureRegion.Width, _captureRegion.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var g = System.Drawing.Graphics.FromImage(bitmap);
                var hdcDest = g.GetHdc();
                try
                {
                    BitBlt(hdcDest, 0, 0, _captureRegion.Width, _captureRegion.Height,
                        hdcSrc, _captureRegion.Left, _captureRegion.Top, 0x00CC0020);
                }
                finally
                {
                    g.ReleaseHdc(hdcDest);
                }
                return bitmap;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdcSrc);
            }
        }
        catch
        {
            return null;
        }
    }

    private static void EncodeToMp4(string frameDir, string outputPath, int frameRate)
    {
        var outputDir = Path.GetDirectoryName(outputPath);
        if (outputDir != null) Directory.CreateDirectory(outputDir);

        var frames = Directory.GetFiles(frameDir, "frame_*.png").OrderBy(f => f).ToList();
        if (frames.Count == 0) return;

        var fps = $"fps={frameRate}";
        var firstFrame = frames[0];
        var inputPattern = Path.Combine(frameDir, "frame_%08d.png");

        var actualOutput = outputPath;
        if (!actualOutput.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            actualOutput = Path.ChangeExtension(actualOutput, ".mp4");

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-y -framerate {frameRate} -i \"{inputPattern}\" -c:v libx264 -pix_fmt yuv420p -preset fast -crf 23 \"{actualOutput}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit(30000);
        }
        catch
        {
            throw new InvalidOperationException(
                "FFmpeg is required for video encoding. Please install FFmpeg or add it to PATH.");
        }

        try { Directory.Delete(frameDir, true); } catch { }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int x, int y, int w, int h,
        IntPtr hdcSrc, int sx, int sy, uint rop);

    public void Dispose()
    {
        if (_isRecording) Stop();
        _cts?.Dispose();
    }
}
