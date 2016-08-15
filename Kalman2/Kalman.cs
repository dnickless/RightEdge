#define BACKTEST

#region Using statements
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RightEdge.Common;
using RightEdge.Common.ChartObjects;
using RightEdge.Indicators;
using dnAnalytics.LinearAlgebra;
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
	UserSeries observations;
	UserSeries kalmanOutput1;
	UserSeries kalmanOutput2;
	UserSeries kalmanOutput3;
	UserSeries kalmanOutput4;
	
	KalmanFilter kf;
	
	RelativeStrength rsi2;
	RelativeStrength rsi14;
	
	EMA ema;
	EMA ema2;
	
	public override void Startup()
	{
		CommonGlobals.SystemRunUpdateRate = TimeSpan.FromSeconds(5);
		
		observations = new UserSeries();
//		observations.ChartSettings.ChartPaneName = "observationsPane";
		observations.ChartSettings.Color = Color.Red;
		observations.ChartSettings.DisplayName = "Close prices";
		
		kalmanOutput1 = new UserSeries();
//		kalmanOutput1.ChartSettings.ChartPaneName = "observationsPane";
		kalmanOutput1.ChartSettings.Color = Color.Aqua;
		kalmanOutput1.ChartSettings.DisplayName = "Kalman output1";
		
		kalmanOutput2 = new UserSeries();
//		kalmanOutput2.ChartSettings.ChartPaneName = "observationsPane";
		kalmanOutput2.ChartSettings.Color = Color.Magenta;
//		kalmanOutput2.ChartSettings.DisplayName = "Kalman output2";
		
		kalmanOutput3 = new UserSeries();
//		kalmanOutput3.ChartSettings.ChartPaneName = "observationsPane";
		kalmanOutput3.ChartSettings.Color = Color.Orange;
//		kalmanOutput3.ChartSettings.DisplayName = "Kalman output3";
		
		kalmanOutput4 = new UserSeries();
		kalmanOutput4.ChartSettings.ChartPaneName = "StandardisedPane";
		kalmanOutput4.ChartSettings.Color = Color.Green;
		kalmanOutput4.ChartSettings.DisplayName = "Kalman output4";
				
//		rsi2 = new RelativeStrength(2, Close);
//		rsi2.ChartSettings.Color = Color.DarkBlue;
//		rsi2.ChartSettings.ChartPaneName = "StandardisedPane";
		
//		rsi14 = new RelativeStrength(14, Close);
//		rsi14.ChartSettings.Color = Color.Aqua;
//		rsi14.ChartSettings.ChartPaneName = "StandardisedPane";
		
		ema2 = new EMA(2, Close);
		ema2.ChartSettings.Color = Color.Blue;
		ema2.ChartSettings.DisplayName = "EMA 2";
		
		ema = new EMA(14, Close);
		ema.ChartSettings.Color = Color.DarkGray;
		ema.ChartSettings.DisplayName = "EMA 14";
		
		//init filter
		kf = KalmanFilter.buildKF(1, 1, 1);
		kf.X = new DenseMatrix(new double[,]{{0}, {1}, {1}});
		kf.predict(SystemData.Output);
		
		//results
		/*
		println("True:"); new Matrix(new double[][]{{x}, {vx}, {ax}}).print(3, 1);
		println("Last measurement:\n\n " + m + "\n");
		println("Estimate:"); kf.X.ToString(3, 1);
		println("Estimate Error Cov:"); KF.getP().print(3, 3);
		*/
	}
	
	double lastTrigger;
	
	public override void NewBar()
	{
		double m = Close.Current;
		observations.Current = m;


		
		//filter update
		kf.correct(new DenseMatrix(new double[,]{{m}}), SystemData.Output);
		kalmanOutput1.Current = kf.X[0,0];
		kf.predict(SystemData.Output);
		kalmanOutput2.Current = kf.X0[0,0];
		kalmanOutput3.Current = kf.X[0,0];
		kalmanOutput4.Current = 100-(100/(1+(kf.X0[0,0]/Close.Current)));
		
		if(lastTrigger <= SystemParameters["index1"] && kalmanOutput4.Current > SystemParameters["index1"])
		{
			
// Habe dies hier eingesetzt
		
		     PositionSettings ps = new PositionSettings();
//             ps.Description = "Buy";
#if BACKTEST
						ps.OrderType = OrderType.Limit;
						ps.LimitPrice = m;
#else
						ps.OrderType = OrderType.Market;
#endif
             ps.PositionType = PositionType.Long;
//             ps.StopLoss = this.Indicators["Lowest"].Current;
//             ps.StopLossType = TargetPriceType.Price;
             ps.TrailingStopType = TargetPriceType.RelativePrice;
             ps.TrailingStop = SystemParameters["TS"];
			
			Position pos = this.OpenPosition(ps);
		
// ... bis hier hin!
			
			
//			Trade(PositionType.Long);
		}
//		else if(lastTrigger >= SystemParameters["index2"] && kalmanOutput4.Current < SystemParameters["index2"])
		{
//			foreach(Position p in OpenPositions)
			{
//				p.CloseAtMarket();
				
				
			}
//			Trade(PositionType.Short);
		}
		lastTrigger = kalmanOutput4.Current;
	}
	
	private void Trade(PositionType positionType)
	{
		foreach(Position p in OpenPositions)
		{
			p.CloseAtMarket();
		}
		OpenPosition(positionType, OrderType.Market);
	}
}