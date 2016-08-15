using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using RightEdge.Common;
using RightEdge.Indicators;

public class CutlersRSIIndicator : IndicatorBase
{
	private readonly CutlersRSIIndicatorMath cutlersRSIIndicatorMath;
	
	public CutlersRSIIndicator(int n)
	{
		cutlersRSIIndicatorMath = new CutlersRSIIndicatorMath(n);
	}
	
	public override double CalcNextValue(BarData barData)
	{
		return cutlersRSIIndicatorMath.CalcNextValue(barData.Close);
	}
}

public class CutlersRSIIndicatorMath
{
	double lastClose = 0;
	int c = 0;
	readonly int n;
	double[] lastGains;
	double[] lastLosses;
	
	public double CalcNextValue(double close)
	{
		double res;
		var lastResult = close - lastClose;
		var i = c % n;
		lastGains[i] = lastResult > 0 ? lastResult : 0;
		lastLosses[i] = lastResult < 0 ? lastResult : 0;
		
		c++;
		if (c > n)
		{
			var avgLoss = lastLosses.Average();
			if (avgLoss == 0)
			{
				 res = 100;
			}
			else
			{
				res = 100.0-100.0/(1+(Math.Abs(lastGains.Average())/Math.Abs(lastLosses.Average())));
			}
			
		}
		else
		{
			 res = 0;
		}
		
		lastClose = close;
		
		return res;
	}
	
	public CutlersRSIIndicatorMath(int n)
	{
		lastGains = new double[n];
		lastLosses = new double[n];
		this.n = n;
	}
}
