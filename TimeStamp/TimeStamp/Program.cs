using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Pastel;

namespace TimeStamp {
    class Program {
        public static void Main(string[] args) {
            var rootCommand = new RootCommand(description: "Adds TimeStamps to the beginning of each Line from the Pipe |");

            var opt_UseLocalTimezone = new Option<bool>(aliases: new string[] { "--local", "-l", "/l" },
                getDefaultValue: () => false,
                description: "Will use your local Timezone to generate a Timestamp instead of UTC. \n" +
                    "Warning: Local timezones may be subject to adjustments due to \"Daylight Savings\" Time or similar.");
            rootCommand.Add(opt_UseLocalTimezone);

            var opt_TimestampNewlines = new Option<bool>(aliases: new string[] { "--timestamp-Newlines", "-n", "/n" },
                getDefaultValue: () => false,
                description: "Will print a Timestamp whenever an empty line arrives.");
            rootCommand.Add(opt_TimestampNewlines);

            var opt_ColorMode = new Option<int>(aliases: new string[] { "--color-Mode", "-c", "/c" },
                getDefaultValue: () => 1,
                description: "Specify how Colors will be used.\n" +
                "0 - do not use color and remove existing\n" +
                "1 - use old 16-color mode.\n" +
                "2 - use full RGB with 'ANSI codes'\n" +
                "    may cause issues when output is forwarded to other programs");
            rootCommand.Add(opt_ColorMode);

            var opt_OutputFile = new Option<FileInfo>(aliases: new string[] { "--output-File", "-o", "/o" },
                getDefaultValue: () => null,
                description: "Specify path to a Text File. All output will be written " +
                "to this file IN ADDITION to standard output (console)\n" +
                "Similar to the linux command 'tee'");
            rootCommand.Add(opt_OutputFile);

            rootCommand.Handler = CommandHandler.Create<bool, bool, int, FileInfo>(handleInput);

            // listen to ctrl+c from now on
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ExitHandler);
            rootCommand.Invoke(args);
        }

        /* Watches Standard Input (pipe) and starts printing */
        private static void handleInput(bool local, bool TimestampNewlines, int ColorMode, FileInfo OutputFile) {
            bool UseLocalTimezone = local;

            /* Console.WriteLine($"UseLocalTimezone {UseLocalTimezone}, TimestampNewlines {TimestampNewlines}, " +
                $"ColorMode {ColorMode}, OutputFile { OutputFile}"); */

            var defaultPalette = new ColorPalette(DateColor: "#FF7800", TimeColor: "#FF3500", 
                TimeZoneColor: "#808080", BGcolor: "#000000");
            var colorPalette = defaultPalette; // TODO: Add support for custom, user-defined ColorPalettes

            if (Console.IsInputRedirected) {
                // Program was called with a pipe input
                string s;
                while ((s = Console.ReadLine()) != null) {
                    if (TimestampNewlines || s.Length != 0) {
                        PrintTimeStamp(" - ");
                    }
                    // print the line itself
                    Console.WriteLine(s);
                }
            } else {
                // Program was called directly
                PrintTimeStamp("\n");
            }

            /* Prints the Timestamp in UTC or local time, plus another string after it. */
            void PrintTimeStamp(string postFix) {
                DateTime time = UseLocalTimezone ? DateTime.Now : DateTime.UtcNow;
                string[] stamp = time.ToString("u", System.Globalization.CultureInfo.InvariantCulture).TrimEnd('Z').Split(' ', 2);
                stamp[0] += " ";
                string end = UseLocalTimezone ? "" : (ColorMode >= 2 ? "Z".Pastel(colorPalette.TimeZoneColor) : "Z");
                StringBuilder builder;
                string output;

                Console.Write('[');
                if (ColorMode >= 1) {
                    // store colors before Timestamp
                    ConsoleColor oldBackground = Console.BackgroundColor;
                    ConsoleColor oldForeground = Console.ForegroundColor;
                    if (ColorMode >= 2) {
                        builder = new StringBuilder(stamp[0].Pastel(colorPalette.DateColor))
                            .Append(stamp[1].Pastel(colorPalette.TimeColor)).Append(end);
                        output = builder.ToString().PastelBg(colorPalette.BGcolor);
                        Console.Write(output);
                    } else {
                        // Use oldschool color
                        Console.BackgroundColor = ConsoleColor.Black;
                        Console.ForegroundColor = ConsoleColor.Blue;

                        PrintSimpleTimeStamp();
                    }
                    // restore previous colors
                    Console.ForegroundColor = oldForeground;
                    Console.BackgroundColor = oldBackground;
                } else {
                    // No color
                    PrintSimpleTimeStamp();
                }
                Console.Write(']' + postFix);

                // puts together and prints the Timestamp without any ANSI codes
                void PrintSimpleTimeStamp() {
                    builder = new StringBuilder(stamp[0]).Append(stamp[1]).Append(end);
                    output = builder.ToString();
                    Console.Write(output);
                }
            }
        }

        private static void CleanUp() {
            //Console.ResetColor();
        }

        /* // handle ctrl+c */
        protected static void ExitHandler(object sender, ConsoleCancelEventArgs e) {
            CleanUp();
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
