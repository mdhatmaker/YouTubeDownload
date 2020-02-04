using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Google.Apis;
using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;
//using NReco.VideoConverter;
//using YoutubeExplode.DemoConsole.Internal;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;


// https://stackoverflow.com/questions/39877884/c-sharp-download-the-sound-of-a-youtube-video
// https://github.com/Tyrrrz/YoutubeExplode
// https://github.com/Tyrrrz/YoutubeExplode/blob/master/ReadMe.md
// https://docs.microsoft.com/en-us/windows/win32/wmp/creating-the-windows-media-player-control-programmatically?redirectedfrom=MSDN


namespace YouTubeDownload
{
    public partial class YoutubeDownloadForm : Form
    {
        //string destFolder = @"<your destination folder>";
        string destFolder = @"D:\Users\mhatm\Downloads\";

        Timer timerVideo = new Timer();

        System.Media.SoundPlayer splayer = new System.Media.SoundPlayer();
        WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

        public YoutubeDownloadForm()
        {
            InitializeComponent();
        }

        private async void btnDownload_Click(object sender, EventArgs e)
        {
            btnDownload.Enabled = false;
            showPlayButtons(false);

            txtMessages.BackColor = SystemColors.ControlLight;

            /*if (chkAudioOnly.Checked)
                DownloadYoutubeAudio(txtYoutubeUrl.Text);
            else
                DownloadYoutubeVideo(txtYoutubeUrl.Text);*/

            // Main method in consoles cannot be asynchronous so we run everything synchronously
            // (only valid if using this from static main method in Console App)
            //DownloadVideoAsync(txtYoutubeUrl.Text).GetAwaiter().GetResult();
            await DownloadVideoAsync(txtYoutubeUrl.Text);

            btnDownload.Enabled = true;
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            wplayer.controls.play();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            wplayer.controls.pause();
        }

        private void txtYoutubeUrl_DoubleClick(object sender, EventArgs e)
        {
            txtYoutubeUrl.SelectAll();
        }

        private void showPlayButtons(bool visible)
        {
            btnPlay.Visible = visible;
            btnStop.Visible = visible;
        }

        private void DownloadYoutubeVideo(string youtubeVideoUrl, string saveToFolder = null)
        {
            display("Downloading Video please wait ... ");

            var localFolder = @saveToFolder ?? destFolder;

            YouTube youtube = YouTube.Default;
            Video vid = youtube.GetVideo(youtubeVideoUrl);
            var localFilename = localFolder + vid.FullName;
            System.IO.File.WriteAllBytes(localFilename, vid.GetBytes());

            display($" Video Download Completed: {localFilename}");
            txtMessages.BackColor = Color.White;
        }

        /*private void DownloadYoutubeAudio(string videoUrl)
        {
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(videoUrl);
            File.WriteAllBytes(destFolder + vid.FullName, vid.GetBytes());

            var inputFile = new MediaFile { Filename = destFolder + vid.FullName };
            var outputFile = new MediaFile { Filename = $"{destFolder + vid.FullName}.mp3" };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }
        }*/

        private void DownloadYoutubeAudio(string youtubeVideoUrl, string localMp3Name = null, string saveToFolder = null)
        {
            display("Downloading Video please wait ... ");

            var localFolder = @saveToFolder ?? destFolder;
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(youtubeVideoUrl);
            // If you want the best audio:
            //var vid = youtube.GetAllVideos(youtubeVideoUrl).OrderByDescending(v => v.AudioBitrate).First();
            // If you want the "worst" audio (smallest filesize?):
            //var vid = youtube.GetAllVideos(youtubeVideoUrl).OrderBy(v => v.AudioBitrate).First();

            var localFilename = localFolder + vid.FullName;
            System.IO.File.WriteAllBytes(localFilename, vid.GetBytes());

            display("Video Download Completed - Converting to MP3 ... ");
            
            var localName = localMp3Name ?? vid.Title;
            var localMp3Filename = localFolder + localName + ".mp3";

            var inputFile = new MediaFile { Filename = localFilename };
            var outputFile = new MediaFile { Filename = localMp3Filename };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }

            display($"File Converted to MP3: '{localMp3Filename}'");
            txtMessages.BackColor = Color.White;

            if (chkAutoplay.Checked)
            {
                showPlayButtons(true);
                PlayMp3(localMp3Filename);
            }
        }

        private void display(string text)
        {
            Console.WriteLine(text);
            txtMessages.Text = " " + text;
            Application.DoEvents();
        }

        /// <summary>
        /// If given a YouTube URL, parses video id from it.
        /// Otherwise returns the same string.
        /// </summary>
        //private static string NormalizeVideoId(string input) =>
        private string NormalizeVideoId(string input) =>
          YoutubeClient.TryParseVideoId(input, out var videoId)
              ? videoId : input;

