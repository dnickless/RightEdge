#define LONG
//#define SHORT
//#define DEBUG

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

//		PositionManager.Allocation = 1000;
//		PositionManager.AllocationType = PositionAllocationType.FixedValue;
		PositionManager.Allocation = Convert.ToDouble(SystemParameters["allocation"]);
		PositionManager.AllocationType = PositionAllocationType.Percentage;

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
	private const string INDEX = "NDX";
	private const double FEES_PER_TRADE = 0.004;
	private const double MAX_FEES_PERCENT_OF_VALUE = 0.005;
	
    Logger logger;
	
#if LONG
#if DEBUG
	public UserSeries KBuy;
#endif
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
  	double xUp_Up;
  	double xUp_Down;
	double indexUp1;
	double indexUp2;
	double indexUp3;
	double indexUp4;
	double xUp;
#endif
	
	MACD macd9 = new MACD(12, 26);
	EMA ema3 = new EMA(3);
	UserSeries index = new UserSeries();
	
#if DEBUG
	SMA indexSma = new SMA(12);	
#endif
	
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
		var price = Bars.Current.Close;
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
		logger = LogManager.GetLogger(Symbol.Name);
		
		profitPerTradeThreshold = Convert.ToDouble(SystemParameters["profitthreashold"]);
		
		if(PositionManager.AllocationType == PositionAllocationType.FixedValue)
		{
			CheckProfitabilityIncludingFees = delegate()
			{ 
				var size = (long)(PositionManager.Allocation / Bars.Current.Close);
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
				var size = (long)(value / Bars.Current.Close);
				return CheckProfitability(size);
			};
		}
		
#if DEBUG
#else
		//Bars.MaxLookBack = 100;
		SystemData.CreateTicksFromBars = false;
		SystemData.SystemHistory.BuyAndHoldStatistics.Enabled = false;
		SystemData.SystemHistory.LongStatistics.Enabled = false;
		SystemData.SystemHistory.ShortStatistics.Enabled = false;
		//SystemData.SystemHistory.SystemStatistics.Enabled = false;
#endif		
		macd9.SetInputs(Close);
		ema3.SetInputs(Close);
		
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
#endif
		
#if SHORT
		indexUp1 = SystemParameters["indexup1"];
		indexUp2 = SystemParameters["indexup2"];
		indexUp3 = SystemParameters["indexup3"];
		indexUp4 = SystemParameters["indexup4"];
		xUp = SystemParameters["xup"];
#endif
		
#if DEBUG
		index.ChartSettings.Color = Color.Green;
		index.ChartSettings.ChartPaneName = "Index";
		indexSma.SetInputs(OtherSymbols[INDEX].Close);
		indexSma.ChartSettings.ChartPaneName = "Index";
#endif
	}

	public override void NewBar()
	{
		// we do not want to execute trades where we are likely to lose on the commission/fees
		
		if (!CheckProfitabilityIncludingFees())
		{
			return;
		}
		
		if(Symbol.Name == INDEX) return;
		
		// Need a couple of bars before we can trade
		if (Bars.Count < 26)
		{
			return;
		}
		
		index.Current = (OtherSymbols[INDEX].Close.Current);
		
		BarData yesterday = Bars.LookBack(1);
		BarData yesterday3 = Bars.LookBack(3);
		BarData today = Bars.Current;
		
#if LONG
		if (index.Current > index.LookBack(1))
		{
			int c1down=0;
			
			if (yesterday.Close < yesterday.Open) c1down++;   // yesterday was negative
			if (today.Close < today.Open) c1down++;           // today was negative
			if (today.Close < yesterday.Close) c1down++;      // today went further down than yesterday
			if (yesterday.Close < yesterday3.Close) c1down++; // yesterday went further down than three days ago
			
			double multdown=0;
			
			if (c1down <= 1) multdown=SystemParameters["indexdown1"];
			if (c1down == 2) multdown=SystemParameters["indexdown2"];
			if (c1down == 3) multdown=SystemParameters["indexdown3"];
			if (c1down == 4) multdown=SystemParameters["indexdown4"];
			
			xDown_Up=ema3.Current*multdown;
			xDown_Down=ema3.Current*SystemParameters["xdown"];
			
			double kBuy = ((xDown_Up-xDown_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xDown_Down;
#if DEBUG
			KBuy.Current=kBuy;
#endif
			
			PositionSettings settings = new PositionSettings();
			settings.BarsValid = 1;
			settings.PositionType = PositionType.Long;
			settings.OrderType = OrderType.Limit;
			settings.LimitPrice = kBuy;
			OpenPosition(settings);
		}
#endif
		
#if SHORT
		if (index.Current < index.LookBack(1))
		{
			int c1up=0;
			
			if (yesterday.Close > yesterday.Open) c1up++;   // yesterday was positive
			if (today.Close > today.Open) c1up++;			// today was positive
			if (today.Close > yesterday.Close) c1up++;		// today went further up than yesterday
			if (yesterday.Close > yesterday3.Close) c1up++; // yesterday went further up than three days ago
			
			double multup=0;
			
			if (c1up <= 1) multup=indexUp1;
			if (c1up == 2) multup=indexUp2;
			if (c1up == 3) multup=indexUp3;
			if (c1up == 4) multup=indexUp4;
			
			xUp_Up=ema3.Current*multup;
			xUp_Down=ema3.Current*xUp;
			
			double kSell = ((xUp_Up-xUp_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xUp_Down;
#if DEBUG
			KSell.Current=kSell;
#endif

			PositionSettings settings = new PositionSettings();
			settings.BarsValid = 1;
			settings.PositionType = PositionType.Short;
			settings.OrderType = OrderType.Limit;
			settings.LimitPrice = kSell;
			OpenPosition(settings);
		}
#endif
	}
	
	public override void OrderFilled(Position position, Trade trade) 
	{
		if (trade.TradeType != TradeType.OpenPosition)
		{
			logger.Debug("Time: {0} P&L: {2:0.000}% ({1:0})", SystemData.SystemStatistics.CurStat.CalculatedDate, SystemData.SystemStatistics.CurStat.NetProfit, SystemData.SystemStatistics.CurStat.NetProfitPct);
		}
	}
}