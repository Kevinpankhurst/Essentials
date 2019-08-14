﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common;

namespace PepperDash.Essentials.Bridges
{
	public static class DisplayControllerApiExtensions
	{

		public static int InputNumber;
		public static IntFeedback InputNumberFeedback;
		public static List<string> InputKeys = new List<string>();
		public static void LinkToApi(this PepperDash.Essentials.Core.DisplayBase displayDevice, BasicTriList trilist, uint joinStart, string joinMapKey)
		{

				var joinMap = JoinMapHelper.GetJoinMapForDevice(joinMapKey) as DisplayControllerJoinMap;

				if (joinMap == null)
				{
					joinMap = new DisplayControllerJoinMap();
				}

				joinMap.OffsetJoinNumbers(joinStart);

				Debug.Console(1, "Linking to Trilist '{0}'",trilist.ID.ToString("X"));
				Debug.Console(0, "Linking to Bridge Type {0}", displayDevice.GetType().Name.ToString());

                trilist.StringInput[joinMap.Name].StringValue = displayDevice.GetType().Name.ToString();			

				var commMonitor = displayDevice as ICommunicationMonitor;
                if (commMonitor != null)
                {
                    commMonitor.CommunicationMonitor.IsOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline]);
                }

                InputNumberFeedback = new IntFeedback(() => { return InputNumber; });

                // Two way feedbacks
                var twoWayDisplay = displayDevice as PepperDash.Essentials.Core.TwoWayDisplayBase;
                if (twoWayDisplay != null)
                {
                    trilist.SetBool(joinMap.IsTwoWayDisplay, true);

                    twoWayDisplay.CurrentInputFeedback.OutputChange += new EventHandler<FeedbackEventArgs>(CurrentInputFeedback_OutputChange);


                    InputNumberFeedback.LinkInputSig(trilist.UShortInput[joinMap.InputSelect]);
                }

				// Power Off
				trilist.SetSigTrueAction(joinMap.PowerOff, () =>
					{
						InputNumber = 102;
						InputNumberFeedback.FireUpdate();
						displayDevice.PowerOff();
					});

				displayDevice.PowerIsOnFeedback.OutputChange += new EventHandler<FeedbackEventArgs>(PowerIsOnFeedback_OutputChange);
				displayDevice.PowerIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.PowerOff]);

				// PowerOn
				trilist.SetSigTrueAction(joinMap.PowerOn, () =>
					{
						InputNumber = 0;
						InputNumberFeedback.FireUpdate();
						displayDevice.PowerOn();
					});

				
				displayDevice.PowerIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn]);

				int count = 1;
				foreach (var input in displayDevice.InputPorts)
				{
					InputKeys.Add(input.Key.ToString());
					var tempKey = InputKeys.ElementAt(count - 1);
					trilist.SetSigTrueAction((ushort)(joinMap.InputSelectOffset + count), () => { displayDevice.ExecuteSwitch(displayDevice.InputPorts[tempKey].Selector); });
                    Debug.Console(2, displayDevice, "Setting Input Select Action on Digital Join {0} to Input: {1}", joinMap.InputSelectOffset + count, displayDevice.InputPorts[tempKey].Key.ToString());
					trilist.StringInput[(ushort)(joinMap.InputNamesOffset + count)].StringValue = input.Key.ToString();
					count++;
				}

                Debug.Console(2, displayDevice, "Setting Input Select Action on Analog Join {0}", joinMap.InputSelect);
				trilist.SetUShortSigAction(joinMap.InputSelect, (a) =>
				{
					if (a == 0)
					{
						displayDevice.PowerOff();
						InputNumber = 0;
					}
					else if (a > 0 && a < displayDevice.InputPorts.Count && a != InputNumber)
					{
						displayDevice.ExecuteSwitch(displayDevice.InputPorts.ElementAt(a - 1).Selector);
						InputNumber = a;
					}
					else if (a == 102)
					{
						displayDevice.PowerToggle();

					}
                    if (twoWayDisplay != null)
					    InputNumberFeedback.FireUpdate();
				});


                var volumeDisplay = displayDevice as IBasicVolumeControls;
                if (volumeDisplay != null)
                {
                    trilist.SetBoolSigAction(joinMap.VolumeUp, (b) => volumeDisplay.VolumeUp(b));
                    trilist.SetBoolSigAction(joinMap.VolumeDown, (b) => volumeDisplay.VolumeDown(b));
                    trilist.SetSigTrueAction(joinMap.VolumeMute, () => volumeDisplay.MuteToggle());

                    var volumeDisplayWithFeedback = volumeDisplay as IBasicVolumeWithFeedback;
                    if(volumeDisplayWithFeedback != null)
                    {
                        volumeDisplayWithFeedback.VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[joinMap.VolumeLevelFB]);
                        volumeDisplayWithFeedback.MuteFeedback.LinkInputSig(trilist.BooleanInput[joinMap.VolumeMute]);
                    }
                }
			}

		static void CurrentInputFeedback_OutputChange(object sender, FeedbackEventArgs e)
		{

			Debug.Console(0, "CurrentInputFeedback_OutputChange {0}", e.StringValue);

		}

		static void PowerIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
		{

			// Debug.Console(0, "PowerIsOnFeedback_OutputChange {0}",  e.BoolValue);
			if (!e.BoolValue)
			{
				InputNumber = 102;
				InputNumberFeedback.FireUpdate();

			}
			else
			{
				InputNumber = 0;
				InputNumberFeedback.FireUpdate();
			}
		}




	}
    public class DisplayControllerJoinMap : JoinMapBase
	{
        // Digital
        public uint PowerOff { get; set; }
        public uint PowerOn { get; set; }
        public uint IsTwoWayDisplay { get; set; }
        public uint VolumeUp { get; set; }
        public uint VolumeDown { get; set; }
        public uint VolumeMute { get; set; }
        public uint InputSelectOffset { get; set; }
		public uint ButtonVisibilityOffset { get; set; }
        public uint IsOnline { get; set; }

        // Analog
        public uint InputSelect { get; set; }
        public uint VolumeLevelFB { get; set; }

        // Serial
        public uint Name { get; set; }
        public uint InputNamesOffset { get; set; }


		public DisplayControllerJoinMap()
		{
			// Digital
			IsOnline = 50;
			PowerOff = 1;
			PowerOn = 2;
            IsTwoWayDisplay = 3;
            VolumeUp = 5;
            VolumeDown = 6;
            VolumeMute = 7;

			ButtonVisibilityOffset = 40;
            InputSelectOffset = 10;

			// Analog
            InputSelect = 11;
            VolumeLevelFB = 5;

            // Serial
            Name = 1;
            InputNamesOffset = 10;
        }

		public override void OffsetJoinNumbers(uint joinStart)
		{
			var joinOffset = joinStart - 1;

			IsOnline = IsOnline + joinOffset;
			PowerOff = PowerOff + joinOffset;
			PowerOn = PowerOn + joinOffset;
            IsTwoWayDisplay = IsTwoWayDisplay + joinOffset;
			ButtonVisibilityOffset = ButtonVisibilityOffset + joinOffset;
			Name = Name + joinOffset;
			InputNamesOffset = InputNamesOffset + joinOffset;
			InputSelectOffset = InputSelectOffset + joinOffset;

            InputSelect = InputSelect + joinOffset;

            VolumeUp = VolumeUp + joinOffset;
            VolumeDown = VolumeDown + joinOffset;
            VolumeMute = VolumeMute + joinOffset;
            VolumeLevelFB = VolumeLevelFB + joinOffset;
		}
	}
}