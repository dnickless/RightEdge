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
        //PositionManager.ProfitTarget = SystemParameters["ProfitTarget"];
		
		//PositionManager.StopLoss = SystemParameters["StopLoss"];

		// Here is your time out exit value.  This will override whatever
		// is set in the Trading System properties.
		PositionManager.BarCountExit = Convert.ToInt32(SystemParameters["BarCountExit"]);
	}
}
#endregion

public class MySymbolScript : MySymbolScriptBase
{
	public UserSeries KBuy;
	
	// Create a standard MACD indicator with a 12 period fast
	// line and a 26 period slow line.
	MACD macd9 = new MACD(12, 26);
	
	// Create a EMA with a 3 period line
	EMA ema3 = new EMA(3);
	
	const string MAX_OPEN = "MaxOpen";
	const string MAX_OPEN_PER_SYMBOL = "MaxOpenPerSymbol";
	
	public override void Startup()
	{
		this.PositionManager.MaxOpenPositions = Convert.ToInt32(SystemParameters[MAX_OPEN]);
		this.PositionManager.MaxOpenPositionsPerSymbol = Convert.ToInt32(SystemParameters[MAX_OPEN_PER_SYMBOL]);
		//	Perform initialization here.
		macd9.SetInputs(Close);
		ema3.SetInputs(Close);
		SystemData.Output.Add(OutputSeverityLevel.Informational, "Trading system startup for symbol " + Symbol.Name);
		KBuy = new UserSeries();
	}
	
	public override void NewBar()
	{
		SystemData.Output.Add(OutputSeverityLevel.Informational, "InLeadData??? " + (SystemData.InLeadBars ? "ja" : "nein"));
		
		// Need a couple of bars before we can trade
		if (Bars.Count < 26)
		{
			return;
		}
		
		BarData yesterday = Bars.LookBack(1);
		BarData yesterday3 = Bars.LookBack(3);
		BarData today = Bars.LookBack(0);
		
		
		int c1=0;
		
		if ((yesterday.Close < yesterday.Open))c1++;
		if ((today.Close < today.Open))c1++;
		if ((today.Close < yesterday.Close))c1++;
		if ((yesterday.Close < yesterday3.Close))c1++;
		
		double mult=0;
		
		if (c1 <= 1) mult=SystemParameters["index1"];
		if (c1 == 2) mult=SystemParameters["index2"];
		if (c1 == 3) mult=SystemParameters["index3"];
		if (c1 == 4) mult=SystemParameters["index4"];
		
		double xUp;
		double xDown;
		
		xUp=ema3.LookBack(0)*mult;
	    xDown=ema3.LookBack(0)*SystemParameters["xdown"];
		
		KBuy.Current=(xUp-xDown)/2*Math.Max(macd9.LookBack(0),-0.5)+xDown;
		
		double buyLimitPrice = KBuy.LookBack(0);
		
		SystemData.Output.Add(OutputSeverityLevel.Informational, "Placing orders for '" + Symbol.Name + "'");		
		Position pos = OpenPosition(PositionType.Long, OrderType.Limit, buyLimitPrice);
		
		SystemData.Output.Add(OutputSeverityLevel.Informational, "Stop loss at " + pos.StopLoss + " (" + pos.StopLossType.ToString() + ")");		
		foreach(Order order in pos.Orders) 
		{
			SystemData.Output.Add(OutputSeverityLevel.Informational, "order stop price " + order.StopPrice);		
		}
	}
	
	public override void OrderFilled(Position position, Trade trade)
	{
		SystemData.Output.Add(OutputSeverityLevel.Informational, trade.TransactionType + " trade for order " + position.ID + " got filled because of " + trade.TradeType + " for " + trade.BuyingPowerChange + " of '" + Symbol.Name + "'");
		//	This method is called when an order is filled
	}
	
	public override void OrderCancelled(Position position, Order order, string information)
	{
		SystemData.Output.Add(OutputSeverityLevel.Informational, "Order cancelled '" + order.ID + "'");
		//	This method is called when an order is cancelled or rejected
	}
}