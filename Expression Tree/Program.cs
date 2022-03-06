using System.Linq.Expressions;
using System.Reflection;
namespace EFG.DMA.Services.FixIn.Routing.Extensions
{
    public static class OriginalClOrderIdExtension
    {
      
        public static List<Messsage> ToFixMessage(this List<string> summeryFileLines)
        {
            try
            {
                return summeryFileLines
                           .Where(summeryFileLine => summeryFileLine?.LinesFilters() ?? false)?
                           .Select(summeryFileLine => Message.Parse(summeryFileLine?.MapSummeryFileLines()))?
                           .ToList() ?? null;
            }
            catch
            {
                return null;

            }
        }


        private static bool LinesFilters(this string summeryFileLines)
        {
            return string.IsNullOrEmpty(summeryFileLines) ? false :
                          summeryFileLines.Trim()
                         .StartsWith("IN") &&
                         (summeryFileLines.Contains("35=D") ||
                          summeryFileLines.Contains("35=G"));
        }

        private static string MapSummeryFileLines(this string summeryFileLine)
        {
            if (string.IsNullOrEmpty(summeryFileLine))
                return summeryFileLine;

            int indexOfBeginString = GetIndexOfBeginString(summeryFileLine);
            if (indexOfBeginString < 0)
                return summeryFileLine;

            return summeryFileLine[indexOfBeginString..^1];

        }


        private static Expression<Func<string, bool>> ExpressionFilter()
        {

            #region Method Parameter
            ParameterExpression summeryFileLine = Expression.Parameter(typeof(string), "summeryFileLine");
            #endregion

            #region Get String Methods Using Reflection
            MethodInfo startWithString = typeof(string).GetMethod(nameof(string.StartsWith), new Type[] { typeof(string) });
            MethodInfo trimString = typeof(string).GetMethod(nameof(string.Trim), new Type[0]);
            MethodInfo containsString = typeof(string).GetMethod(nameof(string.Contains), new Type[] { typeof(string) });
            #endregion

            #region Constant Values For String Methods 
            ConstantExpression startWithValue = Expression.Constant("IN", typeof(string));
            ConstantExpression newOrderSingle = Expression.Constant("35=D", typeof(string));
            ConstantExpression orderCancelReplaceRequest = Expression.Constant("35=G", typeof(string));
            #endregion

            #region String Methods Execution
            MethodCallExpression trimExecution = Expression.Call(summeryFileLine, trimString);
            MethodCallExpression startWithExecution = Expression.Call(trimExecution, startWithString, startWithValue);
            BinaryExpression containsExecution = Expression.Or(
                                                     Expression.Call(summeryFileLine, containsString, newOrderSingle),
                                                     Expression.Call(summeryFileLine, containsString, orderCancelReplaceRequest));
            #endregion

            #region Execute Where Filter
            BinaryExpression whereExecution = Expression.And(startWithExecution, containsExecution);
            #endregion

            #region Return Lambda Exepression 
            return Expression.Lambda<Func<string, bool>>(whereExecution, summeryFileLine);
            #endregion

        }

        private static Expression<Func<string, Message>> ExpressionMapping()
        {
            #region Get Parameter And Property From String 
            ParameterExpression summeryFileLine = Expression.Parameter(typeof(string), "summeryFileLine");
            MemberExpression lenghtProperty = Expression.PropertyOrField(summeryFileLine, "Length");
            #endregion
            #region Get String Methods Using Reflection
            MethodInfo indexOfString = typeof(string).GetMethod(nameof(string.IndexOf), new Type[] { typeof(string) });
            MethodInfo subString = typeof(string).GetMethod(nameof(string.Substring), new Type[] { typeof(Int32), typeof(Int32) });
            MethodInfo parseMessage = typeof(Message).GetMethod(nameof(Message.Parse), new Type[] { typeof(string), typeof(MessageValidationFlags) });
            #endregion

            #region Constant Values For String Methods 
            ConstantExpression messageValidation = Expression.Constant(MessageValidationFlags.None, typeof(MessageValidationFlags));
            ConstantExpression MatchingValueIndexOf = Expression.Constant("8=", typeof(string));
            #endregion

            #region String Methods Execution
            MethodCallExpression indexOfBeginString = Expression.Call(summeryFileLine, indexOfString, MatchingValueIndexOf);
            BinaryExpression lengthSubstructByOne = Expression.Subtract(Expression.Subtract(lenghtProperty, indexOfBeginString), Expression.Constant(1));
            MethodCallExpression subStringExecution = Expression.Call(summeryFileLine, subString, indexOfBeginString, lengthSubstructByOne);
            MethodCallExpression messageParseExecution = Expression.Call(parseMessage, subStringExecution, messageValidation);
            #endregion

            #region Return Lambda Exepression 
            return Expression.Lambda<Func<string, Message>>(messageParseExecution, summeryFileLine);
            #endregion
        }

        private static int GetIndexOfBeginString(this string summeryFileLine) => summeryFileLine.IndexOf("8=");


    }

}
