﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Antlr.Runtime;
using NCalcAsync.Domain;

namespace NCalcAsync
{
    public class Expression
    {
        public EvaluateOptions Options { get; set; }

        /// <summary>
        /// Textual representation of the expression to evaluate.
        /// </summary>
        protected string OriginalExpression;

        public EvaluateParameterAsyncHandler EvaluateParameterAsync { get; set; }
        public EvaluateFunctionAsyncHandler EvaluateFunctionAsync { get; set; }

        public Expression(string expression) : this(expression, EvaluateOptions.None)
        {
        }

        public Expression(string expression, EvaluateOptions options)
        {
            if (String.IsNullOrEmpty(expression))
                throw new
            ArgumentException("Expression can't be empty", "expression");

            OriginalExpression = expression;
            Options = options;
        }

        public Expression(LogicalExpression expression) : this(expression, EvaluateOptions.None)
        {
        }

        public Expression(LogicalExpression expression, EvaluateOptions options)
        {
            if (expression == null)
                throw new
            ArgumentException("Expression can't be null", "expression");
            
            ParsedExpression = expression;
            Options = options;
        }

        #region Cache management
        private static bool _cacheEnabled = true;
        private static Dictionary<string, WeakReference> _compiledExpressions = new Dictionary<string, WeakReference>();
        private static readonly ReaderWriterLock Rwl = new ReaderWriterLock();

        public static bool CacheEnabled
        {
            get { return _cacheEnabled; }
            set
            {
                _cacheEnabled = value;

                if (!CacheEnabled)
                {
                    // Clears cache
                    _compiledExpressions = new Dictionary<string, WeakReference>();
                }
            }
        }

        /// <summary>
        /// Removed unused entries from cached compiled expression
        /// </summary>
        private static void CleanCache()
        {
            var keysToRemove = new List<string>();

            try
            {
                Rwl.AcquireWriterLock(Timeout.Infinite);
                foreach (var de in _compiledExpressions)
                {
                    if (!de.Value.IsAlive)
                    {
                        keysToRemove.Add(de.Key);
                    }
                }


                foreach (string key in keysToRemove)
                {
                    _compiledExpressions.Remove(key);
                    Trace.TraceInformation("Cache entry released: " + key);
                }
            }
            finally
            {
                Rwl.ReleaseWriterLock();
            }
        }

        #endregion

        public static LogicalExpression Compile(string expression, bool nocache)
        {
            LogicalExpression logicalExpression = null;

            if (_cacheEnabled && !nocache)
            {
                try
                {
                    Rwl.AcquireReaderLock(Timeout.Infinite);

                    if (_compiledExpressions.ContainsKey(expression))
                    {
                        Trace.TraceInformation("Expression retrieved from cache: " + expression);
                        var wr = _compiledExpressions[expression];
                        logicalExpression = wr.Target as LogicalExpression;

                        if (wr.IsAlive && logicalExpression != null)
                        {
                            return logicalExpression;
                        }
                    }
                }
                finally
                {
                    Rwl.ReleaseReaderLock();
                }
            }

            if (logicalExpression == null)
            {
                var lexer = new NCalcLexer(new ANTLRStringStream(expression));
                var parser = new NCalcParser(new CommonTokenStream(lexer));

                logicalExpression = parser.ncalcExpression().value;

                if (parser.Errors != null && parser.Errors.Count > 0)
                {
                    throw new EvaluationException(String.Join(Environment.NewLine, parser.Errors.ToArray()));
                }

                if (_cacheEnabled && !nocache)
                {
                    try
                    {
                        Rwl.AcquireWriterLock(Timeout.Infinite);
                        _compiledExpressions[expression] = new WeakReference(logicalExpression);
                    }
                    finally
                    {
                        Rwl.ReleaseWriterLock();
                    }

                    CleanCache();

                    Trace.TraceInformation("Expression added to cache: " + expression);
                }
            }

            return logicalExpression;
        }

        /// <summary>
        /// Pre-compiles the expression in order to check syntax errors.
        /// If errors are detected, the Error property contains the message.
        /// </summary>
        /// <returns>True if the expression syntax is correct, otherwiser False</returns>
        public bool HasErrors()
        {
            try
            {
                if (ParsedExpression == null)
                {
                    ParsedExpression = Compile(OriginalExpression, (Options & EvaluateOptions.NoCache) == EvaluateOptions.NoCache);
                }

                // In case HasErrors() is called multiple times for the same expression
                return ParsedExpression != null && Error != null;
            }
            catch (Exception e)
            {
                Error = e.Message;
                ErrorException = e;
                return true;
            }
        }

        public string Error { get; private set; }
        public Exception ErrorException { get; private set; }

        public LogicalExpression ParsedExpression { get; private set; }

        protected Dictionary<string, IEnumerator> ParameterEnumerators;
        protected Dictionary<string, object> ParametersBackup;

