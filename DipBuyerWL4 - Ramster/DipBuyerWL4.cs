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
		
		CommonGlobals.LogSettings.LoggingEnabled = false;
    }
}
#endregion

public class MySymbolScript : MySymbolScriptBase
{
    public UserSeries KBuyDown;
    public UserSeries KBuyUp;
    
    MACD macd9 = new MACD(12, 26);
	Highest highest = new Highest(4);
    EMA ema3 = new EMA(3);
    UserSeries index = new UserSeries();
    
	Frequency freqAux;
	StochRSI stochRsi9Aux;
	
    public override void Startup()
    {
        Bars.MaxLookBack = 26;
        
        highest.SetInputs(Close);
        macd9.SetInputs(Close);
        ema3.SetInputs(Close);

		freqAux = GetFrequency(BarFrequency.FiveMinute);
		freqAux.NewBar += NewAuxiliaryBar;
		
		stochRsi9Aux = new StochRSI(Convert.ToInt32(SystemParameters["rsilength"]), freqAux.Close);
		SystemData.IndicatorManager.SetFrequency(stochRsi9Aux, freqAux);
		
        KBuyDown = new UserSeries();
        KBuyDown.ChartSettings.Color = Color.Red;
        
//        KBuyUp = new UserSeries();
//        KBuyUp.ChartSettings.Color = Color.Green;
        
        index.ChartSettings.Color = Color.Green;
        index.ChartSettings.ChartPaneName = "Index";
    }

	DateTime dipTime = DateTime.MinValue;
	
	public void NewAuxiliaryBar(object sender, SingleBarEventArgs args)
	{
        if (Bars.Count < 1 || Symbol.Name == "^NDX")
        {
			OutputMessage("Returning after " + Bars.Count + " bars.");
            return;
        }
		
		//OutputMessage("New auxiliary bar: " + args.Bar.BarStartTime.ToString());
		
		if(args.Bar.Close < KBuyDown.Current && args.Bar.BarStartTime.TimeOfDay < TimeSpan.FromMinutes(Convert.ToInt32(SystemParameters["cutofftime"])))
		{
			dipTime = args.Bar.BarStartTime;
			if(SystemData.LiveMode)
			{
				OutputMessage("Dip detected: " + args.Bar.BarStartTime.ToString());
			}
		}
		
		if (dipTime.Date == args.Bar.BarStartTime.Date && args.Bar.Close < KBuyDown.Current && stochRsi9Aux.Current > 90 && OpenPositions.Count == 0)
		{
//			OutputMessage("BUYING 5 minutes after: " + args.Bar.BarStartTime.ToString());
			PositionSettings settings = new PositionSettings();
			//settings.BarsValid = 1;
			settings.PositionType = PositionType.Long;
			settings.OrderType = OrderType.Market;
////            settings.OrderType = OrderType.Limit;
////            settings.LimitPrice = KBuyDown.Current;
//			settings.StopLossType = TargetPriceType.RelativeRatio;
//			settings.StopLoss = 0.01;
//			settings.StopLossType = TargetPriceType.RelativePrice;
//			settings.StopLoss = (Bars.Current.Close - Bars.PartialItem.Low) / SystemParameters["stoploss"];
			settings.StopLossType = TargetPriceType.AbsolutePrice;
			settings.StopLoss = Math.Min(Bars.PartialItem.Low, Bars.Current.Close * 0.995);
			settings.ProfitTargetType = TargetPriceType.RelativePrice;
			settings.ProfitTarget = (highest.Current - Bars.PartialItem.Low) /  SystemParameters["takeprofit"];
			settings.CustomString = lastCustomString;
			//OutputMessage("SL: " + settings.StopLoss + " PT: " + settings.ProfitTarget);
			OpenPosition(settings);
		}
	}
	
	string lastCustomString;
	
    public override void NewBar()
    {
		//OutputMessage("New Bar: " + Symbol.Name);
        if (Bars.Count < 4 || Symbol.Name == "^NDX")
        {
//			OutputMessage("Returning after " + Bars.Count + " bars.");
            return;
        }
        
        BarData yesterday = Bars.LookBack(1);
        BarData yesterday3 = Bars.LookBack(3);
        BarData today = Bars.Current;
        
        int c1down=0;
        lastCustomString = string.Empty;
		
		var sum = 0;
        if ((yesterday.Close < yesterday.Open)) { sum += 1; c1down++; };
		if ((today.Close < today.Open)) { sum += 2; c1down++; };
        if ((today.Close < yesterday.Close)) { sum += 4; c1down++; };
        if ((yesterday.Close < yesterday3.Close)) { sum += 8;  c1down++; };
		lastCustomString += sum + ",";
        
		lastCustomString += c1down + ",";
		
		double multdown=0;
        
        if ((c1down <= 1)) multdown=SystemParameters["indexdown1"];
        if ((c1down == 2)) multdown=SystemParameters["indexdown2"];
        if ((c1down == 3)) multdown=SystemParameters["indexdown3"];
        if ((c1down == 4)) multdown=SystemParameters["indexdown4"];
        
        var xDown_Up=ema3.Current*multdown;
        var xDown_Down=ema3.Current*SystemParameters["xdown"];
        
        KBuyDown.Current=((xDown_Up-xDown_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xDown_Down;
		//OutputMessage(Bars.Current.BarStartTime +  ": " + c1down + " : "  + Bars.Current.Close + " - " + KBuyDown.Current.ToString() + " = " + (Bars.Current.Close - KBuyDown.Current));
        
//        int c1up=0;
        
//        if ((yesterday.Close > yesterday.Open))c1up++;
//        if ((today.Close > today.Open))c1up++;
//        if ((today.Close > yesterday.Close))c1up++;
//        if ((yesterday.Close > yesterday3.Close))c1up++;
        
//        double multup=0;
        
//        if ((c1up <= 1)) multup=SystemParameters["indexup1"];
//        if ((c1up == 2)) multup=SystemParameters["indexup2"];
//        if ((c1up == 3)) multup=SystemParameters["indexup3"];
//        if ((c1up == 4)) multup=SystemParameters["indexup4"];
        
//        var xUp_Up=ema3.Current*multup;
//        var xUp_Down=ema3.Current*(SystemParameters["xup"]);
        
//        KBuyUp.Current=((xUp_Up-xUp_Down)/2)*(Math.Max(Math.Min(macd9.Current,-0.5),+0.5))+xUp_Down;
        
        //index.Current = (OtherSymbols["^NDX"].Close.Current);
        //if (index.Current > index.LookBack((int)SystemParameters["ndx"]))
        {
            PositionSettings settings = new PositionSettings();
            settings.BarsValid = 1;
            settings.PositionType = PositionType.Long;
            settings.OrderType = OrderType.Limit;
            settings.LimitPrice = KBuyDown.Current;
//            OpenPosition(settings);
        }
//        else if (index.Current < index.LookBack(1))
//        {
//            PositionSettings settings = new PositionSettings();
//            settings.BarsValid = 1;
//            settings.PositionType = PositionType.Short;
//            settings.OrderType = OrderType.Limit;
//            settings.LimitPrice = KBuyUp.Current;
//            OpenPosition(settings);
//        }
    }

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