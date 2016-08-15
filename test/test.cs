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
	public UserSeries KBuyDown;
	public UserSeries KBuyUp;
	
  	double xDown_Up;
  	double xDown_Down;

  	double xUp_Up;
  	double xUp_Down;
	
	MACD macd9 = new MACD(12, 26);
	
	SMA sma3 = new SMA(3);
	SMA sma13 = new SMA(13);
	SMA sma39 = new SMA(39);
	
	EMA ema3 = new EMA(3);
	UserSeries index = new UserSeries();
	SMA indexSma = new SMA(12);	
	
	public override void Startup()
	{
		macd9.SetInputs(Close);
		ema3.SetInputs(Close);
		sma3.SetInputs(Close);
		sma13.SetInputs(Close);
		sma39.SetInputs(Close);
		
		KBuyDown = new UserSeries();
		KBuyDown.ChartSettings.Color = Color.Red;
		
		KBuyUp = new UserSeries();
		KBuyUp.ChartSettings.Color = Color.Green;
		
		index.ChartSettings.Color = Color.Green;
		index.ChartSettings.ChartPaneName = "Index";
		indexSma.SetInputs(OtherSymbols["MSFT"].Close);
		indexSma.ChartSettings.ChartPaneName = "Index";
	}

	public override void NewBar()
	{
		// Need a couple of bars before we can trade
		if (Bars.Count < 26)
		{
			return;
		}
		
		index.Current = (OtherSymbols["MSFT"].Close.Current);
		
		BarData yesterday = Bars.LookBack(1);
		BarData yesterday3 = Bars.LookBack(2);
		BarData today = Bars.Current;
		
		int c1down=0;
		
		if ((yesterday.Close < yesterday.Open))c1down++;
		if ((today.Close < today.Open))c1down++;
		if ((today.Close < yesterday.Close))c1down++;
		if ((yesterday.Close < yesterday3.Close))c1down++;
		
		double multdown=0;
		
		if ((c1down <= 1)) multdown=SystemParameters["indexdown1"];
		if ((c1down == 2)) multdown=SystemParameters["indexdown2"];
		if ((c1down == 3)) multdown=SystemParameters["indexdown3"];
		if ((c1down == 4)) multdown=SystemParameters["indexdown4"];
		
		xDown_Up=ema3.Current*multdown;
	    xDown_Down=ema3.Current*SystemParameters["xdown"];
		
		KBuyDown.Current=((xDown_Up-xDown_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xDown_Down;
		
		int c1up=0;
		
		if ((yesterday.Close > yesterday.Open))c1up++;
		if ((today.Close > today.Open))c1up++;
		if ((today.Close > yesterday.Close))c1up++;
		if ((yesterday.Close > yesterday3.Close))c1up++;
		
		double multup=0;
		
		if ((c1up <= 1)) multup=SystemParameters["indexup1"];
		if ((c1up == 2)) multup=SystemParameters["indexup2"];
		if ((c1up == 3)) multup=SystemParameters["indexup3"];
		if ((c1up == 4)) multup=SystemParameters["indexup4"];
		
		xUp_Up=ema3.Current*multup;
	    xUp_Down=ema3.Current*(SystemParameters["xup"]);
		
		KBuyUp.Current=((xUp_Up-xUp_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xUp_Down;
		
		if (index.Current > index.LookBack(1))
		//if (sma3.Current > sma13.Current)
		//if (sma13.Current > sma39.Current)	
		
		{
			PositionSettings settings = new PositionSettings();
			settings.BarsValid = 1;
			settings.PositionType = PositionType.Long;
			settings.OrderType = OrderType.Limit;
			settings.LimitPrice = KBuyDown.Current;
			OpenPosition(settings);
		}
		else if (index.Current < index.LookBack(1))
		{
			PositionSettings settings = new PositionSettings();
			settings.BarsValid = 1;
			settings.PositionType = PositionType.Short;
			settings.OrderType = OrderType.Limit;
			settings.LimitPrice = KBuyUp.Current;
			OpenPosition(settings);
		}
	}

	public override void OrderFilled(Position position, Trade trade)
	{
	}

	public override void OrderCancelled(Position position, Order order, string information)
	{
	}
}