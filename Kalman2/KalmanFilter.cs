using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using RightEdge.Common;
using RightEdge.Indicators;
using dnAnalytics.LinearAlgebra;

public class KalmanFilter {
	public Matrix X, X0;
	public Matrix F, B, U, Q;
	public Matrix H, R;
	public Matrix P, P0;
	
	public void predict(OutputManager opm)
	{
		X0 = new DenseMatrix(3,1);
		Matrix t = B.Multiply(U);
		Matrix t1 = F.Multiply(X);
		t1.Add(t, X0);
		P0 = new DenseMatrix(3,3);
		Matrix t2 = F.Transpose();
		Matrix t3 = F.Multiply(P);
		Matrix t4 = t3.Multiply(t2);
		t4.Add(Q, P0);
	}
	
	public void correct(Matrix Z, OutputManager opm)
	{
		Matrix S = new DenseMatrix(1, 1);
		Matrix t = H.Multiply(P0);
		Matrix t1 = t.Multiply(H.Transpose());
		t1.Add(R, S);
		
		Matrix K = P0.Multiply(H.Transpose()).Multiply(S.Inverse());
		
		Matrix t2 = new DenseMatrix(1, 1);
		H.Multiply(X0, t2);
		Matrix t3 = new DenseMatrix(1, 1);
		Z.Subtract(t2, t3);
		
		X0.Add(K.Multiply(t3), X);
		
		Matrix I = new DenseMatrix(3, 3);
		I.SetDiagonal(new double[] {1,1,1});
		
		Matrix t4 = new DenseMatrix(3, 3);
		I.Subtract(K.Multiply(H), t4);
		P = t4.Multiply(P0);
	}
	
	public static KalmanFilter buildKF(double dt, double processNoisePSD, double measurementNoiseVariance) {
		KalmanFilter KF = new KalmanFilter();
		
		//state vector
		KF.X = new DenseMatrix(new double[,]{{0, 0, 0}}).Transpose();
		
		//error covariance matrix
		KF.P = new DenseMatrix(3,3);
		KF.P.SetDiagonal(new double[] {1,1,1});
		
		//transition matrix
		KF.F = new DenseMatrix(new double[,]{
			{1, dt, Math.Pow(dt, 2)/2},
			{0,  1, dt},
			{0,  0, 1}
		});
		
		//input gain matrix
		KF.B = new DenseMatrix(new double[,]{{0, 0, 0}}).Transpose();
		
		//input vector
		KF.U = new DenseMatrix(new double[,]{{0}});
		
		//process noise covariance matrix
		KF.Q = new DenseMatrix(3,3);
		new DenseMatrix(new double[,]{
			{ Math.Pow(dt, 5) / 4, Math.Pow(dt, 4) / 2, Math.Pow(dt, 3) / 2},
			{ Math.Pow(dt, 4) / 2, Math.Pow(dt, 3) / 1, Math.Pow(dt, 2) / 1},
			{ Math.Pow(dt, 3) / 1, Math.Pow(dt, 2) / 1, Math.Pow(dt, 1) / 1}
		}).Multiply(processNoisePSD, KF.Q);
		
		//measurement matrix
		KF.H = new DenseMatrix(new double[,]{{1, 0, 0}});
		
		//measurement noise covariance matrix
		KF.R = new DenseMatrix(1,1);
		Matrix t = new DenseMatrix(1,1,1);
		t.Multiply(measurementNoiseVariance, KF.R);
		
		return KF;
	}
}