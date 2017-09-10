﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MahApps.Metro.Controls;

namespace ImageManager
{
	public delegate void CreateControlPanelDelegate();
	public partial class MainWindow : MetroWindow
	{

		private Image currentImage;
		private List<string> allImagesPath;
		private KeyManager keyManager = new KeyManager();
		private Settings settings = new Settings();
		private List<ControlPanel> controlPanels = new List<ControlPanel>();
		private bool isFullScreenEnabled = false;


		public MainWindow()
		{

			InitializeComponent();
			LoadSettingsFromFile();
			WindowButtonCommandsOverlayBehavior = WindowCommandsOverlayBehavior.Never;
		}

		private void Image_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
			{
				SetFullscreenSettings(!isFullScreenEnabled);
			}
		}

		private void SetFullscreenSettings(bool fullscreen)
		{
			WindowStyle = fullscreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
			ButtonsGrid.Visibility = fullscreen ? Visibility.Hidden : Visibility.Visible;
			ResizeMode = fullscreen ? ResizeMode.NoResize : ResizeMode.CanResize;
			WindowState = fullscreen ? WindowState.Maximized : WindowState.Normal;

			IgnoreTaskbarOnMaximize = fullscreen;
			ShowTitleBar = !fullscreen;
			isFullScreenEnabled = fullscreen;
			ShowCloseButton = !fullscreen;
			ShowMinButton = !fullscreen;
			ShowMaxRestoreButton = !fullscreen;
		}

		private void SaveSettings(object sender)
		{
			BinaryFormatter binFormat = new BinaryFormatter();
			using (Stream fStream = new FileStream("settings.dat", FileMode.Create, FileAccess.Write, FileShare.None))
			{
				binFormat.Serialize(fStream, sender);
			}
		}

		private void LoadSettingsFromFile()
		{
			BinaryFormatter binFormat = new BinaryFormatter();
			Settings settingsFromFile;

			if (!File.Exists("settings.dat"))
				return;

			using (Stream fStream = File.OpenRead("settings.dat"))
			{
				settingsFromFile = fStream.Length != 0 
					? (Settings) binFormat.Deserialize(fStream) 
					: new Settings();
			}

			SettingsManager.LoadSettings(controlPanels, settingsFromFile, CreateControlPanel);
		}

		private void AddControlHeadPanel()
		{
			var stackPanel = ControlPanel.CreateHeaderStackPanel(AddKeyButton_Click);
			var rd = new RowDefinition
			{
				Height = new GridLength(30, GridUnitType.Pixel)
			};
			SettingsGrid.RowDefinitions.Add(rd);
			SettingsGrid.Children.Add(stackPanel);
		}

		private string ShowSelectFolderDialog()
		{
			var selectedPath = string.Empty;

			using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
			{
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					selectedPath = dlg.SelectedPath;
				}
			}

