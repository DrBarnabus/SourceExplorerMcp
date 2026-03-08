using ICSharpCode.Decompiler.CSharp.Syntax;

namespace SourceExplorerMcp.Core.Services;

public sealed class SignatureOnlyVisitor : DepthFirstAstVisitor
{
    public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
    {
        RemoveBody(methodDeclaration.Body);
    }

    public override void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
    {
        RemoveBody(constructorDeclaration.Body);
    }

    public override void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
    {
        RemoveBody(destructorDeclaration.Body);
    }

    public override void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
    {
        RemoveBody(operatorDeclaration.Body);
    }

    public override void VisitAccessor(Accessor accessor)
    {
        RemoveBody(accessor.Body);
    }

    public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
    {
        RemoveExpressionBody(propertyDeclaration);
        base.VisitPropertyDeclaration(propertyDeclaration);
    }

    public override void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
    {
        RemoveExpressionBody(indexerDeclaration);
        base.VisitIndexerDeclaration(indexerDeclaration);
    }

    private static void RemoveBody(BlockStatement? body)
    {
        if (body is not null && !body.IsNull)
            body.Remove();
    }

    private static void RemoveExpressionBody(EntityDeclaration declaration)
    {
        var expressionBody = declaration.GetChildByRole(PropertyDeclaration.ExpressionBodyRole);
        if (expressionBody is not null && !expressionBody.IsNull)
            expressionBody.Remove();
    }
}
