#define LONG
#define SHORT
//#define DEBUG

#region Using statements
using System;
using System.Drawing;
using System.Collections.Generic;
using RightEdge.Common;
using RightEdge.Indicators;
#endregion

#region System class
public class MySystem : MySystemBase
{
	public override void Startup()
	{
		//	Perform initialization or set system wide options here

		// Here is your profit target value, this will override whatever
		// is set in the Trading System properties.
        PositionManager.ProfitTarget = Convert.ToDouble(SystemParameters["takeprofit"]);
		PositionManager.ProfitTargetType = TargetPriceType.RelativeRatio;

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
	
	public override void Startup()
	{
#if DEBUG
#else
		SystemData.SystemHistory.BuyAndHoldStatistics.Enabled = false;
		SystemData.SystemHistory.LongStatistics.Enabled = false;
		SystemData.SystemHistory.ShortStatistics.Enabled = false;
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
}