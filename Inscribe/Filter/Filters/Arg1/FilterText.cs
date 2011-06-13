﻿using Inscribe.Filter.Filters.Common;

namespace Inscribe.Filter.Filters.Arg1
{
    public class FilterText : TextFilterBase
    {
        private FilterText() { }

        public FilterText(string needle) : this(needle, false) { }

        public FilterText(string needle, bool isCaseSensitive)
        {
            this.needle = needle;
            this.isCaseSensitive = isCaseSensitive;
        }

        protected override bool FilterStatus(Dulcet.Twitter.TwitterStatusBase status)
        {
            return this.Match(status.Text, this.needle, this.isCaseSensitive);
        }

        public override string Identifier
        {
            get { return "text"; }
        }

        public override string Description
        {
            get { return "本文"; }
        }
    }
}
