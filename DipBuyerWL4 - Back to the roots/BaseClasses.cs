using System;
using System.Collections.Generic;
using System.Text;
using RightEdge.Common;

//	This is an auto-generated file.  You should not need to edit it.

public abstract class MySystemBase : SystemBase
{
	private SymbolScriptCollection<MySymbolScript> _symbolScripts = new SymbolScriptCollection<MySymbolScript>();
	public SymbolScriptCollection<MySymbolScript> SymbolScripts
	{
		get { return _symbolScripts; }
	}

	public override void Startup(SystemData data)
	{
		base.Startup(data);
		SymbolScripts.Initialize(this);
		foreach (MySymbolScriptBase symbolScript in SymbolScripts)
		{
			symbolScript.TradingSystem = (MySystem)this;
			symbolScript.Startup();
			SystemData.IndicatorManager.RegisterMembers(symbolScript, symbolScript.Symbol);
		}
	}

	public override void NewBar()
	{
		base.NewBar();
		SymbolScripts.NewBar();
	}

	public override void NewTick(Symbol symbol, BarData bar, TickData tick)
	{
		base.NewTick(symbol, bar, tick);
		SymbolScripts.NewTick(symbol, bar, tick);
	}

	//	Indicators
	public SymbolIndicatorCollection SimpleMovingAverage
	{
		get { return SystemData.Indicators["Simple Moving Average"]; }
	}
	public SymbolIndicatorCollection SimpleMovingAverage10
	{
		get { return SystemData.Indicators["Simple Moving Average10"]; }
	}

}

public class MySymbolScriptBase : SymbolScriptBase
{
	public MySystem TradingSystem;

	public SymbolScriptCollection<MySymbolScript> OtherSymbols
	{
		get { return TradingSystem.SymbolScripts; }
	}

	public ISeries SimpleMovingAverage
	{
		get { return TradingSystem.SystemData.Indicators["Simple Moving Average"][Symbol]; }
	}
	public ISeries SimpleMovingAverage10
	{
		get { return TradingSystem.SystemData.Indicators["Simple Moving Average10"][Symbol]; }
	}

}
