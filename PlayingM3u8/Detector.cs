using PlayingM3u8.Darknet;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PlayingM3u8.Detector
{
    public class Detector
    {
        private static readonly float detectionThreshold = 0.25f; // anything more than 25% of certainty

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// Shorten a string so that total lenght of the string is:
        /// * 10 characters at start
        /// * three dots
        /// * 10 characters at end
        /// 
        /// If total lenght of input is longer than 23, just return it
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string ShortenString(string input, int left = 15, int right = 20)
        {
            if (input.Length < 3 + left + right) return input;

            return $"{input.Substring(0, left)}...{input.Substring(input.Length - right, right)}";
        }

        private static void Main(string[] args)
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string sourceFolder = Path.Combine(currentDirectory, "thumbnails");

            if (!Directory.Exists(sourceFolder))
            {
                Console.WriteLine($"{sourceFolder} does not exist. Exiting ...");
                Console.ReadLine();
            }

            // set dll load path
            SetDllDirectory(Path.Combine(currentDirectory, @"lib"));

            // quick detect
            string inputImage0 = Path.Combine(sourceFolder, "thumbnail-0.jpg");
            string inputImage1 = Path.Combine(sourceFolder, "thumbnail-1.jpg");
            string inputImage2 = Path.Combine(sourceFolder, "thumbnail-2.jpg");
            string[] inputImages = { inputImage0, inputImage1, inputImage2 };

            string objectNames = Path.Combine(currentDirectory, @"config\coco.names");
            string cfgFile = Path.Combine(currentDirectory, @"config\yolov3.cfg");
            string weightsFilename = Path.Combine(currentDirectory, @"weights\yolov3.weights");

            try
            {
                string[] names = File.ReadAllLines(objectNames);

                using (YoloWrapper yolo = new YoloWrapper(cfgFile, weightsFilename, 0))
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    foreach (string inputImage in inputImages)
                    {
                        watch.Restart();
                        YoloWrapper.bbox_t[] objectBoxes = yolo.Detect(inputImage);
                        watch.Stop();
                        long detectMili = watch.ElapsedMilliseconds;

                        watch.Restart();
                        YoloWrapper.bbox_t[] boxes = yolo.Track(objectBoxes);
                        watch.Stop();
                        long trackmili = watch.ElapsedMilliseconds;

                        Console.WriteLine($"File {ShortenString(inputImage)} predicted in\t: {detectMili / 1000d}s");
                        Console.WriteLine($"File {ShortenString(inputImage)} tracking in\t: {trackmili / 1000d}s");
                        Console.WriteLine($"Total detections\t: {boxes.Length}");

                        foreach (YoloWrapper.bbox_t item in boxes)
                        {
                            if (item.prob >= detectionThreshold)
                            {
                                string detectedObject = item.obj_id < names.Length ? names[item.obj_id] : "N/A";
                                Console.Write($"[{item.track_id}]\t{detectedObject}: {Math.Floor(item.prob * 100d)}%\t");
                                Console.WriteLine($"(left_x:\t{item.x}\ttop_y:\t{item.y}\twidth:\t{item.w}\theight:\t{item.h})");
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            finally
            {
                Console.ReadLine();
            }

        }
    }
}
