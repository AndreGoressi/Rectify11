﻿using KPreisser.UI;
using Microsoft.Win32;
using Rectify11Installer.Core;
using Rectify11Installer.Pages;
using Rectify11Installer.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Rectify11Installer
{
	public partial class frmWizard : Form
	{
		#region Variables
		private int timerFrames;
		private int timerFramesTmp;
		private bool isWelcomePage = true;
		private bool acknowledged = false;
		private bool idleinit = false;
		public string InstallerProgress
		{
			get { return progressLabel.Text; }
			set { progressLabel.Text = value; }
		}
		public Image UpdateSideImage
		{
			get { return sideImage.Image; }
			set { sideImage.Image = value; }
		}
		public bool ShowRebootButton
		{
			get { return tableLayoutPanel2.Visible; }
			set
			{
				nextButton.Visible = false;
				progressLabel.Location = new Point(progressLabel.Location.X, progressLabel.Location.Y - 30);
				pictureBox1.Location = new Point(pictureBox1.Location.X, pictureBox1.Location.Y - 30);
				cancelButton.ButtonText = resources.GetString("buttonReboot");
				cancelButton.Click -= CancelButton_Click;
				tableLayoutPanel2.Visible = true;
				if (!Theme.IsUsingDarkMode)
				{
					DarkMode.UpdateFrame(this, true);
				}
			}
		}
		public EventHandler SetRebootHandler
		{
			set { cancelButton.Click += value; }
		}
		private System.ComponentModel.ComponentResourceManager resources = new SingleAssemblyComponentResourceManager(typeof(Strings.Rectify11));
		#endregion
		#region Main
		public frmWizard()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			InitializeComponent();
			if (System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
			{
				RightToLeftLayout = true;
				RightToLeft = RightToLeft.Yes;
			}
			DarkMode.RefreshTitleBarColor(Handle);
			if (Theme.IsUsingDarkMode)
			{
				DarkMode.UpdateFrame(this, true);
			}

			Navigate(RectifyPages.WelcomePage);
			Shown += FrmWizard_Shown;
			FormClosing += frmWizard_FormClosing;
			Application.Idle += Application_Idle;
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			if (!idleinit)
			{
				// initialize installoptonspage here because it needs 
				// current instance to change button state.
				RectifyPages.InstallOptnsPage = new InstallOptnsPage(this);
				RectifyPages.ProgressPage = new ProgressPage(this);
				TabPages.expPage.Controls.Add(RectifyPages.ExperimentalPage);
				TabPages.eulPage.Controls.Add(RectifyPages.EulaPage);
				TabPages.installPage.Controls.Add(RectifyPages.InstallOptnsPage);
				TabPages.themePage.Controls.Add(RectifyPages.ThemeChoicePage);
				TabPages.epPage.Controls.Add(RectifyPages.EPPage);
				TabPages.debPage.Controls.Add(RectifyPages.DebugPage);
				TabPages.progressPage.Controls.Add(RectifyPages.ProgressPage);
				TabPages.summaryPage.Controls.Add(RectifyPages.InstallConfirmation);
				RectifyPages.WelcomePage.InstallButton.Click += InstallButton_Click;
				RectifyPages.WelcomePage.UninstallButton.Click += UninstallButton_Click;
				nextButton.Click += NextButton_Click;
				navBackButton.Click += BackButton_Click;
				cancelButton.Click += CancelButton_Click;
				versionLabel.Text = versionLabel.Text + ProductVersion;
				SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
				idleinit = true;
			}
		}

		private void FrmWizard_Shown(object sender, EventArgs e)
		{
			if (Theme.IsUsingDarkMode)
			{
				BackColor = Color.Black;
				ForeColor = Color.White;
				headerText.ForeColor = Color.White;
			}
			else
			{
				BackColor = Color.White;
				ForeColor = Color.Black;
				headerText.ForeColor = Color.Black;
				if ((Win32.NativeMethods.GetUbr() != -1
					&& Win32.NativeMethods.GetUbr() < 51
					&& Environment.OSVersion.Version.Build == 22000)
					|| (Environment.OSVersion.Version.Build < 22000
					&& Environment.OSVersion.Version.Build >= 21996))
				{
					tableLayoutPanel1.BackColor = Color.White;
					tableLayoutPanel2.BackColor = Color.White;
					headerText.ForeColor = Color.White;
				}
			}

			TabPages.wlcmPage.Controls.Add(RectifyPages.WelcomePage);
			RectifyPages.WelcomePage.UninstallButton.Enabled = InstallStatus.IsRectify11Installed;
		}
		#endregion
		#region Navigation
		private async void Navigate(WizardPage page)
		{
			headerText.Text = page.WizardHeader;
			sideImage.Image = page.SideImage;
			tableLayoutPanel1.Visible = page.HeaderVisible;
			tableLayoutPanel2.Visible = page.FooterVisible;
			navPane.SelectedTab = page.Page;
			if (!sideImage.Enabled)
				sideImage.Enabled = true;
			if (!Theme.IsUsingDarkMode)
			{
				DarkMode.UpdateFrame(this, page.UpdateFrame);
			}
			isWelcomePage = page.IsWelcomePage;
			nextButton.Enabled = page.NextButtonEnabled;
			nextButton.ButtonText = page.NextButtonText;
			if (page == RectifyPages.InstallOptnsPage)
			{
				nextButton.Enabled = Variables.IsItemsSelected;
			}
			else if (page == RectifyPages.InstallConfirmation)
			{
				RectifyPages.InstallConfirmation.Summary = resources.GetString("summaryItems");
				RectifyPages.InstallConfirmation.Summary += Helper.FinalText().ToString();
				timerFrames = 72;
				timerFramesTmp = 0;
				timer.Start();
			}
			else if (page == RectifyPages.ProgressPage)
			{
				versionLabel.Visible = false;
				Helper.FinalizeIRectify11();
				pictureBox1.Visible = true;
				progressLabel.Visible = true;
				RectifyPages.ProgressPage.Start();
				NativeMethods.SetCloseButton(this, false);
				Variables.isInstall = true;
				Installer installer = new();
				Logger.CommitLog();
				if (!await installer.Install(this))
				{
					TaskDialog.Show(text: "Rectify11 setup encountered an error, for more information, see the log in " + Path.Combine(Variables.r11Folder, "installer.log") + ", and report it to rectify11 development server",
						title: "Error",
						buttons: TaskDialogButtons.OK,
						icon: TaskDialogStandardIcon.Error);
					Application.Exit();
				}
				else
				{
					RectifyPages.ProgressPage.StartReset();
				}
			}
		}
		#endregion
		#region Private Methods
		private void timer1_Tick(object sender, EventArgs e)
		{
			timerFramesTmp++;
			if (timerFramesTmp == timerFrames)
			{
				timerFrames = 0;
				timerFramesTmp = 0;
				sideImage.Enabled = false;
				timer.Stop();
			}
		}
		private void CancelButton_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
		private void frmWizard_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!Variables.isInstall)
			{
				TaskDialogResult ok = TaskDialog.Show(text: resources.GetString("exitText"),
					title: resources.GetString("Title"),
					buttons: TaskDialogButtons.Yes | TaskDialogButtons.No,
					icon: TaskDialogStandardIcon.Information);
				if (ok == TaskDialogResult.No)
				{
					e.Cancel = true;
				}
			}
			else if (Variables.isInstall)
			{
				if (e.CloseReason == CloseReason.UserClosing)
				{
					e.Cancel = true;
				}
			}
			SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(SystemEvents_UserPreferenceChanged);
		}
		private void NextButton_Click(object sender, EventArgs e)
		{
			if (navPane.SelectedTab == TabPages.expPage)
			{
				if (!acknowledged)
				{
					acknowledged = true;
				}
				Navigate(RectifyPages.EulaPage);
			}
			else if (navPane.SelectedTab == TabPages.eulPage)
			{
				Navigate(RectifyPages.InstallOptnsPage);
			}
			else if (navPane.SelectedTab == TabPages.installPage)
			{
				Helper.UpdateIRectify11();
				//MessageBox.Show("EP " + InstallOptions.InstallEP + "\nASDF " + InstallOptions.InstallASDF + "\nWallpapers " + InstallOptions.InstallWallpaper + "\nWinver " + InstallOptions.InstallWinver + "\nShell " + InstallOptions.InstallShell);
				if (InstallOptions.InstallThemes)
				{
					Navigate(RectifyPages.ThemeChoicePage);
				}
				else if (InstallOptions.InstallEP)
				{
					Navigate(RectifyPages.EPPage);
				}
				else
				{
					Navigate(RectifyPages.InstallConfirmation);
				}
			}
			else if (navPane.SelectedTab == TabPages.themePage)
			{
				if (InstallOptions.InstallEP)
				{
					Navigate(RectifyPages.EPPage);
				}
				else
				{
					Navigate(RectifyPages.InstallConfirmation);
				}
			}
			else if (navPane.SelectedTab == TabPages.epPage)
			{
				Navigate(RectifyPages.InstallConfirmation);
			}
			else if (navPane.SelectedTab == TabPages.summaryPage)
			{
				Navigate(RectifyPages.ProgressPage);
			}
		}

		private void BackButton_Click(object sender, EventArgs e)
		{
			if (navPane.SelectedTab == TabPages.expPage)
			{
				Navigate(RectifyPages.WelcomePage);
			}
			else if (navPane.SelectedTab == TabPages.eulPage)
			{
				Navigate(RectifyPages.WelcomePage);
			}
			else if (navPane.SelectedTab == TabPages.installPage)
			{
				Navigate(RectifyPages.EulaPage);
			}
			else if (navPane.SelectedTab == TabPages.themePage)
			{
				Navigate(RectifyPages.InstallOptnsPage);
			}
			else if (navPane.SelectedTab == TabPages.debPage)
			{
				Navigate(RectifyPages.WelcomePage);
			}
			else if (navPane.SelectedTab == TabPages.epPage)
			{
				if (InstallOptions.InstallThemes)
				{
					Navigate(RectifyPages.ThemeChoicePage);
				}
				else
				{
					Navigate(RectifyPages.InstallOptnsPage);
				}
			}
			else if (navPane.SelectedTab == TabPages.summaryPage)
			{
				if (InstallOptions.InstallEP)
				{
					Navigate(RectifyPages.EPPage);
				}
				else if (InstallOptions.InstallThemes)
				{
					Navigate(RectifyPages.ThemeChoicePage);
				}
				else
				{
					Navigate(RectifyPages.InstallOptnsPage);
				}
			}
		}

		private void InstallButton_Click(object sender, EventArgs e)
		{
			if (Helper.CheckIfUpdatesPending())
			{
				if (!acknowledged)
				{
					Navigate(RectifyPages.ExperimentalPage);
				}
				else
				{
					Navigate(RectifyPages.EulaPage);
				}
			}
		}

		private void UninstallButton_Click(object sender, EventArgs e)
		{
			if (Helper.CheckIfUpdatesPending())
			{
				TaskDialog.Show(text: "Uninstalling Rectify11 is not yet supported. You can run sfc /scannow to revert icon changes.",
				instruction: "Incompleted Software",
				title: "Rectify11 Setup",
				buttons: TaskDialogButtons.OK,
				icon: TaskDialogStandardIcon.SecurityErrorRedBar);
				//Navigate(UninstallConfirmPage);
			}
		}
		int clicks = 0;
		private void VersionLabel_Click(object sender, EventArgs e)
		{
			clicks++;
			if (clicks == 2)
			{
				clicks = 0;
				Navigate(RectifyPages.DebugPage);
			}
		}
		private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			switch (e.Category)
			{
				case UserPreferenceCategory.General:
					{
						Theme.InitTheme();
						DarkMode.RefreshTitleBarColor(Handle);
						if (Theme.IsUsingDarkMode)
						{
							BackColor = Color.Black;
							ForeColor = Color.White;
							headerText.ForeColor = Color.White;
						}
						else
						{
							headerText.ForeColor = Color.Black;
							BackColor = Color.White;
							ForeColor = Color.Black;
						}
						if (isWelcomePage && !Theme.IsUsingDarkMode)
						{
							DarkMode.UpdateFrame(this, false);
						}
						else
						{
							DarkMode.UpdateFrame(this, true);
						}
						Invalidate(true);
						Update();
					}
					break;
			}
		}
		#endregion
	}
}
