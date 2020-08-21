// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Autofac.Features.OwnedInstances;
using Caliburn.Micro;
using Dapplo.Windows.Desktop;
using Greenshot.Helpers;
using Dapplo.Log;
using Timer = System.Timers.Timer;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.App;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Dpi.Forms;
using Dapplo.Windows.Kernel32;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Enums;
using Greenshot.Addons.Extensions;
using Greenshot.Core.Enums;
using Greenshot.Gfx;
using Greenshot.Ui.Configuration.ViewModels;
using Message = System.Windows.Forms.Message;
using Screen = System.Windows.Forms.Screen;
using Dapplo.Windows.User32;
using Greenshot.Addons.Resources;
using Greenshot.Components;

namespace Greenshot.Forms
{
    /// <summary>
    ///     The MainForm provides the "shell" of the application
    /// </summary>
    public partial class MainForm : GreenshotForm
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly IWindowManager _windowManager;
        private readonly IGreenshotLanguage _greenshotLanguage;
        private readonly Func<Owned<ConfigViewModel>> _configViewModelFactory;
        private readonly Func<IBitmapWithNativeSupport, Bitmap> _valueConverter = bitmap => bitmap?.NativeBitmap;

        // Timer for the double click test
        private readonly Timer _doubleClickTimer = new Timer();

        private readonly DestinationHolder _destinationHolder;
        private readonly CaptureSupportInfo _captureSupportInfo;

        // Thumbnail preview
        private ThumbnailForm _thumbnailForm;

        public DpiHandler ContextMenuDpiHandler { get; private set; }

        public MainForm(ICoreConfiguration coreConfiguration,
            IWindowManager windowManager,
            IGreenshotLanguage greenshotLanguage,
            Func<Owned<ConfigViewModel>> configViewModelFactory,
            DestinationHolder destinationHolder,
            CaptureSupportInfo captureSupportInfo
            ) : base(greenshotLanguage)
        {
            _coreConfiguration = coreConfiguration;
            _windowManager = windowManager;
            _greenshotLanguage = greenshotLanguage;
            _configViewModelFactory = configViewModelFactory;
            _destinationHolder = destinationHolder;
            _captureSupportInfo = captureSupportInfo;
            Instance = this;
        }

        public void Initialize()
        {
            Log.Debug().WriteLine("Initializing MainForm.");
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            try
            {
                InitializeComponent();
                SetupBitmapScaleHandler();
            }
            catch (ArgumentException ex)
            {
                // Added for Bug #1420, this doesn't solve the issue but maybe the user can do something with it.
                ex.Data.Add("more information here", "http://support.microsoft.com/kb/943140");
                throw;
            }

            notifyIcon.Icon = GreenshotResources.Instance.GetGreenshotIcon();

            // Disable access to the settings, for feature #3521446
            contextmenu_settings.Visible = !_coreConfiguration.DisableSettings;

            UpdateUi();

            // Set the Greenshot icon visibility depending on the configuration. (Added for feature #3521446)
            // Setting it to true this late prevents Problems with the context menu
            notifyIcon.Visible = !_coreConfiguration.HideTrayicon;

            // Check if it's the first time launch?
            if (_coreConfiguration.IsFirstLaunch)
            {
                _coreConfiguration.IsFirstLaunch = false;
                Log.Info().WriteLine("FirstLaunch: Created new configuration, showing balloon.");
                try
                {
                    notifyIcon.BalloonTipClicked += BalloonTipClicked;
                    notifyIcon.BalloonTipClosed += BalloonTipClosed;
                    notifyIcon.ShowBalloonTip(2000, "Greenshot", string.Format(_greenshotLanguage.TooltipFirststart, HotkeyControl.GetLocalizedHotkeyStringFromString(_coreConfiguration.RegionHotkey)), ToolTipIcon.Info);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Exception while showing first launch: ");
                }
            }
            
            // Make Greenshot use less memory after startup
            if (_coreConfiguration.MinimizeWorkingSetSize)
            {
                PsApi.EmptyWorkingSet();
            }
        }

        public static MainForm Instance { get; set; }

        public NotifyIcon NotifyIcon => notifyIcon;

        private void BalloonTipClicked(object sender, EventArgs e)
        {
            try
            {
                ShowSetting();
            }
            finally
            {
                BalloonTipClosed(sender, e);
            }
        }

