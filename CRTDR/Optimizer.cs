using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using RightEdge.Common;
using RightEdge.Indicators;

public class Optimizer
{
	public class ParameterSet
	{
		public LimitedQueue Profits { get; set; }
		public Parameter CutOff { get; set; }
		public Parameter RsiLow { get; set; }
		public Parameter RsiHigh { get; set; }
		
		public ParameterSet(int lookBack)
		{
			Profits = new LimitedQueue(lookBack);
		}
		
		public double GetProfit()
		{
			return Profits.Sum;
		}
		
		public override string ToString()
		{
			return string.Format("CutOff: {0}, RsiLow: {1}, RsiHigh: {2}, Profit: {3}", CutOff.GetValue(), RsiLow.GetValue(), RsiHigh.GetValue(), GetProfit());
		}
		
		public override bool Equals(object other)
		{
			ParameterSet p = (ParameterSet) other;
			return p.CutOff.Value == CutOff.Value && p.RsiLow.Value == RsiLow.Value && p.RsiHigh.Value == RsiHigh.Value;
		}
		
		public override int GetHashCode()
		{
			return string.Format("C{0}RL{1}RH{2}", CutOff.Value, RsiLow.Value, RsiHigh.Value).GetHashCode();
		}
	}
	
	public class Parameter
	{
		public string Name { get; set; }
		public int Value { get; set; }
		public int StepSize { get; set; }
		public int Min { get; set; }
		public int Max { get; set; }
		public int Divisor { get; set; }
		
		public double GetValue()
		{
			return (double) Value / Divisor;
		}
	}
	
	ParameterSet[] allCombinations;
	
	public Optimizer(int lookBack)
	{
		List<Parameter> parameters = new List<Parameter>();
		parameters.Add(new Parameter{ Name = "cutOff",  StepSize = 10, Min = 10, Max = 100 });
		parameters.Add(new Parameter{ Name = "rsiLow",  StepSize = 10, Min = 10, Max = 100 });
		parameters.Add(new Parameter{ Name = "rsiHigh", StepSize = 10, Min = 10, Max = 100 });
		
		allCombinations = CartesianProduct<int>(parameters.Select((param) => {
			var list = new List<int>();
			for(var x = param.Min; x <= param.Max; x += param.StepSize)
			{
				list.Add(x);
			}
			return list;
		})).Select(_ => new ParameterSet(lookBack) { CutOff = new Parameter { Value = _.ElementAt(0), Divisor = 100 }, RsiLow = new Parameter { Value = _.ElementAt(1), Divisor = 1 }, RsiHigh = new Parameter { Value = _.ElementAt(2), Divisor = 1 } })
			.Where(_ => _.RsiLow.Value <= _.RsiHigh.Value).Distinct().ToArray();
		lastSignals = new bool[allCombinations.Length];
	}
	
	double lastPrice = 0;
	bool[] lastSignals;
	
	public ParameterSet GetBestParameters()
	{
		return allCombinations.OrderByDescending(_ => _.GetProfit()).First();
	}
	
	public bool[] Run(double price, Func<double, double, double, bool, bool> getSignal)
	{
		double profit = 0;
		if (lastPrice != 0)
		{
			 profit = (price / lastPrice) - 1.0;
		}
		lastPrice = price;
		
		List<bool> ret = new List<bool>();
		long i = 0;
		foreach(var parameterSet in allCombinations)
		{
			if (lastSignals[i])
			{
				if(profit > 0)
				{
					parameterSet.Profits.Enqueue(Math.Abs(profit));
				}
				else
				{
					parameterSet.Profits.Enqueue(-Math.Abs(profit));
				}
			}
			else
			{
				if(profit > 0)
				{
					parameterSet.Profits.Enqueue(-Math.Abs(profit));
				}
				else
				{
					parameterSet.Profits.Enqueue(Math.Abs(profit));
				}
			}
			
			ret.Add(getSignal(parameterSet.CutOff.GetValue(), parameterSet.RsiLow.GetValue(), parameterSet.RsiHigh.GetValue(), lastSignals[i]));
			i++;
		}
		
		lastSignals = ret.ToArray();
		
		return lastSignals;
	}
	
	static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences) 
	{ 
		IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() }; 
		return sequences.Aggregate(emptyProduct, (accumulator, sequence) => 
		from accseq in accumulator 
		from item in sequence 
		select accseq.Concat(new[] {item})); 
	}
}
