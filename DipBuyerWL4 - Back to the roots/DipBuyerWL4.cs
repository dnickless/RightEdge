#define LONG
//#define SHORT
//#define DEBUG
//#define OUTPUT
#define BACKTEST

#region Using statements
using System;
using System.Drawing;
using System.Collections.Generic;
using RightEdge.Common;
using RightEdge.Indicators;
using NLog;
#endregion

#region System class
public class MySystem : MySystemBase
{
	public override void Startup()
	{
		// This will override whatever is set in the Trading System properties.
        PositionManager.ProfitTarget = Convert.ToDouble(SystemParameters["takeprofit"]);
		PositionManager.ProfitTargetType = TargetPriceType.RelativeRatio;

#if BACKTEST
		PositionManager.Allocation = 1000;
		PositionManager.AllocationType = PositionAllocationType.FixedValue;
#else
		PositionManager.Allocation = Convert.ToDouble(SystemParameters["allocation"]);
		PositionManager.AllocationType = PositionAllocationType.Percentage;
#endif

		//PositionManager.StopLoss = Convert.ToDouble(SystemParameters["stoploss"]);
		//PositionManager.StopLossType = TargetPriceType.RelativeRatio;
		
		// Here is your time out exit value.  This will override whatever
		// is set in the Trading System properties.
		PositionManager.BarCountExit = Convert.ToInt32(SystemParameters["exitday"]);
	}
}
#endregion

public class MySymbolScript : MySymbolScriptBase
{
	private const double FEES_PER_TRADE = 0.004;
	private const double MAX_FEES_PERCENT_OF_VALUE = 0.005;
	
    Logger logger;
	
	private TimeSpan lastMinuteOfTheDay = new TimeSpan(0, 15, 50, 0, 0);
	
#if LONG
#if DEBUG
	public UserSeries KBuy;
#endif
	bool tracingDown;
	double percentageUp;
	double kBuy;
  	double xDown_Up;
  	double xDown_Down;
	double indexDown1;
	double indexDown2;
	double indexDown3;
	double indexDown4;
	double xDown;
#endif

#if SHORT
#if DEBUG
	public UserSeries KSell;
#endif
	bool tracingUp;
	double kSell;
  	double xUp_Up;
  	double xUp_Down;
	double indexUp1;
	double indexUp2;
	double indexUp3;
	double indexUp4;
	double xUp;
#endif
	
	bool validParameters = true;
	
	MACD macd9;
	TEMA ema3;
	
	private delegate string GetOutputDelegate();
	
	private void Output(GetOutputDelegate GetOutput)
	{
#if OUTPUT
		OutputMessage(GetOutput());
#endif
	}
	
	private delegate bool CheckProfitabilityIncludingFeesDelegate();
	CheckProfitabilityIncludingFeesDelegate CheckProfitabilityIncludingFees;
	
	private bool CheckProfitability(double size)
	{
		var price = Close.Current;
		var value = size * price;
		var maxFees = value * MAX_FEES_PERCENT_OF_VALUE; // fees are capped in IB
		var calcFees = size * FEES_PER_TRADE;
		var realFees = Math.Min(maxFees, calcFees);
		var profitThreshold = value * profitPerTradeThreshold;
		var deemedProfitable = realFees < profitThreshold;
		
		Output(delegate() { return string.Format("Value: {0} * Price: {1} = Size: {2}", value, price, size); });
		Output(delegate() { return string.Format("Fees (Max/Calc/Real): {0}7{1}/{2}", maxFees, calcFees, realFees); });
		Output(delegate() { return string.Format("Profit threshold: {0}", profitThreshold); });
		Output(delegate() { return string.Format("Deemed profitable: {0}", deemedProfitable); });
		
		return deemedProfitable;
	}	
	
	double profitPerTradeThreshold;
	
	TEMA minuteBarEMAShortIn;
	TEMA minuteBarEMALongIn;
	EMA minuteBarEMAShortOut;
	EMA minuteBarEMALongOut;
	
	Frequency dailyBarFrequency;
	Frequency smallBarFrequency;
	
