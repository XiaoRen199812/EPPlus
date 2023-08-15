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
using System.Text;
using System.Collections;
using System.Linq;
using OfficeOpenXml.FormulaParsing.ExcelUtilities;

namespace OfficeOpenXml
{
    /// <summary>
    /// Collection for named ranges
    /// </summary>
    public class ExcelNamedRangeCollection : IEnumerable<ExcelNamedRange>
    {
        internal ExcelWorksheet _ws;
        internal ExcelWorkbook _wb;
        internal ExcelNamedRangeCollection(ExcelWorkbook wb)
        {
            _wb = wb;
            _ws = null;
        }
        internal ExcelNamedRangeCollection(ExcelWorkbook wb, ExcelWorksheet ws)
        {
            _wb = wb;
            _ws = ws;
        }
        List<ExcelNamedRange> _list = new List<ExcelNamedRange>();
        Dictionary<string, int> _dic = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Adds a new named range
        /// </summary>
        /// <param name="Name">The name</param>
        /// <param name="Range">The range</param>
        /// <param name="allowRelativeAddress">If true, the address will be retained as it is, if false the address will always be converted to an absolute/fixed address</param>
        /// <returns></returns>
        public ExcelNamedRange Add(string Name, ExcelRangeBase Range, bool allowRelativeAddress)
        {
            if (!ExcelAddressUtil.IsValidName(Name))
            {
                throw (new ArgumentException("Name contains invalid characters or is not valid."));
            }
            if(_wb!=Range._workbook)
            {
                throw (new InvalidOperationException("The range must be in the same package. "));
            }
            return AddName(Name, Range, allowRelativeAddress);
        }

        /// <summary>
        /// Adds a new named range
        /// </summary>
        /// <param name="Name">The name</param>
        /// <param name="Range">The range</param>
        public ExcelNamedRange Add(string Name, ExcelRangeBase Range)
        {
            return Add(Name, Range, false);
        }

        /// <summary>
        /// Adds the name without validation as Excel allows some names on load that is not permitted in the GUI
        /// </summary>
        /// <param name="Name">The Name</param>
        /// <param name="Range">The Range</param>
        /// <param name="allowRelativeAddress">If true, the address will be retained as it is, if false the address will always be converted to an absolute/fixed address</param>
        /// <returns></returns>
        internal ExcelNamedRange AddName(string Name, ExcelRangeBase Range, bool allowRelativeAddress = false)
        {
            ExcelNamedRange item;
            if (Range.IsName)
            {

                item = new ExcelNamedRange(Name, _wb, _ws, _dic.Count, allowRelativeAddress);
            }
            else
            {
                item = new ExcelNamedRange(Name, _ws, Range.Worksheet, Range.Address, _dic.Count, allowRelativeAddress);
            }

            AddName(Name, item);

            return item;
        }

        private void AddName(string Name, ExcelNamedRange item)
        {
            if(_dic.ContainsKey(Name)) //Excel allows duplicate names on load. Duplicates can be generated by for example Libre Office print areas. Always pick the last in the list.
            {
                _list.RemoveAt(_dic[Name]);
                _list.Insert(_dic[Name], item);
            }
            else
            {
                _dic.Add(Name, _list.Count);
                _list.Add(item);
            }
        }
        /// <summary>
        /// Add a defined name referencing value
        /// </summary>
        /// <param name="Name">The name</param>
        /// <param name="value">The value for the name</param>
        /// <returns></returns>
        public ExcelNamedRange AddValue(string Name, object value)
        {
            var item = new ExcelNamedRange(Name,_wb, _ws, _dic.Count);
            item.NameValue = value;
            AddName(Name, item);
            return item;
        }

        /// <summary>
        /// Add a defined name referencing a formula
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Formula"></param>
        /// <returns></returns>
        public ExcelNamedRange AddFormula(string Name, string Formula)
        {
            var item = new ExcelNamedRange(Name, _wb, _ws, _dic.Count);
            item.NameFormula = Formula;
            AddName(Name, item);
            return item;
        }

        internal void Insert(int rowFrom, int colFrom, int rows, int cols, int lowerLimint = 0, int upperLimit = int.MaxValue)
        {
            Insert(rowFrom, colFrom, rows, cols, n => true, lowerLimint, upperLimit);
        }

