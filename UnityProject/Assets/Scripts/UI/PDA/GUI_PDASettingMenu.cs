﻿using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDASettingMenu : NetPage
	{
		[SerializeField] private GUI_PDA controller;

		[SerializeField] public NetLabel reset;

		[SerializeField] public NetLabel input;

		private bool selectionCheck; // a simple variable to make sure the PDA asks the player to confirm the reset

		//Logic pushed to controller for safety checks, cant have client fucking shit up
		public void SetNotificationSound(string notificationString)
		{
			if (controller.TestForUplink(notificationString) != true)
			{
				Debug.LogError("Sounds not implimented");
			}
			input.SetValueServer("");
		}
		/// <summary>
		/// Tells the PDA to unregister the name and tell the messenger that it is "unknown"
		/// </summary>
		public void FactoryReset()
		{
			if (selectionCheck)
			{
				selectionCheck = false;
				reset.SetValueServer("Factory Reset");
				controller.ResetPda();

			}
			else
			{
				selectionCheck = true;
				reset.SetValueServer("Click again to confirm factory reset");
			}
		}
		// Supposed to handle the changing of UI themes, might drop this one
		public void Themes()
		{
			Debug.LogError("UI themes are not implimented yet!");
		}
	}
}