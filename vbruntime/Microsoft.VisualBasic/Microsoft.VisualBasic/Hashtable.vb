'
' Hashtable.vb
'
' Author:
'   Rolf Bjarne Kvinge (RKvinge@novell.com)
'
' Copyright (C) 2009 Novell, Inc (http://www.novell.com)
'
' Permission is hereby granted, free of charge, to any person obtaining
' a copy of this software and associated documentation files (the
' "Software"), to deal in the Software without restriction, including
' without limitation the rights to use, copy, modify, merge, publish,
' distribute, sublicense, and/or sell copies of the Software, and to
' permit persons to whom the Software is furnished to do so, subject to
' the following conditions:
' 
' The above copyright notice and this permission notice shall be
' included in all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
' EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
' MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
' NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
' LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
' OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
' WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

'
' Silverlight does not include the ArrayList class, and MS.VB.dll uses it a lot
' so instead of changing a lot of code and adding hacks to make it work on all
' profiles, just add a wrapper class for Moonlight.

#If Moonlight Then
Friend Class Hashtable
    Inherits System.Collections.Generic.Dictionary(Of Object, Object)

    Public ReadOnly Property IsSynchronized() As Boolean
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot() As Object
        Get
            Return Me
        End Get
    End Property

    Public Default Property Item (Key As Object) As Object
        Get
            Dim result As Object
            If Not MyBase.TryGetValue (Key, result) Then Return Nothing
            Return result
        End Get
        Set
            MyBase (Key) = Value
        End Set
    End Property
End Class
#End If