        public async Task<Func<TResult>> ToLambdaAsync<TResult>()
        {
            if (HasErrors())
            {
                throw new EvaluationException(Error, ErrorException);
            }

            if (ParsedExpression == null)
            {
                ParsedExpression = Compile(OriginalExpression, (Options & EvaluateOptions.NoCache) == EvaluateOptions.NoCache);
            }

            var visitor = new LambdaExpressionVistor(Parameters, Options);
            await ParsedExpression.AcceptAsync(visitor);

            var body = visitor.Result;
            if (body.Type != typeof(TResult))
            {
                body = System.Linq.Expressions.Expression.Convert(body, typeof(TResult));
            }

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<TResult>>(body);
            return lambda.Compile();
        }

        public async Task<Func<TContext, TResult>> ToLambdaAsync<TContext, TResult>()
        {
            if (HasErrors())
            {
                throw new EvaluationException(Error, ErrorException);
            }

            if (ParsedExpression == null)
            {
                ParsedExpression = Compile(OriginalExpression, (Options & EvaluateOptions.NoCache) == EvaluateOptions.NoCache);
            }

            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TContext), "ctx");
            var visitor = new LambdaExpressionVistor(parameter, Options);
            await ParsedExpression.AcceptAsync(visitor);

            var body = visitor.Result;
            if (body.Type != typeof(TResult))
            {
                body = System.Linq.Expressions.Expression.Convert(body, typeof(TResult));
            }

            var lambda = System.Linq.Expressions.Expression.Lambda<Func<TContext, TResult>>(body, parameter);
            return lambda.Compile();
        }

        /// <summary>
        /// Evaluate the expression asynchronously.
        /// </summary>
        /// <returns>A task that resolves to the result of the expression.</returns>
        public async Task<object> EvaluateAsync()
        {
            return await EvaluateAsync(EvaluateParameterAsync, EvaluateFunctionAsync);
        }

        /// <summary>
        /// Evaluate the expression asynchronously.
        /// </summary>
        /// <param name="evaluateParameterAsync">Override the value of <see cref="EvaluateParameterAsync"/></param>
        /// <param name="evaluateFunctionAsync">Override the value of <see cref="EvaluateFunctionAsync"/></param>
        /// <returns>A task that resolves to the result of the expression.</returns>
        public async Task<object> EvaluateAsync(EvaluateParameterAsyncHandler evaluateParameterAsync, EvaluateFunctionAsyncHandler evaluateFunctionAsync)
        {
            if (HasErrors())
            {
                throw new EvaluationException(Error);
            }

            if (ParsedExpression == null)
            {
                ParsedExpression = Compile(OriginalExpression, (Options & EvaluateOptions.NoCache) == EvaluateOptions.NoCache);
            }

            var visitor = new EvaluationVisitor(Options, evaluateParameterAsync, evaluateFunctionAsync)
            {
                Parameters = Parameters
            };

            // if array evaluation, execute the same expression multiple times
            if ((Options & EvaluateOptions.IterateParameters) == EvaluateOptions.IterateParameters)
            {
                int size = -1;
                ParametersBackup = new Dictionary<string, object>();
                foreach (string key in Parameters.Keys)
                {
                    ParametersBackup.Add(key, Parameters[key]);
                }

                ParameterEnumerators = new Dictionary<string, IEnumerator>();

                foreach (object parameter in Parameters.Values)
                {
                    if (parameter is IEnumerable)
                    {
                        int localsize = 0;
                        foreach (object o in (IEnumerable)parameter)
                        {
                            localsize++;
                        }

                        if (size == -1)
                        {
                            size = localsize;
                        }
                        else if (localsize != size)
                        {
                            throw new EvaluationException("When IterateParameters option is used, IEnumerable parameters must have the same number of items");
                        }
                    }
                }

                foreach (string key in Parameters.Keys)
                {
                    var parameter = Parameters[key] as IEnumerable;
                    if (parameter != null)
                    {
                        ParameterEnumerators.Add(key, parameter.GetEnumerator());
                    }
                }

                var results = new List<object>();
                for (int i = 0; i < size; i++)
                {
                    foreach (string key in ParameterEnumerators.Keys)
                    {
                        IEnumerator enumerator = ParameterEnumerators[key];
                        enumerator.MoveNext();
                        Parameters[key] = enumerator.Current;
                    }

                    await ParsedExpression.AcceptAsync(visitor);
                    results.Add(visitor.Result);
                }

                return results;
            }

            await ParsedExpression.AcceptAsync(visitor);
            return visitor.Result;
        }

        private Dictionary<string, object> _parameters;

        public Dictionary<string, object> Parameters
        {
            get { return _parameters ?? (_parameters = new Dictionary<string, object>()); }
            set { _parameters = value; }
        }
    }
}