using Microsoft.Win32;
using NAudio.Lame; // For LameMP3FileWriter and LAMEPreset
using NAudio.Wave; // Make sure you have NAudio installed
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MP42MP3
{
    public partial class MainWindow : Window
    {
        #region Private Fields

        private FullscreenVideoWindow _fullscreenVideoWindow;
        private bool _isDragging = false;
        private WindowState _storedWindowState = WindowState.Normal;
        private DispatcherTimer _timer;
        private bool isMediaPlaying = false;
        private MediaPlayer mediaPlayer = new MediaPlayer();

        #endregion Private Fields

        #region Public Constructors

        public MainWindow()
        {
            InitializeComponent();
            // Initialize the timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200); // Update every 200ms
            _timer.Tick += Timer_Tick;
            mediaPlayer.MediaEnded += (s, e) =>
            {
                // Update the UI on the UI thread
                Dispatcher.Invoke(() =>
                {
                    isMediaPlaying = false;
                    playPauseButton.Content = "Play";
                });
            };
        }

        #endregion Public Constructors

        #region Private Methods

        private async void ConvertToMp3_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            progressBar.Minimum = 0;
            progressBar.Maximum = filesListBox.Items.Count;
            progressBar.Value = 0;

            int selectedBitrate = GetSelectedBitrate();
            // set a deafult bitrate if none is selected
            if (selectedBitrate == 0)
            {
                selectedBitrate = 320000;
            }
            // need to get all the files in the list box and store the full path in a variable
            var filePaths = filesListBox.Items.Cast<string>().ToList();
            foreach (string filePath in filePaths)
            {
                string outputFilePath = Path.ChangeExtension(filePath, ".mp3");
                try
                {
                    using (var reader = new MediaFoundationReader(filePath))
                    {
                        using (var writer = new LameMP3FileWriter(outputFilePath, reader.WaveFormat, selectedBitrate))
                        {
                            await reader.CopyToAsync(writer);
                        }
                    }
                    UpdateStatusBar($"Converted: {Path.GetFileName(filePath)}");
                    if (deleteFilesCheckBox.IsChecked == true)
                    {
                        
                        Dispatcher.Invoke(() => filesListBox.Items.Remove(filePath));
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatusBar($"Error converting {Path.GetFileName(filePath)}: {ex.Message}");
                }

                progressBar.Value += 1;
            }

            progressBar.Visibility = Visibility.Hidden;
            MessageBox.Show("Conversion complete!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (filesListBox.SelectedItem != null)
            {
                string selectedFile = filesListBox.SelectedItem.ToString();
                videoDisplay.Source = new Uri(selectedFile, UriKind.RelativeOrAbsolute);
                // You don't necessarily want to play immediately upon selection, you may want to
                // wait for the user to press 'play' videoDisplay.Play();
            }
        }

        private int GetSelectedBitrate()
        {
            string selectedBitrateStr = ((ComboBoxItem)bitrateComboBox.SelectedItem)?.Content.ToString();
            return selectedBitrateStr switch
            {
                "128 kbps" => 128000,
                "192 kbps" => 192000,
                "256 kbps" => 256000,
                "320 kbps" => 320000,
                _ => 192000, // default bitrate if none is selected
            };
        }

        private void LoadMedia(string path)
        {
            videoDisplay.Source = new Uri(path);
            videoDisplay.Play();
            // Update the seekSlider maximum value based on media duration
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if there is a selected item and it is not the last item
            if (filesListBox.SelectedItem != null && filesListBox.SelectedIndex < filesListBox.Items.Count - 1)
            {
                // Select the next item
                filesListBox.SelectedIndex++;

                // Play the selected item
                PlaySelectedMedia();
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (filesListBox.SelectedItem == null) return;

            string selectedFile = filesListBox.SelectedItem.ToString();
            Uri fileUri = new Uri(selectedFile, UriKind.RelativeOrAbsolute);

            // Check if a different track is selected or if the media is not currently loaded
            if (videoDisplay.Source == null || videoDisplay.Source != fileUri)
            {
                // Load and play the new track
                videoDisplay.Source = fileUri;
                videoDisplay.Play();
                isMediaPlaying = true;
                playPauseButton.Content = "Pause";
                _timer.Start(); // Start the timer to update the slider
            }
            else
            {
                // If the same track is selected, toggle play/pause
                if (isMediaPlaying)
                {
                    videoDisplay.Pause();
                    isMediaPlaying = false;
                    playPauseButton.Content = "Play";
                    _timer.Stop(); // Stop updating the slider
                }
                else
                {
                    videoDisplay.Play();
                    isMediaPlaying = true;
                    playPauseButton.Content = "Pause";
                    _timer.Start(); // Continue updating the slider
                }
            }
        }

        private void PlaySelectedMedia()
        {
            if (filesListBox.SelectedItem == null) return;

            string selectedFile = filesListBox.SelectedItem.ToString();
            videoDisplay.Source = new Uri(selectedFile, UriKind.RelativeOrAbsolute);
            videoDisplay.Play();
            isMediaPlaying = true;
            playPauseButton.Content = "Pause";
            _timer.Start();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if there is a selected item and it is not the first item
            if (filesListBox.SelectedItem != null && filesListBox.SelectedIndex > 0)
            {
                // Select the previous item
                filesListBox.SelectedIndex--;

                // Play the selected item
                PlaySelectedMedia();
            }
        }

        private void SeekSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isDragging = false;
            videoDisplay.Position = TimeSpan.FromSeconds(seekSlider.Value);
        }

        private void SeekSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isDragging = true;
        }

        private void SeekSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var slider = sender as Slider;
                if (slider != null)
                {
                    Point position = e.GetPosition(slider);
                    double dValue = slider.Minimum + (slider.Maximum - slider.Minimum) * (position.X / slider.ActualWidth);

                    // Check if the media is loaded
                    if (videoDisplay.NaturalDuration.HasTimeSpan)
                    {
                        // Set the position of the media to the clicked position
                        videoDisplay.Position = TimeSpan.FromSeconds(dValue);

                        // Update the slider value
                        slider.Value = dValue;
                    }
                }
            }
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (mediaPlayer != null && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(seekSlider.Value);
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string[] files = Directory.GetFiles(folderDialog.SelectedPath, "*.mp4");
                foreach (string file in files)
                {
                    filesListBox.Items.Add(file);
                }
                UpdateConvertButtonState();
            }
        }

        private void SelectMp4File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP4 files (*.mp4)|*.mp4",
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    filesListBox.Items.Add(filename);
                }
                UpdateConvertButtonState();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (videoDisplay.NaturalDuration.HasTimeSpan && !_isDragging)
            {
                seekSlider.Maximum = videoDisplay.NaturalDuration.TimeSpan.TotalSeconds;
                seekSlider.Value = videoDisplay.Position.TotalSeconds;
            }
        }

        // This method should only handle the logic to toggle fullscreen
        // Method to toggle fullscreen mode
        private void ToggleFullscreen()
        {
            // Check if fullscreen video window is not created or not loaded
            if (_fullscreenVideoWindow == null || !_fullscreenVideoWindow.IsLoaded)
            {
                // Create a new instance of FullscreenVideoWindow
                _fullscreenVideoWindow = new FullscreenVideoWindow();

                // Attach the Closed event handler
                _fullscreenVideoWindow.Closed += (sender, e) =>
                {
                    // Set the position of the main window video to the position of the fullscreen video
                    videoDisplay.Position = _fullscreenVideoWindow.fullscreenMediaElement.Position;
                    // Set the source of the fullscreen video to null
                    _fullscreenVideoWindow.fullscreenMediaElement.Source = null;
                    // Set the fullscreen video window to null
                    _fullscreenVideoWindow = null;
                    // Resume playing the main window video
                    videoDisplay.Play();
                };

                // Show the fullscreen video window
                _fullscreenVideoWindow.Show();
                // Set the source of the fullscreen video to the source of the main window video
                _fullscreenVideoWindow.fullscreenMediaElement.Source = videoDisplay.Source;
                // Set the position of the fullscreen video to the position of the main window video
                _fullscreenVideoWindow.fullscreenMediaElement.Position = videoDisplay.Position;
                // Pause the main window video
                videoDisplay.Pause();
                // Start playing the fullscreen video
                _fullscreenVideoWindow.fullscreenMediaElement.Play();
            }
            else
            {
                // Set the position of the main window video to the position of the fullscreen video
                videoDisplay.Position = _fullscreenVideoWindow.fullscreenMediaElement.Position;
                // Stop playing the fullscreen video
                _fullscreenVideoWindow.fullscreenMediaElement.Stop();
                // Close the fullscreen video window
                _fullscreenVideoWindow.Close();
                // Resume playing the main window video
                videoDisplay.Play();
            }
        }

        // Assuming you have a transparent button overlaying the MediaElement in your XAML
        private void TransparentButtonForVideo_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Check if it's a double click

            ToggleFullscreen();
        }

        private void UpdateConvertButtonState()
        {
            convertButton.IsEnabled = filesListBox.Items.Count > 0;
        }

        // Don't forget to handle the MediaEnded event to reset isMediaPlaying
        private void UpdateStatusBar(string message)
        {
            statusBar.Items[0] = message;
        }

        private void VideoDisplay_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                ToggleFullscreen();
            }
        }

        #endregion Private Methods
    }
}