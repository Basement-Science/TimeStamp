using System;
using System.Text;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Pastel;
using System.Drawing;
using System.Threading.Tasks;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.Help;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace TimeStamp {
    class Program {
        private static StreamWriter fileWriter;
        private static string ProgramName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

        private static RootCommand rootCommand;
        private static Option opt_local, opt_TimestampNewlines, opt_allowRedirectingANSIcolor, opt_ColorMode, opt_OutputFile;

        private static bool ranMainProgram = false;

        public static async Task<int> Main(string[] args) {
            rootCommand = new RootCommand(description: "Adds TimeStamps to the beginning of each Line from the Pipe |");

            opt_local = new Option<bool>(
                aliases: new string[] { "--local", "-l", "/l" },
                getDefaultValue: () => false,
                description: "Will use your local Timezone to generate a Timestamp instead of UTC. \n" +
                    "Warning: Local timezones may be subject to adjustments due to \"Daylight Savings\" Time or similar.");

            opt_TimestampNewlines = new Option<bool>(
                aliases: new string[] { "--timestamp-Newlines", "-n", "/n" },
                getDefaultValue: () => false,
                description: "Will print a Timestamp whenever an empty line arrives.");

            opt_allowRedirectingANSIcolor = new Option<bool>(
                aliases: new string[] { "--allow-redirecting-ANSI-color" },
                getDefaultValue: () => false,
                description: $"Only relevant when redirecting {ProgramName}'s output to other programs.\n" +
                $"Use this option to allow passing ANSI-colorCoded text to redirected stdout.");

            opt_ColorMode = new Option<int>(
                aliases: new string[] { "--color-Mode", "-c", "/c" },
                getDefaultValue: () => 1,
                description: "Specify how Colors will be used.\n" +
                "0 - do not use color and remove existing\n" +
                "1 - use legacy 16-color mode.\n" +
                "2 - use full RGB with 'ANSI codes'\n" +
                "    may cause issues when output is forwarded to other programs");

            opt_OutputFile = new Option<FileInfo>(
                aliases: new string[] { "--output-File", "-o", "/o" },
                getDefaultValue: () => null,
                description: "(Optional) Specify path to a Text File. All output will be written " +
                "to this file IN ADDITION to standard output (console)\n" +
                "Similar to the linux command 'tee'");

            foreach (var option in new Option[] { opt_local, opt_TimestampNewlines, opt_allowRedirectingANSIcolor, opt_ColorMode, opt_OutputFile }) {
                rootCommand.Add(option);
            }

            rootCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.Handler = CommandHandler.Create<bool, bool, bool, int, FileInfo>(handleInput);

            // listen to ctrl+c from now on
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ExitHandler);

            // start the main program
            int mainExecution = await rootCommand.InvokeAsync(args).ConfigureAwait(false);

            // if main program did not run (when help was displayed)
            if (!ranMainProgram) {
                try {
                    // define example command parameters
                    const string exampleSwitches_TS = " -c 2";
                    string exampleCommand;

                    using (Process p = new Process()) {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                            // windows always has cmd and ping command.
                            exampleCommand = "ping -n 3 localhost | ";
                            p.StartInfo.FileName = "cmd";

                            p.StartInfo.Arguments = "/c " + exampleCommand + "\"" +
                                System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\"" +
                                exampleSwitches_TS;
                        } else if ( // try to handle all UNIX systems
                            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                            RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
                            // unix systems may have different shells and tools.
                            var candidates = new List<string>();
                            foreach (string line in System.IO.File.ReadLines("/etc/shells")) {
                                // check if the line is commented out
                                if (line[line.TakeWhile(c => char.IsWhiteSpace(c)).Count()] != '#') {
                                    if (File.Exists(line)) {
                                        candidates.Add(line);
                                    } else {
                                        Console.WriteLine(line + " does not exist!");
                                    }
                                }
                            }
                            var preferredShells = ImmutableArray.Create(new[] { "bash", "zsh", "sh" });
                            foreach (string preferred in preferredShells) {
                                foreach (string candidate in candidates) {
                                    if (candidate.Contains(preferred)) {
                                        // found the best shell. Set it and exit from loops.
                                        p.StartInfo.FileName = candidate;
                                        goto foundShell;
                                    }
                                }
                            }
                            // didnt find a preferred shell. Try the first one and hope for the best.
                            p.StartInfo.FileName = candidates.First();
                        foundShell:

                            exampleCommand = "ping -c 3 localhost | ";

                            p.StartInfo.Arguments = "-c \"" + exampleCommand + "\\\"" +
                                System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\\\"" +
                                exampleSwitches_TS + "\"";
                        } else {
                            throw new NotSupportedException("unsupported Operating System.");
                        }

                        // print FULL example command
                        Console.WriteLine("Example usage: '" + exampleCommand + ProgramName
                            + exampleSwitches_TS + "' ");
                        Console.WriteLine("Running example using " + p.StartInfo.FileName + " ...");
                        // actually run the example command

                        //Console.WriteLine(p.StartInfo.Arguments);
                        p.Start();
                        p.WaitForExit();
                    }
                } catch (NotSupportedException e) {
                    Console.WriteLine(e);
                    Console.WriteLine(" Will not attempt to run an Example command. Exiting...");
                } catch (Exception e) {
                    Console.WriteLine(e);
                    Console.WriteLine("Something went wrong while trying to run an Example command. Exiting...");
                }
            }
            return mainExecution;
        }

        /* Watches Standard Input (pipe) and starts printing */
        private static void handleInput(bool local, bool TimestampNewlines, bool allowRedirectingANSIcolor, int ColorMode, FileInfo OutputFile) {
            ranMainProgram = true;
            bool UseLocalTimezone = local;

            /* Console.WriteLine($"UseLocalTimezone {UseLocalTimezone}, TimestampNewlines {TimestampNewlines}, " +
                $"ColorMode {ColorMode}, OutputFile { OutputFile}"); */

            // Open text file if it was specified
            FileStream fileHandle = null;
            if (OutputFile != null) {
                fileHandle = OutputFile.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                fileWriter = new StreamWriter(fileHandle);
            }

            // define RGB colors to be used
            var defaultPalette = new ColorPalette(DateColor: "#FF7800", TimeColor: "#FF3500", 
                TimeZoneColor: "#808080", BGcolor: "#000000");
            var colorPalette = defaultPalette; // TODO: Add support for custom, user-defined ColorPalettes

            if (Console.IsOutputRedirected && ColorMode >= 2 && !allowRedirectingANSIcolor) {
                string warning = $"{ProgramName} - Warning: Output was redirected AND an ANSI colorMode was selected.\n" +
                    $"This will cause problems with many other programs. ColorMode has been overridden to default.\n" +
                    $"To force an ANSI colorMode, please specify option: {opt_allowRedirectingANSIcolor.Name}";
                Console.Error.WriteLine(Console.IsErrorRedirected ? warning : warning.Pastel(Color.Yellow));

                ColorMode = 1;
            }

            if (Console.IsInputRedirected) {
                // Program was called with a pipe input
                string s;
                while ((s = Console.ReadLine()) != null) {
                    if (TimestampNewlines || s.Length != 0) {
                        PrintTimeStamp(" - ");
                    }
                    // print the line itself
                    Console.WriteLine(s);
                    WriteToFile(s + '\n');
                }
            } else {
                // Program was called directly
                PrintTimeStamp("\n");
            }
            CleanUp();

            /* Prints the Timestamp in UTC or local time, plus another string after it. */
            void PrintTimeStamp(string postFix) {
                DateTime time = UseLocalTimezone ? DateTime.Now : DateTime.UtcNow;
                string[] stamp = time.ToString("u", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('Z').Split(' ', 2);
                stamp[0] += " ";
                string end = UseLocalTimezone ? "" : "Z";
                StringBuilder builder;
                string output;

                Console.Write('[');
                if (ColorMode >= 1) {
                    // store colors before Timestamp
                    ConsoleColor oldBackground = Console.BackgroundColor;
                    ConsoleColor oldForeground = Console.ForegroundColor;
                    if (ColorMode >= 2) {
                        // Use ANSI code colors (mode 2+)
                        builder = new StringBuilder(stamp[0].Pastel(colorPalette.DateColor))
                            .Append(stamp[1].Pastel(colorPalette.TimeColor)).Append(end.Pastel(colorPalette.TimeZoneColor));
                        output = builder.ToString().PastelBg(colorPalette.BGcolor);
                        Console.Write(output);

                        // set output like in the other modes, for File output
                        GetSimpleTimeStamp(out output);
                    } else {
                        // Use oldschool color (mode 1)
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Blue;

                        GetSimpleTimeStamp(out output);
                        Console.Write(output);
                    }
                    // restore previous colors
                    Console.ForegroundColor = oldForeground;
                    Console.BackgroundColor = oldBackground;
                } else {
                    // No color (mode 0 or invalid)
                    GetSimpleTimeStamp(out output);
                    Console.Write(output);
                }
                Console.Write(']' + postFix);

                // try to write timestamp to file
                WriteToFile('[' + output + ']' + postFix);


                // puts together and prints the Timestamp without any ANSI codes
                void GetSimpleTimeStamp(out string output) {
                    builder = new StringBuilder(stamp[0]).Append(stamp[1]).Append(end);
                    output = builder.ToString();
                } // END PrintSimpleTimeStamp
            } // END PrintTimeStamp
            void WriteToFile(string output) {
                if (fileHandle != null) {
                    try {
                        fileWriter.Write(output);
                    } catch (Exception e) {
                        ConsoleColor oldBackground = Console.BackgroundColor;
                        ConsoleColor oldForeground = Console.ForegroundColor;
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine(e);
                        Console.ForegroundColor = oldForeground;
                        Console.BackgroundColor = oldBackground;
                    }
                }
            } // END WriteToFile
        } // END handleInput

        private static void CleanUp() {
            if (fileWriter != null) {
                fileWriter.Close();
            }
        }

        /* // handle ctrl+c */
        protected static void ExitHandler(object sender, ConsoleCancelEventArgs e) {
            e.Cancel = true;
        }
    }
    class ColorPalette {
        public ColorPalette(string DateColor, string TimeColor, string TimeZoneColor, string BGcolor) {
            this.DateColor = DateColor;
            this.TimeColor = TimeColor;
            this.TimeZoneColor = TimeZoneColor;
            this.BGcolor = BGcolor;
        }

        public string DateColor { get; set; }
        public string TimeColor { get; set; }
        public string TimeZoneColor { get; set; }
        public string BGcolor { get; set; }
    }
}
