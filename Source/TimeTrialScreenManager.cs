using MenuBuddy;
using GameTimer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic; 
using Ouya.Console.Api;
using Ouya.Csharp;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Xna.Framework.GamerServices;

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

		private Task<IList<Product>> TaskRequestProducts = null;
		private Task<bool> TaskRequestPurchase = null;
		private Task<string> TaskRequestGamer = null;
		private Task<IList<Receipt>> TaskRequestReceipts = null;

		/// <summary>
		/// For purchases all transactions need a unique id
		/// </summary>
		private string m_uniquePurchaseId = string.Empty;

		/// <summary>
		/// Whether or not we already checked all the receipts
		/// </summary>
		private bool ReceiptsChecked = false;
		private bool GamerChecked = false;
		private bool PurchasablesChecked = false;

		#endregion //Member Variables

		#region Properties

		/// <summary>
		/// The purchase facade.
		/// </summary>
		public OuyaFacade PurchaseFacade { get; set; }

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
			//always start in trial mode
			Guide.IsTrialMode = true;
			TrialLength = 300.0f;

			//start the countdown timer
			m_TrialModeTimer.Start(TrialLength);

			//Get the list of purchasable items
			this.PurchaseFacade = purchaseFacade;
			TaskRequestProducts = PurchaseFacade.RequestProductListAsync(purchasables);
		}

		/// <summary>
		/// Initializes the screen manager component.
		/// </summary>
		public override void Initialize()
		{
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
			m_uniquePurchaseId = string.Empty;
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

			//if 3 seconds have passed, query the player data to see if they've bought the game or not
			if ((m_TrialModeTimer.PreviousTime() <= 3.0f) && (m_TrialModeTimer.CurrentTime > 3.0f))
			{
				//get the player uuid
				Debug.WriteLine("Requesting gamer uuid...");
				TaskRequestGamer = PurchaseFacade.RequestGamerUuidAsync();


				//get the receipts
				Debug.WriteLine("Requesting receipts...");
				TaskRequestReceipts = PurchaseFacade.RequestReceiptsAsync();
			}

			//Check on that request products task...
			if ((null != TaskRequestProducts) && !PurchasablesChecked)
			{
				AggregateException exception = TaskRequestProducts.Exception;
				if (null != exception)
				{
					Debug.WriteLine(string.Format("Request Products has exception. {0}", exception));
					TaskRequestProducts.Dispose();
					TaskRequestProducts = null;
				}
				else if (Guide.IsTrialMode)
				{
					if (TaskRequestProducts.IsCanceled)
					{
						Debug.WriteLine("Request Products has cancelled.");
						PurchasablesChecked = true;
					}
					else if (TaskRequestProducts.IsCompleted)
					{
						Debug.WriteLine("Request Products has completed with results.");
						if (null != TaskRequestProducts.Result)
						{
							CheckReceipt(TaskRequestProducts.Result.Count);
						}
						PurchasablesChecked = true;
					}
				}
			}

			//Check on that purchase task...
			if (null != TaskRequestPurchase)
			{
				AggregateException exception = TaskRequestPurchase.Exception;
				if (null != exception)
				{
					Debug.WriteLine(string.Format("Request Purchase has exception. {0}", exception));
					TaskRequestPurchase.Dispose();
					TaskRequestPurchase = null;
					ClearPurchaseId();
				}
				else if (Guide.IsTrialMode)
				{
					if (TaskRequestPurchase.IsCanceled)
					{
						Debug.WriteLine("Request Purchase has cancelled.");
						TaskRequestPurchase = null;
						ClearPurchaseId(); //clear the purchase id
					}
					else if (TaskRequestPurchase.IsCompleted)
					{
						if (TaskRequestPurchase.Result)
						{
							//TODO: does this mean they were able to buy it?
							Debug.WriteLine("Request Purchase has completed succesfully.");
						}
						else
						{
							Debug.WriteLine("Request Purchase has completed with failure.");
						}
						TaskRequestPurchase = null;
						ClearPurchaseId(); //clear the purchase id
					}
				}
			}

			//Check on our receipt task...
			if ((null != TaskRequestReceipts) && !ReceiptsChecked)
			{
				//Did it blow up?  Clear it out to prevent killing the app.
				AggregateException exception = TaskRequestReceipts.Exception;
				if (null != exception)
				{
					Debug.WriteLine(string.Format("Request Receipts has exception. {0}", exception));
					TaskRequestReceipts.Dispose();
					TaskRequestReceipts = null;
				}
				else if (Guide.IsTrialMode)
				{
					//If it is still trial mode, check if that thing has completed.
					if (TaskRequestReceipts.IsCanceled)
					{
						Debug.WriteLine("Request Receipts has cancelled.");
						ReceiptsChecked = true;
					}
					else if (TaskRequestReceipts.IsCompleted)
					{
						//Ok, the receipts task has come back with an answer.
						Debug.WriteLine("Request Receipts has completed.");
						if (null != TaskRequestReceipts.Result)
						{
							CheckReceipt(TaskRequestReceipts.Result.Count);
						}
						ReceiptsChecked = true;
					}
				}
			}

			// touch exception property to avoid killing app
			if ((null != TaskRequestGamer) && !GamerChecked)
			{
				AggregateException exception = TaskRequestGamer.Exception;
				if (null != exception)
				{
					Debug.WriteLine(string.Format("Request Gamer UUID has exception. {0}", exception));
					TaskRequestGamer.Dispose();
					TaskRequestGamer = null;
				}
				else if (Guide.IsTrialMode)
				{
					//If it is still trial mode, check if that thing has completed.
					if (TaskRequestGamer.IsCanceled)
					{
						Debug.WriteLine("Request Gamer UUID has cancelled.");
						GamerChecked = true;
					}
					else if (TaskRequestGamer.IsCompleted)
					{
						//ok, the gamer task cam back with an answer...
						Debug.WriteLine("Request Gamer UUID has completed.");
//						if (null != TaskRequestReceipts &&
//						    null != TaskRequestReceipts.Result)
//						{
//							Debug.WriteLine("Trying to CheckReceipt...");
//							CheckReceipt(TaskRequestReceipts.Result.Count);
//						}

						GamerChecked = true;
					}
				}
			}

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
		/// Got a message back from Ouya... check the receipt, has the player bought the game already
		/// </summary>
		/// <param name="receiptIndex">Receipt index.</param>
		public void CheckReceipt(int itemIndex)
		{
			//If we've already done this check, don't keep doing it
			if (ReceiptsChecked)
			{
				return;
			}

			Debug.WriteLine("Checking receipt...");

			//Get the text from the receipt
			string strReceiptText = null;
			if ((null != TaskRequestReceipts) &&
			    (null == TaskRequestReceipts.Exception) &&
			    !TaskRequestReceipts.IsCanceled &&
			    TaskRequestReceipts.IsCompleted)
			{
				if  ((null != TaskRequestReceipts.Result) &&
				     (TaskRequestReceipts.Result.Count > 0))
				{
					Receipt receipt = TaskRequestReceipts.Result[itemIndex];
					strReceiptText = receipt.Identifier;
					Debug.WriteLine(string.Format("The receipt item is {0}", strReceiptText));
				}
			}

			Debug.WriteLine("Checking purchasable...");

			//Get teh text from the purchasable item
			string strPurchasableItem = null;
			if (null != TaskRequestProducts &&
			    null == TaskRequestProducts.Exception &&
			    !TaskRequestProducts.IsCanceled &&
			    TaskRequestProducts.IsCompleted &&
			    null != TaskRequestProducts.Result &&
			    TaskRequestProducts.Result.Count > 0)
			{
				Product product = TaskRequestProducts.Result[itemIndex];
				strPurchasableItem = product.Identifier;

				Debug.WriteLine(string.Format("The purchasable item is {0}", strPurchasableItem));
			}

			if (!string.IsNullOrEmpty(strReceiptText) &&
				!string.IsNullOrEmpty(strPurchasableItem))
			{
				if (strReceiptText == strPurchasableItem)
				{
					//ok, we got the purchasable item and the receipt for it, so trial mode is OVER
					Debug.WriteLine("Trial mode is over!");
					Guide.IsTrialMode = false;
				}
				else
				{
					Debug.WriteLine("Checked receipts, and player has not purchased.");
				}
			}
		}

		/// <summary>
		/// User selected an item to try and buy the full game
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		public void PurchaseFullVersion(object sender, PlayerIndexEventArgs e)
		{
			if (Guide.IsTrialMode)
			{
				if (null != TaskRequestProducts &&
					null == TaskRequestProducts.Exception &&
					!TaskRequestProducts.IsCanceled &&
					TaskRequestProducts.IsCompleted)
				{
					Product product = TaskRequestProducts.Result[0];
					if (string.IsNullOrEmpty(m_uniquePurchaseId))
					{
						m_uniquePurchaseId = Guid.NewGuid().ToString().ToLower();
					}
					TaskRequestPurchase = PurchaseFacade.RequestPurchaseAsync(product, m_uniquePurchaseId);
				}
			}
		}

		#endregion //Public Methods
	}
}