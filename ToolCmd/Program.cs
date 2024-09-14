using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text.RegularExpressions;

using DiscUtils.Iso9660;

namespace ToolCmd;

public static class Program
{
    private const string DateTimeFormat = @"yyyy-HH-dd\Thh:mm:ss.fffffff";
    private const string TimeSpanFormat = @"dd\Thh\:mm\:ss\.fffffff";
    public static readonly string eol = Environment.NewLine;

    public static async Task<int> Main(string[] args)
    {
        RootCommand rootCmd;
        // Set up new scope so nothing leaks into the CommandLineBuilder
        {
            rootCmd = new RootCommand("A collection of personal tools.");

            #region Dummy subcommand "dummy"
            Command dummyCmd;
            {
                dummyCmd = new Command("dummy", "Serves as a dummy command that allows passing 'nothing' into the program. It invokes the standard command middleware without actually doing anything.")
                {
                    IsHidden = true
                };
                rootCmd.AddCommand(dummyCmd);
            }
            #endregion

            #region Copy CD-ROM subcommand "copycd"
            Command copyCdCmd;
            {
                copyCdCmd = new Command("copycd", "Copies the contents of all inserted CD-ROMs into ISO files.");
                copyCdCmd.SetHandler(() =>
                {
                    var drives = DriveInfo.GetDrives().Where(d =>
                        d.DriveType == DriveType.CDRom
                        && d.IsReady
                    );
                    Parallel.ForEach(drives, drive =>
                    {
                        using (var temp = new TempDirectory())
                        {
                            var drv = Path.TrimEndingDirectorySeparator(drive.Name);

                            Parallel.ForEach(Directory.EnumerateFiles(drv, "*", SearchOption.AllDirectories), path =>
                            {
                                var localPath = path.Replace(drv, temp.Path);
                                Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                                File.Copy(path, localPath);
                            });

                            var builder = new CDBuilder()
                            {
                                UseJoliet = true,
                                VolumeIdentifier = drive.VolumeLabel,
                            };
                            foreach (var file in Directory.EnumerateFiles(temp.Path, "*", SearchOption.AllDirectories))
                            {
                                builder.AddFile(file.Replace(temp.Path, "").Trim(Path.DirectorySeparatorChar), file);
                            }
                            builder.Build($"C:\\{Regex.Replace(drive.VolumeLabel, $"[{Regex.Escape(string.Concat(Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Distinct()))}]", "")}.iso");
                        }
                    });
                });
                rootCmd.AddCommand(copyCdCmd);
            }
            #endregion

            #region Explorer restart subcommand "reex"
            var reexCmd = new Command("reex", "Restarts explorer.exe.");
            reexCmd.SetHandler(async () =>
            {
                Process.GetProcesses().Where(p => p.ProcessName.Contains("explorer")).ToList().ForEach(p =>
                {
                    try
                    {
                        p.Kill();
                    }
                    catch
                    {
                    }
                });
                await Task.Delay(250);

                Process.Start(@"C:\Windows\explorer.exe");
            });
            rootCmd.AddCommand(reexCmd);
            #endregion

            #region AHKv2 scripts restart subcommand "reahk"
            var reahkCmd = new Command("reahk", "Restarts the AHKv2 scripts.");
            reahkCmd.SetHandler(async () =>
            {
                Process.GetProcesses().Where(p => p.ProcessName.Contains("autohotkey64")).ToList().ForEach(p =>
                {
                    try
                    {
                        p.Kill();
                    }
                    catch
                    {
                    }
                });
                await Task.Delay(250);
                foreach (var script in new List<string>()
                {
                    Environment.ExpandEnvironmentVariables(@"%PROGRAMMING%\AHK Scripts\AutoCorrect.ah2"),
                    Environment.ExpandEnvironmentVariables(@"%PROGRAMMING%\AHK Scripts\Loops.ah2")
                })
                {
                    Process.Start(Environment.ExpandEnvironmentVariables(@"%PROGRAMMING%\AHK Scripts\AutoHotkey\AutoHotkey64.exe"), $"\"{script}\"");
                }
            });
            rootCmd.AddCommand(reahkCmd);
            #endregion

            #region Move into own directory subcommand "mvintodir"
            Command mvIntoDirCmd;
            // Set up new scope so the arguments don't leak into the root command
            {
                var mvIntoDirArg = new Argument<string[]>("path", "The files to move into their own directories.")
                {
                    Arity = ArgumentArity.OneOrMore
                };
                mvIntoDirCmd = new Command("mvintodir", "Moves the specified files into their own directories.")
                {
                    mvIntoDirArg
                };
                mvIntoDirCmd.SetHandler(paths =>
                {
                    foreach (var path in paths)
                    {
                        var dir = Path.GetDirectoryName(path);
                        var file = Path.GetFileName(path);
                        var newDir = Path.Combine(dir!, Path.GetFileNameWithoutExtension(file));
                        var newPath = Path.Combine(newDir, file);

                        if (!Directory.Exists(newDir))
                        {
                            Directory.CreateDirectory(newDir);
                        }

                        File.Move(path, newPath);
                    }
                }, mvIntoDirArg);
            }
            rootCmd.AddCommand(mvIntoDirCmd);
            #endregion

            #region Error lookup subcommand "err"
            Command errCmd;
            // Set up new scope so the arguments don't leak into the root command
            {
                var errArg = new Argument<int>("hresult", "The HResult / error code to look up.");
                errCmd = new Command("err", "Looks up an error code.")
                {
                    errArg
                };
                errCmd.SetHandler(code =>
                {
                    if (new System.ComponentModel.Win32Exception(code) is var ex)
                    {
                        Console.WriteLine($"HResult '{code}' corresponds to the following message:");
                        Console.WriteLine($"    '{ex.Message}'");
                    }
                    else
                    {
                        Console.WriteLine($"HResult '{code}' does not map to any known (predefined) Exception.");
                    }
                }, errArg);
            }
            rootCmd.AddCommand(errCmd);
            #endregion

            #region NVidia repair subcommand "repairnvidia"
            var repairNvidiaCmd = new Command("repairnvidia", "Kills all NVIDIA processes and starts GeForce Experience.");
            repairNvidiaCmd.SetHandler(() =>
            {
                Process.GetProcesses()
                    .Where(p => p.ProcessName.Contains("nvcontainer")
                        || p.ProcessName.Contains("nvdisplay.container")
                        || p.ProcessName.Contains("nvsphelper"))
                    .ToList()
                    .ForEach(p =>
                {
                    try
                    {
                        p.Kill();
                    }
                    catch
                    {
                    }
                });
            });
            rootCmd.AddCommand(repairNvidiaCmd);
            #endregion

            #region TeX render subcommand "rendertex"
            Command renderTexCmd;
            // Set up new scope so the arguments don't leak into the root command
            {
                var renderTexArg = new Argument<string>("tex", "The TeX to render.");
                renderTexCmd = new Command("rendertex", "Renders TeX to the clipboard.")
                {
                    renderTexArg
                };
                renderTexCmd.SetHandler(new Func<InvocationContext, Task>(async context =>
                {
                    var tex = context.ParseResult.GetValueForArgument(renderTexArg);
                    context.BindingContext.AddService(serviceProvider => new HttpClient());

                    using (var client = (HttpClient)context.BindingContext.GetService(typeof(HttpClient))!)
                    using (var response = await client.GetAsync(@"https://latex.codecogs.com/png.image?\dpi{600}" + tex))
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var initial = new Bitmap(stream))
                    using (var final = new Bitmap(initial.Width + 10, initial.Height + 10))
                    {
                        using (var g = Graphics.FromImage(final))
                        {
                            g.Clear(Color.FromArgb(0xD3D3D3));
                            g.DrawImage(initial, new Point(5, 5));
                        }

                        // Delegate the clipboard setting to a new thread with an STA apartment state
                        // We can't do this in here because the Main thread is async, so can by default not be STA
                        var clipboardSetThread = new Thread(state =>
                        {
                            var invocationContext = (InvocationContext)state!;

                            try
                            {
                                Clipboard.SetImage(final);

                                // Wait a bit to make sure the image actually reaches the clipboard
                                Thread.Sleep(250);

                                if (Clipboard.ContainsImage())
                                {
                                    invocationContext.Console.WriteLine($"Copied image from TeX:{eol}{tex}");
                                }
                                else
                                {
                                    invocationContext.Console.WriteLine("Error writing the image to the clipboard.");
                                }
                            }
                            catch (Exception ex)
                            {
                                invocationContext.Console.WriteLine("Error writing the image to the clipboard:");
                                invocationContext.Console.WriteLine($"    '{ex.Message}' ({ex.HResult & 0xFFFF} | 0x{ex.HResult & 0xFFFF:X4})");
                            }
                        });
                        clipboardSetThread.SetApartmentState(ApartmentState.STA);
                        clipboardSetThread.Start(context);
                        clipboardSetThread.Join();
                    }
                }));
            }
            rootCmd.AddCommand(renderTexCmd);
            #endregion

            #region C# clean-up subcommand "cleancs"
            Command cleanCsCmd;
            {
                var deepCleanOption = new Option<bool>("--deep", "Also cleans the NuGet package cache.");
                cleanCsCmd = new Command("cleancs", "Cleans all projects in the predefined projects directory and optionally deletes the NuGet package cache.")
                {
                    deepCleanOption
                };
                cleanCsCmd.SetHandler(deep =>
                {
                    const string topPath = @"E:\PROGRAMMING\Projects\C#";
                    var nuget = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget");
                    var clearSize = 0L;

                    if (!Directory.Exists(nuget))
                    {
                        deep = false;
                    }

                    var paths = Directory
                        .EnumerateDirectories(topPath, "*", SearchOption.AllDirectories)
                        .Where(dirPath => dirPath.EndsWith("bin", StringComparison.OrdinalIgnoreCase)
                            || dirPath.EndsWith("obj", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (deep)
                    {
                        paths.Add(nuget);
                    }

                    Parallel.ForEach(paths, async path =>
                    {
                        clearSize += new DirectoryInfo(path)
                            .EnumerateFiles("*", SearchOption.AllDirectories)
                            .Sum(fileInfo => fileInfo.Length);
                        await Console.Out.WriteLineAsync($"Deleting bin/obj/cache directory '{path.Replace(topPath, ".")}'");
                        try
                        {
                            Directory.Delete(path, true);
                        }
                        catch
                        {
                        }
                    });

                    Console.WriteLine($" -> Cleared {clearSize / 1024d / 1024:0.00} MB");
                }, deepCleanOption);
            }
            rootCmd.AddCommand(cleanCsCmd);
            #endregion

            #region Empty directory subcommand "emptyDir"
            Command emptyDirectory;
            {
                var pathArgument = new Argument<string>("--path", "The path of the directory to empty.");
                emptyDirectory = new Command("emptyDir", "Cleans all projects in the predefined projects directory and optionally deletes the NuGet package cache.")
                {
                    pathArgument
                };
                emptyDirectory.SetHandler(dir =>
                {
                    if (!Directory.Exists(dir))
                    {
                        throw new Exception("The directory to empty must exist.");
                    }

                    Directory.Delete(dir, true);
                    Directory.CreateDirectory(dir);
                }, pathArgument);
            }
            rootCmd.AddCommand(emptyDirectory);
            #endregion

            #region Move R6 clips into monthly subdirectories subcommand "mover6"
            Command moveR6Cmd;
            {
                moveR6Cmd = new Command("mover6", "Moves all unsorted Siege clips into monthly subfolders.");
                moveR6Cmd.SetHandler(() =>
                {
                    const string topPath = @"E:\YOUTUBE\Captures\Tom Clancy's Rainbow Six  Siege";
                    var paths = Directory.GetFiles(topPath, "*.mp4");

                    Parallel.ForEach(paths, async path =>
                    {
                        var time = File.GetLastWriteTime(path);
                        var dirName = Path.Combine(topPath, time.ToString("yyyy-MM"));
                        if (!Directory.Exists(dirName))
                        {
                            Directory.CreateDirectory(dirName);
                        }
                        var newPath = Path.Combine(dirName, Path.GetFileName(path));

                        await Console.Out.WriteLineAsync($"Moved '{path}' to '{newPath}'...");

                        File.Move(path, newPath);
                    });
                });
            }
            rootCmd.AddCommand(moveR6Cmd);
            #endregion
        }

        var cliBuilder = new CommandLineBuilder(rootCmd);

        #region Middlewares
        #region Exception handler
        cliBuilder.AddMiddleware(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                {
                    Console.Error.WriteLine($"[ERROR] Unhandled exception occurred during invocation of command '{context.ParseResult.CommandResult.Command.Name}'");
                    Console.Error.WriteLine($"[ERROR] Raw command-line: '{string.Join(' ', args)}'");
                    Console.Error.WriteLine($"[ERROR] Parsed tokens: {string.Join(", ",
                        context.ParseResult.Tokens.Select(token => $"'{token.Value}'"))}");
                    Console.Error.WriteLine($"[ERROR] Unmatched tokens: {string.Join(", ",
                        context.ParseResult.UnmatchedTokens.Select(arg => $"'{arg}'"))}");
                    Console.Error.WriteLine($"[ERROR] Ignored tokens: {string.Join(", ",
                        context.ParseResult.UnparsedTokens.Select(arg => $"'{arg}'"))}");
                    Console.Error.WriteLine($"[ERROR] {ex.Message} ({ex.HResult & 0xFFFF} | (0x{ex.HResult & 0xFFFF:X4}))");
                    Console.Error.WriteLine($"{ex.StackTrace}");
                }
                Console.ResetColor();