			return selectedPath;
		}

		private void ShowImage(string path)
		{
			if (path != null)
			{
				currentImage = new Image(path);
				Picture.Source = currentImage.Img;
				AppWindow.Title = currentImage.Name;
			}
			else
				Picture.Source = null;
		}

		private void CreateFolder(string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		private void MoveImage(string directoryPath)
		{
			if (Picture.Source == null)
				return;

			CreateFolder(directoryPath);

			if (!File.Exists(Path.Combine(directoryPath, currentImage.Name)))
			{
				File.Copy(currentImage.FullPath, Path.Combine(directoryPath, currentImage.Name));
			}
			if (FileModeSwitcher.IsChecked == true)
			{
				var currentImagePath = currentImage.FullPath;
				allImagesPath.Remove(currentImagePath);

				var nextImageIndex = FileManager.GetNextImageIndex(allImagesPath, currentImage);
				var nextImagePath = allImagesPath.Count != 0 ?
					allImagesPath[nextImageIndex] :
					null;

				ShowImage(nextImagePath);
				File.Delete(currentImagePath);
			}

		}

		private void ClearGrid(Grid grid)
		{
			grid.Children.Clear();
			grid.RowDefinitions.Clear();
			grid.ColumnDefinitions.Clear();
		}

		private void AddControlPanelToGrid(List<ControlPanel> controlPanels)
		{
			foreach (ControlPanel cp in controlPanels)
			{
				AddControlPanelToGrid(cp);
			}
		}

		private void AddControlPanelToGrid(ControlPanel cp)
		{
			SettingsGrid.RowDefinitions.Add(cp.ControlRow);
			SettingsGrid.Children.Add(cp.ControlStackPanel);

			SettingsManager.RefreshSettings(controlPanels, settings);
			SaveSettings(settings);
		}

		private void CreateControlPanel()
		{
			if (controlPanels.Count >= 8)
				return;

			var cp = new ControlPanel(controlPanels.Count + 1);

			cp.KeyTextBox.PreviewKeyDown += Controls_KeyDown;
			cp.DeleteKeyButton.Click += DeleteKeyButton_Click;
			cp.SubfolderTextBox.TextChanged += SubfolderTextBox_TextChanged;
			cp.KeyTextBox.TextChanged += KeyTextBox_TextChanged;

			AddControlPanelToGrid(cp);
			controlPanels.Add(cp);

			SettingsManager.RefreshSettings(controlPanels, settings);
			SaveSettings(settings);
		}

		private void AddKeyButton_Click(object sender, RoutedEventArgs e)
		{
			CreateControlPanel();
			SettingsManager.RefreshSettings(controlPanels, settings);
			SaveSettings(settings);
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			SettingsFlyout.IsOpen = true;
		}

		private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
		{
			string directoryPath = ShowSelectFolderDialog();
			if (directoryPath != String.Empty)
			{
				allImagesPath = Directory.GetFiles(directoryPath).ToList();
				if (allImagesPath.Count != 0)
				{
					ShowImage(allImagesPath[0]);
				}
			}
		}

		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if (SettingsFlyout.IsOpen != true && currentImage != null)
			{
				if (e.Key == Key.Right || e.Key == Key.Space)
				{
					var nextImageIndex = FileManager.GetNextImageIndex(allImagesPath, currentImage);
					ShowImage(allImagesPath[nextImageIndex]);
					return;
				}
				if (e.Key == Key.Left || e.Key == Key.Back)
				{
					var previousImageIndex = FileManager.GetPreviousImageIndex(allImagesPath, currentImage);
					ShowImage(allImagesPath[previousImageIndex]);
					return;
				}
				if (keyManager.IsKeyUsed(e.Key.ToString()))
				{
					var index = ControlPanel.GetControlPanelIndex(controlPanels, e.Key.ToString());
					var subfolderName = ControlPanel.GetSubfolderName(controlPanels, index);
					var imageDirectoryName = Path.GetDirectoryName(currentImage.FullPath);
					var newImagePath = Path.Combine(imageDirectoryName, subfolderName);

					CreateFolder(newImagePath);
					MoveImage(newImagePath);
				}
			}
		}

		private void Controls_KeyDown(object sender, KeyEventArgs e)
		{
			var txtBox = (TextBox)sender;
			var key = e.Key.ToString();

			if (!keyManager.IsKeyUsed(key))
			{
				if (!string.IsNullOrEmpty(txtBox.Text))
				{
					keyManager.RemoveKey(txtBox.Text);
				}
				txtBox.Text = key;
				keyManager.AddKey(key);
			}
		}

		private void KeyTextBox_TextChanged(object sender, RoutedEventArgs e)
		{
			var txtBox = (TextBox)sender;
			if (!keyManager.IsKeyUsed(txtBox.Text))
				keyManager.AddKey(txtBox.Text);
		}

		private void SubfolderTextBox_TextChanged(object sender, RoutedEventArgs e)
		{
			var txtBox = (TextBox)sender;

			var panel = controlPanels.SingleOrDefault(p => p.KeyTextBox.Name == txtBox.Name);

			if (panel?.KeyTextBox.Text == String.Empty
				&& txtBox.Text.Length == 1
				&& !keyManager.IsKeyUsed(txtBox.Text))
			{
				panel.KeyTextBox.Text = txtBox.Text.ToUpper();
			}

			SettingsManager.RefreshSettings(controlPanels, settings);
			SaveSettings(settings);
		}

		private void DeleteKeyButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var index = ControlPanel.GetIndex(button);
			ClearGrid(SettingsGrid);
			AddControlHeadPanel();
			var cpToDelete = ControlPanel.GetControlPanel(controlPanels, index);
			controlPanels.Remove(cpToDelete);
			ControlPanel.ChangeIndex(controlPanels);
			AddControlPanelToGrid(controlPanels);
			SettingsManager.RefreshSettings(controlPanels, settings);
			SaveSettings(settings);
		}
	}
}