        // Call with YouTube video ID or URL
        //private static async Task DownloadVideoAsync(string videoIdOrUrl)
        private async Task DownloadVideoAsync(string videoIdOrUrl, string localName = null, string saveToFolder = null)
        {
            // Client
            var client = new YoutubeClient();

            // Get the video ID
            /*Console.Write("Enter YouTube video ID or URL: ");
            var videoId = Console.ReadLine();*/
            var videoId = NormalizeVideoId(videoIdOrUrl);

            // Get media stream info set
            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(videoId);

            // Choose the best muxed stream
            var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();
            if (streamInfo == null)
            {
                display("This video has no streams");
                return;
            }

            // Compose file name, based on metadata
            var fileExtension = streamInfo.Container.GetFileExtension();
            //var fileName = $"{videoId}.{fileExtension}";
            var localFolder = @saveToFolder ?? destFolder;
            var fileName = localFolder + (localName ?? $"{videoId}") + $".{fileExtension}";

            // Download video
            display($"Downloading stream: {streamInfo.VideoQualityLabel} / {fileExtension}... ");
            using (var progress = new InlineProgress(this.progressDownload))
                await client.DownloadMediaStreamAsync(streamInfo, fileName, progress);

            display($"Video saved to '{fileName}'");
        }

        public string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        public string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        private async void DownloadYoutubeAudioOnly(string youtubeVideoUrl, string saveToFolder = null)
        {
            var mp3FolderPath = @saveToFolder ?? destFolder;

            // Client
            var client = new YoutubeClient();
            var videoId = NormalizeVideoId(youtubeVideoUrl);
            var video = await client.GetVideoAsync(videoId);
            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(videoId);
            // Get the best muxed stream
            var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();
            // Compose file name, based on metadata
            var fileExtension = streamInfo.Container.GetFileExtension();
            var fileName = $"{video.Title}.{fileExtension}";
            // Replace illegal characters in file name
            fileName = RemoveInvalidChars(fileName);    //RemoveIllegalFileNameChars(fileName);
            timerVideo.Enabled = true;
            // Download video
            txtMessages.Text = "Downloading Video please wait ... ";

            //using (var progress = new ProgressBar())
            await client.DownloadMediaStreamAsync(streamInfo, fileName);

            // Add Nuget package: https://www.nuget.org/packages/NReco.VideoConverter/ To Convert MP4 to MP3
            if (chkAudioOnly.Checked)
            {
                var Convert = new NReco.VideoConverter.FFMpegConverter();
                string saveMp3File = mp3FolderPath + fileName.Replace(".mp4", ".mp3");
                Convert.ConvertMedia(fileName, saveMp3File, "mp3");
                //Delete the MP4 file after conversion
                File.Delete(fileName);
                LoadMp3Files();
                txtMessages.Text = $"File Converted to MP3: '{saveMp3File}'";
                timerVideo.Enabled = false;
                txtMessages.BackColor = Color.White;
                if (chkAutoplay.Checked)
                {
                    PlayMp3(saveMp3File);
                }
                return;
            }
        }

        // TODO: What is this method supposed to do?
        private void LoadMp3Files()
        {

        }

        public void PlayWav(string wavFilename)
        {
            splayer.SoundLocation = wavFilename;
            splayer.Play();
        }

        public void StopWav()
        {
            splayer.Stop();
        }

        public void PlayMp3(string mp3Filename)
        {
            wplayer.URL = mp3Filename;
            wplayer.controls.play();
        }

        public void StopMp3()
        {
            wplayer.controls.stop();
        }

        public class InlineProgress : IProgress<double>, IDisposable
        {
            delegate void SetProgressCallback(int value);

            ProgressBar pb = null;

            public InlineProgress(ProgressBar progressBar = null)
            {
                this.pb = progressBar;
                if (this.pb != null) this.pb.Visible = true;
            }

            public void Dispose()
            {
                Console.WriteLine("InlineProgress DISPOSE");
                if (this.pb != null) this.pb.Visible = false;
            }

            private void SetProgress(int value)
            {
                if (this.pb.InvokeRequired)
                {
                    SetProgressCallback d = new SetProgressCallback(SetProgress);
                    this.pb.Invoke(d, new object[] { value });
                }
                else
                {
                    this.pb.Value = value;
                }
            }

            public void Report(double value)
            {
                Console.WriteLine("InlineProgress: {0}", value);
                if (this.pb != null)
                {
                    int ivalue = Convert.ToInt32(value * 100);
                    SetProgress(ivalue);
                    //Application.DoEvents();
                }
            }
        } // end of class InlineProgress

    } // end of class YoutubeDownloadForm



} // end of namespace
