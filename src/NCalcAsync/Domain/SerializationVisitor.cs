using System;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;

namespace NCalcAsync.Domain
{
    public class SerializationVisitor : LogicalExpressionVisitor
    {
        private readonly NumberFormatInfo _numberFormatInfo;

        public SerializationVisitor()
        {
            Result = new StringBuilder();
            _numberFormatInfo = new NumberFormatInfo { NumberDecimalSeparator = "." };
        }

        public StringBuilder Result { get; protected set; }

        public override Task VisitAsync(LogicalExpression expression)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override async Task VisitAsync(TernaryExpression expression)
        {
            await EncapsulateNoValue(expression.LeftExpression).ConfigureAwait(false);

            Result.Append("? ");

            await EncapsulateNoValue(expression.MiddleExpression).ConfigureAwait(false);

            Result.Append(": ");

            await EncapsulateNoValue(expression.RightExpression).ConfigureAwait(false);
        }

        public override async Task VisitAsync(BinaryExpression expression)
        {
            await EncapsulateNoValue(expression.LeftExpression).ConfigureAwait(false);

            switch (expression.Type)
            {
                case BinaryExpressionType.And:
                    Result.Append("and ");
                    break;

                case BinaryExpressionType.Or:
                    Result.Append("or ");
                    break;

                case BinaryExpressionType.Div:
                    Result.Append("/ ");
                    break;

                case BinaryExpressionType.Equal:
                    Result.Append("= ");
                    break;

                case BinaryExpressionType.Greater:
                    Result.Append("> ");
                    break;

                case BinaryExpressionType.GreaterOrEqual:
                    Result.Append(">= ");
                    break;

                case BinaryExpressionType.Lesser:
                    Result.Append("< ");
                    break;

                case BinaryExpressionType.LesserOrEqual:
                    Result.Append("<= ");
                    break;

                case BinaryExpressionType.Minus:
                    Result.Append("- ");
                    break;

                case BinaryExpressionType.Modulo:
                    Result.Append("% ");
                    break;

                case BinaryExpressionType.NotEqual:
                    Result.Append("!= ");
                    break;

                case BinaryExpressionType.Plus:
                    Result.Append("+ ");
                    break;

                case BinaryExpressionType.Times:
                    Result.Append("* ");
                    break;

                case BinaryExpressionType.BitwiseAnd:
                    Result.Append("& ");
                    break;

                case BinaryExpressionType.BitwiseOr:
                    Result.Append("| ");
                    break;

                case BinaryExpressionType.BitwiseXOr:
                    Result.Append("~ ");
                    break;

                case BinaryExpressionType.LeftShift:
                    Result.Append("<< ");
                    break;

                case BinaryExpressionType.RightShift:
                    Result.Append(">> ");
                    break;
            }

            await EncapsulateNoValue(expression.RightExpression).ConfigureAwait(false);
        }

        public override async Task VisitAsync(UnaryExpression expression)
        {
            switch (expression.Type)
            {
                case UnaryExpressionType.Not:
                    Result.Append("!");
                    break;

                case UnaryExpressionType.Negate:
                    Result.Append("-");
                    break;

                case UnaryExpressionType.BitwiseNot:
                    Result.Append("~");
                    break;
            }

            await EncapsulateNoValue(expression.Expression).ConfigureAwait(false);
        }

        public override Task VisitAsync(ValueExpression expression)
        {
            switch (expression.Type)
            {
                case ValueType.Boolean:
                    Result.Append(expression.Value.ToString()).Append(" ");
                    break;

                case ValueType.DateTime:
                    Result.Append("#").Append(expression.Value.ToString()).Append("#").Append(" ");
                    break;

                case ValueType.Float:
                    Result.Append(decimal.Parse(expression.Value.ToString(), NumberStyles.Any).ToString(_numberFormatInfo)).Append(" ");
                    break;

                case ValueType.Integer:
                    Result.Append(expression.Value.ToString()).Append(" ");
                    break;

                case ValueType.String:
                    Result.Append("'").Append(expression.Value.ToString()).Append("'").Append(" ");
                    break;
            }

            return Task.CompletedTask;
        }

        public override async Task VisitAsync(Function function)
        {
            Result.Append(function.Identifier.Name);

            Result.Append("(");

            for (int i = 0; i < function.Expressions.Length; i++)
            {
                await function.Expressions[i].AcceptAsync(this);
                if (i < function.Expressions.Length - 1)
                {
                    Result.Remove(Result.Length - 1, 1);
                    Result.Append(", ");
                }
            }

            // trim spaces before adding a closing paren
            while (Result[Result.Length - 1] == ' ')
                Result.Remove(Result.Length - 1, 1);

            Result.Append(") ");
        }

        public override Task VisitAsync(Identifier parameter)
        {
            Result.Append("[").Append(parameter.Name).Append("] ");
            return Task.CompletedTask;
        }

        protected async Task EncapsulateNoValue(LogicalExpression expression)
        {
            if (expression is ValueExpression)
            {
                await expression.AcceptAsync(this).ConfigureAwait(false);
            }
            else
            {
                Result.Append("(");
                await expression.AcceptAsync(this).ConfigureAwait(false);

                // trim spaces before adding a closing paren
                while (Result[Result.Length - 1] == ' ')
                    Result.Remove(Result.Length - 1, 1);

                Result.Append(") ");
            }
        }

    }
}