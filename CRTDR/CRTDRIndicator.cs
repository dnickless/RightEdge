using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using RightEdge.Common;
using RightEdge.Indicators;

public class CRTDRIndicator// : IndicatorBase
{
	public double CalcNextValue(BarData barData)
	{
		var high = barData.High;
		var low = barData.Low;
		var close = barData.Close;
		
		return CalcNextValue(high, low, close);
	}
	
	public static double CalcNextValue(double high, double low, double close)
	{
		return high != low ? (close - low) / (high - low) : -1;
	}
}