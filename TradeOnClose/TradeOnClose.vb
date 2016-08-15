#Region "Imports statements"
Imports System
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Linq
Imports RightEdge.Common
Imports RightEdge.Common.ChartObjects
Imports RightEdge.Indicators
#End Region


Public Class MySystem
	Inherits MySystemBase
	Public Overloads Overrides Sub Startup()
		' Perform initialization or set system wide options here 
        SystemData.EnableTradeOnClose = True
        AddHandler SystemData.BarClosing, AddressOf BarClosing
    End Sub

    Public Sub BarClosing(ByVal sender As Object, ByVal args As NewBarEventArgs)
        For Each SymbolScript As MySymbolScript In SymbolScripts
            SymbolScript.BarClosing()
        Next
    End Sub
End Class


Public Class MySymbolScript
	Inherits MySymbolScriptBase
	Public Overloads Overrides Sub Startup()
		' Perform initialization here

	End Sub

	Public Overloads Overrides Sub NewBar()
        ' Put your new bar trading code here 

    End Sub

    Public Sub BarClosing()
        If Bars.Count < 2 Then
            Return
        End If
        If Close.Current < Open.Current AndAlso Close.LookBack(1) < Open.LookBack(1) Then
            ' Open a position after two down bars in a row.	
            OpenPosition(PositionType.Long, OrderType.Market)
        End If
    End Sub

	Public Overloads Overrides Sub OrderFilled(ByVal position As Position, ByVal trade As Trade)
		' This method is called when an order is filled

	End Sub

	Public Overloads Overrides Sub OrderCancelled(ByVal position As Position, ByVal order As Order, ByVal information As String)
		' This method is called when an order is cancelled or rejected

	End Sub
End Class
