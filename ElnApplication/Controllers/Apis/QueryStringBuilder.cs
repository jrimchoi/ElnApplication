using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace ElnApplication.Controllers.LoginApis
{
    // See https://weblogs.asp.net/bradvincent/helper-class-querystring-builder-chainable
    /// <summary>
    /// A query string helper class.
    /// </summary>
    public class QueryStringBuilder : NameValueCollection
    {
        public QueryStringBuilder() : base() { }

        public QueryStringBuilder(NameValueCollection coll) : base(coll ?? new NameValueCollection()) { }

        public bool Contains(string name)
        {
            return base.AllKeys.Contains(name);
        }

        public void AddRange(string name, IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    this.Add(name, value);
                }
            }
        }

        public bool TryRemoveItem(string name, out string value)
        {
            bool retval = false;
            value = null;
            var values = base.GetValues(name);
            if (values != null)
            {
                var valueList = new List<string>(values.Where(x => !string.IsNullOrWhiteSpace(x))).ToList();
                base.Remove(name);
                if (valueList.Count > 0)
                {
                    retval = true;
                    value = valueList[0];
                    valueList.RemoveAt(0);
                    if (valueList.Count > 0)
                    {
                        foreach (var tmpVal in valueList)
                        {
                            base.Add(name, tmpVal);
                        }
                    }
                }
            }

            return retval;
        }

        public void RemoveItems(string name)
        {
            string[] values;
            this.TryRemoveItems(name, out values);
        }

        public bool TryRemoveItems(string name, out string[] values)
        {
            if (this.Contains(name))
            {
                values = this.GetValues(name).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                this.Remove(name);
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }

        public bool TryGetFirstValue(string name, out string value)
        {
            value = null;
            var values = this.GetValues(name);
            if (values != null)
            {
                value = values.Where(x => !string.IsNullOrWhiteSpace(x)).FirstOrDefault();
                return true;
            }

            return value != null;
        }

        /// <summary>
        /// outputs the querystring object to a URL encoded query string
        /// </summary>
        /// <returns>the encoded querystring as it would appear in a browser</returns>
        public string GetQueryString(bool urlEncode)
        {
            var tmpList = new List<string>();
            foreach (var name in this.AllKeys)
            {
                var values = this.GetValues(name);
                if (values != null)
                {
                    var fragments = (urlEncode)
                                  ? values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => HttpUtility.UrlEncodeUnicode(name) + "=" + HttpUtility.UrlEncodeUnicode(x))
                                  : values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => name + "=" + x);
                    tmpList.AddRange(fragments);
                }
            }

            return (tmpList.Count == 0) ? string.Empty : "?" + string.Join("&", tmpList);
        }
    }
}
