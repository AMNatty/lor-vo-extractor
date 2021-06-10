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
        private const string WW2OGG_URL = "https://github.com/Vextil/Wwise-Unpacker/";

        private bool OfferedWW2OGG { get; set; }

        private readonly HashSet<ArchiveFile> openArchiveFiles;

        private DataGrid? _dataContainer;
        
        public MainWindow()
        {
            this.openArchiveFiles = new HashSet<ArchiveFile>();
            this.InitializeComponent();
        }

        private void LoadVO_Click(object sender, RoutedEventArgs e)
        {
            this.Load(LoRExtractor.AudioType.VO);
        }
        
        private void LoadSFX_Click(object sender, RoutedEventArgs e)
        {
            this.Load(LoRExtractor.AudioType.SFX);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Load(LoRExtractor.AudioType audioType)
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
            
            this.LoadVOMenuOption.IsEnabled = false;
            this.LoadSFXMenuOption.IsEnabled = false;
            Task.Run(() => this.LoadItems(fileName, audioType));
        }

        private void LoadItems(string fileName, LoRExtractor.AudioType audioType)
        {
            try
            {
                var entries = LoRExtractor.OpenDirectory(fileName, audioType);

                this.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        AudioListView audioListView = new ();
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
                        this.LoadVOMenuOption.IsEnabled = true;
                        this.LoadSFXMenuOption.IsEnabled = true;
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
            this.LoadVOMenuOption.IsEnabled = false;
            this.LoadSFXMenuOption.IsEnabled = false;
            
            this.ExtractItems(selected);
            
            this.ExportMenu.IsEnabled = true;
            this.LoadVOMenuOption.IsEnabled = true;
            this.LoadSFXMenuOption.IsEnabled = true;
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
                        this.LoadVOMenuOption.IsEnabled = false;
                        this.LoadSFXMenuOption.IsEnabled = false;
                    });

                    int count = -1;

                    bool hadBanks = false;
                    
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

                        byte[] data = item.ArchiveEntry.ExtractAndRead(out bool isOgg);
                        string outPath = Path.Join(targetDir, item.Name);

                        if (isOgg)
                            outPath = Regex.Replace(outPath, @"\.wem$", ".ogg");
                        else
                            if (outPath.EndsWith(".wem"))
                                outPath = Regex.Replace(outPath, @"\.wem$", ".wav");

                        hadBanks |= !isOgg;
                                
                        string outDir = Path.GetDirectoryName(outPath)!;
                        Directory.CreateDirectory(outDir);
                        File.WriteAllBytes(outPath, data);
                    }

                    if (hadBanks && !this.OfferedWW2OGG)
                        this.Dispatcher.Invoke(() =>
                        {
                            var result = MessageBox.Show(this, "You extracted some files not processable by this extractor.\n" +
                                                  $"Consider using {WW2OGG_URL} for .WAV and .BNK files.\n" +
                                                  "Do you want to visit that page?",
                                "Information", MessageBoxButton.YesNo, MessageBoxImage.Information);

                            if (result == MessageBoxResult.Yes)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = WW2OGG_URL,
                                        UseShellExecute = true
                                    });
                                }
                                catch (Exception)
                                {
                                    MessageBox.Show(this, $"See {WW2OGG_URL}.", "Repository URL");
                                }
                            }

                            this.OfferedWW2OGG = true;
                        });
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
                        this.LoadVOMenuOption.IsEnabled = true;
                        this.LoadSFXMenuOption.IsEnabled = true;
                    });
                }
            }, ct);

            progressWindow.Closed += CancelDelegate;
            progressWindow.ShowDialog();
        }
    }
}