	public override void Startup()
	{
//		var ds = new RightEdge.DataStorage.BinaryDataStore();
//		ds.DataDirectory = @"C:\Users\Daniel Hegener\AppData\Roaming\Yye Software\RightEdge\2010.1.0.0\Data Store";
//		var bs = ds.GetBarStorage(new SymbolFreq(Symbol, (int)BarFrequency.FiveMinute));
//		bs.Delete(new DateTime(2014, 2, 10), DateTime.MaxValue);
		
		CommonGlobals.SystemRunUpdateRate = TimeSpan.FromSeconds(5);
		
		logger = LogManager.GetLogger(Symbol.Name);
		
		// Get minute frequency and subscribe to new bars
		dailyBarFrequency = GetFrequency(BarFrequency.Daily);
		dailyBarFrequency.NewBar += NewDailyBar;
		
		smallBarFrequency = GetFrequency(SystemData.FrequencyManager.MainFrequency);
		smallBarFrequency.NewBar += NewSmallBar;
		
		profitPerTradeThreshold = Convert.ToDouble(SystemParameters["profitthreashold"]);
		
		if(PositionManager.AllocationType == PositionAllocationType.FixedValue)
		{
			CheckProfitabilityIncludingFees = delegate()
			{ 
				var size = (long)(PositionManager.Allocation / Close.Current);
				return CheckProfitability(size);
			};
		}
		else if(PositionManager.AllocationType == PositionAllocationType.FixedSize)
		{
			CheckProfitabilityIncludingFees = delegate()
			{ 
				return CheckProfitability(PositionManager.Allocation);
			};
		}
		else if(PositionManager.AllocationType == PositionAllocationType.Percentage)
		{
			CheckProfitabilityIncludingFees = delegate()
			{ 
				var value = SystemData.CurrentCapital * PositionManager.Allocation / 100d;
				var size = (long)(value / Close.Current);
				return CheckProfitability(size);
			};
		}
		
		SystemData.CreateTicksFromBars = false;
		
#if DEBUG
#else
		//Bars.MaxLookBack = 100;
		SystemData.SystemHistory.BuyAndHoldStatistics.Enabled = false;
		SystemData.SystemHistory.LongStatistics.Enabled = false;
		SystemData.SystemHistory.ShortStatistics.Enabled = false;
		//SystemData.SystemHistory.SystemStatistics.Enabled = false;
#endif
		macd9 = new MACD(12, 26, dailyBarFrequency.Close);
		SystemData.IndicatorManager.SetFrequency(macd9, dailyBarFrequency);
		macd9.ChartSettings.Color = Color.AliceBlue;
		ema3 = new TEMA(3, dailyBarFrequency.Low);
		SystemData.IndicatorManager.SetFrequency(ema3, dailyBarFrequency);
		ema3.ChartSettings.Color = Color.AntiqueWhite;
		
#if DEBUG
#if LONG
		KBuy = new UserSeries();
		KBuy.SetFrequency(dailyBarFrequency);
		KBuy.ChartSettings.Color = Color.Red;
#endif
		
#if SHORT
		KSell = new UserSeries();
		KSell.SetFrequency(dailyBarFrequency);
		KSell.ChartSettings.Color = Color.Green;
#endif
#endif

#if LONG
		percentageUp = SystemParameters["percentageUp"];
		indexDown1 = SystemParameters["indexdown1"];
		indexDown2 = SystemParameters["indexdown2"];
		indexDown3 = SystemParameters["indexdown3"];
		indexDown4 = SystemParameters["indexdown4"];
		xDown = SystemParameters["xdown"];
		if ((xDown + indexDown1 + indexDown2 + indexDown3 + indexDown4) > 1d)
		{
			 validParameters = false;
		}
#endif

		var emaShortIn = SystemParameters["emaShortIn"];
		var emaLongIn = SystemParameters["emaLongIn"];
		var emaShortOut = SystemParameters["emaShortOut"];
		var emaLongOut = SystemParameters["emaLongOut"];
		if (emaShortIn >= emaLongIn || emaShortOut >= emaLongOut)
		{
			 validParameters = false;
		}

		
#if SHORT
		indexUp1 = SystemParameters["indexup1"];
		indexUp2 = SystemParameters["indexup2"];
		indexUp3 = SystemParameters["indexup3"];
		indexUp4 = SystemParameters["indexup4"];
		xUp = SystemParameters["xup"];
		if ((xUp - indexUp1 - indexUp2 - indexUp3 - indexUp4) < 1d)
		{
			 validParameters = false;
		}
#endif

		minuteBarEMAShortIn = new TEMA(Convert.ToInt32(emaShortIn), smallBarFrequency.Close);
		SystemData.IndicatorManager.SetFrequency(minuteBarEMAShortIn, smallBarFrequency);
		minuteBarEMAShortIn.ChartSettings.Color = Color.Aqua;
		minuteBarEMALongIn = new TEMA(Convert.ToInt32(emaLongIn), smallBarFrequency.Close);
		SystemData.IndicatorManager.SetFrequency(minuteBarEMALongIn, smallBarFrequency);
		minuteBarEMALongIn.ChartSettings.Color = Color.Aquamarine;
		minuteBarEMAShortOut = new EMA(Convert.ToInt32(emaShortOut), smallBarFrequency.Close);
		SystemData.IndicatorManager.SetFrequency(minuteBarEMAShortOut, smallBarFrequency);
		minuteBarEMAShortOut.ChartSettings.Color = Color.Azure;
		minuteBarEMALongOut = new EMA(Convert.ToInt32(emaLongOut), smallBarFrequency.Close);
		SystemData.IndicatorManager.SetFrequency(minuteBarEMALongOut, smallBarFrequency);
		minuteBarEMALongOut.ChartSettings.Color = Color.Beige;
	}

