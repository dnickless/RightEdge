#define LONG
//#define SHORT
//#define DEBUG
//#define BACKTEST

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

		PositionManager.StopLoss = Convert.ToDouble(SystemParameters["stoploss"]);
		PositionManager.StopLossType = TargetPriceType.RelativeRatio;
		
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
	
#if LONG
#if DEBUG
	public UserSeries KBuy;
#endif
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
	
	public override void Startup()
	{
    CommonGlobals.SystemRunUpdateRate = TimeSpan.FromSeconds(5);

		logger = LogManager.GetLogger(Symbol.Name);
		
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
		macd9 = new MACD(12, 26, Close);
		ema3 = new TEMA(3, Low);
		
#if DEBUG
#if LONG
		KBuy = new UserSeries();
		KBuy.ChartSettings.Color = Color.Red;
#endif
		
#if SHORT
		KSell = new UserSeries();
		KSell.ChartSettings.Color = Color.Green;
#endif
#endif

#if LONG
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
	}

	public override void NewBar()
	{
		if (!validParameters)
		{
			 //return;
		}
		
		// we do not want to execute trades where we are likely to lose on the commission/fees
		if (!CheckProfitabilityIncludingFees())
		{
			return;
		}
		
		// Need a couple of bars before we can trade
		if (Bars.Count < 26)
		{
			return;
		}
		
		BarData yesterday = Bars.LookBack(1);
		BarData yesterday3 = Bars.LookBack(3);
		BarData today = Bars.Current;
		
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

		// only trade if yesterday was negative and today was positive
		//if (today.Close < today.Open && yesterday.Close < yesterday.Open)
		{
			PositionSettings settings = new PositionSettings();
			settings.BarsValid = 1;
			settings.PositionType = PositionType.Long;
//#if BACKTEST
      		settings.OrderType = OrderType.Limit;
      		settings.LimitPrice = kBuy;
//#else
//            settings.OrderType = OrderType.Market;
//#endif
			OpenPosition(settings).Tag = kBuy;
		}
#endif
		
#if SHORT
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
			//logger.Debug(string.Join(";", new [] { Open.LookBack(2) < Close.LookBack(2) ? "POS" : "NEG", Open.LookBack(3) < Close.LookBack(3) ? "POS" : "NEG", x, position.OpenDate.ToShortDateString(), position.Tag.ToString(), Low.LookBack(1).ToString(), position.ExitPrice.ToString(), position.RealizedProfit.ToString() }));
			logger.Debug("Time: {0} P&L: {2:0.000}% ({1:0})", SystemData.SystemStatistics.CurStat.CalculatedDate, SystemData.SystemStatistics.CurStat.NetProfit, SystemData.SystemStatistics.CurStat.NetProfitPct);
		}
		else
		{
			x = position.EntryPrice.SymbolPrice.ToString();
			OutputMessage("Entry: " + string.Format("{0:0.###}", position.EntryPrice.SymbolPrice) + ", limit:" + string.Format("{0:0.###}", position.Tag) + ", difference: " + string.Format("{0:0.###}", (double)position.Tag - position.EntryPrice.SymbolPrice));
		}
	}
}