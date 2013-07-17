using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
using GameTimer;
using MenuBuddy;

namespace OuyaTimeTrialBuddy
{
	/// <summary>
	/// This is a special screen that forces the player to buy or exit the game
	/// </summary>
	class PurchaseScreen : MessageBoxScreen
	{
		#region Members

		/// <summary>
		/// The _timer userd to make sure player stares at this screen for a few seconds rather than just skip past it
		/// </summary>
		private CountdownTimer _timer = new CountdownTimer();

		const string message = "Thank you for trying the trial mode of Opposites\n" +
				"Would you like to buy the full version and continue playing?\n\n" +
				"\nO button: Buy Game\nA button: Exit Game";

		#endregion //Members

		#region Initialization

		/// <summary>
		/// Constructor fills in the menu contents.
		/// </summary>
		public PurchaseScreen() : base(message, false)
		{
			Cancelled += MarketplaceDenied;
			TimeTrialScreenManager myScreenManager = ScreenManager as TimeTrialScreenManager;
			Accepted += myScreenManager.PurchaseFullVersion;

			TransitionOnTime = TimeSpan.FromSeconds(1.0f);

			//make the player stare at this screen for 2 seconds before they can quit
			_timer.Start(1.0f);
		}

		#endregion //Initialization

		#region Methods

		public override void LoadContent()
		{
		}

		/// <summary>
		/// Updates the menu.
		/// </summary>
		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			//update the timers
			_timer.Update(gameTime);

			//First check the receipts
			TimeTrialScreenManager myScreenManager = ScreenManager as TimeTrialScreenManager;
			if (!myScreenManager.TrialMode)
			{
				//Why are we here?
				myScreenManager.RemoveScreen(this);
			}

			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		#endregion //Methods

		#region Handle Input

		/// <summary>
		/// Player don't want to buy the game, dump their ass back to the dashboard
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void MarketplaceDenied(object sender, PlayerIndexEventArgs e)
		{
			ScreenManager.Game.Exit();
		}

		#endregion //Handle Input
	}
}