﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Emgu.CV;
using MessageBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.Operations;
using UVtools.WPF.Controls;
using UVtools.WPF.Controls.Calibrators;
using UVtools.WPF.Controls.Tools;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using UVtools.WPF.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Helpers = UVtools.WPF.Controls.Helpers;
using Path = System.IO.Path;
using Point = Avalonia.Point;

namespace UVtools.WPF
{
    public partial class MainWindow : WindowEx
    {
        #region Redirects

        public AppVersionChecker VersionChecker => App.VersionChecker;
        public static ClipboardManager Clipboard => ClipboardManager.Instance;
        #endregion

        #region Controls

        public ProgressWindow ProgressWindow = new ();

        public static MenuItem[] MenuTools { get; } =
        {
            new()
            {
                Tag = new OperationEditParameters(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/wrench-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRepairLayers(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/toolbox-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationMove(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/move-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationResize(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/crop-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationFlip(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/flip-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRotate(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/sync-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationSolidify(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/square-solid-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationMorph(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/geometry-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRaftRelief(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/cookie-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationRedrawModel(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/code-branch-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationThreshold(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/th-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationArithmetic(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/square-root-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationMask(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/mask-16x16.png"))
                    }
            },
            new()
            {
                Tag = new OperationPixelDimming(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pixel-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationInfill(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/stroopwafel-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationBlur(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/blur-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationPattern(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/pattern-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationDynamicLayerHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/dynamic-layers-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerReHeight(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/ladder-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationChangeResolution(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/resize-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalculator(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/calculator-16x16.png"))
                }
            },
        };

        public static MenuItem[] MenuCalibration { get; } =
        {
            new()
            {
                Tag = new OperationCalibrateExposureFinder(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/sun-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateElephantFoot(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/elephant-foot-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateXYZAccuracy(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/cubes-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateTolerance(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/dot-circle-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateGrayscale(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/chart-pie-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateStressTower(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/chess-rook-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationCalibrateExternalTests(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/bookmark-16x16.png"))
                }
            },
        };

        public static MenuItem[] LayerActionsMenu { get; } =
        {
            new()
            {
                Tag = new OperationLayerImport(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/file-import-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerClone(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/copy-16x16.png"))
                }
            },
            new()
            {
                Tag = new OperationLayerRemove(),
                Icon = new Avalonia.Controls.Image
                {
                    Source = new Bitmap(App.GetAsset("/Assets/Icons/trash-16x16.png"))
                }
            },
        };

        #region DataSets

        

        #endregion

        #endregion

        #region Members

        public Stopwatch LastStopWatch = new Stopwatch();
        
        private bool _isGUIEnabled = true;
        private uint _savesCount;
        private bool _canSave;
        private MenuItem[] _menuFileConvertItems;

        private PointerEventArgs _globalPointerEventArgs;
        private PointerPoint _globalPointerPoint;
        private KeyModifiers _globalModifiers = KeyModifiers.None;
        private TabItem _selectedTabItem;
        private TabItem _lastSelectedTabItem;

        #endregion

        #region  GUI Models
        public bool IsGUIEnabled
        {
            get => _isGUIEnabled;
            set
            {
                if (!RaiseAndSetIfChanged(ref _isGUIEnabled, value)) return;
                if (!_isGUIEnabled)
                {
                    ProgressWindow = new ProgressWindow();
                    return;
                }
                //if (ProgressWindow is null) return;

                LastStopWatch = ProgressWindow.StopWatch;
                ProgressWindow.Close();
                ProgressWindow.Dispose();
                /*if (Dispatcher.UIThread.CheckAccess())
                {
                    ProgressWindow.Close();
                    ProgressWindow.Dispose();
                }
                else
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ProgressWindow.Close();
                        ProgressWindow.Dispose();
                    });
                }*/
            }
        }

        public bool IsFileLoaded
        {
            get => SlicerFile is not null;
            set => RaisePropertyChanged();
        }

        public TabItem TabInformation { get; }
        public TabItem TabGCode { get; }
        public TabItem TabIssues { get; }
        public TabItem TabPixelEditor { get; }
        public TabItem TabLog { get; }

        public TabItem SelectedTabItem
        {
            get => _selectedTabItem;
            set
            {
                var lastTab = _selectedTabItem;
                if (!RaiseAndSetIfChanged(ref _selectedTabItem, value)) return;
                LastSelectedTabItem = lastTab;
                if (_firstTimeOnIssues)
                {
                    _firstTimeOnIssues = false;
                    if (ReferenceEquals(_selectedTabItem, TabIssues) && Settings.Issues.ComputeIssuesOnClickTab)
                    {
                        OnClickDetectIssues();
                    }
                }
            }
        }

        public TabItem LastSelectedTabItem
        {
            get => _lastSelectedTabItem;
            set => RaiseAndSetIfChanged(ref _lastSelectedTabItem, value);
        }

        #endregion

        public uint SavesCount
        {
            get => _savesCount;
            set => RaiseAndSetIfChanged(ref _savesCount, value);
        }

        public bool CanSave
        {
            get => IsFileLoaded && _canSave;
            set => RaiseAndSetIfChanged(ref _canSave, value);
        }

        public MenuItem[] MenuFileConvertItems
        {
            get => _menuFileConvertItems;
            set => RaiseAndSetIfChanged(ref _menuFileConvertItems, value);
        }


        #region Constructors

        public MainWindow()
        {
            InitializeComponent();

            App.ThemeSelector?.EnableThemes(this);
            InitInformation();
            InitIssues();
            InitPixelEditor();
            InitClipboardLayers();
            InitLayerPreview();


            TabInformation = this.FindControl<TabItem>("TabInformation");
            TabGCode = this.FindControl<TabItem>("TabGCode");
            TabIssues = this.FindControl<TabItem>("TabIssues");
            TabPixelEditor = this.FindControl<TabItem>("TabPixelEditor");
            TabLog = this.FindControl<TabItem>("TabLog");


            foreach (var menuItem in new[] { MenuTools, MenuCalibration, LayerActionsMenu })
            {
                foreach (var menuTool in menuItem)
                {
                    if (menuTool.Tag is not Operation operation) continue;
                    menuTool.Header = operation.Title;
                    menuTool.Click += async (sender, args) => await ShowRunOperation(operation.GetType());
                }
            }


            /*LayerSlider.PropertyChanged += (sender, args) =>
            {
                Debug.WriteLine(args.Property.Name);
                if (args.Property.Name == nameof(LayerSlider.Value))
                {
                    LayerNavigationTooltipPanel.Margin = LayerNavigationTooltipMargin;
                    return;
                }
            };*/
            //PropertyChanged += OnPropertyChanged;

            UpdateTitle();

            var clientSizeObs = this.GetObservable(ClientSizeProperty);
            clientSizeObs.Subscribe(size => UpdateLayerTrackerHighlightIssues());
            var windowStateObs = this.GetObservable(WindowStateProperty);
            windowStateObs.Subscribe(size => UpdateLayerTrackerHighlightIssues());


            DataContext = this;

            AddHandler(DragDrop.DropEvent, (sender, e) =>
            {
                ProcessFiles(e.Data.GetFileNames().ToArray());
            });
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            var windowSize = this.GetScreenWorkingArea();

            if (Settings.General.StartMaximized
                || ClientSize.Width > windowSize.Width
                || ClientSize.Height > windowSize.Height)
            {
                WindowState = WindowState.Maximized;
            }

            AddLog($"{About.Software} start", Program.ProgramStartupTime.Elapsed.TotalSeconds);

            if (Settings.General.CheckForUpdatesOnStartup)
            {
                Task.Factory.StartNew(VersionChecker.Check);
            }

            ProcessFiles(Program.Args);
            if (!IsFileLoaded && Settings.General.LoadDemoFileOnStartup)
            {
                ProcessFile(About.DemoFile);
            }

            DispatcherTimer.Run(() =>
            {
                UpdateTitle();
                return true;
            }, TimeSpan.FromSeconds(1));
            Program.ProgramStartupTime.Stop();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.PropertyName);
            /*if (e.PropertyName == nameof(ActualLayer))
            {
                LayerSlider.Value = ActualLayer;
                ShowLayer();
                return;
            }*/
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        #endregion

        #region Overrides


        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            _globalPointerEventArgs = e;
            _globalModifiers = e.KeyModifiers;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            _globalPointerPoint = e.GetCurrentPoint(this);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _globalPointerPoint = e.GetCurrentPoint(this);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _globalModifiers = e.KeyModifiers;
            if (e.Handled
                || !IsFileLoaded
                || LayerImageBox.IsPanning
                || !(LayerImageBox.TrackerImage is null)
                || LayerImageBox.Cursor == StaticControls.CrossCursor
                || LayerImageBox.Cursor == StaticControls.HandCursor
                || LayerImageBox.SelectionMode == AdvancedImageBox.SelectionModes.Rectangle
                ) return;

            var imageBoxMousePosition = _globalPointerEventArgs?.GetPosition(LayerImageBox) ?? new Point(-1, -1);
            if (imageBoxMousePosition.X < 0 || imageBoxMousePosition.Y < 0) return;

            // Pixel Edit is active, Shift is down, and the cursor is over the image region.
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                if (IsPixelEditorActive)
                {
                    LayerImageBox.AutoPan = false;
                    LayerImageBox.Cursor = StaticControls.CrossCursor;
                    TooltipOverlayText = "Pixel editing is on (SHIFT):\n" +
                                                      "» Click over a pixel to draw\n" +
                                                      "» Hold CTRL to clear pixels";
                    UpdatePixelEditorCursor();
                }
                else
                {
                    LayerImageBox.Cursor = StaticControls.CrossCursor;
                    LayerImageBox.SelectionMode = AdvancedImageBox.SelectionModes.Rectangle;
                    TooltipOverlayText = "ROI & Mask selection mode (SHIFT):\n" +
                                         "» Left-click drag to select a fixed ROI region\n" +
                                         "» Left-click + ALT drag to select specific objects ROI\n" +
                                         "» Right-click on a specific object to select it ROI\n" +
                                         "» Right-click + ALT on a specific object to select it as a Mask\n" +
                                         "Press Esc to clear the ROI & Masks";
                }

                IsTooltipOverlayVisible = Settings.LayerPreview.TooltipOverlay;
                e.Handled = true;
                return;
            }

            if (e.KeyModifiers == KeyModifiers.Control)
            {
                LayerImageBox.Cursor = StaticControls.HandCursor;
                LayerImageBox.AutoPan = false;
                TooltipOverlayText = "Issue selection mode:\n" +
                                     "» Click over an issue to select it";

                IsTooltipOverlayVisible = Settings.LayerPreview.TooltipOverlay;
                e.Handled = true;
                return;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            _globalModifiers = e.KeyModifiers;
            if ((e.Key == Key.LeftShift ||
                 e.Key == Key.RightShift ||
                (e.KeyModifiers & KeyModifiers.Shift) != 0) &&
                 (e.KeyModifiers & KeyModifiers.Control) != 0 &&
                 e.Key == Key.Z)
            {
                e.Handled = true;
                ClipboardUndo(true);
                return;
            }
                
            if (e.Key == Key.LeftShift ||
                e.Key == Key.RightShift ||
                (e.KeyModifiers & KeyModifiers.Shift) == 0 ||
                (e.KeyModifiers & KeyModifiers.Control) == 0)
            {
                LayerImageBox.TrackerImage = null;
                LayerImageBox.Cursor = StaticControls.ArrowCursor;
                LayerImageBox.AutoPan = true;
                LayerImageBox.SelectionMode = AdvancedImageBox.SelectionModes.None;
                IsTooltipOverlayVisible = false;
                e.Handled = true;
            }
        }
        
        public void OpenContextMenu(string name)
        {
            var menu = this.FindControl<ContextMenu>($"{name}ContextMenu");
            if (menu is null) return;
            var parent = this.FindControl<Button>($"{name}Button");
            if (parent is null) return;
            menu.Open(parent);
        }



        #endregion

        #region Events

        public void MenuFileOpenClicked() => OpenFile();
        public void MenuFileOpenNewWindowClicked() => OpenFile(true);

        public async void MenuFileSaveClicked()
        {
            if (!CanSave) return;
            await SaveFile();
        }

        public async void MenuFileSaveAsClicked()
        {
            //await this.MessageBoxInfo(Path.Combine(App.ApplicationPath, "Assets", "Themes"));
            if (!IsFileLoaded) return;
            var filename = FileFormat.GetFileNameStripExtensions(SlicerFile.FileFullPath, out var ext);
            //var ext = Path.GetExtension(SlicerFile.FileFullPath);
            //var extNoDot = ext.Remove(0, 1);
            var extension = FileExtension.Find(ext);
            if (extension is null)
            {
                await this.MessageBoxError("Unable to find the target extension.", "Invalid extension");
                return;
            }
            SaveFileDialog dialog = new()
            {
                DefaultExtension = extension.Extension,
                Filters = new List<FileDialogFilter>
                {
                    new()
                    {
                        Name = extension.Description,
                        Extensions = new List<string>
                        {
                            ext
                        }
                    }
                },
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectorySaveFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectorySaveFile,
                InitialFileName = $"{Settings.General.FileSaveNamePrefix}{filename}{Settings.General.FileSaveNameSuffix}"
            };
            var file = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(file)) return;
            await SaveFile(file);
        }

        public async void OpenFile(bool newWindow = false)
        {
            var filters = Helpers.ToAvaloniaFileFilter(FileFormat.AllFileFiltersAvalonia);
            var orderedFilters = new List<FileDialogFilter> {filters[Settings.General.DefaultOpenFileExtensionIndex]};
            for (int i = 0; i < filters.Count; i++)
            {
                if(i == Settings.General.DefaultOpenFileExtensionIndex) continue;
                orderedFilters.Add(filters[i]);
            }

            var dialog = new OpenFileDialog
            {
                AllowMultiple = true,
                Filters = orderedFilters,
                Directory = Settings.General.DefaultDirectoryOpenFile
            };
            var files = await dialog.ShowAsync(this);
            ProcessFiles(files, newWindow);
        }

        public async void OnMenuFileCloseFile()
        {
            if (CanSave && await this.MessageBoxQuestion("There are unsaved changes. Do you want close this file without saving?") !=
                ButtonResult.Yes)
            {
                return;
            }

            CloseFile();
        }

        public void CloseFile()
        {
            if (SlicerFile is null) return;

            MenuFileConvertItems = null;

            ClipboardManager.Instance.Reset();

            SlicerFile?.Dispose();
            SlicerFile = null;

            SlicerProperties.Clear();
            Issues.Clear();
            IgnoredIssues.Clear();
            _issuesSliderCanvas.Children.Clear();
            Drawings.Clear();

            SelectedTabItem = TabInformation;
            _firstTimeOnIssues = true;
            IsPixelEditorActive = false;
            CanSave = false;

            _actualLayer = 0;
            LayerCache.Clear();

            VisibleThumbnailIndex = 0;

            LayerImageBox.Image = null;
            LayerPixelPicker.Reset();

            ClearROIAndMask();

            ResetDataContext();
        }

        public void OnMenuFileFullscreen()
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Maximized : WindowState.FullScreen;
        }

        public async void MenuFileSettingsClicked()
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);
        }

        public void OpenWebsite()
        {
            App.OpenBrowser(About.Website);
        }

        public void OpenDonateWebsite()
        {
            App.OpenBrowser(About.Donate);
        }

        public async void MenuHelpAboutClicked()
        {
            await new AboutWindow().ShowDialog(this);
        }

        public async void MenuHelpBenchmarkClicked()
        {
            await new BenchmarkWindow().ShowDialog(this);
        }

        public void MenuHelpOpenSettingsFolderClicked()
        {
            App.StartProcess(UserSettings.SettingsFolder);
        }

        private async void MenuHelpMaterialManagerClicked()
        {
            await new MaterialManagerWindow().ShowDialog(this);
        }

        public async void MenuHelpInstallProfilesClicked()
        {
            var PEFolder = App.GetPrusaSlicerDirectory();
            if (string.IsNullOrEmpty(PEFolder) || !Directory.Exists(PEFolder))
            {
                if(await this.MessageBoxQuestion(
                    "Unable to detect PrusaSlicer on your system, please ensure you have latest version installed.\n" +
                    $"Was looking on: {PEFolder}\n\n" +
                    "Click 'Yes' to open the PrusaSlicer webpage for download\n" +
                    "Click 'No' to dismiss",
                    "Unable to detect PrusaSlicer") == ButtonResult.Yes) App.OpenBrowser("https://www.prusa3d.com/prusaslicer/");
                return;
            }
            await new PrusaSlicerManagerWindow().ShowDialog(this);
        }

        public async void MenuNewVersionClicked()
        {
            var result =
                await this.MessageBoxQuestion(
                    $"Do you like to auto-update {About.Software} v{App.VersionStr} to v{VersionChecker.Version}?\n" +
                    "Yes: Auto update\n" +
                    "No:  Manual update\n" +
                    "Cancel: No action\n\n" +
                    "Changelog:\n" +
                    $"{VersionChecker.Changelog}", $"Update UVtools to v{VersionChecker.Version}?", ButtonEnum.YesNoCancel);


            if (result == ButtonResult.No || OperatingSystem.IsMacOS())
            {
                App.OpenBrowser(VersionChecker.UrlLatestRelease);
                return;
            }
            if (result == ButtonResult.Yes)
            {
                IsGUIEnabled = false;

                var task = await Task.Factory.StartNew(async () =>
                {
                    ShowProgressWindow($"Downloading: {VersionChecker.Filename}");
                    try
                    {
                        VersionChecker.AutoUpgrade(ProgressWindow.RestartProgress(false));
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception exception)
                    {
                        Dispatcher.UIThread.InvokeAsync(async () =>
                            await this.MessageBoxError(exception.ToString(), "Error opening the file"));
                    }

                    return false;
                });

                IsGUIEnabled = true;
                
                return;
            }
            
        } 

        #endregion

        #region Methods

        private void UpdateTitle()
        {
            Title = SlicerFile is null
                    ? $"{About.Software}   Version: {App.VersionStr}"
                    : $"{About.Software}   File: {Path.GetFileName(SlicerFile.FileFullPath)} ({Math.Round(LastStopWatch.ElapsedMilliseconds / 1000m, 2)}s)   Version: {App.VersionStr}"
                    ;

            Title += $"   RAM: {SizeExtensions.SizeSuffix(Environment.WorkingSet)}";

#if DEBUG
            Title += "   [DEBUG]";
#endif
        }

        public void ProcessFiles(string[] files, bool openNewWindow = false)
        {
            if (files is null || files.Length == 0) return;

            for (int i = 0; i < files.Length; i++)
            {
                if (i == 0 && !openNewWindow && (_globalModifiers & KeyModifiers.Shift) == 0)
                {
                    ProcessFile(files[i]);
                    continue;
                }

                App.NewInstance(files[i]);

            }
        }

        void ReloadFile() => ReloadFile(_actualLayer);

        void ReloadFile(uint actualLayer)
        {
            if (App.SlicerFile is null) return;
            ProcessFile(SlicerFile.FileFullPath, _actualLayer);
        }

        async void ProcessFile(string fileName, uint actualLayer = 0)
        {
            if (!File.Exists(fileName)) return;
            CloseFile();
            var fileNameOnly = Path.GetFileName(fileName);
            SlicerFile = FileFormat.FindByExtension(fileName, true, true);
            if (SlicerFile is null) return;

            IsGUIEnabled = false;
            
            var task = await Task.Factory.StartNew( () =>
            {
                ShowProgressWindow($"Opening: {fileNameOnly}");
                try
                {
                    SlicerFile.Decode(fileName, ProgressWindow.RestartProgress());
                    return true;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(exception.ToString(), "Error opening the file"));
                }

                return false;
            });

            IsGUIEnabled = true;

            if (!task)
            {
                SlicerFile.Dispose();
                SlicerFile = null;
                return;
            }

            if (SlicerFile.LayerCount == 0)
            {
                await this.MessageBoxError("It seems this file has no layers.  Possible causes could be:\n" +
                                "- File is empty\n" +
                                "- File is corrupted\n" +
                                "- File has not been sliced\n" +
                                "- An internal programing error\n\n" +
                                "Please check your file and retry", "Error reading file");
                SlicerFile.Dispose();
                SlicerFile = null;
                return;
            }

            if (SlicerFile is SL1File sl1File && Settings.Automations.AutoConvertSL1Files)
            {
                string fileExtension = sl1File.LookupCustomValue<string>(SL1File.Keyword_FileFormat, null);
                if (!string.IsNullOrWhiteSpace(fileExtension))
                {
                    fileExtension = fileExtension.ToLower(CultureInfo.InvariantCulture);
                    var convertToFormat = FileFormat.FindByExtension(fileExtension);
                    if (convertToFormat is not null)
                    {
                        var directory = Path.GetDirectoryName(sl1File.FileFullPath);
                        var filename = FileFormat.GetFileNameStripExtensions(sl1File.FileFullPath);
                        FileFormat convertedFile = null;

                        IsGUIEnabled = false;

                        task = await Task.Factory.StartNew(() =>
                        {
                            ShowProgressWindow($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {fileExtension}");
                            try
                            {
                                convertedFile = sl1File.Convert(convertToFormat, Path.Combine(directory, $"{filename}.{fileExtension}"), ProgressWindow.RestartProgress());
                                return true;
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            catch (Exception exception)
                            {
                                Dispatcher.UIThread.InvokeAsync(async () =>
                                    await this.MessageBoxError(exception.ToString(), "Error while converting the file"));
                            }

                            return false;
                        });

                        IsGUIEnabled = true;

                        if (task && convertedFile is not null)
                        {
                            SlicerFile = convertedFile;
                        }
                    }
                }
            }

            bool modified = false;
            if (
                Settings.Automations.BottomLightOffDelay > 0 &&
                SlicerFile.PrintParameterModifiers is not null &&
                SlicerFile.PrintParameterModifiers.Contains(FileFormat.PrintParameterModifier.BottomLightOffDelay) &&
                (!Settings.Automations.ChangeOnlyLightOffDelayIfZero || Settings.Automations.ChangeOnlyLightOffDelayIfZero && SlicerFile.BottomLightOffDelay <= 0)
                )
            {
                var lightOff = OperationCalculator.LightOffDelayC.CalculateSeconds(SlicerFile.BottomLiftHeight,
                    SlicerFile.BottomLiftSpeed, SlicerFile.RetractSpeed, (float)Settings.Automations.BottomLightOffDelay);
                if (lightOff != SlicerFile.BottomLightOffDelay)
                {
                    modified = true;
                    SlicerFile.BottomLightOffDelay = lightOff;
                }
            }

            if (Settings.Automations.LightOffDelay > 0 &&
                SlicerFile.PrintParameterModifiers is not null &&
                SlicerFile.PrintParameterModifiers.Contains(FileFormat.PrintParameterModifier.LightOffDelay) &&
                (!Settings.Automations.ChangeOnlyLightOffDelayIfZero || Settings.Automations.ChangeOnlyLightOffDelayIfZero && SlicerFile.LightOffDelay <= 0))
            {
                var lightOff = OperationCalculator.LightOffDelayC.CalculateSeconds(SlicerFile.LiftHeight,
                    SlicerFile.LiftSpeed, SlicerFile.RetractSpeed, (float)Settings.Automations.LightOffDelay);
                if (lightOff != SlicerFile.LightOffDelay)
                {
                    modified = true;
                    SlicerFile.LightOffDelay = lightOff;
                }
            }

            if (modified)
            {
                CanSave = true;
                if (Settings.Automations.SaveFileAfterModifications)
                {
                    var saveCount = _savesCount;
                    await SaveFile(null, true);
                    _savesCount = saveCount;
                }
            }

            ClipboardManager.Instance.Init(SlicerFile);

            if (SlicerFile is not ImageFile)
            {
                List<MenuItem> menuItems = new();
                foreach (var fileFormat in FileFormat.AvailableFormats)
                {
                    if(fileFormat is ImageFile) continue;
                    foreach (var fileExtension in fileFormat.FileExtensions)
                    {
                        var menuItem = new MenuItem
                        {
                            Header = fileExtension.Description,
                            Tag = fileExtension
                        };

                        menuItem.Tapped += ConvertToOnTapped;

                        menuItems.Add(menuItem);
                    }
                    /*string extensions = fileFormat.FileExtensions.Length > 0
                        ? $" ({fileFormat.GetFileExtensions()})"
                        : string.Empty;

                    var menuItem = new MenuItem
                    {
                        Header = fileFormat.GetType().Name.Replace("File", extensions),
                        Tag = fileFormat
                    };

                    menuItem.Tapped += ConvertToOnTapped;

                    menuItems.Add(menuItem);*/
                }

                MenuFileConvertItems = menuItems.ToArray();
            }

            using Mat mat = SlicerFile[0].LayerMat;

            VisibleThumbnailIndex = 1;

            RefreshProperties();

            UpdateTitle();


            if (mat is not null && Settings.LayerPreview.AutoRotateLayerBestView)
            {
                _showLayerImageRotated = mat.Height > mat.Width;
            }

            if (SlicerFile.MirrorDisplay)
            {
                _showLayerImageFlipped = true;
            }

            ResetDataContext();

            ForceUpdateActualLayer(actualLayer.Clamp(actualLayer, SliderMaximumValue));

            if (Settings.LayerPreview.ZoomToFitPrintVolumeBounds)
            {
                ZoomToFit();
            }

            if (Settings.Issues.ComputeIssuesOnLoad)
            {
                _firstTimeOnIssues = false;
                await OnClickDetectIssues();
                if (Issues.Count > 0)
                {
                    SelectedTabItem = TabIssues;
                    if(Settings.Issues.AutoRepairIssuesOnLoad)
                        await RunOperation(ToolRepairLayersControl.GetOperationRepairLayers());
                }
            }
        }

        private async void ShowProgressWindow(string title)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ProgressWindow.SetTitle(title);
                await ProgressWindow.ShowDialog(this);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        ProgressWindow.SetTitle(title);
                        await ProgressWindow.ShowDialog(this);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                    
                });
            }
        }

        private void ShowProgressWindowSync(string title)
        {
            ProgressWindow = new ProgressWindow(title);
        }

        

        private async void ConvertToOnTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem item) return;
            if (item.Tag is not FileExtension fileExtension) return;

            SaveFileDialog dialog = new SaveFileDialog
            {
                InitialFileName = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath),
                Filters = Helpers.ToAvaloniaFilter(fileExtension.Description, fileExtension.Extension),
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectoryConvertFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectoryConvertFile
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;


            IsGUIEnabled = false;

            var task = await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow($"Converting {Path.GetFileName(SlicerFile.FileFullPath)} to {Path.GetExtension(result)}");
                try
                {
                    return SlicerFile.Convert(fileExtension.GetFileFormat(), result, ProgressWindow.RestartProgress()) is not null;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    string extraMessage = string.Empty;
                    if (SlicerFile.FileFullPath.EndsWith(".sl1"))
                    {
                        extraMessage = "Note: When converting from SL1 make sure you have the correct printer selected, you MUST use a UVtools base printer.\n" +
                                       "Go to \"Help\" -> \"Install profiles into PrusaSlicer\" to install printers.\n";
                    }

                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError($"Convertion was not successful! Maybe not implemented...\n{extraMessage}{ex}", "Convertion unsuccessful"));
                }
                
                return false;
            });

            IsGUIEnabled = true;
           
            if (task)
            {
                if (await this.MessageBoxQuestion(
                    $"Conversion completed in {LastStopWatch.ElapsedMilliseconds / 1000}s\n\n" +
                    $"Do you want to open {Path.GetFileName(result)} in a new window?",
                    "Conversion complete") == ButtonResult.Yes)
                {
                    App.NewInstance(result);
                }
            }
            else
            {
                try
                {
                    if (File.Exists(result))
                    {
                        File.Delete(result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }




        public async Task<bool> SaveFile(string filepath = null, bool ignoreOverwriteWarning = false)
        {
            if (filepath is null)
            {
                if (!ignoreOverwriteWarning && SavesCount == 0 && Settings.General.PromptOverwriteFileSave)
                {
                    var result = await this.MessageBoxQuestion(
                        "Original input file will be overwritten.  Do you wish to proceed?", "Overwrite file?");

                    if(result != ButtonResult.Yes) return false;
                }

                filepath = SlicerFile.FileFullPath;
            }

            var oldFile = SlicerFile.FileFullPath;
            var tempFile = filepath + FileFormat.TemporaryFileAppend;

            IsGUIEnabled = false;

            var task = await Task.Factory.StartNew( () =>
            {
                ShowProgressWindow($"Saving {Path.GetFileName(filepath)}");

                try
                {
                    SlicerFile.SaveAs(tempFile, ProgressWindow.RestartProgress());
                    if (File.Exists(filepath))
                    {
                        File.Delete(filepath);
                    }
                    File.Move(tempFile, filepath);
                    SlicerFile.FileFullPath = filepath;
                    return true;
                }
                catch (OperationCanceledException)
                {
                    SlicerFile.FileFullPath = oldFile;
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch (Exception ex)
                {
                    SlicerFile.FileFullPath = oldFile;
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Error while saving the file"));
                }

                return false;
            });

            IsGUIEnabled = true;

            if (task)
            {
                SavesCount++;
                CanSave = false;
                UpdateTitle();
            }

            return task;
        }

        public async void IPrintedThisFile()
        {
            await ShowRunOperation(typeof(OperationIPrintedThisFile));
        }

        public async void ExtractFile()
        {
            if (!IsFileLoaded) return;
            string fileNameNoExt = Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath);
            OpenFolderDialog dialog = new OpenFolderDialog
            {
                Directory = string.IsNullOrEmpty(Settings.General.DefaultDirectoryExtractFile)
                    ? Path.GetDirectoryName(SlicerFile.FileFullPath)
                    : Settings.General.DefaultDirectoryExtractFile,
                Title =
                    $"A \"{fileNameNoExt}\" folder will be created within your selected folder to dump the contents."
            };

            var result = await dialog.ShowAsync(this);
            if (string.IsNullOrEmpty(result)) return;

            string finalPath = Path.Combine(result, fileNameNoExt);

            IsGUIEnabled = false;

            await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow($"Extracting {Path.GetFileName(SlicerFile.FileFullPath)}");
                try
                {
                    SlicerFile.Extract(finalPath, true, true, ProgressWindow.RestartProgress());
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), "Error while try extracting the file"));
                }
            });
 

            IsGUIEnabled = true;


            if (await this.MessageBoxQuestion(
                $"Extraction to {finalPath} completed in ({LastStopWatch.ElapsedMilliseconds / 1000}s)\n\n" +
                "'Yes' to open target folder, 'No' to continue.",
                "Extraction complete") == ButtonResult.Yes)
            {
                App.StartProcess(finalPath);
            }

        }

        #region Operations
        public async Task<Operation> ShowRunOperation(Type type, Operation loadOperation = null)
        {
            var operation = await ShowOperation(type, loadOperation);
            await RunOperation(operation);
            return operation;
        }

        public async Task<Operation> ShowOperation(Type type, Operation loadOperation = null)
        {
            var toolTypeBase = typeof(ToolControl);
            var calibrateTypeBase = typeof(CalibrateElephantFootControl);
            var classname = type.Name.StartsWith("OperationCalibrate") ?
                $"{calibrateTypeBase.Namespace}.{type.Name.Remove(0, Operation.ClassNameLength)}Control" :
                $"{toolTypeBase.Namespace}.Tool{type.Name.Remove(0, Operation.ClassNameLength)}Control"; ;
            var controlType = Type.GetType(classname);
            ToolControl control;

            bool removeContent = false;
            if (controlType is null)
            {
                //controlType = toolTypeBase;
                removeContent = true;
                control = new ToolControl(type.CreateInstance<Operation>());
            }
            else
            {
                control = controlType.CreateInstance<ToolControl>();
                if (control is null) return null;
            }

            if(loadOperation is not null)
                control.BaseOperation = loadOperation;

            if (!control.CanRun)
            {
                return null;
            }

            if (removeContent)
            {
                control.IsVisible = false;
            }

            var window = new ToolWindow(control);
            //window.ShowDialog(this);
            await window.ShowDialog(this);
            if (window.DialogResult != DialogResults.OK) return null;
            var operation = control.BaseOperation;
            return operation;
        }

        public async Task<bool> RunOperation(Operation baseOperation)
        {
            if (baseOperation is null) return false;

            switch (baseOperation)
            {
                case OperationEditParameters operation:
                    operation.Execute();
                    RefreshProperties();
                    RefreshCurrentLayerData();
                    ResetDataContext();

                    CanSave = true;
                    return true;
                case OperationIPrintedThisFile operation:
                    operation.Execute();
                    return true;
                case OperationRepairLayers operation:
                    if (Issues is null)
                    {
                        var islandConfig = GetIslandDetectionConfiguration();
                        islandConfig.Enabled = operation.RepairIslands && operation.RemoveIslandsBelowEqualPixelCount > 0;
                        var overhangConfig = new OverhangDetectionConfiguration { Enabled = false };
                        var resinTrapConfig = GetResinTrapDetectionConfiguration();
                        resinTrapConfig.Enabled = operation.RepairResinTraps;
                        var touchingBoundConfig = new TouchingBoundDetectionConfiguration { Enabled = false };

                        if (islandConfig.Enabled || resinTrapConfig.Enabled)
                        {
                            ComputeIssues(islandConfig, overhangConfig, resinTrapConfig, touchingBoundConfig, Settings.Issues.ComputeEmptyLayers);
                        }
                    }

                    operation.Issues = Issues.ToList();
                    operation.IslandDetectionConfig = GetIslandDetectionConfiguration();
                    break;
            }

            IsGUIEnabled = false;

            LayerManager backup = null;
            var result = await Task.Factory.StartNew(() =>
            {
                ShowProgressWindow(baseOperation.ProgressTitle);
                backup = SlicerFile.LayerManager.Clone();

                try
                {
                    return baseOperation.Execute(ProgressWindow.RestartProgress(baseOperation.CanCancel));
                }
                catch (OperationCanceledException)
                {
                    SlicerFile.LayerManager = backup;
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        await this.MessageBoxError(ex.ToString(), $"{baseOperation.Title} Error"));
                }

                return false;
            });


            IsGUIEnabled = true;

            if (result)
            {
                ClipboardManager.Instance.Clip(baseOperation, backup);

                ShowLayer();
                RefreshProperties();
                ResetDataContext();

                CanSave = true;

                switch (baseOperation)
                {
                    // Tools
                    case OperationRepairLayers operation:
                        OnClickDetectIssues();
                        break;
                }
            }

            if (baseOperation.Tag is not null)
            {
                var message = baseOperation.Tag.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    //message += $"\nExecution time: ";
                    
                    await this.MessageBoxInfo(message, $"{baseOperation.Title} report ({LastStopWatch.Elapsed.Hours}h{LastStopWatch.Elapsed.Minutes}m{LastStopWatch.Elapsed.Seconds}s)");
                }
            }

            return result;
        }

        #endregion

        

        #endregion
        }
}
