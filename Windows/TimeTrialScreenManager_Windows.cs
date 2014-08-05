using MenuBuddy;
using GameTimer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic; 
using System.Threading.Tasks;
using System.Diagnostics;
using TrialModeBuddy;

namespace OuyaTimeTrialBuddy
{
	public abstract class TimeTrialScreenManager : ScreenManager
	{
		#region Member Variables

		private CountdownTimer m_TrialModeTimer = new CountdownTimer();

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
		                              string strMenuSelect) : 
			base(game, strTitleFont, strMenuFont, strMessageBoxFont, strMenuChange, strMenuSelect)
		{
#if OUYA
			//always start in trial mode
			Guide.IsTrialMode = true;
			TrialLength = 270.0f;
#else
			Guide.IsTrialMode = false;
#endif
		}

		/// <summary>
		/// Initializes the screen manager component.
		/// </summary>
		public override void Initialize()
		{
#if OUYA
			//start the countdown timer
			m_TrialModeTimer.Start(TrialLength);
#endif

			base.Initialize();
		}

		/// <summary>
		/// Load your graphics content.
		/// </summary>
		protected override void LoadContent()
		{
			base.LoadContent();
		}

		/// <summary>
		/// Unload your graphics content.
		/// </summary>
		protected override void UnloadContent()
		{
			base.UnloadContent();
		}

		void ClearPurchaseId()
		{
		}

		#endregion //Initialization

		#region Update and Draw

		/// <summary>
		/// Allows each screen to run logic.
		/// </summary>
		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			//If it is't trial mode, don't do any of this other stuff
			if (!Guide.IsTrialMode)
			{
				return;
			}

			//update the trial mode timer
			m_TrialModeTimer.Update(gameTime);

			//is trial mode out of time?
			AddPurchaseScreen();
		}

		/// <summary>
		/// Tells each screen to draw itself.
		/// </summary>
		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);
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
			if (Guide.IsTrialMode)
			{
				Console.WriteLine("Full Version Purchased!");
				Guide.IsTrialMode = false;
			}
		}

		/// <summary>
		/// Sets the trial mode flag.
		/// </summary>
		/// <param name="IsTrialMode">If set to <c>true</c> is trial mode.</param>
		public virtual void SetTrialMode(bool bIsTrialMode)
		{
			//no trial mode in windows
			Guide.IsTrialMode = false;
		}

		#endregion //Public Methods
	}
}

