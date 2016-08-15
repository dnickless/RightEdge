#region Using statements
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using RightEdge.Common;
using RightEdge.Common.ChartObjects;
using RightEdge.Indicators;
#endregion

#region System class
public class MySystem : MySystemBase
{
	public override void Startup()
	{
		// Perform initialization or set system wide options here

	}
}
#endregion

public class MySymbolScript : MySymbolScriptBase
{
	public override void Startup()
	{
		// Perform initialization here.
SystemData.CreateTicksFromBars = false;
SystemData.SystemHistory.LongStatistics.Enabled = false;
SystemData.SystemHistory.ShortStatistics.Enabled = false;	
SystemData.SystemHistory.BuyAndHoldStatistics.Enabled = false;	}

	public override void NewBar()
	{
		// Put your trading code here
		if (OpenPositions.Count == 0)
		{
			OpenPosition(PositionType.Long, OrderType.Market, 0, 1);
		}
		else
		{
			PositionManager.CloseAllPositions();
		}
	}

	public override void OrderFilled(Position position, Trade trade)
	{
		// This method is called when an order is filled

	}

	public override void OrderCancelled(Position position, Order order, string information)
	{
		// This method is called when an order is cancelled or rejected
        if (!order.CancelPending)
        {
            OutputWarning("Unexpected order cancel: " + order.ToString() + " " + information);
        }
	}
}