        internal void Insert(int rowFrom, int colFrom, int rows, int cols, Func<ExcelNamedRange, bool> filter, int lowerLimint = 0, int upperLimit=int.MaxValue)
        {
            var namedRanges = this._list.Where(filter);
            foreach(var namedRange in namedRanges)
            {
                if (namedRange._fromRow <= 0) continue;
                var address = new ExcelAddressBase(namedRange.Address);
                if (rows > 0 && address._toCol<=upperLimit && address._fromCol>=lowerLimint && address.Rows < ExcelPackage.MaxRows)
                {
                    address = address.AddRow(rowFrom, rows, false);
                }
                if(cols > 0 && colFrom > 0 && address._toRow <= upperLimit && address._fromRow >= lowerLimint && address.Columns < ExcelPackage.MaxColumns)
                {
                    address = address.AddColumn(colFrom, cols, false,false);
                }
                namedRange.Address = address.Address;
            }
        }
        internal void Delete(int rowFrom, int colFrom, int rows, int cols, int lowerLimint = 0, int upperLimit = int.MaxValue)
        {
            Delete(rowFrom, colFrom, rows, cols, n => true, lowerLimint, upperLimit);
        }
        internal void Delete(int rowFrom, int colFrom, int rows, int cols, Func<ExcelNamedRange, bool> filter, int lowerLimint = 0, int upperLimit = int.MaxValue)
        {
            var namedRanges = this._list.Where(filter);
            foreach (var namedRange in namedRanges)
            {
                if (namedRange._fromRow <= 0) continue;
                var address = new ExcelAddressBase(namedRange.Address);
                if (rows > 0 && address._toCol <= upperLimit && address._fromCol >= lowerLimint)
                {
                    address = namedRange.DeleteRow(rowFrom, rows,false,false);
                }
                if (cols > 0 && colFrom > 0 && address._toRow <= upperLimit && address._fromRow >= lowerLimint)
                {
                    address = namedRange.DeleteColumn(colFrom, cols,false,false);
                }

                if (address == null)
                {
                    namedRange.Address = "#REF!";
                }
                else
                {
                    namedRange.Address = address.Address;
                }
            }
        }


        /// <summary>
        /// Remove a defined name from the collection
        /// </summary>
        /// <param name="Name">The name</param>
        public void Remove(string Name)
        {
            if(_dic.ContainsKey(Name))
            {
                var ix = _dic[Name];

                for (int i = ix+1; i < _list.Count; i++)
                {
                    _dic.Remove(_list[i].Name);
                    _list[i].Index--;
                    _dic.Add(_list[i].Name, _list[i].Index);
                }
                _dic.Remove(Name);
                _list.RemoveAt(ix);
            }
        }
        /// <summary>
        /// Checks collection for the presence of a key
        /// </summary>
        /// <param name="key">key to search for</param>
        /// <returns>true if the key is in the collection</returns>
        public bool ContainsKey(string key)
        {
            return _dic.ContainsKey(key);
        }
        /// <summary>
        /// The current number of items in the collection
        /// </summary>
        public int Count
        {
            get
            {
                return _dic.Count;
            }
        }
        /// <summary>
        /// Name indexer
        /// </summary>
        /// <param name="Name">The name (key) for a Named range</param>
        /// <returns>a reference to the range</returns>
        /// <remarks>
        /// Throws a KeyNotFoundException if the key is not in the collection.
        /// </remarks>
        public ExcelNamedRange this[string Name]
        {
            get
            {
                return _list[_dic[Name]];
            }
        }
        /// <summary>
        /// Indexer for the collection
        /// </summary>
        /// <param name="Index">The index</param>
        /// <returns>The named range</returns>
        public ExcelNamedRange this[int Index]
        {
            get
            {
                return _list[Index];
            }
        }

        #region "IEnumerable"
        #region IEnumerable<ExcelNamedRange> Members
        /// <summary>
        /// Implement interface method IEnumerator&lt;ExcelNamedRange&gt; GetEnumerator()
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ExcelNamedRange> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
        #region IEnumerable Members
        /// <summary>
        /// Implement interface method IEnumeratable GetEnumerator()
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
        #endregion

        internal void Clear()
        {
            while(Count>0)
            {
                Remove(_list[0].Name);
            }
        }
    }
}