	Position openPosition;
	
    public void NewSmallBar(object sender, SingleBarEventArgs args)
    {
		if (openPosition != null)
		{
			if(openPosition.OpenDate < args.BarEndTime.Date)
			{
				if (minuteBarEMAShortOut.Current < minuteBarEMALongOut.Current)
				{
#if OUTPUT
					OutputMessage("Closing position");
#endif
					openPosition.CloseAtMarket();
					openPosition = null;
				}
#if OUTPUT
				else
				{
					OutputMessage("Staying in the market");
				}
#endif
			}
		}
		else if(minuteBarsEnabled)
		{
#if LONG
			double currentLow = args.Bar.Low;
			
			if (!tracingDown && currentLow < kBuy)
			{
#if OUTPUT
				OutputMessage("Tracing down");
#endif
				tracingDown = true;
			}
			
			if (tracingDown)
			{
				if (minuteBarEMAShortIn.Current > minuteBarEMALongIn.Current && args.BarEndTime.TimeOfDay <= lastMinuteOfTheDay)
				{
					double positionWeightFactorBasedOnStatistics = 0.0;
					if (smallBarFrequency.Open.LookBack(0) < smallBarFrequency.Close.LookBack(0))
					{
						positionWeightFactorBasedOnStatistics = 1.0;
						if (smallBarFrequency.Open.LookBack(1) > smallBarFrequency.Close.LookBack(1))
						{
							 positionWeightFactorBasedOnStatistics = 2.0;
						}
					}
					
					// normally, we would buy now.
					// based on Excel statistics, we can predict to an extent, though, whether this deal would be a good one or not.
					// so we only want to make certain deals
					var enableBuyBasedOnStatistics = smallBarFrequency.Open.LookBack(0) < smallBarFrequency.Close.LookBack(0)/* && smallBarFrequency.Open.LookBack(1) > smallBarFrequency.Close.LookBack(1)*/;
					if (enableBuyBasedOnStatistics)
					{
#if OUTPUT
						OutputMessage("Buying kb " + kBuy + " c " + args.Bar.Close);
#endif
						PositionSettings settings = new PositionSettings();
						settings.BarsValid = 1;
						settings.PositionType = PositionType.Long;
#if BACKTEST
						settings.OrderType = OrderType.Limit;
						settings.LimitPrice = args.Bar.Close;
#else
						settings.OrderType = OrderType.Market;
#endif
						OpenPosition(settings).Tag = args.Bar.Close;
					}
					
					// we do not want to trade more than once a day
					minuteBarsEnabled = false;
				}
#if OUTPUT
				else
				{
					OutputMessage("too early to buy");
				}
#endif
			}
#endif
		}
    }
	
	bool minuteBarsEnabled;
	
