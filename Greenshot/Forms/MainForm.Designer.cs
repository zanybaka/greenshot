﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
namespace Greenshot {
	partial class MainForm {
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
				if (_copyData != null) {
					_copyData.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.contextmenu_capturearea = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_capturewindow = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_capturefullscreen = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.contextmenu_capturewindowfromlist = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripOpenFolderSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_settings = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.toolStripCloseSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.contextmenu_exit = new GreenshotPlugin.Controls.GreenshotToolStripMenuItem();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.contextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// contextMenu
			// 
			this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
									this.contextmenu_capturearea,
									this.contextmenu_capturewindow,
									this.contextmenu_capturefullscreen,
									this.contextmenu_capturewindowfromlist,
									this.toolStripOpenFolderSeparator,
									this.contextmenu_settings,
									this.toolStripCloseSeparator,
									this.contextmenu_exit});
			this.contextMenu.Name = "contextMenu";
			this.contextMenu.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.ContextMenuClosing);
			this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuOpening);
			this.contextMenu.Renderer = new Greenshot.Controls.ContextMenuToolStripProfessionalRenderer();
			// 
			// contextmenu_capturearea
			// 
			this.contextmenu_capturearea.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_capturearea.Image")));
			this.contextmenu_capturearea.Name = "contextmenu_capturearea";
			this.contextmenu_capturearea.ShortcutKeyDisplayString = "Print";
			this.contextmenu_capturearea.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_capturearea.Click += new System.EventHandler(this.CaptureAreaToolStripMenuItemClick);
			// 
			// contextmenu_capturewindow
			// 
			this.contextmenu_capturewindow.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_capturewindow.Image")));
			this.contextmenu_capturewindow.Name = "contextmenu_capturewindow";
			this.contextmenu_capturewindow.ShortcutKeyDisplayString = "Alt + Print";
			this.contextmenu_capturewindow.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_capturewindow.Click += new System.EventHandler(this.Contextmenu_CaptureWindow_Click);
			// 
			// contextmenu_capturefullscreen
			// 
			this.contextmenu_capturefullscreen.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_capturefullscreen.Image")));
			this.contextmenu_capturefullscreen.Name = "contextmenu_capturefullscreen";
			this.contextmenu_capturefullscreen.ShortcutKeyDisplayString = "Ctrl + Print";
			this.contextmenu_capturefullscreen.Size = new System.Drawing.Size(170, 22);
			// 
			// contextmenu_capturewindowfromlist
			// 
			this.contextmenu_capturewindowfromlist.Name = "contextmenu_capturewindowfromlist";
			this.contextmenu_capturewindowfromlist.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_capturewindowfromlist.DropDownClosed += new System.EventHandler(this.CaptureWindowFromListMenuDropDownClosed);
			this.contextmenu_capturewindowfromlist.DropDownOpening += new System.EventHandler(this.CaptureWindowFromListMenuDropDownOpening);
			// 
			// toolStripOpenFolderSeparator
			// 
			this.toolStripOpenFolderSeparator.Name = "toolStripOpenFolderSeparator";
			this.toolStripOpenFolderSeparator.Size = new System.Drawing.Size(167, 6);
			// 
			// contextmenu_settings
			// 
			this.contextmenu_settings.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_settings.Image")));
			this.contextmenu_settings.Name = "contextmenu_settings";
			this.contextmenu_settings.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_settings.Click += new System.EventHandler(this.Contextmenu_SettingsClick);
			// 
			// toolStripCloseSeparator
			// 
			this.toolStripCloseSeparator.Name = "toolStripCloseSeparator";
			this.toolStripCloseSeparator.Size = new System.Drawing.Size(167, 6);
			// 
			// contextmenu_exit
			// 
			this.contextmenu_exit.Image = ((System.Drawing.Image)(resources.GetObject("contextmenu_exit.Image")));
			this.contextmenu_exit.Name = "contextmenu_exit";
			this.contextmenu_exit.Size = new System.Drawing.Size(170, 22);
			this.contextmenu_exit.Click += new System.EventHandler(this.Contextmenu_exitClick);
			// 
			// notifyIcon
			// 
			this.notifyIcon.ContextMenuStrip = this.contextMenu;
			this.notifyIcon.Text = "Greenshot";
			this.notifyIcon.MouseUp += new System.Windows.Forms.MouseEventHandler(this.NotifyIconClickTest);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(0, 0);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.LanguageKey = "application_title";
			this.Name = "MainForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.Activated += new System.EventHandler(this.MainFormActivated);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
			this.contextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturewindowfromlist;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturewindow;
		private System.Windows.Forms.ToolStripSeparator toolStripOpenFolderSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturefullscreen;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_capturearea;
		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ToolStripSeparator toolStripCloseSeparator;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_exit;
		private System.Windows.Forms.ContextMenuStrip contextMenu;
		private GreenshotPlugin.Controls.GreenshotToolStripMenuItem contextmenu_settings;
	}
}
