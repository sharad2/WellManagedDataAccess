using System;
using System.Collections.Generic;
using System.Xml.Xsl;

namespace HappyOracle.WellManagedDataAccess.Helpers.XPath
{
    /// <summary>
    /// Implements the ResolveVariable() function which simply returns an IXsltContextVariable corresponding
    /// to the passed variable name. We also represent an empty variable since that is such a common case.
    /// If the value in null, we return our own reference.
    /// </summary>
    internal class ParseDataContext : XsltContext
    {
        private readonly IDictionary<string, object> _valuesDictionary;

        /// <summary>
        /// You can pass a null dictionary if you have no values to resolve
        /// </summary>
        /// <param name="valuesDictionary"></param>
        public ParseDataContext(IDictionary<string, object> valuesDictionary)
        {
            _valuesDictionary = valuesDictionary;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix">Not used</param>
        /// <param name="name">The name of the variable to lookup</param>
        /// <returns>An interface representing the variable</returns>
        /// <exception cref="KeyNotFoundException">The variable was not found in the dictionary
        /// and missing values are not allowed.
        /// </exception>
        public override IXsltContextVariable ResolveVariable(string prefix, string name)
        {
            ParseContextVariable var;
            if (_valuesDictionary == null)
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "Dictionary of variables is null");
            }
            try
            {
                var.Value = _valuesDictionary[name];
            }
            catch (KeyNotFoundException ex)
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, ex.Message);
            }

            return var;
        }

        #region Unused functionality
        public override int CompareDocument(string baseUri, string nextbaseUri)
        {
            throw new NotImplementedException();
        }

        public override bool PreserveWhitespace(System.Xml.XPath.XPathNavigator node)
        {
            throw new NotImplementedException();
        }

        public override IXsltContextFunction ResolveFunction(string prefix, string name, System.Xml.XPath.XPathResultType[] argTypes)
        {
            throw new NotImplementedException();
        }

        public override bool Whitespace
        {
            get { throw new NotImplementedException(); }
        }
        #endregion


        //#region IXsltContextVariable Members

        //public object Evaluate(XsltContext xsltContext)
        //{
        //    return null;
        //}

        //public bool IsLocal
        //{
        //    get { throw new NotImplementedException(); }
        //}

        //public bool IsParam
        //{
        //    get { throw new NotImplementedException(); }
        //}

        //public System.Xml.XPath.XPathResultType VariableType
        //{
        //    get { throw new NotImplementedException(); }
        //}

        //#endregion
    }
}
