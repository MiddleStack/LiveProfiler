using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiddleStack.Profiling
{
    /// <summary>
    ///     Extension methods for the <see cref="Type"/> class.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly Regex GenericTypeNamePattern = new Regex(@"^(?<name>[^`]+)`(?<paramCount>\d+)", RegexOptions.Compiled);
        /// <summary>
        ///     Converts a <see cref="Type"/> to a LiveProfiler-friendly
        ///     name, with enough specificity but not too long, for use in the Name
        ///     or DisplayName property of a step or transaction.
        /// </summary>
        /// <param name="type">
        ///     The type to extract the name from.
        /// </param>
        /// <returns>
        ///     A LiveProfiler-friendly name of the specified type. Never <see langword="null"/>.
        /// </returns>
        public static string ToLiveProfilerName(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var stringBuilder = new StringBuilder();

            type.BuildProfilerName(false, stringBuilder);

            return stringBuilder.ToString();
        }

        private static void BuildProfilerName(this Type type, bool shortName, StringBuilder stringBuilder)
        {
            if (type.IsGenericType)
            {
                var match = GenericTypeNamePattern.Match(shortName ? type.Name : type.FullName);

                stringBuilder.Append(match.Groups["name"].Value);

                if (type.IsGenericTypeDefinition)
                {
                    var parameterCount = Int32.Parse(match.Groups["paramCount"].Value);
                    stringBuilder.Append("<");
                    stringBuilder.Append(new String(',', parameterCount - 1));
                    stringBuilder.Append(">");
                }
                else
                {
                    stringBuilder.Append("<");

                    for (var i = 0; i < type.GenericTypeArguments.Length; i++)
                    {
                        var typeParameter = type.GenericTypeArguments[i];

                        if (i > 0) stringBuilder.Append(",");

                        typeParameter.BuildProfilerName(true, stringBuilder);
                    }

                    stringBuilder.Append(">");
                }
            }
            else if (type.IsArray)
            {
                BuildProfilerName(type.GetElementType(), false, stringBuilder);
                stringBuilder.Append("[]");
            }
            else
            {
                stringBuilder.Append(shortName ? type.Name : type.FullName);
            }
        }
    }
}
