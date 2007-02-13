' 
' Visual Basic.Net Compiler
' Copyright (C) 2004 - 2007 Rolf Bjarne Kvinge, RKvinge@novell.com
' 
' This library is free software; you can redistribute it and/or
' modify it under the terms of the GNU Lesser General Public
' License as published by the Free Software Foundation; either
' version 2.1 of the License, or (at your option) any later version.
' 
' This library is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
' Lesser General Public License for more details.
' 
' You should have received a copy of the GNU Lesser General Public
' License along with this library; if not, write to the Free Software
' Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
' 

Public Class EventDeclaration
    Inherits MemberDeclaration
    Implements IDefinableMember, INonTypeMember, IHasImplicitMembers

    Private m_Descriptor As New EventDescriptor(Me)

    'Set during parse phase
    Private m_Identifier As Identifier
    Private m_ImplementsClause As MemberImplementsClause

    'Set during parse phase (in CreateCompilerGeneratedElements) 
    'or by the Custom Event declaration.
    ''' <summary>The add method.</summary>
    Private m_AddMethod As EventHandlerDeclaration
    ''' <summary>The remove method.</summary>
    Private m_RemoveMethod As EventHandlerDeclaration
    ''' <summary>The raise method. Is only something if it is a custom event.</summary>
    Private m_RaiseMethod As EventHandlerDeclaration

    'Set during resolve member phase (delegate explicitly defined)
    'or during parse phase (delegate implicitly defined)
    ''' <summary>The type of the event. Is the implicitly defined delegate, or the explicitly defined delegate.</summary>
    Private m_EventType As Type

    'Set during define phase
    Private m_Builder As EventBuilder

    Sub New(ByVal Parent As TypeDeclaration)
        MyBase.new(Parent)
    End Sub

    Shadows Sub Init(ByVal Attributes As Attributes, ByVal Modifiers As Modifiers, ByVal Identifier As Identifier, ByVal ImplementsClause As MemberImplementsClause)
        MyBase.Init(Attributes, Modifiers, Identifier.Name)

        m_Identifier = Identifier
        m_ImplementsClause = ImplementsClause

        Helper.Assert(m_Identifier IsNot Nothing)
    End Sub

    Public ReadOnly Property EventBuilder() As System.Reflection.Emit.EventBuilder
        Get
            Return m_Builder
        End Get
    End Property

    Public ReadOnly Property EventDescriptor() As EventDescriptor
        Get
            Return m_Descriptor
        End Get
    End Property

    Public Property EventType() As System.Type
        Get
            Return m_EventType
        End Get
        Set(ByVal value As System.Type)
            Helper.Assert(m_EventType Is Nothing)
            m_EventType = value
        End Set
    End Property

    Public Function GetAddMethod(ByVal nonPublic As Boolean) As System.Reflection.MethodInfo
        Return DirectCast(m_AddMethod.MethodDescriptor, MethodInfo)
    End Function

    Public Function GetRaiseMethod(ByVal nonPublic As Boolean) As System.Reflection.MethodInfo
        If m_RaiseMethod Is Nothing Then Return Nothing
        Return DirectCast(m_RaiseMethod.MethodDescriptor, MethodInfo)
    End Function

    Public Function GetRemoveMethod(ByVal nonPublic As Boolean) As System.Reflection.MethodInfo
        Return DirectCast(m_RemoveMethod.MethodDescriptor, MethodInfo)
    End Function

    Property AddMethod() As EventHandlerDeclaration
        Get
            Return m_AddMethod
        End Get
        Set(ByVal value As EventHandlerDeclaration)
            Helper.Assert(m_AddMethod Is Nothing)
            m_AddMethod = value
        End Set
    End Property

    Property RemoveMethod() As EventHandlerDeclaration
        Get
            Return m_RemoveMethod
        End Get
        Set(ByVal value As EventHandlerDeclaration)
            Helper.Assert(m_RemoveMethod Is Nothing)
            m_RemoveMethod = value
        End Set
    End Property

    Property RaiseMethod() As EventHandlerDeclaration
        Get
            Return m_RaiseMethod
        End Get
        Set(ByVal value As EventHandlerDeclaration)
            Helper.Assert(m_RaiseMethod Is Nothing)
            m_RaiseMethod = value
        End Set
    End Property

    Public Overrides ReadOnly Property MemberDescriptor() As System.Reflection.MemberInfo
        Get
            Return m_Descriptor
        End Get
    End Property

    ReadOnly Property Identifier() As Identifier
        Get
            Return m_Identifier
        End Get
    End Property

    ReadOnly Property ImplementsClause() As MemberImplementsClause
        Get
            Return m_ImplementsClause
        End Get
    End Property

    Public Overrides Function ResolveTypeReferences() As Boolean
        Dim result As Boolean = True

        If m_ImplementsClause IsNot Nothing Then result = m_ImplementsClause.ResolveTypeReferences AndAlso result

        result = MyBase.ResolveTypeReferences AndAlso result

        Helper.Assert(m_EventType IsNot Nothing)

        Return result
    End Function

    Private Function CreateImplicitMembers() As Boolean Implements IHasImplicitMembers.CreateImplicitMembers
        Dim result As Boolean = True

        Helper.Assert(m_AddMethod IsNot Nothing)
        Helper.Assert(m_RemoveMethod IsNot Nothing)

        DeclaringType.Members.Add(m_AddMethod) : result = m_AddMethod.ResolveTypeReferences AndAlso result
        DeclaringType.Members.Add(m_RemoveMethod) : result = m_RemoveMethod.ResolveTypeReferences AndAlso result
        If m_RaiseMethod IsNot Nothing Then DeclaringType.Members.Add(m_RaiseMethod) : result = m_RaiseMethod.ResolveTypeReferences AndAlso result

        Return result
    End Function

    Public Function ResolveMember(ByVal Info As ResolveInfo) As Boolean Implements INonTypeMember.ResolveMember
        Dim result As Boolean = True

        If m_ImplementsClause IsNot Nothing Then result = m_ImplementsClause.ResolveCode(Info) AndAlso result

        Helper.Assert(m_EventType IsNot Nothing)

        Return result
    End Function

    Public Overrides Function ResolveCode(ByVal Info As ResolveInfo) As Boolean
        Dim result As Boolean = True

        result = MyBase.ResolveCode(Info) AndAlso result

        Return result
    End Function

    Public Function DefineMember() As Boolean Implements IDefinableMember.DefineMember
        Dim result As Boolean = True

        Dim parent As TypeDeclaration = Me.FindFirstParent(Of TypeDeclaration)()
        m_EventType = Helper.GetTypeOrTypeBuilder(m_EventType)
        m_Builder = parent.TypeBuilder.DefineEvent(Name, EventAttributes.None, m_EventType)
        Helper.NotImplementedYet("Cannot register event builder, it is not a memberinfo...")
        'Compiler.TypeManager.RegisterReflectionMember(m_Builder, Me.MemberDescriptor)

        Return result
    End Function

    Friend Overrides Function GenerateCode(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True

        Helper.Assert(m_AddMethod.MethodBuilder IsNot Nothing)
        Helper.Assert(m_RemoveMethod.MethodBuilder IsNot Nothing)

        m_Builder.SetAddOnMethod(m_AddMethod.MethodBuilder)
        m_Builder.SetRemoveOnMethod(m_RemoveMethod.MethodBuilder)
        If m_RaiseMethod IsNot Nothing Then m_Builder.SetRaiseMethod(m_RaiseMethod.MethodBuilder)

        result = MyBase.GenerateCode(Info) AndAlso result

        Return result
    End Function
End Class