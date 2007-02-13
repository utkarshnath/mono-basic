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

#If DEBUG Then
#Const EXTENDEDDEBUG = 0
#End If
''' <summary>
''' A single identifier followed by an optional type argument list.
''' Classifications: Variable, Type, Value, Namespace
''' 
''' SimpleNameExpression ::= Identifier [ "(" "Of" TypeArgumentList ")" ]
''' </summary>
''' <remarks></remarks>
Public Class SimpleNameExpression
    Inherits Expression

    Private m_Identifier As IdentifierToken
    Private m_TypeArgumentList As TypeArgumentList

    Sub New(ByVal Parent As ParsedObject)
        MyBase.New(Parent)
    End Sub

    Sub Init(ByVal Identifier As IdentifierToken, ByVal TypeArgumentList As TypeArgumentList)
        m_Identifier = Identifier
        m_TypeArgumentList = TypeArgumentList
    End Sub

    Public Overrides Function Clone(Optional ByVal NewParent As ParsedObject = Nothing) As Expression
        If NewParent Is Nothing Then NewParent = Me.Parent
        Dim result As New SimpleNameExpression(NewParent)
        If m_TypeArgumentList Is Nothing Then
            result.Init(m_Identifier, Nothing)
        Else
            result.Init(m_Identifier, m_TypeArgumentList.Clone(result))
        End If

        Return result
    End Function

    Property Identifier() As IdentifierToken
        Get
            Return m_Identifier
        End Get
        Set(ByVal value As IdentifierToken)
            m_Identifier = value
        End Set
    End Property

    Overrides ReadOnly Property ExpressionType() As Type
        Get
            Dim result As Type
            Select Case Classification.Classification
                Case ExpressionClassification.Classifications.Value
                    result = Classification.AsValueClassification.Type
                Case ExpressionClassification.Classifications.Variable
                    result = Classification.AsVariableClassification.Type
                Case ExpressionClassification.Classifications.Type
                    Helper.NotImplemented() : result = Nothing
                Case ExpressionClassification.Classifications.Namespace
                    Helper.NotImplemented() : result = Nothing
                Case ExpressionClassification.Classifications.PropertyGroup
                    result = Classification.AsPropertyGroup.Type
                Case ExpressionClassification.Classifications.PropertyAccess
                    result = Classification.AsPropertyAccess.Type
                Case ExpressionClassification.Classifications.MethodGroup
                    result = Classification.AsMethodGroupClassification.Type
                Case Else
                    Throw New InternalException(Me)
            End Select
            Helper.Assert(result IsNot Nothing)
            Return result
        End Get
    End Property

    Public Overrides Function ResolveTypeReferences() As Boolean
        Dim result As Boolean = True

        If m_TypeArgumentList IsNot Nothing Then result = m_TypeArgumentList.ResolveTypeReferences AndAlso result

        Return result
    End Function

    Public Overrides Function ToString() As String
        Return m_Identifier.Identifier
    End Function

    Protected Overrides Function GenerateCodeInternal(ByVal Info As EmitInfo) As Boolean
        Dim result As Boolean = True

        Helper.Assert(Info.DesiredType IsNot Nothing OrElse Info.RHSExpression IsNot Nothing)

        If Info.IsRHS Then
            If Info.DesiredType IsNot Nothing Then
                If Info.DesiredType.IsGenericParameter AndAlso Me.ExpressionType.IsGenericParameter Then
                    Helper.Assert(Me.Classification.CanBeValueClassification)
                    Dim tmp As Expression
                    tmp = Me.ReclassifyToValueExpression()
                    result = tmp.ResolveExpression(ResolveInfo.Default(Info.Compiler)) AndAlso result
                    result = tmp.GenerateCode(Info) AndAlso result
                ElseIf Info.DesiredType.IsByRef = False AndAlso Me.ExpressionType.IsGenericParameter = False Then
                    If Me.Classification.CanBeValueClassification Then
                        Dim tmp As Expression
                        tmp = Me.ReclassifyToValueExpression()
                        result = tmp.ResolveExpression(ResolveInfo.Default(Info.Compiler)) AndAlso result
                        result = tmp.Classification.GenerateCode(Info) AndAlso result
                    Else
                        Throw New InternalException(Me)
                    End If
                Else
                    If Me.Classification.IsVariableClassification Then
                        If Me.ExpressionType.IsByRef Then
                            Emitter.EmitLoadVariable(Info, Me.Classification.AsVariableClassification)
                        Else
                            Emitter.EmitLoadVariableLocation(Info, Me.Classification.AsVariableClassification)
                        End If
                    Else
                        Throw New InternalException(Me)
                    End If
                End If
            Else
                If Me.Classification.CanBeValueClassification Then
                    Dim tmp As Expression
                    tmp = Me.ReclassifyToValueExpression()
                    result = tmp.ResolveExpression(ResolveInfo.Default(Info.Compiler)) AndAlso result
                    result = tmp.GenerateCode(Info) AndAlso result
                Else
                    Throw New InternalException(Me)
                End If
            End If
        ElseIf Info.IsLHS Then
            If Me.Classification.IsVariableClassification Then
                result = Me.Classification.AsVariableClassification.GenerateCode(Info) AndAlso result
            ElseIf Me.Classification.IsValueClassification Then
                Throw New InternalException(Me)
            Else
                Throw New InternalException(Me)
            End If
        Else
            Throw New InternalException(Me)
        End If

        Return result
    End Function

    Public Overrides ReadOnly Property IsConstant() As Boolean
        Get
            Return Classification.IsConstant
        End Get
    End Property

    Public Overrides ReadOnly Property ConstantValue() As Object
        Get
            Return Classification.ConstantValue
        End Get
    End Property

    Shared Function IsMe(ByVal tm As tm) As Boolean
        Return tm.CurrentToken.IsIdentifier
    End Function

    Protected Overrides Function ResolveExpressionInternal(ByVal Info As ResolveInfo) As Boolean
        Dim Name As String = m_Identifier.Identifier

        If False Then Stop

        '---------------------------------------------------------------------------------------------------------
        'A simple name expression consists of a single identifier followed by an optional type argument list. 
        'The name is resolved and classified as follows:
        '---------------------------------------------------------------------------------------------------------
        '* Starting with the immediately enclosing block and continuing with each enclosing outer block (if any),
        '  if the identifier matches the name of a local variable, static variable, constant local, method type 
        '  parameter, or parameter, then the identifier refers to the matching entity. The expression is 
        '  classified as a variable if it is a local variable, static variable, or parameter. The expression 
        '  is classified as a type if it is a method type parameter. The expression is classified as a value 
        '  if it is a constant local with the following exception. If the local variable matched is the 
        '  implicit function or Get accessor return local variable, and the expression is part of an 
        '  invocation expression, invocation statement, or an AddressOf expression, then no match occurs and 
        '  resolution continues.
        '---------------------------------------------------------------------------------------------------------
        '* For each nested type containing the expression, starting from the innermost and going to the 
        '  outermost, if a lookup of the identifier in the type produces a match with an accessible member:
        '** If the matching type member is a type parameter, then the result is classified as a type and 
        '   is the matching type parameter.
        '** Otherwise, if the type is the immediately enclosing type and the lookup identifies a non-shared 
        '   type member, then the result is the same as a member access of the form Me.E, where E is 
        '   the identifier.
        '** Otherwise, the result is exactly the same as a member access of the form T.E, where T is the 
        '   type containing the matching member and E is the identifier. In this case, it is an error for the 
        '   identifier to refer to a non-shared member.
        '---------------------------------------------------------------------------------------------------------
        '* For each nested namespace, starting from the innermost and going to the outermost namespace, 
        '  do the following:
        '** If the namespace contains an accessible namespace member with the given name, then the identifier
        '   refers to that member and, depending on the member, is classified as a namespace or a type.
        '** Otherwise, if the namespace contains one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then the 
        '   result is exactly the same as a member access of the form M.E, where M is the standard module 
        '   containing the matching member and E is the identifier. If the identifier matches accessible type 
        '   members in more than one standard module, a compile-time error occurs.
        '---------------------------------------------------------------------------------------------------------
        '* If the source file has one or more import aliases, and the identifier matches the name of one of them,
        '   then the identifier refers to that namespace or type.
        '---------------------------------------------------------------------------------------------------------
        '* If the source file containing the name reference has one or more imports:
        '** If the identifier matches the name of an accessible type or type member in exactly one import, 
        '   then the identifier refers to that type or type member. If the identifier matches the name of 
        '   an accessible type or type member in more than one import, a compile-time error occurs.
        '** If the identifier matches the name of a namespace in exactly one import, then the identifier 
        '   refers to that namespace. If the identifier matches the name of a namespace in more than one import, 
        '   a compile-time error occurs.
        '** Otherwise, if the imports contain one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then 
        '   the result is exactly the same as a member access of the form M.E, where M is the standard 
        '   module containing the matching member and E is the identifier. If the identifier matches 
        '   accessible type members in more than one standard module, a compile-time error occurs.
        '---------------------------------------------------------------------------------------------------------
        '* If the compilation environment defines one or more import aliases, and the identifier matches 
        '  the name of one of them, then the identifier refers to that namespace or type.
        '---------------------------------------------------------------------------------------------------------
        '* If the compilation environment defines one or more imports:
        '** If the identifier matches the name of an accessible type or type member in exactly one import, 
        '   then the identifier refers to that type or type member. If the identifier matches the name 
        '   of an accessible type or type member in more than one import, a compile-time error occurs.
        '** If the identifier matches the name of a namespace in exactly one import, then the identifier 
        '   refers to that namespace. If the identifier matches the name of a namespace in more than 
        '   one import, a compile-time error occurs.
        '** Otherwise, if the imports contain one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then the result 
        '   is exactly the same as a member access of the form M.E, where M is the standard module containing 
        '   the matching member and E is the identifier. If the identifier matches accessible type members in 
        '   more than one standard module, a compile-time error occurs.
        '---------------------------------------------------------------------------------------------------------
        '* Otherwise, the name given by the identifier is undefined and a compile-time error occurs.
        '---------------------------------------------------------------------------------------------------------
        'If a simple name with a type argument list resolves to anything other than a type or method, 
        'a compile time error occurs. If a type argument list is supplied, only types with the same arity as 
        'the type argument list are considered but type members, including methods with different arities, 
        'are still considered. This is because type inference can be used to fill in missing type arguments. 
        'As a result, names with type arguments may bind differently to types and methods:
        '---------------------------------------------------------------------------------------------------------

        If m_TypeArgumentList IsNot Nothing Then If m_TypeArgumentList.ResolveCode(info) = False Then Return False

        '* Starting with the immediately enclosing block and continuing with each enclosing outer block (if any),
        '  if the identifier matches the name of a local variable, static variable, constant local, method type 
        '  parameter, or parameter, then the identifier refers to the matching entity. 
        '  - The expression is classified as a variable if it is a local variable, static variable, or parameter.
        '  - The expression is classified as a type if it is a method type parameter. 
        '  - The expression is classified as a value if it is a constant local.
        '  * With the following exception:
        '  If the local variable matched is the implicit function or Get accessor return local variable, 
        '  and the expression is part of an  invocation expression, invocation statement, 
        '  or an AddressOf expression, then no match occurs and resolution continues.
        Dim block As CodeBlock = Me.FindFirstParent(Of CodeBlock)()
        While block IsNot Nothing
            Dim var As IAttributableNamedDeclaration
            var = block.FindVariable(Name)
            If TypeOf var Is ConstantDeclaration Then
                'The expression is classified as a value if it is a constant local (...)
                Classification = New ValueClassification(Me, DirectCast(var, ConstantDeclaration))
                Return True
            ElseIf TypeOf var Is VariableDeclaration Then
                'The expression is classified as a variable if it is a local variable, static variable (...)
                Dim varDecl As VariableDeclaration
                varDecl = DirectCast(var, VariableDeclaration)
                If varDecl.Modifiers.ContainsAny(KS.Static) AndAlso varDecl.DeclaringMethod.IsShared = False Then
                    Classification = New VariableClassification(Me, varDecl, CreateMeExpression)
                Else
                    Classification = New VariableClassification(Me, varDecl)
                End If
                Return True
            ElseIf var IsNot Nothing Then
                Throw New InternalException(Me)
            End If
            block = block.FindFirstParent(Of CodeBlock)()
        End While

        Dim method As IMethod
        method = Me.FindFirstParent(Of IMethod)()
        If method IsNot Nothing Then
            If method.Signature.TypeParameters IsNot Nothing Then
                Dim typeparam As TypeParameter = method.Signature.TypeParameters.Parameters.Item(Name)
                If typeparam IsNot Nothing Then
                    'The expression is classified as a type if it is a method type parameter. 
                    Classification = New TypeClassification(Me, typeparam)
                    Return True
                End If
            End If
        End If

        If method IsNot Nothing Then
            If method.Signature.Parameters IsNot Nothing Then
                Dim param As Parameter = method.Signature.Parameters.Item(Name)
                If param IsNot Nothing Then
                    'The expression is classified as a variable if it is a (...) parameter
                    Classification = New VariableClassification(Me, param)
                    Return True
                End If
            End If
        End If

        '  If the local variable matched is the implicit function or Get accessor return local variable, 
        '  and the expression is part of an  invocation expression, invocation statement, 
        '  or an AddressOf expression, then no match occurs and resolution continues.
        If method IsNot Nothing Then
            If method.HasReturnValue AndAlso Info.SkipFunctionReturnVariable = False Then
                If NameResolution.CompareName(method.Name, Name) Then
                    'The expression is classified as a variable if it is a local variable, static variable (...)
                    Classification = New VariableClassification(Me, method)
                    Return True
                End If
            End If
        End If

        '* For each nested type containing the expression, starting from the innermost and going to the 
        '  outermost, if a lookup of the identifier in the type produces a match with an accessible member:
        '** If the matching type member is a type parameter, then the result is classified as a type and 
        '   is the matching type parameter.
        '** Otherwise, if the type is the immediately enclosing type and the lookup identifies a non-shared 
        '   type member, then the result is the same as a member access of the form Me.E, where E is 
        '   the identifier.
        '** Otherwise, the result is exactly the same as a member access of the form T.E, where T is the 
        '   type containing the matching member and E is the identifier. In this case, it is an error for the 
        '   identifier to refer to a non-shared member.
        Dim firstcontainer As IType = Me.FindFirstParent(Of IType)()
        Dim container As IType = firstcontainer
        While container IsNot Nothing
            Dim constructable As IConstructable = TryCast(container, IConstructable)
            If constructable IsNot Nothing AndAlso constructable.TypeParameters IsNot Nothing Then
                Dim typeparam As TypeParameter = constructable.TypeParameters.Parameters.Item(Name)
                If typeparam IsNot Nothing Then
                    'If the matching type member is a type parameter, then the result is classified 
                    'as a type and is the matching type parameter.
                    Classification = New TypeClassification(Me, typeparam)
                    Return True
                End If
            End If

            Dim members As Generic.List(Of MemberInfo)
            members = Compiler.TypeManager.GetCache(container.TypeDescriptor).LookupMembersFlattened(Name)
            members = Helper.FilterExternalInaccessible(Compiler, members)

#If EXTENDEDDEBUG Then
            Compiler.Report.WriteLine("Found " & membersArray.Length & " members, after filtering by name it's " & members.Count & " members")
#End If

            Helper.ApplyTypeArguments(members, m_TypeArgumentList)

            If members.Count > 0 Then
                'Otherwise, if the type is the immediately enclosing type and the lookup identifies a non-shared 
                'type member, then the result is the same as a member access of the form Me.E, where E is 
                'the identifier.

                'Otherwise, the result is exactly the same as a member access of the form T.E, where T is the 
                'type containing the matching member and E is the identifier. In this case, it is an error for the 
                'identifier to refer to a non-shared member.

                'NOTE: it is not possible to determine yet if the resolved member is shared or not 
                '(it can resolve to a method group with several methods, some shared, some not. 
                'So we create a classification with an instance expression, if the member is 
                'shared, the instance expression should not be used.
                Dim hasInstanceExpression As Boolean
                Dim hasNotInstanceExpression As Boolean

                For Each member As MemberInfo In members
                    If member.MemberType = MemberTypes.TypeInfo OrElse member.MemberType = MemberTypes.NestedType Then
                        hasNotInstanceExpression = True
                    ElseIf Helper.IsShared(member) Then
                        hasNotInstanceExpression = True
                    Else
                        hasInstanceExpression = True
                    End If
                Next

                If container Is firstcontainer AndAlso hasInstanceExpression Then
                    'Otherwise, if the type is the immediately enclosing type and the lookup identifies a non-shared 
                    'type member, then the result is the same as a member access of the form Me.E, where E is 
                    'the identifier.
                    Classification = GetMeClassification(members, firstcontainer)
                    Return True
                Else
                    'Otherwise, the result is exactly the same as a member access of the form T.E, where T is the 
                    'type containing the matching member and E is the identifier. In this case, it is an error for the                    'identifier to refer to a non-shared member.
                    Classification = GetTypeClassification(members, firstcontainer)
                    Return True
                End If
            End If
            container = DirectCast(container, BaseObject).FindFirstParent(Of IType)()
        End While

        '* For each nested namespace, starting from the innermost and going to the outermost namespace, 
        '  do the following:
        '** If the namespace contains an accessible namespace member with the given name, then the identifier
        '   refers to that member and, depending on the member, is classified as a namespace or a type.
        '** Otherwise, if the namespace contains one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then the 
        '   result is exactly the same as a member access of the form M.E, where M is the standard module 
        '   containing the matching member and E is the identifier. If the identifier matches accessible type 
        '   members in more than one standard module, a compile-time error occurs.
        Dim currentNS As String = firstcontainer.Namespace
        While currentNS IsNot Nothing
            Dim foundType As Type
            foundType = Compiler.TypeManager.GetTypesByNamespace(currentNS).Item(Name)
            If foundType IsNot Nothing Then
                Classification = New TypeClassification(Me, foundType)
                Return True
            End If
            If currentNS <> "" Then
                Dim foundNS As [Namespace]
                foundNS = Compiler.TypeManager.Namespaces(currentNS & "." & Name)
                If foundNS IsNot Nothing Then
                    Classification = New NamespaceClassification(Me, foundNS)
                    Return True
                End If
            End If

            'Otherwise, if the namespace contains one or more accessible standard modules, and a member name 
            'lookup of the identifier produces an accessible match in exactly one standard module, then the 
            'result is exactly the same as a member access of the form M.E, where M is the standard module 
            'containing the matching member and E is the identifier. If the identifier matches accessible type 
            'members in more than one standard module, a compile-time error occurs.
            Dim modulemembers As Generic.List(Of MemberInfo)
            modulemembers = Helper.GetMembersOfTypes(Compiler, Compiler.TypeManager.GetModulesByNamespace(currentNS), Name)
            If modulemembers.Count = 1 Then
                Helper.NotImplemented()
                Return True
            ElseIf modulemembers.Count > 1 Then
                Helper.AddError()
            End If

            currentNS = Helper.GetNamespaceParent(currentNS)
        End While
        If CheckOutermostNamespace(Name) Then Return True

        '* If the source file has one or more import aliases, and the identifier matches the name of one of them,
        '   then the identifier refers to that namespace or type.
        If ResolveAliasImports(Me.Location.File.Imports, Name) Then Return True

        '* If the source file containing the name reference has one or more imports:
        '** If the identifier matches the name of an accessible type or type member in exactly one import, 
        '   then the identifier refers to that type or type member. If the identifier matches the name of 
        '   an accessible type or type member in more than one import, a compile-time error occurs.
        '** If the identifier matches the name of a namespace in exactly one import, then the identifier 
        '   refers to that namespace. If the identifier matches the name of a namespace in more than one import, 
        '   a compile-time error occurs.
        '** Otherwise, if the imports contain one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then 
        '   the result is exactly the same as a member access of the form M.E, where M is the standard 
        '   module containing the matching member and E is the identifier. If the identifier matches 
        '   accessible type members in more than one standard module, a compile-time error occurs.
        If ResolveImports(Me.Location.File.Imports, Name) Then Return True

        '* If the compilation environment defines one or more import aliases, and the identifier matches 
        '  the name of one of them, then the identifier refers to that namespace or type.
        If ResolveAliasImports(Me.Compiler.CommandLine.Imports.Clauses, Name) Then Return True

        '* If the compilation environment defines one or more imports:
        '** If the identifier matches the name of an accessible type or type member in exactly one import, 
        '   then the identifier refers to that type or type member. If the identifier matches the name 
        '   of an accessible type or type member in more than one import, a compile-time error occurs.
        '** If the identifier matches the name of a namespace in exactly one import, then the identifier 
        '   refers to that namespace. If the identifier matches the name of a namespace in more than 
        '   one import, a compile-time error occurs.
        '** Otherwise, if the imports contain one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then the result 
        '   is exactly the same as a member access of the form M.E, where M is the standard module containing 
        '   the matching member and E is the identifier. If the identifier matches accessible type members in 
        '   more than one standard module, a compile-time error occurs.
        If ResolveImports(Me.Compiler.CommandLine.Imports.Clauses, Name) Then Return True

        '* Otherwise, the name given by the identifier is undefined and a compile-time error occurs.
        Helper.AddError("Name '" & Name & "' could not be resolved.")

        Return False
    End Function

    Private Function GetTypeClassification(ByVal members As Generic.List(Of MemberInfo), ByVal type As IType) As ExpressionClassification
        'Otherwise, the result is exactly the same as a member access of the form T.E, where T is the 
        'type containing the matching member and E is the identifier. In this case, it is an error for the 
        'identifier to refer to a non-shared member.

        Dim first As MemberInfo = members(0)
        '* If E is a built-in type or an expression classified as a type, and I is the name of an accessible 
        '  member of E, then E.I is evaluated and classified as follows:

        '** If I is the keyword New, then a compile-time error occurs.
        '(not applicable)

        '** If I identifies one or more methods, then the result is a method group with the associated 
        '   type argument list and no associated instance expression.
        If Helper.IsMethodDeclaration(first) Then
            Return New MethodGroupClassification(Me, Nothing, Nothing, members)
        End If

        '** If I identifies one or more properties, then the result is a property group with no associated 
        '   instance expression.
        If Helper.IsPropertyDeclaration(first) Then
            Return New PropertyGroupClassification(Me, Nothing, members)
        End If

        If members.Count > 1 Then Throw New InternalException(Me)

        '** If I identifies a type, then the result is that type.
        If Helper.IsTypeDeclaration(first) Then
            Return New TypeClassification(Me, first)
        End If

        '** If I identifies a shared variable, and if the variable is read-only, and the reference occurs 
        '   outside the shared constructor of the type in which the variable is declared, then the result is the 
        '   value of the shared variable I in E. Otherwise, the result is the shared variable I in E.
        If Helper.IsFieldDeclaration(first) Then
            Dim var As FieldInfo = TryCast(first, FieldInfo)
            Dim constructor As ConstructorDeclaration = Me.FindFirstParent(Of ConstructorDeclaration)()
            If var.IsStatic AndAlso var.IsInitOnly AndAlso _
             (constructor Is Nothing OrElse constructor.Modifiers.Is(KS.Shared) = True) Then
                Return New ValueClassification(Me, var, Nothing)
            Else
                Return New VariableClassification(Me, var, Nothing)
            End If
        End If

        '** If I identifies a shared event, the result is an event access with no associated instance expression.
        If Helper.IsEventDeclaration(first) Then
            Dim red As EventInfo = DirectCast(first, EventInfo)
            If red.GetAddMethod.IsStatic OrElse red.GetRemoveMethod.IsStatic Then
                Return New EventAccessClassification(Me, red, Nothing)
            End If
        End If

        '** If I identifies a constant, then the result is the value of that constant.
        If first.MemberType = MemberTypes.Field AndAlso DirectCast(first, FieldInfo).IsLiteral Then
            Return New ValueClassification(Me, DirectCast(first, FieldInfo), Nothing)
        End If

        '** If I identifies an enumeration member, then the result is the value of that enumeration member.
        If Helper.IsEnumFieldDeclaration(first) Then
            Return New ValueClassification(Me, DirectCast(first, FieldInfo), Nothing)
        End If

        '** Otherwise, E.I is an invalid member reference, and a compile-time error occurs.
        Helper.AddError()

        Return Nothing
    End Function

    Private Function CreateMeExpression() As MeExpression
        Dim result As New MeExpression(Me)
        If result.ResolveExpression(ResolveInfo.Default(Parent.Compiler)) = False Then Throw New InternalException(Me)
        Return result
    End Function

    Private Function GetMeClassification(ByVal members As Generic.List(Of MemberInfo), ByVal type As IType) As ExpressionClassification
        Dim result As ExpressionClassification
        Dim first As MemberInfo = members(0)

        'Otherwise, if the type is the immediately enclosing type and the lookup identifies a non-shared 
        'type member, then the result is the same as a member access of the form Me.E, where E is 
        'the identifier.


        '* If E is classified as a variable or value, the type of which is T, and I is the name of an accessible 
        '  member of E, then E.I is evaluated and classified as follows:

        '** If I is the keyword New and E is an instance expression (Me, MyBase, or MyClass), then the result is 
        '   a method group representing the instance constructors of the type of E with an associated 
        '   instance expression of E and no type argument list. Otherwise, a compile-time error occurs.
        '(not applicable)

        '** If I identifies one or more methods, then the result is a method group with the associated type 
        '   argument list and an associated instance expression of E.
        If Helper.IsMethodDeclaration(first) Then
            result = New MethodGroupClassification(Me, CreateMeExpression, Nothing, members)
            Return result
        End If

        '** If I identifies one or more properties, then the result is a property group with an 
        '   associated instance expression of E.
        If Helper.IsPropertyDeclaration(first) Then
            result = New PropertyGroupClassification(Me, CreateMeExpression, members)
            Return result
        End If

        If members.Count > 1 Then
            Compiler.Report.WriteLine("Found " & members.Count & " members for SimpleNameExpression = " & Me.ToString & ", " & Me.Location.ToString)
            For i As Integer = 0 To members.Count - 1
                Compiler.Report.WriteLine(">#" & (i + 1).ToString & ".MemberType=" & members(i).MemberType.ToString & ",DeclaringType=" & members(i).DeclaringType.FullName)
            Next
            Helper.Stop()
        End If

        '** If I identifies a shared variable or an instance variable, and if the variable is read-only, 
        '   and the reference occurs outside a constructor of the class in which the variable is declared 
        '   appropriate for the kind of variable (shared or instance), then the result is the value of the 
        '   variable I in the object referenced by E. 
        '   If T is a reference type, then the result is the variable 
        '   I in the object referenced by E. 
        '   Otherwise, if T is a value type and the expression E is classified 
        '   as a variable, the result is a variable; otherwise the result is a value.
        If Helper.IsFieldDeclaration(first) Then
            Dim var As FieldInfo = DirectCast(first, FieldInfo)
            Helper.Assert(Parent.FindFirstParent(Of EnumDeclaration)() Is Nothing)

            Dim ctorParent As ConstructorDeclaration
            Dim methodParent As IMethod
            Dim typeParent As TypeDeclaration
            Dim isNotInCtorAndReadOnly As Boolean
            ctorParent = FindFirstParent(Of ConstructorDeclaration)()
            methodParent = FindFirstParent(Of IMethod)()
            typeParent = FindFirstParent(Of TypeDeclaration)()

            isNotInCtorAndReadOnly = var.IsInitOnly AndAlso (ctorParent Is Nothing OrElse ctorParent.Modifiers.Is(KS.Shared) <> var.IsStatic) AndAlso (typeParent Is Nothing OrElse typeParent.IsShared <> var.IsStatic)

            If isNotInCtorAndReadOnly Then ' >?? (Parent.FindFirstParent(Of IMethod).Modifiers.Is(KS.Shared) <> var.IsStatic) Then
                Return New ValueClassification(Me, var, CreateMeExpression)
            ElseIf TypeOf type Is ClassDeclaration Then
                Return New VariableClassification(Me, var, CreateMeExpression)
            ElseIf TypeOf type Is StructureDeclaration Then
                Return New VariableClassification(Me, var, CreateMeExpression)
            Else
                Throw New InternalException(Me)
            End If
        End If

        '** If I identifies an event, the result is an event access with an associated instance expression of E.
        If Helper.IsEventDeclaration(first) Then
            If TypeOf first Is EventInfo Then
                Return New EventAccessClassification(Me, DirectCast(first, EventInfo), CreateMeExpression)
            Else
                Throw New InternalException(Me)
            End If
        End If

        '** If I identifies a constant, then the result is the value of that constant.
        If first.MemberType = MemberTypes.Field AndAlso DirectCast(first, FieldInfo).IsLiteral Then
            Return New ValueClassification(Me, DirectCast(first, FieldInfo), Nothing)
        End If

        '** If I identifies an enumeration member, then the result is the value of that enumeration member.
        '(not applicable)

        '** If T is Object, then the result is a late-bound member lookup classified as a late-bound access 
        '   with an associated instance expression of E.
        '(not applicable)

        '** Otherwise, E.I is an invalid member reference, and a compile-time error occurs.
        Helper.AddError()

        Return Nothing
    End Function

    Private Function CheckOutermostNamespace(ByVal R As String) As Boolean

        '---------------------------------------------------------------------------------------------------------
        '* For each nested namespace, starting from the innermost and going to the outermost namespace, 
        '  do the following:
        '** If the namespace contains an accessible namespace member with the given name, then the identifier
        '   refers to that member and, depending on the member, is classified as a namespace or a type.
        '** Otherwise, if the namespace contains one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then the 
        '   result is exactly the same as a member access of the form M.E, where M is the standard module 
        '   containing the matching member and E is the identifier. If the identifier matches accessible type 
        '   members in more than one standard module, a compile-time error occurs.
        '**	If R matches the name of an accessible type or nested namespace in the current namespace, then the
        '** unqualified name refers to that type or nested namespace.
        '---------------------------------------------------------------------------------------------------------
        Dim foundNamespace As [Namespace] = Nothing
        Dim foundType As Type

        foundType = Compiler.TypeManager.TypesByNamespace("").Item(R)
        If foundType Is Nothing AndAlso Compiler.Assembly.Name <> "" Then
            foundType = Compiler.TypeManager.TypesByNamespace(Compiler.Assembly.Name).Item(R)
        End If

        foundNamespace = Compiler.TypeManager.Namespaces(R)
        If foundNamespace IsNot Nothing AndAlso foundType Is Nothing Then
            Classification = New NamespaceClassification(Me, foundNamespace)
            Return True
        ElseIf foundNamespace Is Nothing AndAlso foundType IsNot Nothing Then
            Classification = New TypeClassification(Me, foundtype)
            Return True
        ElseIf foundNamespace IsNot Nothing AndAlso foundType IsNot Nothing Then
            Helper.AddError()
        End If

        If foundNamespace Is Nothing Then Return False

        Dim modules As TypeDictionary
        Dim members As Generic.List(Of MemberInfo)
        modules = Compiler.TypeManager.GetModulesByNamespace(foundNamespace)
        members = Helper.GetMembersOfTypes(Compiler, modules, R)
        If members.Count = 1 Then
            Helper.Assert(Helper.IsTypeDeclaration(members(0)))
            Classification = New TypeClassification(Me, members(0))
        ElseIf members.Count > 1 Then
            Helper.AddError()
        End If

        Return False
    End Function

    Private Function ResolveAliasImports(ByVal imps As ImportsClauses, ByVal Name As String) As Boolean
        Dim import As ImportsClause = imps.Item(Name)
        Dim nsimport As ImportsNamespaceClause
        If import IsNot Nothing Then
            nsimport = import.AsAliasClause.NamespaceClause
            If nsimport.IsNamespaceImport Then
                Classification = New NamespaceClassification(Me, nsimport.NamespaceImported)
                Return True
            ElseIf nsimport.IsTypeImport Then
                Classification = New TypeClassification(Me, nsimport.TypeImported)
                Return True
            Else
                Throw New InternalException(Me)
            End If
        End If
        Return False
    End Function

    Private Function ResolveImports(ByVal imps As ImportsClauses, ByVal Name As String) As Boolean
        '---------------------------------------------------------------------------------------------------------
        '* If the (source file / compilation environment) containing the name reference has one or more imports:
        '** If the identifier matches the name of an accessible type or type member in exactly one import, 
        '   then the identifier refers to that type or type member. If the identifier matches the name of 
        '   an accessible type or type member in more than one import, a compile-time error occurs.
        '** If the identifier matches the name of a namespace in exactly one import, then the identifier 
        '   refers to that namespace. If the identifier matches the name of a namespace in more than one import, 
        '   a compile-time error occurs.
        '** Otherwise, if the imports contain one or more accessible standard modules, and a member name 
        '   lookup of the identifier produces an accessible match in exactly one standard module, then 
        '   the result is exactly the same as a member access of the form M.E, where M is the standard 
        '   module containing the matching member and E is the identifier. If the identifier matches 
        '   accessible type members in more than one standard module, a compile-time error occurs.
        '---------------------------------------------------------------------------------------------------------
        Dim impmembers As New Generic.List(Of MemberInfo)
        Dim result As Generic.List(Of MemberInfo) = Nothing
        For Each imp As ImportsClause In imps
            If imp.IsNamespaceClause Then
                If imp.AsNamespaceClause.IsNamespaceImport Then
                    'The specified name can only be a type.
                    If Compiler.TypeManager.TypesByNamespace.ContainsKey(imp.AsNamespaceClause.Name) Then
                        result = Compiler.TypeManager.GetTypesByNamespaceAndName(imp.AsNamespaceClause.Name, Name)
                        'Helper.FilterByName(Compiler.TypeManager.TypesByNamespace(imp.AsNamespaceClause.Name).ToTypeList, Name, result)
                    End If
                ElseIf imp.AsNamespaceClause.IsTypeImport Then
                    'result.AddRange(Helper.FilterByName(imp.AsNamespaceClause.TypeImported.GetMembers, Name))
                    'result.AddRange(Compiler.TypeManager.GetCache(imp.AsNamespaceClause.TypeImported).LookupMembersFlattened(Name))
                    result = Compiler.TypeManager.GetCache(imp.AsNamespaceClause.TypeImported).LookupMembersFlattened(Name)
                Else
                    Throw New InternalException(Me)
                End If
            End If
            If result IsNot Nothing AndAlso result.Count > 0 Then
                If impmembers.Count > 0 Then
                    Helper.AddError("If the identifier matches the name of an accessible type or type member in more than one import, a compile-time error occurs.")
                End If
                impmembers.AddRange(result)
                'result.Clear()
            End If
        Next

        If impmembers.Count > 0 Then
            'If the identifier matches the name of an accessible type or type member in exactly one import, 
            'then the identifier refers to that type or type member. If the identifier matches the name of 
            'an accessible type or type member in more than one import, a compile-time error occurs.
            If Helper.IsMethodDeclaration(impmembers(0)) Then
                Classification = New MethodGroupClassification(Me, Nothing, Nothing, impmembers)
                Return True
            End If
            If Helper.IsTypeDeclaration(impmembers(0)) Then
                Classification = New TypeClassification(Me, impmembers(0))
                Return True
            End If
            Helper.NotImplemented()
        End If

        Dim nsmembers As Generic.List(Of [Namespace])
        nsmembers = imps.GetNamespaces(Me, Name)
        If nsmembers.Count = 1 Then
            'If the identifier matches the name of a namespace in exactly one import, then the identifier 
            'refers to that namespace. If the identifier matches the name of a namespace in more than one import, 
            'a compile-time error occurs.
            Classification = New NamespaceClassification(Me, nsmembers(0))
            Return True
        ElseIf nsmembers.Count > 1 Then
            Helper.AddError()
        End If

        'Otherwise, if the imports contain one or more accessible standard modules, and a member name 
        'lookup of the identifier produces an accessible match in exactly one standard module, then 
        'the result is exactly the same as a member access of the form M.E, where M is the standard 
        'module containing the matching member and E is the identifier. If the identifier matches 
        'accessible type members in more than one standard module, a compile-time error occurs.
        Dim modules As TypeList = imps.GetModules(Me)
        Dim found As Generic.List(Of MemberInfo)
        found = Helper.GetMembersOfTypes(Compiler, modules, Name)
        If found.Count >= 1 Then
            If Helper.IsMethodDeclaration(found(0)) Then
                Classification = New MethodGroupClassification(Me, Nothing, Nothing, found)
                Return True
            End If
            If found.Count > 1 Then
                helper.adderror()
            End If
            If Helper.IsTypeDeclaration(found(0)) Then
                Classification = New TypeClassification(Me, found(0))
                Return True
            End If
            Dim first As MemberInfo = found(0)
            If Helper.IsFieldDeclaration(first) Then
                Dim var As FieldInfo = DirectCast(first, FieldInfo)
                Helper.Assert(Parent.FindFirstParent(Of EnumDeclaration)() Is Nothing)

                Classification = New VariableClassification(Me, var, Nothing)
                Return True
            End If
            Helper.NotImplemented()
            Return True
        End If

        Return False
    End Function


#If DEBUG Then
    Public Overrides Sub Dump(ByVal Dumper As IndentedTextWriter)
        m_Identifier.Dump(Dumper)
        If m_TypeArgumentList IsNot Nothing Then
            Dumper.Write("(Of ")
            Compiler.Dumper.Dump(m_TypeArgumentList)
            Dumper.Write(")")
        End If
    End Sub
#End If

End Class