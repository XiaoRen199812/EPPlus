/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  01/27/2020         EPPlus Software AB       Initial release EPPlus 5
 *************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficeOpenXml.FormulaParsing;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Exceptions;
using OfficeOpenXml.FormulaParsing.ExpressionGraph.Rpn;
using OfficeOpenXml.FormulaParsing.Ranges;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace OfficeOpenXml.FormulaParsing.ExpressionGraph.Rpn.FunctionCompilers
{
    /// <summary>
    /// Why do the If function require a compiler of its own you might ask;)
    /// 
    /// It is because it only needs to evaluate one of the two last expressions. This
    /// compiler handles this - it ignores the irrelevant expression.
    /// </summary>
    internal class RpnIfFunctionCompiler : RpnFunctionCompiler
    {
        internal RpnIfFunctionCompiler(ExcelFunction function, ParsingContext context)
            : base(function, context)
        {
            Require.That(function).Named("function").IsNotNull();
            if (!(function is If)) throw new ArgumentException("function must be of type If");
        }

        public override CompileResult Compile(IEnumerable<RpnExpression> children)
        {
            // 2 is allowed, Excel returns FALSE if false is the outcome of the expression
            if (children.Count() < 2) throw new ExcelErrorValueException(eErrorType.Value);
            var args = new List<FunctionArgument>();
            Function.BeforeInvoke(Context);
            var firstChild = children.ElementAt(0);
            var v = firstChild.Compile().Result;
            if(v is InMemoryRange mr)
            {
                var range = new InMemoryRange(mr.Size);
                for(var row = 0; row < mr.Size.NumberOfRows;row++)
                {
                    for(var col = 0; col < mr.Size.NumberOfCols; col++)
                    {
                        args.Clear();
                        var cell = mr.GetCell(row, col);
                        object val = null;
                        if(cell != null)
                        {
                            val = cell.Value;
                        }
                        var res = CallFunction(val, children, args);
                        range.SetValue(row, col, res.Result);
                    }
                }
                return new CompileResult(range, DataType.ExcelRange);
            }
            else
            {
                return CallFunction(v, children, args);
            }
        }

        private CompileResult CallFunction(object v, IEnumerable<RpnExpression> children, List<FunctionArgument> args)
        {
            if (v is INameInfo)
            {
                v = ((INameInfo)v).Value;
            }

            if (v is IRangeInfo)
            {
                var r = ((IRangeInfo)v);
                if (r.GetNCells() > 1)
                {
                    throw (new ArgumentException("Logical can't be more than one cell"));
                }
                v = r.GetOffset(0, 0);
            }
            bool boolVal;
            if (v is bool)
            {
                boolVal = (bool)v;
            }
            else if(v is ExcelErrorValue eev)
            {
                return new CompileResult(eev.Type);
            }
            else if (!Utils.ConvertUtil.TryParseBooleanString(v as string, out boolVal))
            {
                if (Utils.ConvertUtil.IsNumericOrDate(v))
                {
                    boolVal = OfficeOpenXml.Utils.ConvertUtil.GetValueDouble(v) != 0;
                }
                else
                {
                    throw (new ArgumentException("Invalid logical test"));
                }
            }
            /****  End Handle names and ranges ****/

            args.Add(new FunctionArgument(boolVal));
            if (boolVal)
            {
                var result = children.ElementAt(1).Compile();
                args.Add(new FunctionArgument(result == CompileResult.Empty ? 0d : result.Result));
                args.Add(new FunctionArgument(null));
            }
            else
            {
                object val;
                var child = children.ElementAtOrDefault(2);
                if (child == null)
                {
                    // if no false expression given, Excel returns false
                    val = false;
                }
                else
                {
                    var result = child.Compile();
                    val = (result == CompileResult.Empty) ? 0d : result.Result;
                }
                args.Add(new FunctionArgument(null));
                args.Add(new FunctionArgument(val));
            }
            return Function.Execute(args, Context);
        }
    }
}
