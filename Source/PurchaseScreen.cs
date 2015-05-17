using GameTimer;
using MenuBuddy;
using Microsoft.Xna.Framework;
using System;
using TrialModeBuddy;

namespace OuyaTimeTrialBuddy
{
	/// <summary>
	/// This is a special screen that forces the player to buy or exit the game
	/// </summary>
	public class PurchaseScreen : MessageBoxScreen
	{
		#region Members

		/// <summary>
		/// The _timer userd to make sure player stares at this screen for a few seconds rather than just skip past it
		/// </summary>
		private CountdownTimer _timer = new CountdownTimer();

		const string message = "Thank you for trying the trial mode of RoboJets\n" +
				"Would you like to buy the full version and continue playing?";

		#endregion //Members

		#region Initialization

		/// <summary>
		/// Constructor fills in the menu contents.
		/// </summary>
		public PurchaseScreen() : base(message, false)
		{
			Cancelled += MarketplaceDenied;
			Accepted += PurchaseFullVersion;

			//make the player stare at this screen for 2 seconds before they can quit
			_timer.Start(1.5f);
		}

		#endregion //Initialization

		#region Methods

		/// <summary>
		/// Updates the menu.
		/// </summary>
		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			//update the timers
			_timer.Update(gameTime);

			//First check the receipts
			TimeTrialScreenManager myScreenManager = ScreenManager as TimeTrialScreenManager;
			if (!Guide.IsTrialMode)
			{
				//Why are we here?
				myScreenManager.RemoveScreen(this);
			}

			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		#endregion //Methods

		#region Handle Input

		/// <summary>
		/// Event handler for when the Purchase menu entry is selected.
		/// </summary>
		private void PurchaseFullVersion(object sender, PlayerIndexEventArgs e)
		{
			if (0.0f >= _timer.RemainingTime())
			{
				TimeTrialScreenManager myScreenManager = ScreenManager as TimeTrialScreenManager;
				myScreenManager.PurchaseFullVersion(sender, e);
			}
		}

		/// <summary>
		/// Player don't want to buy the game, dump their ass back to the dashboard
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		private void MarketplaceDenied(object sender, PlayerIndexEventArgs e)
		{
			if (0.0f >= _timer.RemainingTime())
			{
				ScreenManager.Game.Exit();
			}
		}

		#endregion //Handle Input
	}
}