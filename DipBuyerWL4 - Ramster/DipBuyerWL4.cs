#region Using statements
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using RightEdge.Common;
using RightEdge.Indicators;
#endregion

#region System class
public class MySystem : MySystemBase
{
    public override void Startup()
    {
		//CommonGlobals.SystemRunUpdateRate = TimeSpan.FromSeconds(1);
		
//        PositionManager.ProfitTarget = SystemParameters["takeprofit"];
//        PositionManager.ProfitTargetType = TargetPriceType.RelativeRatio;
        
//        PositionManager.StopLoss = SystemParameters["stoploss"];
//        PositionManager.StopLossType = TargetPriceType.RelativeRatio;
        
        //PositionManager.BarCountExit = 10;
		
        PositionManager.CalculateMAEMFE = true;
		CommonGlobals.SystemRunUpdateRate = TimeSpan.FromSeconds(5);
		CommonGlobals.LogSettings.LoggingEnabled = false;
		//SystemData.CreateTicksFromBars = false;
    }
}
#endregion

public class MySymbolScript : MySymbolScriptBase
{
	double kBuyDownCurrent;
	bool onlyOneSymbol;
    public UserSeries KBuyDown;
    public UserSeries KBuyUp;
    
    MACD macd9;
	Highest highest;
    EMA ema3;
    UserSeries index = new UserSeries();
    
	Frequency freqFive;
	Frequency freqDaily;
	StochRSI stochRsi;
	
	// system parameters
	double indexDown1, indexDown2, indexDown3, indexDown4, stopLoss, takeProfit, xDown;
	TimeSpan cutOffTime;
	
    public override void Startup()
    {
		freqFive = GetFrequency(BarFrequency.FiveMinute);
		freqFive.NewBar += NewFiveMinuteBar;
        freqFive.Bars.MaxLookBack = 2;
		
		stochRsi = new StochRSI(Convert.ToInt32(SystemParameters["rsilength"]), freqFive.Close);
		SystemData.IndicatorManager.SetFrequency(stochRsi, freqFive);
		
		freqDaily = GetFrequency(BarFrequency.Daily);
		freqDaily.NewBar += NewDailyBar;
        freqDaily.Bars.MaxLookBack = 26;
		
		highest = new Highest(4, freqDaily.Close);
		SystemData.IndicatorManager.SetFrequency(highest, freqDaily);
		
		ema3 = new EMA(3, freqDaily.Close);
		SystemData.IndicatorManager.SetFrequency(ema3, freqDaily);
		
		macd9 = new MACD(12, 26, freqDaily.Close);
		SystemData.IndicatorManager.SetFrequency(macd9, freqDaily);
		
        KBuyDown = new UserSeries();
        KBuyDown.ChartSettings.Color = Color.Red;
        
		onlyOneSymbol = SystemData.Symbols.Count == 1;
		
        index.ChartSettings.Color = Color.Green;
        index.ChartSettings.ChartPaneName = "Index";
		
		indexDown1 = SystemParameters["indexdown1"];
		indexDown2 = SystemParameters["indexdown2"];
		indexDown3 = SystemParameters["indexdown3"];
		indexDown4 = SystemParameters["indexdown4"];
		
		cutOffTime = TimeSpan.FromMinutes(Convert.ToInt32(SystemParameters["cutofftime"]));
		stopLoss = SystemParameters["stoploss"];
		takeProfit = SystemParameters["takeprofit"];
		xDown = SystemParameters["xdown"];
    }

	DateTime dipTime = DateTime.MinValue;
	
