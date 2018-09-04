using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Vlc.DotNet.Core;

namespace PlayingM3u8.Streaming
{
    internal class StreamCapture
    {
        private static void Main(string[] args)
        {
            PlayerVLCService vlcService = new PlayerVLCService(isDebugEnabled: false);

            string[] mediaOptions = new string[]
            {
                ":sout=#file{dst="+Path.Combine(vlcService.DestinationFolder, $"record-{DateTime.Now.Ticks}.mpeg")+"}",
                ":sout-keep"
            };

            //Uri playPathUri = new Uri("https://kamere.amss.org.rs/horgos1/horgos1.m3u8");
            Uri playPathUri = new Uri("https://mnmedias.api.telequebec.tv/m3u8/29880.m3u8");
            vlcService.Play(playPathUri, mediaOptions);

            // Ugly, sorry, that's just an example...
            while (!vlcService.PlayFinished)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }


            vlcService.Stop();

            Console.ReadLine();
        }
    }

    public class PlayerVLCService
    {
        private VlcMediaPlayer _vlcMediaPlayer;
        public readonly string DestinationFolder;

        public PlayerVLCService(bool isDebugEnabled = false)
        {
            Assembly currentAssembly = Assembly.GetEntryAssembly();
            string currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory == null)
                return;

            DirectoryInfo vlcLibDirectory;
            if (IntPtr.Size == 4)
                vlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"libvlc\win-x86\"));
            else
                vlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"libvlc\win-x64\"));

            string[] vlcOptions = InitVlcOptions(isDebugEnabled);

            DestinationFolder = Path.Combine(currentDirectory, "streams");

            if (!Directory.Exists(DestinationFolder))
            {
                Directory.CreateDirectory(DestinationFolder);
            }

            _vlcMediaPlayer = new VlcMediaPlayer(vlcLibDirectory, vlcOptions);

            InitEventHandlers();
        }

        /// <summary>
        /// Init the options
        /// </summary>
        /// <param name="isDebugEnabled"></param>
        /// <returns></returns>
        private static string[] InitVlcOptions(bool isDebugEnabled)
        {
            string[] options = new[]
            {
                "--intf", "dummy", /* no interface                   */
                "--vout", "dummy", /* we don't want video output     */
                "--no-audio", /* we don't want audio decoding   */
                "--no-video-title-show", /* nor the filename displayed     */
                "--no-stats", /* no stats */
                "--no-sub-autodetect-file", /* we don't want subtitles        */
                "--no-snapshot-preview", /* no blending in dummy vout      */
            };

            string[] vlcDebugOptions = new string[]
            {
                "-vvv",
                "--extraintf=logger",
                "--logfile=" + Path.Combine(Environment.CurrentDirectory, "Logs.log")
            };

            string[] vlcOptions = new string[isDebugEnabled ? options.Length + vlcDebugOptions.Length : options.Length];

            Array.Copy(options, vlcOptions, options.Length);
            if (isDebugEnabled)
                Array.Copy(vlcDebugOptions, 0, vlcOptions, options.Length, vlcDebugOptions.Length);

            return vlcOptions;
        }

        public bool PlayFinished { get; private set; }

        public void Play(Uri playPathUri, string[] mediaOptions)
        {
            try
            {
                _vlcMediaPlayer.SetMedia(playPathUri, mediaOptions);

                _vlcMediaPlayer.Play();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                PlayFinished = true;
            }
        }

        public void Stop()
        {
            _vlcMediaPlayer.Stop();
            PlayFinished = false;
        }

        /// <summary>
        /// Setup the event handlers for the media player
        /// </summary>
        private void InitEventHandlers()
        {
            _vlcMediaPlayer.TimeChanged += _vlcMediaPlayer_TimeChanged;
            _vlcMediaPlayer.EncounteredError += (sender, e) =>
            {
                Console.Error.WriteLine("An error occurred: " + e.ToString());
                PlayFinished = true;
            };

            _vlcMediaPlayer.EndReached += (sender, e) =>
            {
                PlayFinished = true;
            };

        }

        private void _vlcMediaPlayer_TimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
        {
            Console.WriteLine($"time: {e.NewTime / 1000}");
        }
    }
}
