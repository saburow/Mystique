﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dulcet.Twitter;
using Inscribe.Filter.Filters;

namespace Inscribe.Filter
{
    /// <summary>
    /// フィルタクラスタ
    /// </summary>
    public class FilterCluster : IUserFilter
    {
        public FilterCluster() { }
        public FilterCluster(IEnumerable<IFilter> filters, bool negate = false, bool concatAnd = false)
        {
            this.Filters = filters;
            this.Negate = negate;
            this.ConcatenateAnd = concatAnd;
        }

        /// <summary>
        /// 登録されているフィルタ
        /// </summary>
        private IEnumerable<IFilter> _filters = null;

        /// <summary>
        /// このクラスタが含むフィルタ
        /// </summary>
        public IEnumerable<IFilter> Filters
        {
            get
            {
                if (_filters == null)
                    return new IFilter[0];
                else
                    return _filters;
            }
            set
            {
                if (value == _filters) return;
                var prev = _filters;
                _filters = (value ?? new IFilter[0]).ToArray();
                UpdateChain(prev, false);
                UpdateChain(value, true);
            }
        }

        #region Reacception chain

        private void UpdateChain(IEnumerable<IFilter> filter, bool attach)
        {
            if (filter != null)
            {
                if (attach)
                {
                    filter.ForEach(f => f.RequireReaccept += filter_RequireReaccept);
                    filter.ForEach(f => f.RequirePartialReaccept += filter_RequirePartialReaccept);
                }
                else
                {
                    filter.ForEach(f => f.RequireReaccept -= filter_RequireReaccept);
                    filter.ForEach(f => f.RequirePartialReaccept -= filter_RequirePartialReaccept);
                }
            }
        }

        void filter_RequireReaccept()
        {
            System.Diagnostics.Debug.WriteLine("chainning reacception");
            // 再適用の伝播
            this.RequireReaccept();
        }

        void filter_RequirePartialReaccept(TwitterStatusBase tsb)
        {
            this.RequirePartialReaccept(tsb);
        }

        #endregion

        /// <summary>
        /// このクラスタの真偽値がクラスタに登録されているフィルタ全体のOR結合で判定されるかを設定します。
        /// </summary>
        public bool ConcatenateAnd { get; set; }

        /// <summary>
        /// このフィルタクラスタに所属するフィルタについて、フィルタ条件に合致するかの判定を行います。<para />
        /// Negateは内部で考慮されます。<para />
        /// フィルタが一つもない場合は、U=∅のときの∀(AND)、∃(OR)のBoolean値に準じます。<para />
        /// (ANDのとき:TRUE ORのとき:FALSE)
        /// </summary>
        /// <param name="status">フィルタテストするステータス</param>
        /// <returns>ステータスがフィルタに合致するか</returns>
        public bool Filter(TwitterStatusBase status)
        {
            if (_filters != null)
            {
                foreach (var f in _filters)
                {
                    // ANDのとき => FALSEに出会ったらFALSEを返す
                    // ORのとき => TRUEに出会ったらTRUEを返す　
                    if (f.Filter(status) != ConcatenateAnd)
                        return ConcatenateAnd == Negate;
                }
            }
            // ANDのとき => FALSEに出会っていないのでTRUEを返す
            // ORのとき => TRUEに出会っていないのでFALSEを返す
            return ConcatenateAnd == !Negate;
        }

        public bool FilterUser(TwitterUser user)
        {
            if (_filters != null)
            {
                foreach (var f in _filters.OfType<IUserFilter>())
                {
                    // ANDのとき => FALSEに出会ったらFALSEを返す
                    // ORのとき => TRUEに出会ったらTRUEを返す　
                    if (f.FilterUser(user) != ConcatenateAnd)
                        return ConcatenateAnd == Negate;
                }
            }
            // ANDのとき => FALSEに出会っていないのでTRUEを返す
            // ORのとき => TRUEに出会っていないのでFALSEを返す
            return ConcatenateAnd == !Negate;
        }

        public event Action RequireReaccept = () => { };

        public event Action<TwitterStatusBase> RequirePartialReaccept = _ => { };

        /// <summary>
        /// 否定フィルタであるか
        /// </summary>
        public bool Negate { get; set; }

        public FilterCluster GetOnlyForUserFilter()
        {
            var nfc = new FilterCluster();
            nfc.ConcatenateAnd = this.ConcatenateAnd;
            nfc.Negate = this.Negate;
            nfc.Filters = _filters
                .OfType<IUserFilter>()
                .Select(f => f is FilterCluster ? ((FilterCluster)f).GetOnlyForUserFilter() : f)
                .ToArray();
            return nfc;
        }

        /// <summary>
        /// このフィルタをクエリ化します。
        /// </summary>
        public string ToQuery()
        {
            if (_filters == null || _filters.Count() == 0)
            {
                // When Filters = ∅
                // AND -> ALL TRUE -> TRUE
                // OR -> ANY TRUE -> FALSE
                if (this.ConcatenateAnd)
                    return this.Negate ? "!()" : "()";
                else
                    return this.Negate ? "()" : "!()";
            }
            else
            {
                var strs = from f in _filters
                           select f.ToQuery();
                return (this.Negate ? "!(" : "(") + String.Join(
                    ConcatenateAnd ? " & " : " | ",
                    strs) + ")";
            }
        }
    }
}
