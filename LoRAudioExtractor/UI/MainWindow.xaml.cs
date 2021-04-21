using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LoRAudioExtractor.Extractor;
using LoRAudioExtractor.Wwise;
using Microsoft.WindowsAPICodePack.Dialogs;
using MessageBox = System.Windows.MessageBox;

namespace LoRAudioExtractor.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string REPO_URL = "http://github.com/493msi/lor-vo-extractor";
        
        private readonly HashSet<ArchiveFile> openArchiveFiles;

        private DataGrid? _dataContainer;
        
        public MainWindow()
        {
            this.openArchiveFiles = new HashSet<ArchiveFile>();
            this.InitializeComponent();
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            using CommonOpenFileDialog dialog = new () {
                InitialDirectory = "C:\\Riot Games\\",
                Title = "Please select Legends of Runeterra's root directory",
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) 
                return;
            
            string fileName = dialog.FileName;

            this.ExportMenu.IsEnabled = false;
            
            foreach (var archiveFile in this.openArchiveFiles)
                archiveFile.Dispose();
                
            this.openArchiveFiles.Clear();
            this.MainAppPanel.Children.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            AppInitialBox initialBox = new ();
            LoadingBox loadingBox = new ();
            initialBox.ChildContainer.Child = loadingBox;
            this.MainAppPanel.Children.Add(initialBox);
            
            this.OpenDirMenuOption.IsEnabled = false;
            Task.Run(() => this.LoadItems(fileName));
        }

        private void LoadItems(string fileName)
        {
            try
            {
                var entries = LoRExtractor.OpenDirectory(fileName);

                this.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        AudioListView audioListView = new();
                        audioListView.ListViewTitle.Content = fileName;

                        foreach (var entry in entries)
                        {
                            this._dataContainer = audioListView.ListViewContainer;

                            this._dataContainer.Items.Add(entry);

                            if (entry?.ArchiveEntry != null)
                                this.openArchiveFiles.Add(entry.ArchiveEntry.Parent);
                        }

                        int itemCount = entries.Count;
                        audioListView.ListViewStatus.Content = $"{itemCount} items total";

                        this.MainAppPanel.Children.Clear();
                        this.MainAppPanel.Children.Add(audioListView);

                        this.ExportMenu.IsEnabled = true;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        MessageBox.Show(exception.Message, "Error");
                    }
                    finally
                    {
                        this.OpenDirMenuOption.IsEnabled = true;
                    }
                });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show(exception.Message, "Error");
            }
        }

        private void SourceCode_Click(object sender, RoutedEventArgs eventArgs)
        {
            try
            {
                Process.Start(new ProcessStartInfo {
                    FileName = REPO_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                MessageBox.Show(this, $"See {REPO_URL}.", "Repository URL");
            }
        }

        private void Licenses_Click(object sender, RoutedEventArgs e)
        {
            LicenseWindow licenseWindow = new ();
            licenseWindow.LicenseContent.Text = Properties.Resources.Licenses;
            licenseWindow.Owner = this;
            licenseWindow.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new ();
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            aboutWindow.VersionString.Content = $"Version {versionInfo.FileVersion}";
            aboutWindow.Owner = this;
            aboutWindow.ShowDialog();
        }

        private void ExtractSelected_Click(object sender, RoutedEventArgs e)
        {
            if (this._dataContainer == null)
                return;

            var selected = this._dataContainer.SelectedItems;

            if (selected.Count == 0)
                MessageBox.Show(this, "No items are selected.", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            
            this.ExportMenu.IsEnabled = false;
            this.OpenDirMenuOption.IsEnabled = false;
            
            this.ExtractItems(selected);
            
            this.ExportMenu.IsEnabled = true;
            this.OpenDirMenuOption.IsEnabled = true;
        }

        private void ExtractAll_Click(object sender, RoutedEventArgs e)
        {
            if (this._dataContainer == null)
                return;
            
            this.ExtractItems(this._dataContainer.Items);
            
        }

        private void ExtractItems(IList items)
        {
            using CommonOpenFileDialog dialog = new () {
                Title = "Please select a directory to output the files",
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) 
                return;

            string targetDir = dialog.FileName;
            
            if (!Directory.Exists(targetDir))
            {
                MessageBox.Show(this, $"Could not open {targetDir}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ExportProgressWindow progressWindow = new ();
            progressWindow.ExtractedCount.Text = $"0 / {items.Count}(0.00%)";
            progressWindow.ExtractProgress.Minimum = 0;
            progressWindow.ExtractProgress.Maximum = items.Count;
            progressWindow.ExtractProgress.Value = 0;
            progressWindow.Owner = this;

            var ctSource = new CancellationTokenSource();
            var ct = ctSource.Token;

            void CancelDelegate(object? sender, EventArgs args)
            {
                ctSource.Cancel();
            }

            Task.Run(() =>
            {
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.ExportMenu.IsEnabled = false;
                        this.OpenDirMenuOption.IsEnabled = false;
                    });

                    int count = -1;
                    
                    foreach (var selectedItem in items)
                    {                
                        if (ct.IsCancellationRequested)
                        {
                            ct.ThrowIfCancellationRequested();
                        }
                        
                        count++;

                        int countVal = count;
                        
                        if (selectedItem is not ExtractableItem item)
                            continue;
                        
                        this.Dispatcher.Invoke(() =>
                        {
                            progressWindow.CurrentExtracted.Text = $"Extracting... {item.Name}";
                            progressWindow.ExtractedCount.Text = $"{countVal} / {items.Count} ({(countVal / (double) items.Count * 100):F2}%)";
                            progressWindow.ExtractProgress.Value = countVal;
                        });

                        if (item.ArchiveEntry == null)
                            continue;
                        
                        byte[] data = item.ArchiveEntry.ExtractAndRead();
                        string outPath = Path.Join(targetDir, item.Name);

                        if (outPath.EndsWith(".wem"))
                            outPath = Regex.Replace(outPath, @"\.wem$", ".ogg");
                        else
                            outPath += ".ogg";
                        
                        string outDir = Path.GetDirectoryName(outPath)!;
                        Directory.CreateDirectory(outDir);
                        File.WriteAllBytes(outPath, data);
                    }
                }
                catch (OperationCanceledException)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(this, "Extraction cancelled.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    
                    this.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(this, "An error has ocurred while exporting files.\n" + 
                                              "Please make sure you have write access to the target directory.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                finally
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        progressWindow.Closed -= CancelDelegate;
                        progressWindow.Close();
                        this.ExportMenu.IsEnabled = true;
                        this.OpenDirMenuOption.IsEnabled = true;
                    });
                }
            }, ct);

            progressWindow.Closed += CancelDelegate;
            progressWindow.ShowDialog();
        }
    }
}