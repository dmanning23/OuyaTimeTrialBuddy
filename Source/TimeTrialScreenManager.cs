using GameTimer;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Ouya.Console.Api;
using OuyaPurchaseHelper;
using System.Collections.Generic;
using TrialModeBuddy;

/*
 * 
 * This class assumes that you have already initialized your application key in the main Activty class
 * You get the application key from Ouya.tv when you register your game.
 * Put the following in Activity.Create:
 * 
			//Get the application key from the .der file that was supplied on ouya.tv
            byte[] applicationKey = null;
            using (var stream = Resources.OpenRawResource(Resource.Raw.key))
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    applicationKey = ms.ToArray();
                }
            }

			//initialize the ouya purchase facade
            PurchaseFacade = OuyaFacade.Instance;
            PurchaseFacade.Init(this, DEVELOPER_ID, applicationKey);
            
 * */

namespace OuyaTimeTrialBuddy
{
	public abstract class TimeTrialScreenManager : ScreenManager
	{
		#region Member Variables

		private CountdownTimer m_TrialModeTimer = new CountdownTimer();

		protected OuyaPurchaseBuddy buddy;

		#endregion //Member Variables

		#region Properties

		/// <summary>
		/// Gets or sets the length of the trial mode, in seconds.
		/// Defaults to 5 minutes
		/// </summary>
		/// <value>The length of the trial.</value>
		public float TrialLength { get; set; }

		#endregion //Properties

		#region Initialization

		/// <summary>
		/// Constructs a new screen manager component.
		/// </summary>
		public TimeTrialScreenManager(Game game, 
		                     string strTitleFont, 
		                     string strMenuFont, 
		                     string strMessageBoxFont,
		                     string strMenuChange,
		                     string strMenuSelect,
		                     IList<Purchasable> purchasables,
		                     OuyaFacade purchaseFacade) : 
			base(game, strTitleFont, strMenuFont, strMessageBoxFont, strMenuChange, strMenuSelect)
		{
			buddy = new OuyaPurchaseBuddy(game, purchasables, purchaseFacade, "RoboJets_FullGame");

			//start the countdown timer
			TrialLength = 150.0f;
			m_TrialModeTimer.Start(TrialLength);
		}

		#endregion //Initialization

		#region Update and Draw

		/// <summary>
		/// Allows each screen to run logic.
		/// </summary>
		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			//update the trial mode timer
			m_TrialModeTimer.Update(gameTime);

			//if 3 seconds have passed, query the player data to see if they've bought the game or not
			if ((m_TrialModeTimer.PreviousTime() <= 2.0f) && (m_TrialModeTimer.CurrentTime > 2.0f))
			{
				buddy.RequestReceipts();
			}

			buddy.Update();

			//is trial mode out of time?
			AddPurchaseScreen();
		}

		#endregion //Update and Draw

		#region Public Methods

		/// <summary>
		/// Adds a new screen to the screen manager.
		/// </summary>
		public override void AddScreen(GameScreen screen, PlayerIndex? controllingPlayer)
		{
			base.AddScreen(screen, controllingPlayer);

			//is trial mode out of time?
			AddPurchaseScreen();
		}

		/// <summary>
		/// Removes a screen from the screen manager. You should normally
		/// use GameScreen.ExitScreen instead of calling this directly, so
		/// the screen can gradually transition off rather than just being
		/// instantly removed.
		/// </summary>
		public override void RemoveScreen(GameScreen screen)
		{
			base.RemoveScreen(screen);

			//is trial mode out of time?
			AddPurchaseScreen();
		}

		/// <summary>
		/// Check if we are in trial mode and time has run out.
		/// If those conditions are true, pop up a purchase screen.
		/// </summary>
		private void AddPurchaseScreen()
		{
			//is trial mode out of time?
			if (Guide.IsTrialMode && (0.0f >= m_TrialModeTimer.RemainingTime()))
			{
				//is there already purchase screen in the stack?
				foreach (GameScreen screen in Screens)
				{
					if (screen is PurchaseScreen)
					{
						//There is already a purchase screen on the stack 
						return;
					}
				}

				//add a Purchase screen
				AddScreen(new PurchaseScreen(), null);
			}
		}

		/// <summary>
		/// User selected an item to try and buy the full game
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		public virtual void PurchaseFullVersion(object sender, PlayerIndexEventArgs e)
		{
			buddy.PurchaseFullVersion();
		}

		/// <summary>
		/// Sets the trial mode flag.
		/// this method gets called when:
		/// 	the player purchases the game
		/// 	we have verified that they already purchased 
		/// 	the ouya service gets back to us that they have not purchased
		/// </summary>
		/// <param name="IsTrialMode">If set to <c>true</c> is trial mode.</param>
		public virtual void SetTrialMode(bool bIsTrialMode)
		{
			buddy.SetTrialMode(bIsTrialMode);
		}

		#endregion //Public Methods
	}
}