using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using System;
using System.IO;
using System.Reflection;

namespace PlayingM3u8.Thumbnailer
{
    public class Program
    {
        protected static int origRow;
        protected static int origCol;

        private static readonly int barLen = 20;
        private static void Main(string[] args)
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            string sourceFolder = Path.Combine(currentDirectory, "streams");
            string destinationFolder = Path.Combine(currentDirectory, "thumbnails");

            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            MediaFile inputFile = new MediaFile { Filename = Path.Combine(sourceFolder, "record3.mpeg") };

            // Clear the screen, then save the top and left coordinates.
            Console.Clear();
            origRow = Console.CursorTop;
            origCol = Console.CursorLeft;

            using (Engine engine = new Engine())
            {
                engine.GetMetadata(inputFile);

                int i = 0;
                int seconds = inputFile.Metadata.Duration.Seconds;

                WriteAt($"File length: {seconds} sec", 0, 1);
                WriteAt("[", 0, 0);
                WriteAt("]", barLen + 2, 0);

                while (i < inputFile.Metadata.Duration.Seconds)
                {
                    ConversionOptions options = new ConversionOptions { Seek = TimeSpan.FromSeconds(i) };
                    MediaFile outputFile = new MediaFile { Filename = Path.Combine(destinationFolder, $"thumbnail-{i}.jpg") };
                    engine.GetThumbnail(inputFile, outputFile, options);
                    i++;

                    int posX = (int)Math.Floor(i / (double)seconds * barLen);
                    WriteAt("#", posX + 1, 0);
                }

            }

            WriteAt("Done", 0, 2);
            Console.ReadLine();
        }

        /// <summary>
        /// Console positioning made easy
        /// </summary>
        /// <param name="s">String to output</param>
        /// <param name="x">x coordinate where to output. relative to origCol position captured.</param>
        /// <param name="y">y coordinate where to output. relative to origRow position captured.</param>
        protected static void WriteAt(string s, int x, int y)
        {
            try
            {
                Console.SetCursorPosition(origCol + x, origRow + y);
                Console.Write(s);
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.Clear();
                Console.WriteLine(e.Message);
            }
        }
    }
}
