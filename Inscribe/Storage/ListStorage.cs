﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dulcet.Twitter;
using Dulcet.Twitter.Rest;
using Inscribe.Authentication;
using Inscribe.Common;
using Inscribe.Configuration;
using Inscribe.Util;
using Inscribe.ViewModels.PartBlocks.MainBlock;

namespace Inscribe.Storage
{
    /// <summary>
    /// Twitter Listデータの保持ストレージ
    /// </summary>
    public static class ListStorage
    {
        private static ReaderWriterLockWrap listLock = new ReaderWriterLockWrap();

        private static LinkedList<TwitterList> lists = new LinkedList<TwitterList>();

        private static ReaderWriterLockWrap memberLock = new ReaderWriterLockWrap();
        private static Dictionary<Tuple<string, string>, IEnumerable<UserViewModel>> listMemberDicts = new Dictionary<Tuple<string, string>, IEnumerable<UserViewModel>>();

        private static AccountInfo GetSuitableAccount(string screenName, string listName)
        {
            var acInfo = AccountStorage.Get(screenName);
            if (acInfo == null)
                acInfo = AccountStorage.GetRandom(ai => ai.IsFollowingList(screenName, listName), true);
            return acInfo;
        }

        /// <summary>
        /// Twitter Listを登録、または更新します。
        /// </summary>
        public static void Register(TwitterList list)
        {
            using (listLock.GetWriterLock())
            {
                // すでに登録されているデータを取得
                var old = Lookup(list.User.ScreenName, list.Name);
                if (old != null)
                    old.Value = list; // 上書き
                else
                    lists.AddLast(list); // 新規追加
            }
        }

        public static bool IsCacheExists(string screenName, string listName)
        {
            using (listLock.GetReaderLock())
            {
                return Lookup(screenName, listName) != null;
            }
        }

        /// <summary>
        /// リストデータを取得します。<para />
        /// キャッシュに存在しない場合はTwitterにアクセスします。
        /// </summary>
        public static TwitterList Get(string screenName, string listName)
        {
            using (listLock.GetReaderLock())
            {
                var list = Lookup(screenName, listName);
                if (list != null)
                    return list.Value;
                else
                    return Receive(screenName, listName);
            }
        }

        /// <summary>
        /// リストデータを取得します。キャッシュは考慮しませんが、更新されます。
        /// </summary>
        public static TwitterList Receive(string screenName, string listName)
        {
            try
            {
                if (!Setting.IsInitialized) return null;
                var acInfo = GetSuitableAccount(screenName, listName);
                if (acInfo == null) return null;
                var list = ApiHelper.ExecApi(() => acInfo.GetList(screenName, listName));
                if (list == null)
                    throw new Exception("リストの読み込みを行えませんでした。");
                return list;
            }
            catch (Exception e)
            {
                ExceptionStorage.Register(e, ExceptionCategory.TwitterError, "リストデータの読み込みに失敗しました。", () => Receive(screenName, listName));
                return null;
            }
        }

        /// <summary>
        /// 指定したTwitter Listを格納するLinkedList Nodeを取得します。<para />
        /// ReaderLock以上の状態で利用してください。
        /// </summary>
        private static LinkedListNode<TwitterList> Lookup(string screenName, string listName)
        {
            var list = lists.FirstOrDefault(l =>
                l.User.ScreenName.Equals(screenName, StringComparison.CurrentCultureIgnoreCase) &&
                l.CompareListName(listName));
            if (list != null)
                return lists.Find(list);
            else
                return null;
        }

        public static IEnumerable<UserViewModel> GetListMembers(TwitterList list)
        {
            return GetListMembers(list.User.ScreenName, list.Name);
        }

        public static IEnumerable<UserViewModel> GetListMembers(string screenName, string listName)
        {
            Tuple<string, string> key;
            using (memberLock.GetReaderLock())
            {
                key = listMemberDicts.Keys.FirstOrDefault(f =>
                        f.Item1.Equals(screenName, StringComparison.CurrentCultureIgnoreCase) &&
                        f.Item2.ToLower().Replace('_', '-').Equals(listName.ToLower().Replace('_', '-')));
            }
            if (key != null)
            {
                return listMemberDicts[key];
            }
            else
            {
                var acInfo = GetSuitableAccount(screenName, listName);
                if (acInfo == null) return null;
                IEnumerable<TwitterUser> members = null;
                try
                {
                    members = ApiHelper.ExecApi(() => acInfo.GetListMembersAll(screenName, listName).ToArray());
                    if (members == null)
                        throw new WebException("Twitter returns empty");
                }
                catch (Exception e)
                {
                    ExceptionStorage.Register(e, ExceptionCategory.TwitterError, "リストメンバーを読み込めませんでした: @" + screenName + "/" + listName);
                }
                var users = members.Select(u => UserStorage.Get(u)).ToArray();
                // キャッシュ作成
                using (memberLock.GetWriterLock())
                {
                    var wkey = new Tuple<string, string>(screenName, listName);
                    if (!listMemberDicts.ContainsKey(wkey))
                        listMemberDicts.Add(wkey, users);
                }
                return users;
            }
        }

        public static bool IsListMemberCached(string screenName, string listName)
        {
            using (memberLock.GetReaderLock())
            {
                return listMemberDicts.Keys.FirstOrDefault(f =>
                        f.Item1.Equals(screenName, StringComparison.CurrentCultureIgnoreCase) &&
                        f.Item2.ToLower().Replace('_', '-').Equals(listName.ToLower().Replace('_', '-'))) != null;
            }
        }
    }
}
