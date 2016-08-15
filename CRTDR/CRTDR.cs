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
        SystemData.EnableTradeOnClose = true;
        //SystemData.BarClosing += BarClosing;
        SystemData.PositionManager.CalculateMAEMFE = true;
    }
    
    bool DoReinvestment = false;
    
    public override void NewBar()
    //void BarClosing(object sender, NewBarEventArgs args)
    {
        foreach (MySymbolScript symbolScript in SymbolScripts)
        {
           //symbolScript.BarClosing();
            symbolScript.NewBar();
        }
        
        var numberOfEntries = 0;
        var listOfStrategiesWithOrders = new List<MySymbolScript>();

        double sumCRTDR = 0.0;
        double changeInMoney = 0.0;
                        
        foreach (MySymbolScript symbolScript in SymbolScripts)
        {
            if (symbolScript.Long)
            {
                listOfStrategiesWithOrders.Add(symbolScript);
                numberOfEntries++;
                sumCRTDR += (1.0 - (double)symbolScript.Crtdr);
            }
            
            changeInMoney += symbolScript.IncomingCash;
            Console.WriteLine(string.Format("change in money {0}", changeInMoney));
        }
        
        double sumInvested = 0.0;
        foreach (MySymbolScript symbolScript in listOfStrategiesWithOrders)
        {
            var crtdr = symbolScript.Crtdr;
            double availableCash = 0.0;
            if(DoReinvestment)
            {
                //SystemData.Broker.GetAccountBalance()
                availableCash = SystemData.CurrentCapital + changeInMoney;
            }
            else
            {
                // do not reinvest potential profits, cap @ InitialCapital
                availableCash = Math.Min(SystemData.StartingCapital, SystemData.StartingCapital + changeInMoney - SystemData.CurrentEquity);
            }
            
            var cashToInvest = availableCash * (sumCRTDR == 0.0 ? 1.0 / (double)numberOfEntries : ((1.0 - crtdr) / sumCRTDR));
            sumInvested += cashToInvest;
            
            var amount = (int)(cashToInvest / symbolScript.Bars.Current.Close /* * 0.995*/);
            Console.WriteLine(string.Format("available cash {0} amount {1} cashToInvest {2} sumInvested  {3}", availableCash, amount, cashToInvest, sumInvested));
            
            symbolScript.OpenPosition(PositionType.Long, OrderType.Market, 0, amount);
        }
    }
}
#endregion

public class MySymbolScript : MySymbolScriptBase
{
    CRTDRIndicator crtdrIndicator = new CRTDRIndicator();
    CutlersRSIIndicatorMath cutlersRSIIndicatorDown;
    CutlersRSIIndicatorMath cutlersRSIIndicatorFlat;
    CutlersRSIIndicatorMath cutlersRSIIndicatorUp;
    
    //System.IO.StreamWriter f = new System.IO.StreamWriter(@"c:\temp\o.txt", false);
    
    EMA xAverageShort;
    EMA xAverageLong;
    
    //Optimizer optimizer;
    
    double rsiSellLevelDown  = 0;
    double rsiSellLevelFlat = 0;
    double rsiSellLevelUp = 0;
    double longLimitDown = 0;
    double longLimitFlat = 0;
    double longLimitUp = 0;
    
    public double Crtdr = 0.0;
    public bool Long = false;
    public double IncomingCash = 0.0;
    
    public override void Startup()
    {
        Bars.MaxLookBack = 150;
        cutlersRSIIndicatorDown = new CutlersRSIIndicatorMath(Convert.ToInt32(SystemParameters["rsiLengthDown"]));
        cutlersRSIIndicatorFlat = new CutlersRSIIndicatorMath(Convert.ToInt32(SystemParameters["rsiLengthFlat"]));
        cutlersRSIIndicatorUp = new CutlersRSIIndicatorMath(Convert.ToInt32(SystemParameters["rsiLengthUp"]));
        
        xAverageShort = new EMA(Convert.ToInt32(SystemParameters["xAverageShort"]), Close);
        xAverageLong = new EMA(Convert.ToInt32(SystemParameters["xAverageLong"]), Close);
        
        rsiSellLevelDown = SystemParameters["rsiSellLevelDown"];
        rsiSellLevelFlat = SystemParameters["rsiSellLevelFlat"];
        rsiSellLevelUp   = SystemParameters["rsiSellLevelUp"];
        longLimitDown = SystemParameters["longLimitDown"];
        longLimitFlat = SystemParameters["longLimitFlat"];
        longLimitUp   = SystemParameters["longLimitUp"];
        //optimizer = new Optimizer(Convert.ToInt32(SystemParameters["optimizerLookBack"]));
    }
    
