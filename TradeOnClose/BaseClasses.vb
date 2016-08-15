Imports System 
Imports System.Collections.Generic 
Imports System.Text 
Imports RightEdge.Common 

' This is an auto-generated file.  You should not need to edit it.

Public MustInherit Class MySystemBase 
    Inherits SystemBase 
	Private _symbolScripts As New SymbolScriptCollection(Of MySymbolScript)()
	Public ReadOnly Property SymbolScripts() As SymbolScriptCollection(Of MySymbolScript)
		Get
			Return _symbolScripts
		End Get
	End Property
    
    Public Overloads Overrides Sub Startup(ByVal data As SystemData) 
        MyBase.Startup(data) 
		SymbolScripts.Initialize(Me)
		For Each symbolScript As MySymbolScriptBase In SymbolScripts
            symbolScript.TradingSystem = DirectCast(Me, MySystem)
			symbolScript.Startup()
			SystemData.IndicatorManager.RegisterMembers(symbolScript, symbolScript.Symbol)
		Next
    End Sub

	Public Overloads Overrides Sub NewBar()
		MyBase.NewBar()
		SymbolScripts.NewBar()
	End Sub

	Public Overloads Overrides Sub NewTick(ByVal symbol As Symbol, ByVal bar As BarData, ByVal tick As TickData)
		MyBase.NewTick(symbol, bar, tick)
		SymbolScripts.NewTick(symbol, bar, tick)
	End Sub

	' Indicators 

End Class

Public Class MySymbolScriptBase
	Inherits SymbolScriptBase
    Public TradingSystem As MySystem 

	Public ReadOnly Property OtherSymbols() As SymbolScriptCollection(Of MySymbolScript)
		Get
			Return TradingSystem.SymbolScripts
		End Get
	End Property


End Class
