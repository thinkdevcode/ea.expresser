namespace EA.Expresser.Libs {
    using Simple.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class ExParser {
        internal static Dictionary<string, SimpleExpressionType> dataOperators;
        internal static Dictionary<string, SimpleExpressionType> exprOperators;
        internal static Dictionary<string, SimpleExpression> exprLookup;

        static ExParser() {
            exprOperators = new Dictionary<string, SimpleExpressionType>();
            dataOperators = new Dictionary<string, SimpleExpressionType>();
            exprLookup = new Dictionary<string, SimpleExpression>();

            exprOperators.Add("(", SimpleExpressionType.Empty);
            exprOperators.Add(")", SimpleExpressionType.Empty);
            exprOperators.Add("&&", SimpleExpressionType.And);
            exprOperators.Add("||", SimpleExpressionType.Or);

            dataOperators.Add("==", SimpleExpressionType.Equal);
            dataOperators.Add("!=", SimpleExpressionType.NotEqual);
            dataOperators.Add(">=", SimpleExpressionType.GreaterThanOrEqual);
            dataOperators.Add("<=", SimpleExpressionType.LessThanOrEqual);
            dataOperators.Add(">", SimpleExpressionType.GreaterThan);
            dataOperators.Add("<", SimpleExpressionType.LessThan);
            dataOperators.Add("*", SimpleExpressionType.Function);
        }

        public static SimpleExpression Parse(string query, SimpleReference table) {
            var res = query.RemoveWhiteSpace().Data(table).Order().Operator().ToExpression();
            exprLookup.Clear();
            return res;
        }

        private static SimpleExpression ToExpression(this string query) {
            return exprLookup["E" + (exprLookup.Count - 1)];
        }

        private static string Order(this string query) {
            query = query.Replace(" ", "");
            int count = 0;
            while (query.Contains('(')) {
                int lastIndex = query.LastIndexOf('('),
                    endIndex = query.IndexOf(')', lastIndex);
                string final = query.Slice(lastIndex + 1, endIndex);

                if (endIndex - lastIndex <= 4) { // sanity check for single expr
                    query = query.Replace("(" + final + ")", final);
                    continue;
                }
                else {
                    count++;
                    Operator(final);
                    string exprName = "E" + (exprLookup.Count - 1);
                    query = query.Replace("(" + final + ")", exprName);
                }
            }
            return query;
        }

        private static string Operator(this string query) {
            foreach (var oper in exprOperators) {
                while (query.Contains(oper.Key)) {
                    string[] sub = query.Split(new string[] { oper.Key }, 2, StringSplitOptions.None);

                    // left
                    int lLastIndex = sub[0].LastIndexOf(sub[0].LastIndexOfKey(exprOperators));
                    if (lLastIndex == -1)
                        lLastIndex = 0;
                    else
                        lLastIndex += oper.Key.Length;
                    string lStr = sub[0].Substring(lLastIndex);

                    // right
                    int rFirstIndex = sub[1].IndexOf(sub[1].IndexOfKey(exprOperators));
                    if (rFirstIndex == -1)
                        rFirstIndex = sub[1].Length;
                    string rStr = sub[1].Substring(0, rFirstIndex);

                    string final = lStr + oper.Key + rStr;
                    string exprName = "E" + exprLookup.Count;
                    exprLookup.Add(exprName, BuildSimpleOperatorExpression(final));
                    query = query.Replace(final, exprName);
                }
            }

            return query;
        }

        private static string Data(this string query, dynamic table) {
            foreach (var oper in dataOperators) {
                while (query.Contains(oper.Key)) {
                    string[] sub = query.Split(new string[] { oper.Key }, 2, StringSplitOptions.None);

                    // left
                    string lLastOp = sub[0].LastIndexOfKey(exprOperators);
                    int lLastIndex = sub[0].LastIndexOf(lLastOp);
                    if (lLastIndex == -1)
                        lLastIndex = 0;
                    else
                        lLastIndex += lLastOp.Length;
                    string lStr = sub[0].Substring(lLastIndex);

                    // right
                    int rFirstIndex = sub[1].IndexOf(sub[1].IndexOfKey(exprOperators));
                    if (rFirstIndex == -1)
                        rFirstIndex = sub[1].Length;
                    string rStr = sub[1].Substring(0, rFirstIndex);
                    string rVal = rStr.Replace("'", "");

                    // hack: get rid of space _
                    string exprName = "E" + exprLookup.Count;
                    exprLookup.Add(exprName, BuildSimpleDataExpression(lStr + oper.Key + " " + rVal, table));
                    query = query.Replace(lStr + oper.Key + rStr, exprName);
                }
            }
            return query;
        }

        public static string RemoveWhiteSpace(this string text) {
            string result = String.Empty;
            var parts = text.Split('"');
            for (int i = 0; i < parts.Length; i++) {
                if (i % 2 == 0)
                    result += Regex.Replace(parts[i], " ", "");
                else
                    result += String.Format("\"{0}\"", parts[i]);
            }
            return result;
        }

        /// <summary>
        /// Build an operator expression
        /// </summary>
        public static SimpleExpression BuildSimpleOperatorExpression(string expression) {
            var op = expression.MatchOperator(exprOperators);

            if (String.IsNullOrWhiteSpace(op.Item1))
                throw new Exception("Invalid operator found in expression.");

            string[] subq2 = expression.Split(new string[] { op.Item1 }, StringSplitOptions.None);
            return new SimpleExpression(exprLookup[subq2[0].Trim()], exprLookup[subq2[1].Trim()], op.Item2);
        }

        /// <summary>
        /// Build a data expression
        /// </summary>
        public static SimpleExpression BuildSimpleDataExpression(string expression, dynamic table) {
            var op = expression.MatchOperator(dataOperators);

            if (String.IsNullOrWhiteSpace(op.Item1))
                throw new Exception("Invalid operator found in expression.");

            string[] subq2 = expression.Split(new string[] { op.Item1 }, StringSplitOptions.None);
            return new SimpleExpression(table[subq2[0].Trim()], subq2[1].Trim(), op.Item2);
        }

        /// <summary>
        /// Extension method to determine which operator an expression uses
        /// </summary>
        private static Tuple<string, SimpleExpressionType> MatchOperator(this string expression, Dictionary<string, SimpleExpressionType> operators) {
            foreach (var oper in operators)
                if (expression.Contains(oper.Key))
                    return new Tuple<string, SimpleExpressionType>(oper.Key, oper.Value);

            return new Tuple<string, SimpleExpressionType>("", SimpleExpressionType.Empty);
        }

        /// <summary>
        /// Extension method to grab the last operator from expression
        /// TODO: Maybe make less hackish?
        /// </summary>
        private static string LastIndexOfKey(this string expression, Dictionary<string, SimpleExpressionType> operators) {
            string[] keys = operators.Keys.GetKeyArray();
            int[] indexes = new int[operators.Keys.Count];
            for (int i = 0; i < operators.Keys.Count; i++)
                indexes[i] = expression.LastIndexOf(keys[i]);
            return keys[Array.IndexOf(indexes, indexes.Max())];
        }

        /// <summary>
        /// Extension method to grab the first operator from expression
        /// TODO: Maybe make less hackish?
        /// </summary>
        private static string IndexOfKey(this string expression, Dictionary<string, SimpleExpressionType> operators) {
            string[] keys = operators.Keys.GetKeyArray();
            int[] indexes = new int[operators.Keys.Count];
            for (int i = 0; i < operators.Keys.Count; i++)
                indexes[i] = expression.IndexOf(keys[i]) == -1 ? 999999 : expression.IndexOf(keys[i]); // This is so hacky
            return keys[Array.IndexOf(indexes, indexes.Min())];
        }

        /// <summary>
        /// Extension method to create an array of keys from dictonary keycollection
        /// </summary>
        private static T[] GetKeyArray<T, T2>(this Dictionary<T, T2>.KeyCollection keyCollection) {
            T[] keys = new T[keyCollection.Count];
            keyCollection.CopyTo(keys, 0);
            return keys;
        }

        private static string Slice(this string source, int start, int end) {
            if (end < 0) // Keep this for negative end support
	        {
                end = source.Length + end;
            }
            int len = end - start;               // Calculate length
            return source.Substring(start, len); // Return Substring of length
        }
    }
}