	public void NewDailyBar(object sender, SingleBarEventArgs args)
	{
		//OutputMessage("New Bar: " + Symbol.Name);
        if (freqDaily.Bars.Count < 4)
        {
//			OutputMessage("Returning after " + freqDaily.Bars.Count + " bars.");
            return;
        }
		
        BarData yesterday = freqDaily.Bars.LookBack(1);
        BarData yesterday3 = freqDaily.Bars.LookBack(3);
        BarData today = freqDaily.Bars.Current;
        
        int c1down=0;
        lastCustomString = string.Empty;
		
		var sum = 0;
        if ((yesterday.Close < yesterday.Open)) { sum += 1; c1down++; };
		if ((today.Close < today.Open)) { sum += 2; c1down++; };
        if ((today.Close < yesterday.Close)) { sum += 4; c1down++; };
        if ((yesterday.Close < yesterday3.Close)) { sum += 8;  c1down++; };
		//lastCustomString += sum + ",";
        
		//lastCustomString += c1down + ",";
		
		double multdown=0;
        
        if ((c1down <= 1)) multdown=indexDown1;
        if ((c1down == 2)) multdown=indexDown2;
        if ((c1down == 3)) multdown=indexDown3;
        if ((c1down == 4)) multdown=indexDown4;
        
        var xDown_Up=ema3.Current*multdown;
        var xDown_Down=ema3.Current*xDown;
        
		kBuyDownCurrent = ((xDown_Up-xDown_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xDown_Down;
        if(onlyOneSymbol)
		{
			KBuyDown.Current = kBuyDownCurrent;
		}
		
		//OutputMessage(freqDaily.Bars.Current.BarStartTime +  ": " + c1down + " : "  + freqDaily.Bars.Current.Close + " - " + kBuyDownCurrent.ToString() + " = " + (freqDaily.Bars.Current.Close - kBuyDownCurrent));
	}
	
	private void NewFiveMinuteBar(object sender, SingleBarEventArgs args)
	{
        if (freqFive.Bars.Count < 1)
        {
			if(SystemData.LiveMode)
			{
				OutputMessage("Returning after " + freqFive.Bars.Count + " bars.");
			}
			
            return;
        }
		
		//OutputMessage("New auxiliary bar: " + args.Bar.BarStartTime.ToString());
		
		if(args.Bar.Close < kBuyDownCurrent && args.Bar.BarStartTime.TimeOfDay < cutOffTime)
		{
			dipTime = args.Bar.BarStartTime;
			if(SystemData.LiveMode)
			{
				OutputMessage("Dip detected: " + args.Bar.BarStartTime.ToString());
			}
		}
		
		if (dipTime.Date == args.Bar.BarStartTime.Date && args.Bar.Close < kBuyDownCurrent && stochRsi.Current > 90 && OpenPositions.Count == 0)
		{
			//OutputMessage("BUYING 5 minutes after: " + args.Bar.BarStartTime.ToString());
			PositionSettings settings = new PositionSettings();
			//settings.BarsValid = 1;
			settings.PositionType = PositionType.Long;
			settings.OrderType = OrderType.Market;
////            settings.OrderType = OrderType.Limit;
////            settings.LimitPrice = kBuyDownCurrent;
//			settings.StopLossType = TargetPriceType.RelativeRatio;
//			settings.StopLoss = 0.01;
//			settings.StopLossType = TargetPriceType.RelativePrice;
//			settings.StopLoss = (freqDaily.Bars.Current.Close - freqDaily.Bars.PartialItem.Low) / SystemParameters["stoploss"];
			settings.StopLossType = TargetPriceType.AbsolutePrice;
			settings.StopLoss = Math.Min(freqDaily.Bars.PartialItem.Low, args.Bar.Close) * stopLoss;
			settings.ProfitTargetType = TargetPriceType.RelativePrice;
			settings.ProfitTarget = (highest.Current - freqDaily.Bars.PartialItem.Low) /  takeProfit;
			settings.CustomString = lastCustomString;
			//OutputMessage("SL: " + settings.StopLoss + " PT: " + settings.ProfitTarget);
			OpenPosition(settings);
		}
	}
	
	string lastCustomString;

    public override void OrderFilled(Position position, Trade trade)
    {
		if(trade.TradeType != TradeType.OpenPosition)
			//System.IO.File.AppendAllText(@"C:\temp\RE out\result.txt", Symbol + "," + position.OpenDate + "," + position.CustomString + position.CurrentStats.RealizedProfit + Environment.NewLine);
		
		base.OrderFilled(position, trade);
		try
		{
			if (SystemData.LiveMode)
			{
				OutputMessage("sending email");
				var fromAddress = new MailAddress("ramanddan.trading@gmail.com", "RamAndDan Trading");
				const string fromPassword = "AfMxVqFXU6Mm";
				
				var sb = new StringBuilder();
				sb.AppendLine();
				sb.AppendLine("Trade");
				sb.AppendLine("-----");
				sb.AppendFormat("  Execution date: {0}", trade.FilledTime).AppendLine();
				sb.AppendFormat("      Order type: {0}", trade.OrderType).AppendLine();
				sb.AppendFormat("      Trade type: {0}", trade.TradeType).AppendLine();
				sb.AppendFormat("Transaction type: {0}", trade.TransactionType).AppendLine();
				sb.AppendFormat("          Symbol: {0}", Symbol.Name).AppendLine();
				sb.AppendFormat("            Size: {0}", trade.Size).AppendLine();
				sb.AppendFormat("           Price: {0}", trade.Price).AppendLine();
				sb.AppendFormat("      Commission: {0}", trade.Commission).AppendLine();
				sb.AppendLine();
				sb.AppendLine();
				sb.AppendLine("Position");
				sb.AppendLine("--------");
				sb.AppendFormat("            Size: {0}", position.CurrentSize).AppendLine();
				sb.AppendFormat("           Value: {0}", position.CurrentValue).AppendLine();
				sb.AppendFormat("    Realized P/L: {0}", position.RealizedProfit).AppendLine();
				sb.AppendFormat("  Unrealized P/L: {0}", position.UnrealizedProfit).AppendLine();
				
				var smtp = new SmtpClient
				{
					Host = "smtp.gmail.com",
					Port = 587,
					EnableSsl = true,
					DeliveryMethod = SmtpDeliveryMethod.Network,
					UseDefaultCredentials = false,
					Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
				};
				
				using (var message = new MailMessage()
				{
					From = fromAddress,
					Subject = string.Format("Order completed: {0} {1} {2} @ {3}", trade.TransactionType, trade.Size, Symbol.Name, trade.Price),
					Body = sb.ToString()
				})
				{
					message.To.Add("ramon@winter-berg.com");
					message.To.Add("daniel.hegener@gmx.net");
					smtp.Send(message);
				}
			}
		}
		catch(Exception)
		{
		}
//		if (trade.TradeType != TradeType.OpenPosition)
//		{
//			//logger.Debug(string.Join(";", new [] { Open.LookBack(2) < Close.LookBack(2) ? "POS" : "NEG", Open.LookBack(3) < Close.LookBack(3) ? "POS" : "NEG", x, position.OpenDate.ToShortDateString(), position.Tag.ToString(), Low.LookBack(1).ToString(), position.ExitPrice.ToString(), position.RealizedProfit.ToString() }));
//			OutputMessage(string.Format("Time: {0} P&L: {2:0.000}% ({1:0})", SystemData.SystemStatistics.CurStat.CalculatedDate, SystemData.SystemStatistics.CurStat.NetProfit, SystemData.SystemStatistics.CurStat.NetProfitPct));
//		}
//		else
//		{
//			OutputMessage("Entry: " + string.Format("{0:0.###}", position.EntryPrice.SymbolPrice) + ", limit:" + string.Format("{0:0.###}", position.Tag) + ", difference: " + string.Format("{0:0.###}", (double)position.Tag - position.EntryPrice.SymbolPrice));
//		}
   	}

    public override void OrderCancelled(Position position, Order order, string information)
    {
		if (!order.CancelPending)
		{
			OutputWarning("Unexpected order cancel: " + order.ToString() + " " + information);
		}
    }
}