    public void NewDailyBar(object sender, SingleBarEventArgs args)
	{
		minuteBarsEnabled = false;
		
		if (!validParameters)
		{
			 return;
		}
		
		// we do not want to execute trades where we are likely to lose on the commission/fees
//		if (!CheckProfitabilityIncludingFees())
//		{
//			return;
//		}
		
		// Need a couple of bars before we can trade
		if (dailyBarFrequency.Bars.Count < 26)
		{
			return;
		}
		
		BarData yesterday = dailyBarFrequency.Bars.LookBack(1);
		BarData yesterday3 = dailyBarFrequency.Bars.LookBack(3);
		BarData today = dailyBarFrequency.Bars.Current;
		
#if LONG
		int c1down=0;
		
		if (yesterday.Close < yesterday.Open) c1down++;   // yesterday was negative
		if (today.Close < today.Open) c1down++;           // today was negative
		if (today.Close < yesterday.Close) c1down++;      // today went further down than yesterday
		if (yesterday.Close < yesterday3.Close) c1down++; // yesterday went further down than three days ago
		
		double multdown = xDown;
		
		if (c1down <= 1) multdown += indexDown1;
		if (c1down <= 2) multdown += indexDown2;
		if (c1down <= 3) multdown += indexDown3;
		if (c1down <= 4) multdown += indexDown4;
		
		xDown_Up = ema3.Current * multdown;
		xDown_Down = ema3.Current * xDown;
		
		if (xDown_Up < xDown_Down)
		{
			 return;
		}
		
		kBuy = ((xDown_Up-xDown_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xDown_Down;
#if DEBUG
		KBuy.Current=kBuy;
#endif
		
		minuteBarsEnabled = true;
		tracingDown = false;
#endif
		
#if SHORT
		tracingUp = false;
		int c1up=0;
		
		if (yesterday.Close > yesterday.Open) c1up++;   // yesterday was positive
		if (today.Close > today.Open) c1up++;			// today was positive
		if (today.Close > yesterday.Close) c1up++;		// today went further up than yesterday
		if (yesterday.Close > yesterday3.Close) c1up++; // yesterday went further up than three days ago
		
		double multup = xUp;
		
		if (c1up <= 1) multup -= indexUp1;
		if (c1up <= 2) multup -= indexUp2;
		if (c1up <= 3) multup -= indexUp3;
		if (c1up <= 4) multup -= indexUp4;
		
		xUp_Up = ema3.Current * multup;
		xUp_Down = ema3.Current * xUp;
		
		kSell = ((xUp_Up-xUp_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xUp_Down;
#if DEBUG
		KSell.Current=kSell;
#endif
		PositionSettings settings = new PositionSettings();
		settings.BarsValid = 1;
		settings.PositionType = PositionType.Short;
		settings.OrderType = OrderType.Limit;
		settings.LimitPrice = kSell;
		OpenPosition(settings);
#endif
	}
	
	string x;
	
	public override void OrderFilled(Position position, Trade trade) 
	{
		if (trade.TradeType != TradeType.OpenPosition)
		{
			openPosition = null;
#if OUTPUT
			logger.Debug(string.Join(";", new [] { x, position.OpenDate.ToShortDateString(), position.Tag.ToString(), Low.LookBack(1).ToString(), position.ExitPrice.ToString(), position.RealizedProfit.ToString() }));
			OutputMessage(string.Format("Time: {0} P&L: {2:0.000}% ({1:0})", SystemData.SystemStatistics.CurStat.CalculatedDate, SystemData.SystemStatistics.CurStat.NetProfit, SystemData.SystemStatistics.CurStat.NetProfitPct));
#endif
		}
		else
		{
			openPosition = position;
			PositionManager.SetTrailingStop(position.ID, Convert.ToDouble(SystemParameters["stoploss"]), TargetPriceType.RelativeRatio);
#if OUTPUT
			var f = smallBarFrequency;
			x = string.Join(";", new [] { f.Open.LookBack(0) < f.Close.LookBack(0) ? "POS0" : "NEG0", f.Open.LookBack(1) < f.Close.LookBack(1) ? "POS1" : "NEG1", f.Open.LookBack(2) < f.Close.LookBack(2) ? "POS2" : "NEG2", f.Open.LookBack(3) < f.Close.LookBack(3) ? "POS3" : "NEG3", f.Open.LookBack(4) < f.Close.LookBack(4) ? "POS4" : "NEG4", position.EntryPrice.SymbolPrice.ToString()});
			OutputMessage("Entry: " + string.Format("{0:0.###}", position.EntryPrice.SymbolPrice) + ", limit:" + string.Format("{0:0.###}", position.Tag) + ", difference: " + string.Format("{0:0.###}", (double)position.Tag - position.EntryPrice.SymbolPrice));
#endif
		}
	}
}