        private void BalloonTipClosed(object sender, EventArgs e)
        {
            notifyIcon.BalloonTipClicked -= BalloonTipClicked;
            notifyIcon.BalloonTipClosed -= BalloonTipClosed;
        }

        protected override void WndProc(ref Message m)
        {
            if (HotkeyControl.HandleMessages(ref m))
            {
                return;
            }
            // BUG-1809 prevention, filter the InputLangChange messages
            if (WmInputLangChangeRequestFilter.PreFilterMessageExternal(ref m))
            {
                return;
            }
            base.WndProc(ref m);
        }

        public void UpdateUi()
        {
            // As the form is never loaded, call ApplyLanguage ourselves
            ApplyLanguage();
            
            // Show hotkeys in Contextmenu
            contextmenu_capturearea.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_coreConfiguration.RegionHotkey);
            contextmenu_capturewindow.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_coreConfiguration.WindowHotkey);
            contextmenu_capturefullscreen.ShortcutKeyDisplayString = HotkeyControl.GetLocalizedHotkeyStringFromString(_coreConfiguration.FullscreenHotkey);
        }

        /// <summary>
        ///     Handle the notify icon click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIconClickTest(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            // The right button will automatically be handled with the context menu, here we only check the left.
            if (_coreConfiguration.DoubleClickAction == ClickActions.DO_NOTHING)
            {
                // As there isn't a double-click we can start the Left click
                NotifyIconClick(_coreConfiguration.LeftClickAction);
                // ready with the test
                return;
            }
            // If the timer is enabled we are waiting for a double click...
            if (_doubleClickTimer.Enabled)
            {
                // User clicked a second time before the timer tick: Double-click!
                _doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
                _doubleClickTimer.Stop();
                NotifyIconClick(_coreConfiguration.DoubleClickAction);
            }
            else
            {
                // User clicked without a timer, set the timer and if it ticks it was a single click
                // Create timer, if it ticks before the NotifyIconClickTest is called again we have a single click
                _doubleClickTimer.Elapsed += NotifyIconSingleClickTest;
                _doubleClickTimer.Interval = SystemInformation.DoubleClickTime;
                _doubleClickTimer.Start();
            }
        }

        /// <summary>
        ///     Called by the doubleClickTimer, this means a single click was used on the tray icon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIconSingleClickTest(object sender, EventArgs e)
        {
            _doubleClickTimer.Elapsed -= NotifyIconSingleClickTest;
            _doubleClickTimer.Stop();
            BeginInvoke((MethodInvoker) (() => NotifyIconClick(_coreConfiguration.LeftClickAction)));
        }

        /// <summary>
        ///     Handle the notify icon click
        /// </summary>
        private void NotifyIconClick(ClickActions clickAction)
        {
            switch (clickAction)
            {
                case ClickActions.OPEN_LAST_IN_EXPLORER:
                    Contextmenu_OpenRecent(this, null);
                    break;
                case ClickActions.OPEN_LAST_IN_EDITOR:
                    _coreConfiguration.ValidateAndCorrect();

                    if (File.Exists(_coreConfiguration.OutputFileAsFullpath))
                    {
                        CaptureHelper.CaptureFile(_captureSupportInfo, _coreConfiguration.OutputFileAsFullpath, _destinationHolder.SortedActiveDestinations.Find("Editor"));
                    }
                    break;
                case ClickActions.OPEN_SETTINGS:
                    ShowSetting();
                    break;
                case ClickActions.SHOW_CONTEXT_MENU:
                    var oMethodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                    oMethodInfo?.Invoke(notifyIcon, null);
                    break;
            }
        }

        /// <summary>
        ///     The Contextmenu_OpenRecent currently opens the last know save location
        /// </summary>
        private void Contextmenu_OpenRecent(object sender, EventArgs eventArgs)
        {
            _coreConfiguration.ValidateAndCorrect();
            var path = _coreConfiguration.OutputFileAsFullpath;
            if (!File.Exists(path))
            {
                path = FilenameHelper.FillVariables(_coreConfiguration.OutputFilePath, false);
                // Fix for #1470, problems with a drive which is no longer available
                try
                {
                    var lastFilePath = Path.GetDirectoryName(_coreConfiguration.OutputFileAsFullpath);

                    if (lastFilePath != null && Directory.Exists(lastFilePath))
                    {
                        path = lastFilePath;
                    }
                    else if (!Directory.Exists(path))
                    {
                        // What do I open when nothing can be found? Right, nothing...
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine(ex, "Couldn't open the path to the last exported file, taking default.");
                }
            }
            try
            {
                ExplorerHelper.OpenInExplorer(path);
            }
            catch (Exception ex)
            {
                // Make sure we show what we tried to open in the exception
                ex.Data.Add("path", path);
                Log.Warn().WriteLine(ex, "Couldn't open the path to the last exported file");
                // No reason to create a bug-form, we just display the error.
                MessageBox.Show(this, ex.Message, $"Opening {path}", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        ///     Shutdown / cleanup
        /// </summary>
        public void Exit()
        {
            Log.Info().WriteLine("Exit: " + EnvironmentInfo.EnvironmentToString(false));

            ImageOutput.RemoveTmpFiles();

            // make the icon invisible otherwise it stays even after exit!!
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }
        }


        /// <summary>
        ///     Do work in the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundWorkerTimerTick(object sender, EventArgs e)
        {
            if (_coreConfiguration.MinimizeWorkingSetSize)
            {
                PsApi.EmptyWorkingSet();
            }
            
        }

        /// <summary>
        /// Setup the Bitmap scaling (for icons)
        /// </summary>
        private void SetupBitmapScaleHandler()
        {
            ContextMenuDpiHandler = contextMenu.AttachDpiHandler();

            var dpiChangeSubscription = FormDpiHandler.OnDpiChanged.Subscribe(info =>
            {
                // Change the ImageScalingSize before setting the bitmaps
                var width = DpiHandler.ScaleWithDpi(_coreConfiguration.IconSize.Width, info.NewDpi);
                var height = DpiHandler.ScaleWithDpi(_coreConfiguration.IconSize.Height, info.NewDpi);
                var size = new Size(width, height);
                contextMenu.SuspendLayout();
                contextMenu.ImageScalingSize = size;
                contextMenu.ResumeLayout(true);
                contextMenu.Refresh();
                notifyIcon.Icon = GreenshotResources.Instance.GetGreenshotIcon();
            });

            var contextMenuResourceScaleHandler = BitmapScaleHandler.Create<string, IBitmapWithNativeSupport>(ContextMenuDpiHandler, (imageName, dpi) => GreenshotResources.Instance.GetBitmap(imageName, GetType()), (bitmap, dpi) => bitmap.ScaleIconForDisplaying(dpi));

            contextMenuResourceScaleHandler.AddTarget(contextmenu_capturewindow, "contextmenu_capturewindow.Image", _valueConverter);
            contextMenuResourceScaleHandler.AddTarget(contextmenu_capturearea, "contextmenu_capturearea.Image", _valueConverter);
            contextMenuResourceScaleHandler.AddTarget(contextmenu_capturefullscreen, "contextmenu_capturefullscreen.Image", _valueConverter);
            contextMenuResourceScaleHandler.AddTarget(contextmenu_settings, "contextmenu_settings.Image", _valueConverter);
            contextMenuResourceScaleHandler.AddTarget(contextmenu_exit, "contextmenu_exit.Image", _valueConverter);

            // this is special handling, for the icons which come from the executables
            var exeBitmapScaleHandler = BitmapScaleHandler.Create<string, IBitmapWithNativeSupport>(ContextMenuDpiHandler,
                (path, dpi) => PluginUtils.GetCachedExeIcon(path, 0, dpi >= 120),
                (bitmap, dpi) => bitmap.ScaleIconForDisplaying(dpi));

            // Add cleanup
            Application.ApplicationExit += (sender, args) =>
            {
                dpiChangeSubscription.Dispose();
                ContextMenuDpiHandler.Dispose();
                contextMenuResourceScaleHandler.Dispose();
                exeBitmapScaleHandler.Dispose();
            };
        }

        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug().WriteLine("Mainform closing, reason: {0}", e.CloseReason);
            Instance = null;
            Exit();
        }

        private void MainFormActivated(object sender, EventArgs e)
        {
            Hide();
            ShowInTaskbar = false;
        }

        private void CaptureFile()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.greenshot, *.png, *.jpg, *.gif, *.bmp, *.ico, *.tiff, *.wmf)|*.greenshot; *.png; *.jpg; *.jpeg; *.gif; *.bmp; *.ico; *.tiff; *.tif; *.wmf"
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            if (File.Exists(openFileDialog.FileName))
            {
                CaptureHelper.CaptureFile(_captureSupportInfo, openFileDialog.FileName);
            }
        }
        
        private void ContextMenuOpening(object sender, CancelEventArgs e)
        {
            // Multi-Screen captures
            contextmenu_capturefullscreen.Click -= CaptureFullScreenToolStripMenuItemClick;
            contextmenu_capturefullscreen.DropDownOpening -= MultiScreenDropDownOpening;
            contextmenu_capturefullscreen.DropDownClosed -= MultiScreenDropDownClosing;
            if (Screen.AllScreens.Length > 1)
            {
                contextmenu_capturefullscreen.DropDownOpening += MultiScreenDropDownOpening;
                contextmenu_capturefullscreen.DropDownClosed += MultiScreenDropDownClosing;
            }
            else
            {
                contextmenu_capturefullscreen.Click += CaptureFullScreenToolStripMenuItemClick;
            }
        }

        private void ContextMenuClosing(object sender, EventArgs e)
        {
            contextmenu_capturewindowfromlist.DropDownItems.Clear();
            CleanupThumbnail();
        }

        /// <summary>
        ///     MultiScreenDropDownOpening is called when mouse hovers over the Capture-Screen context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MultiScreenDropDownOpening(object sender, EventArgs e)
        {
            var captureScreenMenuItem = (ToolStripMenuItem) sender;
            captureScreenMenuItem.DropDownItems.Clear();
            if (Screen.AllScreens.Length <= 1)
            {
                return;
            }
            var allScreensBounds = DisplayInfo.ScreenBounds;

            var captureScreenItem = new ToolStripMenuItem(_greenshotLanguage.ContextmenuCapturefullscreenAll);
            captureScreenItem.Click += (o, args) => BeginInvoke((MethodInvoker) (() => CaptureHelper.CaptureFullscreen(_captureSupportInfo, false, ScreenCaptureMode.FullScreen)));
            captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
            foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
            {
                var screenToCapture = displayInfo;
                var deviceAlignment = "";
                if (displayInfo.Bounds.Top == allScreensBounds.Top && displayInfo.Bounds.Bottom != allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + _greenshotLanguage.ContextmenuCapturefullscreenTop;
                }
                else if (displayInfo.Bounds.Top != allScreensBounds.Top && displayInfo.Bounds.Bottom == allScreensBounds.Bottom)
                {
                    deviceAlignment += " " + _greenshotLanguage.ContextmenuCapturefullscreenBottom;
                }
                if (displayInfo.Bounds.Left == allScreensBounds.Left && displayInfo.Bounds.Right != allScreensBounds.Right)
                {
                    deviceAlignment += " " + _greenshotLanguage.ContextmenuCapturefullscreenLeft;
                }
                else if (displayInfo.Bounds.Left != allScreensBounds.Left && displayInfo.Bounds.Right == allScreensBounds.Right)
                {
                    deviceAlignment += " " + _greenshotLanguage.ContextmenuCapturefullscreenRight;
                }
                captureScreenItem = new ToolStripMenuItem(deviceAlignment);
                captureScreenItem.Click += (o, args) => BeginInvoke((MethodInvoker) (() => CaptureHelper.CaptureRegion(_captureSupportInfo, false, screenToCapture.Bounds)));
                captureScreenMenuItem.DropDownItems.Add(captureScreenItem);
            }
        }

        /// <summary>
        ///     MultiScreenDropDownOpening is called when mouse leaves the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MultiScreenDropDownClosing(object sender, EventArgs e)
        {
            var captureScreenMenuItem = (ToolStripMenuItem) sender;
            captureScreenMenuItem.DropDownItems.Clear();
        }

        /// <summary>
        ///     Build a selectable list of windows when we enter the menu item
        /// </summary>
        private void CaptureWindowFromListMenuDropDownOpening(object sender, EventArgs e)
        {
            // The Capture window context menu item used to go to the following code:
            // captureForm.MakeCapture(CaptureMode.Window, false);
            // Now we check which windows are there to capture
            var captureWindowFromListMenuItem = (ToolStripMenuItem) sender;
            AddCaptureWindowMenuItems(captureWindowFromListMenuItem, Contextmenu_capturewindowfromlist_Click);
        }

        private void CaptureWindowFromListMenuDropDownClosed(object sender, EventArgs e)
        {
            CleanupThumbnail();
        }

        private void ShowThumbnailOnEnter(object sender, EventArgs e)
        {
            if (!(sender is ToolStripMenuItem captureWindowItem))
            {
                return;
            }

            var window = captureWindowItem.Tag as IInteropWindow;
            if (_thumbnailForm == null)
            {
                _thumbnailForm = new ThumbnailForm(_coreConfiguration);
            }
            _thumbnailForm.ShowThumbnail(window, captureWindowItem.GetCurrentParent().TopLevelControl);
        }

        private void HideThumbnailOnLeave(object sender, EventArgs e)
        {
            _thumbnailForm?.Hide();
        }

        private void CleanupThumbnail()
        {
            if (_thumbnailForm == null)
            {
                return;
            }
            _thumbnailForm.Close();
            _thumbnailForm = null;
        }

        public void AddCaptureWindowMenuItems(ToolStripMenuItem menuItem, EventHandler eventHandler)
        {
            menuItem.DropDownItems.Clear();
            // check if thumbnailPreview is enabled and DWM is enabled
            var thumbnailPreview = _coreConfiguration.ThumnailPreview && Dwm.IsDwmEnabled;

            foreach (var window in InteropWindowQuery.GetTopLevelWindows().Concat(AppQuery.WindowsStoreApps.ToList()))
            {
                var title = window.GetCaption();
                if (title == null)
                {
                    continue;
                }
                if (title.Length > _coreConfiguration.MaxMenuItemLength)
                {
                    title = title.Substring(0, Math.Min(title.Length, _coreConfiguration.MaxMenuItemLength));
                }
                var captureWindowItem = menuItem.DropDownItems.Add(title);
                captureWindowItem.Tag = window;
                captureWindowItem.Image = window.GetDisplayIcon(ContextMenuDpiHandler.Dpi > DpiHandler.DefaultScreenDpi).NativeBitmap;
                captureWindowItem.Click += eventHandler;
                // Only show preview when enabled
                if (thumbnailPreview)
                {
                    captureWindowItem.MouseEnter += ShowThumbnailOnEnter;
                    captureWindowItem.MouseLeave += HideThumbnailOnLeave;
                }
            }
        }

        private void CaptureAreaToolStripMenuItemClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureRegion(_captureSupportInfo, false); });
        }

        private void CaptureClipboardToolStripMenuItemClick(object sender, EventArgs e)
        {
            BeginInvoke(new System.Action(() => CaptureHelper.CaptureClipboard(_captureSupportInfo)));
        }

        private void OpenFileToolStripMenuItemClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) CaptureFile);
        }

        private void CaptureFullScreenToolStripMenuItemClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureFullscreen(_captureSupportInfo, false, _coreConfiguration.ScreenCaptureMode); });
        }

        private void Contextmenu_capturelastregionClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureLastRegion(_captureSupportInfo, false); });
        }

        private void Contextmenu_capturewindow_Click(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate { CaptureHelper.CaptureWindowInteractive(_captureSupportInfo, false); });
        }

        private void Contextmenu_capturewindowfromlist_Click(object sender, EventArgs e)
        {
            var clickedItem = (ToolStripMenuItem) sender;
            BeginInvoke((MethodInvoker) delegate
            {
                try
                {
                    var windowToCapture = (InteropWindow) clickedItem.Tag;
                    CaptureHelper.CaptureWindow(_captureSupportInfo, windowToCapture);
                }
                catch (Exception exception)
                {
                    Log.Error().WriteLine(exception);
                }
            });
        }

        /// <summary>
        ///     Context menu entry "Support Greenshot"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contextmenu_donateClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) delegate
            {
                var processStartInfo = new ProcessStartInfo("http://getgreenshot.org/support/?version=" + Assembly.GetEntryAssembly()?.GetName().Version)
                {
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            });
        }

        /// <summary>
        ///     Context menu entry "Preferences"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contextmenu_settingsClick(object sender, EventArgs e)
        {
            BeginInvoke((MethodInvoker) ShowSetting);
        }

        /// <summary>
        ///     This is called indirectly from the context menu "Preferences"
        /// </summary>
        public void ShowSetting()
        {
            var lang = _coreConfiguration.Language;
            using (var ownedConfigViewModel = _configViewModelFactory())
            {
                _windowManager.ShowDialog(ownedConfigViewModel.Value);
            }
            if (!Equals(lang, _coreConfiguration.Language))
            {
                ApplyLanguage();
            }
        }

        /// <summary>
        ///     The "Exit" entry is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contextmenu_exitClick(object sender, EventArgs e)
        {
            // Gracefull shutdown
            System.Windows.Application.Current.Shutdown(0);
        }
    }
}