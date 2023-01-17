using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using HtmlAgilityPack;
using Microsoft.Win32;

namespace TestTask
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private string filePath;
        private int maximumCount = 0;
        private string maximumUrl = "";

        public MainWindow()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";

            //Shows the open file dialog and gets the filepath when the user selects a file
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                statusBarText.Text = "File selected: " + filePath;
            }
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            //Checks if the filepath is not null or empty
            if (string.IsNullOrEmpty(filePath))
            {
                statusBarText.Text = "Please select a file first.";
                return;
            }

            //Resets the CancellationTokenSource
            cancellationTokenSource = new CancellationTokenSource();

            //Disables the "Open File" and "Start" buttons and enables the "Cancel" button
            btnOpenFile.IsEnabled = false;
            btnStart.IsEnabled = false;
            btnCancel.IsEnabled = true;
            progressBar.Visibility = Visibility.Visible;
            statusBarText.Text = "Processing...";

            try
            {
                var urls = File.ReadAllLines(filePath);
                int totalCount = 0;
                progressBar.Maximum = urls.Length;
                progressBar.Value = 0;
                List<string> urlsWithCounter = new List<string>();

                foreach (string url in urls)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    if (!string.IsNullOrEmpty(url))
                    {
                        var web = new HtmlWeb();
                        var doc = await web.LoadFromWebAsync(url, cancellationTokenSource.Token);
                        int tagCount = 0;
                        var nodes = doc.DocumentNode.SelectNodes("//a");
                        if (nodes != null)
                            tagCount = nodes.Count;
                        totalCount += tagCount;

                        urlsWithCounter.Add($"{(url.Length <= 30 ? url : (url.Substring(0, 30) + "..."))} - {tagCount} tags");

                        //Checks if the current URL has the maximum number of tags found so far
                        //Updates the maximumCount and maximumUrl variables if the current URL has more tags than the previous maximum
                        if (tagCount > maximumCount)
                        {
                            maximumCount = tagCount;
                            maximumUrl = url;
                        }

                        //Sets the ToolTip and Text of the status bar to display the URL and number of tags found on the current page
                        statusBarText.ToolTip = url;
                        statusBarText.Text = $"Processing... {(url.Length <= 30 ? url : (url.Substring(0, 30) + "..."))} - {tagCount} tags";
                    }

                    //Increments the value of the progress bar
                    progressBar.Value += 1;
                }

                //Updates the status bar and shows a message box with the total number of tags found and the URL with the maximum number of tags
                statusBarText.Text = "Processing completed!";
                urlList.ItemsSource = urlsWithCounter;
                urlList.SelectedIndex = urlsWithCounter.IndexOf($"{(maximumUrl.Length <= 30 ? maximumUrl : (maximumUrl.Substring(0, 30) + "..."))} - {maximumCount} tags");
                MessageBox.Show($"Processing completed! \nTotal tags: {totalCount}. \nMaximum tags found at {maximumUrl} - {maximumCount} tags");
            }
            catch (OperationCanceledException)
            {
                statusBarText.Text = $"Processing Canceled!";
            }
            catch (Exception ex)
            {
                statusBarText.Text = $"Processing finished with an error:\n{ex.Message}";
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnOpenFile.IsEnabled = true;
                btnStart.IsEnabled = true;
                btnCancel.IsEnabled = false;
                progressBar.Visibility = Visibility.Hidden;
            }
        }

        //Event that is fired when the "Cancel" button is clicked
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }
    }
}