    private enum Trend
    {
        Up = 1,
        Down = -1,
        Flat = 0
    }
    
    private Trend GetTrend()
    {
        if(Bars.Current.Close > xAverageLong.Current && xAverageLong.Current < xAverageShort.Current)
        {
            return Trend.Up;
        }
        else if (xAverageLong.Current > xAverageShort.Current && xAverageShort.Current > Bars.Current.Close)
        {
            return Trend.Down;
        }
        else return Trend.Flat;
    }
    
    public override void NewBar()
    {
//    public void BarClosing()
//    {
        Crtdr = 0.0;
        Long = false;
        IncomingCash = 0.0;
        
        //OutputMessage(Bars.Current.BarStartTime.ToString());
        
        if(Bars.Count <= 100) return;
        var rsiDown = cutlersRSIIndicatorDown.CalcNextValue(Close.Current);
        var rsiFlat = cutlersRSIIndicatorFlat.CalcNextValue(Close.Current);
        var rsiUp = cutlersRSIIndicatorUp.CalcNextValue(Close.Current);
        
        //if(Bars.Count <= Convert.ToInt32(SystemParameters["optimizerLookBack"])) return;
        
        Crtdr = crtdrIndicator.CalcNextValue(Bars.Current);
        
        //var optResult = optimizer.Run(Close.Current, (cutOffParam, rsiLowParam, rsiHighParam, lastBarHadSignalParam) => GetSignal(crtdr, x, cutOffParam, rsiLowParam, rsiHighParam, lastBarHadSignalParam));
        
        //var bestParameters = optimizer.GetBestParameters();
        //OutputMessage(bestParameters.ToString());
        //cutOff = bestParameters.CutOff.GetValue();
        //rsiLow = bestParameters.RsiLow.GetValue();
        //rsiHigh = bestParameters.RsiHigh.GetValue();
        
        var trend = GetTrend();
        var rsi = trend == Trend.Down ? rsiDown : (trend == Trend.Up ? rsiUp : rsiFlat);
        
        OutputMessage(string.Format("Calculating strategy, high {0} low {1} close {2} rsi {3} crtdr {4} xAverageLong {5} xAverageShort {6}", Bars.Current.High, Bars.Current.Low, Bars.Current.Close, rsi, Crtdr, xAverageLong.Current, xAverageShort.Current));
        
        if (OpenPositions.Count == 0)
        {
            //System.Diagnostics.Debugger.Launch();
            if (GetSignalUp(Crtdr, rsi, trend))
            {
                Long = true;
            }
        }
        else
        {
            var pos = OpenPositions[0];
            if(pos.UnrealizedProfit < 0)
            {
                IncomingCash = pos.CurrentValue;
                pos.CloseAtMarket();
                //PositionManager.CloseAllPositions();
            }
            else if (pos.BarsHeld > 2 && !GetSignalUp(Crtdr, rsi, trend))
            {
                IncomingCash = pos.CurrentValue;
                pos.CloseAtMarket();
            }
            else if(rsi > (trend == Trend.Down ? rsiSellLevelDown : (trend == Trend.Up ? rsiSellLevelUp : rsiSellLevelFlat)))
            {
                IncomingCash = pos.CurrentValue;
                pos.CloseAtMarket();
            }
        }
    }
    
    private bool GetSignalUp(double crtdr, double rsi, Trend trend)
    {
        if(trend == Trend.Up)
        {
            if(crtdr * 100 + rsi <= longLimitUp) return true;
        }
        else if(trend == Trend.Down)
        {
            if(crtdr * 100 + rsi <= longLimitDown) return true;
        }
        else
        {
            if(crtdr * 100 + rsi <= longLimitFlat) return true;
        }
        
        return false;
    }
    
        //OutputMessage("NewBar" + Close.Current);
        //OutputMessage(crtdrIndicator.Current.ToString());
        //OutputMessage(cutlersRSIIndicator.Current.ToString());
        
        //f.WriteLine(Bars.LookBack(0).BarStartTime + ";" + Close.Current + ";" + cutlersRSIIndicator.Current);

    public override void OrderFilled(Position position, Trade trade)
    {
    }

    public override void OrderCancelled(Position position, Order order, string information)
    {
        if (!order.CancelPending)
        {
            OutputWarning("Unexpected order cancel: " + order.ToString() + " " + information);
        }
    }
}