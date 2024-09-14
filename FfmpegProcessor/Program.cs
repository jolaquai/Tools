using System.Diagnostics.CodeAnalysis;

using Xabe.FFmpeg;

namespace FfmpegProcessor;

internal static class Program
{
    static void Main(string[] args)
    {
        #region Args filtering
        if (args.Length == 0)
        {
            Console.WriteLine("No arguments given.");
            return;
        }

        Console.WriteLine("CL arguments:");
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            Console.WriteLine($"    [{i}]{arg}");
        }

        FFmpeg.SetExecutablesPath(@"E:\PROGRAMMING\ffmpeg");

        var options = Array.FindAll(args, arg => arg.StartsWith('-'));
        var files = Array.FindAll(args, o => Array.IndexOf(options, o) == -1 && File.Exists(o));

        if (Array.Find(options, o => o == "--help") is not null)
        {
            Console.WriteLine($"""
                {nameof(FfmpegProcessor)}
                A simple tool to cut the last seconds of a video file with variable options.

                --sendto
                    Automatically sets downscale options, keeping the original FPS and cutting the last 30 seconds if --seek is not specified.

                --downscale[=factor]
                    Downscale the video by a factor of 2/3 by default, or by the specified factor.
                --seek=seconds
                    Cut the last specified seconds of the video.
                --fps=fps
                    Set the FPS of the output video. 0 keeps the original FPS.
                """);
            return;
        }

        if (files.Length == 0)
        {
            Console.WriteLine("No files left after extracting arguments OR none of the files specified exist.");
            return;
        }

        var asSendToHandler = Array.Find(options, o => o == "--sendto") is not null;
        #endregion

        #region Downscaling option
        bool? downscale = null;
        double? downscaleFactor = null;
        if (asSendToHandler)
        {
            Console.WriteLine("--sendto: Enabling downscale.");
            downscale = true;
            downscaleFactor = 0.5;
        }
        else if (Array.Find(options, o => o.StartsWith("--downscale")) is string downscaleOption)
        {
            downscale = true;
            if (downscaleOption.Length > 11 && double.TryParse(downscaleOption[12..], out var downscaleFactorParse))
            {
                downscaleFactor = downscaleFactorParse;
            }
        }
        else
        {
            do
            {
                Console.WriteLine("Downscale? (true/false/factor)");
                var input = Console.ReadLine();
                if (bool.TryParse(input, out var downscaleYesNoParse))
                {
                    downscale = downscaleYesNoParse;
                }
                else if (double.TryParse(input, out var downscaleFactorParse))
                {
                    downscale = true;
                    downscaleFactor = downscaleFactorParse;
                }
                else
                {
                    Console.WriteLine("Invalid input.");
                }
            }
            while (downscale is null);
        }
        #endregion

        #region Seek option
        var seekSeconds = 30;
        if (Array.Find(options, o => o.StartsWith("--seek=")) is string secOption)
        {
            seekSeconds = secOption[7..] == "keep" ? -1 : int.Parse(secOption[7..]);
        }
        else
        {
            Console.WriteLine("Export last? (seconds, default = 30)");
            if (int.TryParse(Console.ReadLine(), out var cutSecondsParse) && cutSecondsParse > 0)
            {
                seekSeconds = cutSecondsParse;
            }
            else
            {
                Console.WriteLine("Defaulting to 30s.");
            }
        }
        #endregion

        #region Trim end option
        var trimEndSeconds = 0;
        if (Array.Find(options, o => o.StartsWith("--trimEnd=")) is string trimEndOption)
        {
            trimEndSeconds = trimEndOption[10..] == "keep" ? -1 : int.Parse(trimEndOption[10..]);
        }
        else
        {
            Console.WriteLine("From those, trim last? (seconds, default = 0)");
            if (int.TryParse(Console.ReadLine(), out var trimSecondsParse) && trimSecondsParse > 0)
            {
                trimEndSeconds = trimSecondsParse;
            }
            else
            {
                Console.WriteLine("Defaulting to 0s.");
            }
        }
        #endregion

        #region FPS option
        var fps = 30;
        if (asSendToHandler)
        {
            fps = 0;
        }
        else if (Array.Find(options, o => o.StartsWith("--fps=")) is string fpsOption)
        {
            fps = int.Parse(fpsOption[6..]);
        }
        else
        {
            Console.WriteLine("FPS? (0 keeps input, default = 30)");
            if (int.TryParse(Console.ReadLine(), out var fpsParse) && fpsParse >= 0)
            {
                fps = fpsParse;
            }
            else
            {
                Console.WriteLine("Defaulting to 30.");
            }
        }
        #endregion

        var tasks = files.Select(async file =>
        {
            var output = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file) + "-c.mp4");

            var conv = EmptyConversion;

            // Open and add a video stream as input
            var streams = await FFmpeg.GetMediaInfo(file);
            var videoStream = streams.VideoStreams;
            var audioStream = streams.AudioStreams;
            var dataVideoStream = videoStream.MaxBy(vs => vs.Duration)!;

            // Add seek parameter
            if (seekSeconds != -1)
            {
                if (trimEndSeconds > 0)
                {
                    conv.AddParameter($"-ss {(int)dataVideoStream.Duration.TotalSeconds - seekSeconds - trimEndSeconds}", ParameterPosition.PreInput);
                    conv.AddParameter($"-t {seekSeconds}", ParameterPosition.PreInput);
                }
                else
                {
                    conv.AddParameter($"-ss {(int)dataVideoStream.Duration.TotalSeconds - seekSeconds}", ParameterPosition.PreInput);
                }
            }
            conv.AddStream(videoStream)
                .AddStream(audioStream)
                ;

            if (downscale is true)
            {
                // Add downscale filter parameters
                // By default, just downscale to 2/3 of the original size
                conv.AddParameter($"-vf scale={dataVideoStream.Width * (downscaleFactor ?? 2 / 3)}:-1");
            }
            conv.SetOutput(output);
            if (fps != 0)
            {
                // Add fps parameter
                conv.SetFrameRate(fps);
            }

            // Console.WriteLine(conv.Build());

            await conv.Start();
        }).ToArray();
        try
        {
            Task.WaitAll(tasks);
        }
        catch (Exception ex)
        {
            ThrowWrappedException(ex);
        }

        Console.WriteLine("Done.");
        Console.ReadLine();
    }

    [DoesNotReturn]
    private static void ThrowWrappedException(Exception ex)
    {
        throw new AggregateException("FFmpeg error:" + string.Join(Environment.NewLine, ex.Message.Split(Environment.NewLine).Where(l => l.StartsWith("Error", StringComparison.OrdinalIgnoreCase))), ex);
    }

    private static IConversion EmptyConversion =>
        FFmpeg.Conversions.New()
        .SetPreset(ConversionPreset.Faster)
        .SetOverwriteOutput(true)
        .AddParameter("-c:a copy")
            ;
}