                context.ExitCode = ex.HResult & 0xFFFF;
                Console.ReadKey();
            }
        }, MiddlewareOrder.ExceptionHandler);
        #endregion

        cliBuilder.AddMiddleware(async (context, next) =>
        {
            if (!context.ParseResult.Tokens.Any())
            {
                // No command was specified, so pass through to the default handler
                await next(context);
                return;
            }

            var started = DateTime.Now;
            context.Console.WriteLine($"[Invoking specified command: '{context.ParseResult.CommandResult.Command.Name}']");
            context.Console.WriteLine($"Parse diagram: {context.ParseResult.Diagram()}");

            await next(context);

            context.Console.WriteLine("");
            context.Console.WriteLine($"[Finished in {(DateTime.Now - started).ToString(TimeSpanFormat)}]");
            Console.ReadLine();
        });

        cliBuilder.UseDefaults();
        #endregion

        return await cliBuilder.Build().InvokeAsync(args);
    }

    public class HttpClientBinder : BinderBase<HttpClient>
    {
        protected override HttpClient GetBoundValue(System.CommandLine.Binding.BindingContext bindingContext)
        {
            bindingContext.AddService(serviceProvider => new HttpClient());
            return (HttpClient)bindingContext.GetService(typeof(HttpClient));
        }
    }
}
