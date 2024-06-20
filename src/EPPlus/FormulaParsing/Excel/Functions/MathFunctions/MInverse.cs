﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  01/27/2020         EPPlus Software AB       Initial release EPPlus 7.2
 *************************************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Helpers;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Metadata;
using OfficeOpenXml.FormulaParsing.FormulaExpressions;
using OfficeOpenXml.FormulaParsing.Ranges;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.MathFunctions
{
    [FunctionMetadata(
        Category = ExcelFunctionCategory.MathAndTrig,
        EPPlusVersion = "7.2",
        Description = "Get the inverse of Matrix",
        SupportsArrays = true)]
    internal class MInverse : ExcelFunction
    {
        public override int ArgumentMinLength => 1;

        public override CompileResult Execute(IList<FunctionArgument> arguments, ParsingContext context)
        {
            var range = ArgToRangeInfo(arguments, 0);
            var r = range.Address.ToRow - range.Address.FromRow + 1;
            var c = range.Address.ToCol - range.Address.FromCol + 1;
            var returnRange = new InMemoryRange(r, (short)c);
            double[][] m = new double[r][];
            var x = range.Address.FromCol;
            var y = range.Address.FromRow;
            if (r != c)
            {
                return CompileResult.GetErrorResult(eErrorType.Value);
            }
            //Check if Matrix is valid
            for (int i = 0; i < r; i++)
            {
                m[i] = new double[c];
                for (int j = 0; j < c; j++)
                {
                    var cell = range.GetValue(y, x);
                    if(cell == null)
                    {
                        return CompileResult.GetErrorResult(eErrorType.Value);
                    }
                    bool e1 = double.TryParse(range.GetValue(y, x).ToString(), out double t);
                    if( !e1 )
                    {
                        return CompileResult.GetErrorResult(eErrorType.Value);
                    }
                    m[i][j] = t;
                    x++;
                }
                x = range.Address.FromCol;
                y++;
            }

            var dm = MatrixHelper.Decompose(m, out int[] permutations, out int rowSwap);
            if(dm == null)
            {
                return CompileResult.GetErrorResult(eErrorType.Num);
            }
            if (MatrixHelper.GetDeterminant(dm, rowSwap) == 0)
            {
                return CompileResult.GetErrorResult(eErrorType.Num);

            }
            var inverse = MatrixHelper.Inverse(dm, permutations, rowSwap);
            if(inverse == null)
            {
                return CompileResult.GetErrorResult(eErrorType.Num);
            }
            for (int i = 0; i < r; i++)
            {
                for (int j = 0; j < c; j++)
                {
                    returnRange.SetValue(i, j, inverse[i][j]);
                }
            }
            return CreateResult(returnRange, DataType.ExcelRange);
        }
    }
}
