﻿using System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Lego.Ev3.Core;
using Lego.Ev3.WinRT;

using SampleApp.Controls;

namespace SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
		private Brick _brick;
		private MotorControl _selectedMotorControl;
		private SensorDataControl _selectedSensorControl;

        public MainPage()
        {
            this.InitializeComponent();

			ConnControl.Visibility = Visibility.Visible;
		}
		
		private async void TryToConnect(object sender, RoutedEventArgs e)
		{
			Overlay.Show("Connecting");

			//ApplicationBar.IsVisible = false;
			ConnControl.Visibility = Visibility.Visible;

			var conType = CreateConnection();

			await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				if (conType != null)
				{
					_brick = new Brick(conType, true);
					_brick.BrickChanged += _brick_BrickChanged;
					try
					{
						await _brick.ConnectAsync();
						ConnControl.Visibility = Visibility.Collapsed;

						ConnTypeRun.Text = ConnControl.GetConnectionType().ToString();
						//ApplicationBar.IsVisible = true;
					}
					catch (Exception)
					{
						new MessageDialog("Could not connect", "Error").ShowAsync();
					}
				}
				else
				{
					MessageDialog dialog = new MessageDialog("Invalid connection type for this device", "Error");

					await dialog.ShowAsync();
				}

				Overlay.Hide();
			});
		}

		void _brick_BrickChanged(object sender, BrickChangedEventArgs e)
		{
			MotorA.Update(_brick);
			MotorB.Update(_brick);
			MotorC.Update(_brick);
			MotorD.Update(_brick);

			InputOne.Update(_brick);
			InputTwo.Update(_brick);
			InputThree.Update(_brick);
			InputFour.Update(_brick);
		}

		private void MotorSettingClicked(object sender, RoutedEventArgs routedEventArgs)
		{
			var control = sender as MotorControl;

			if (control != null)
			{
				MotorSettings.SaveSettings += MotorSettings_SaveSettings;

				_selectedMotorControl = control;

				MotorSettings.Show(
					control.BrickInputPort,
					_brick.Ports[control.BrickInputPort].Type,
					control.MotorMovementType,
					control.DegreeMovement,
					control.PowerRatingMovement,
					control.TimeToMoveInSeconds);
			}
		}

		void MotorSettings_SaveSettings(object sender, MotorSettingsEventArgs e)
		{
			if (_selectedMotorControl == null)
				return;

			MotorSettings.SaveSettings -= MotorSettings_SaveSettings;

			_selectedMotorControl.MotorMovementType = e.MotorMovementType;
			_selectedMotorControl.DegreeMovement = e.DegreeMovement;
			_selectedMotorControl.PowerRatingMovement = e.PowerRatingMovement;
			_selectedMotorControl.TimeToMoveInSeconds = e.TimeToMoveInSeconds;

			_selectedMotorControl.UpdateUx();

			_selectedMotorControl = null;
		}

		private async void MotorPlayClicked(object sender, RoutedEventArgs e)
		{
			var control = sender as MotorControl;

			if (control != null)
			{
				var output = control.BrickOutputPort;

				if (control.MotorMovementType == MotorMovementTypes.Degrees)
				{
					await _brick.DirectCommand.StepMotorAtPowerAsync(output, control.PowerRatingMovement, 0, (uint)control.DegreeMovement, 0, false);
				}
				else
				{
					if (control.TimeToMoveInSeconds == 0)
					{
						await _brick.DirectCommand.TurnMotorAtPowerAsync(output, control.PowerRatingMovement);
					}
					else
					{
						await _brick.DirectCommand.TurnMotorAtPowerForTimeAsync(output, control.PowerRatingMovement, 0, (uint)control.TimeToMoveInSeconds * 1000, 0, false);
					}
				}
			}
		}

		private void SensorSettingClicked(object sender, RoutedEventArgs routedEventArgs)
		{
			var control = sender as SensorDataControl;

			if (control != null)
			{
				SensorSettings.SaveSettings += SensorSettings_SaveSettings;

				_selectedSensorControl = control;

				SensorSettings.Show(
					control.BrickInputPort,
					_brick.Ports[control.BrickInputPort].Type,
					_brick.Ports[control.BrickInputPort].Mode);
			}
		}

		void SensorSettings_SaveSettings(object sender, SensorSettingsEventArgs e)
		{
			if (_selectedSensorControl == null)
				return;

			SensorSettings.SaveSettings -= SensorSettings_SaveSettings;

			_brick.Ports[_selectedSensorControl.BrickInputPort].SetMode(e.SensorMode);
			_selectedSensorControl.UpdateUx();

			_selectedSensorControl = null;
		}

		private ICommunication CreateConnection()
		{
			ICommunication returnType = null;

			switch (ConnControl.GetConnectionType())
			{
				case ConnectionType.Usb:
					returnType = new UsbCommunication();
					break;
				case ConnectionType.Bluetooth:
					returnType = new BluetoothCommunication();
					break;
				case ConnectionType.WiFi:
					returnType = new NetworkCommunication(ConnControl.GetIpAddress());
					break;
			}

			return returnType;
		}

		private async void PlayToneClick(object sender, EventArgs e)
		{
			await _brick.DirectCommand.PlayToneAsync(2, 1000, 400);
		}
    }
}
