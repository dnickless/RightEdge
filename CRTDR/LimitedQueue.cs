using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using RightEdge.Common;
using RightEdge.Indicators;

public class LimitedQueue : Queue<double>
{
	public double Sum { get; set; }
    public int Limit { get; set; }

    public LimitedQueue(int limit)
        : base(limit)
    {
        this.Limit = limit;
    }

    public new void Enqueue(double item)
    {
        if (Count == Limit)
        {
            Sum -= Dequeue();
        }
        base.Enqueue(item);
		Sum += item;
    }
}