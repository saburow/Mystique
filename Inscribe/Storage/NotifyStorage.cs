﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Inscribe.Configuration;
using Inscribe.Util;
using Livet;

namespace Inscribe.Storage
{
    public static class NotifyStorage
    {
        static Timer secondTimer = new Timer(SecondCallback, null, 1000, 1000);

        static ReaderWriterLockWrap notifyLock = new ReaderWriterLockWrap();

        static Stack<NotifyItem> notificationStack = new Stack<NotifyItem>();

        /// <summary>
        /// 表示中のメッセージ
        /// </summary>
        public static string Message { get; private set; }

        static NotifyStorage()
        {
            Message = Define.DefaultStatusMessage;
        }

        #region NotifyTextChangedイベント

        public static event EventHandler<NotifyUpdatedEventArgs> NotifyTextChanged;
        private static Notificator<NotifyUpdatedEventArgs> _NotifyTextChangedEvent;
        public static Notificator<NotifyUpdatedEventArgs> NotifyTextChangedEvent
        {
            get
            {
                if (_NotifyTextChangedEvent == null) _NotifyTextChangedEvent = new Notificator<NotifyUpdatedEventArgs>();
                return _NotifyTextChangedEvent;
            }
            set { _NotifyTextChangedEvent = value; }
        }

        private static void OnNotifyTextChanged(NotifyUpdatedEventArgs e)
        {
            var threadSafeHandler = Interlocked.CompareExchange(ref NotifyTextChanged, null, null);
            if (threadSafeHandler != null) threadSafeHandler(null, e);
            NotifyTextChangedEvent.Raise(e);
        }

        #endregion
      
        public static void Notify(string message, int? showLength = null)
        {
            System.Diagnostics.Debug.WriteLine("Notify: " + message);
            using (notifyLock.GetWriterLock())
            {
                notificationStack.Push(new SecondNotifyItem(message,
                    showLength.GetValueOrDefault(Setting.IsInitialized ? Setting.Instance.ExperienceProperty.StatusMessageDefaultShowLengthSec : 5)));
            }
            UpdateFocus();
        }

        public static NotifyItem NotifyManually(string message)
        {
            var ni = new NotifyItem(message, true);
            using (notifyLock.GetWriterLock())
            {
                notificationStack.Push(ni);
            }
            UpdateFocus();
            return ni;
        }

        static NotifyItem prevChain = null;
        /// <summary>
        /// 現在のフォーカスアイテムを変更します。
        /// </summary>
        private static void UpdateFocus()
        {
            if (prevChain != null)
                prevChain.StateChanged -= CallbackStateChanged;
            using (notifyLock.GetReaderLock())
            {
                if (notificationStack.Count == 0)
                    prevChain = null;
                else
                    prevChain = notificationStack.Peek();
            }
            if (prevChain != null)
                prevChain.StateChanged += CallbackStateChanged;
            // 最後に状態更新して終わり
            CallbackStateChanged();
        }

        private static void CallbackStateChanged()
        {
            string currentMessage = null;
            using (notifyLock.GetReaderLock())
            {
                if (notificationStack.Count > 0)
                {
                    if (!notificationStack.Peek().IsDisposed)
                    {
                        currentMessage = notificationStack.Peek().Message;
                    }
                }
            }
            if (currentMessage != null)
            {
                // 表示継続
                if (Message != currentMessage)
                {
                    Message = currentMessage;
                    OnNotifyTextChanged(new NotifyUpdatedEventArgs(currentMessage));
                }
                return;
            }
            else
            {
                // PeekObject自体が変化している
                // DisposeされていないメッセージまでPopする
                using (notifyLock.GetWriterLock())
                {
                    while (notificationStack.Count > 0 && notificationStack.Peek().IsDisposed)
                    {
                            notificationStack.Pop();
                    }
                    // 新メッセージを適用
                    if (notificationStack.Count > 0)
                    {
                        Message = notificationStack.Peek().Message;
                        OnNotifyTextChanged(new NotifyUpdatedEventArgs(Message));
                        UpdateFocus();
                    }
                    else
                    {
                        Message = Define.DefaultStatusMessage;
                        OnNotifyTextChanged(new NotifyUpdatedEventArgs(Define.DefaultStatusMessage));
                    }
                }
            }
        }

        private static event Action SecondCallbackRaised = () => { };
        private static void SecondCallback(object o)
        {
            SecondCallbackRaised();
        }

        public static bool ContainsLongRunObject
        {
            get
            {
                using (notifyLock.GetReaderLock())
                {
                    return notificationStack.Where(i => i.IsLongRun).FirstOrDefault() != null;
                }
            }
        }

        private class SecondNotifyItem : NotifyItem
        {
            int count = 0;
            int until = 0;
            public SecondNotifyItem(string message, int showLength)
                :base(message)
            {
                this.until = showLength;
                SecondCallbackRaised += Tick;
            }

            void Tick()
            {
                count++;
                if (count >= until)
                {
                    SecondCallbackRaised -= Tick;
                    this.Dispose();
                }
            }
        }
    }

    public class NotifyItem : IDisposable
    {
        public bool IsDisposed { get; set; }
        public bool IsLongRun { get; private set; }
        public NotifyItem(string message, bool isLongRun = false)
        {
            this.IsDisposed = false;
            this.message = message;
            this.IsLongRun = isLongRun;
        }

        private string message;
        public string Message
        {
            get { return this.message; }
            set
            {
                this.message = value;
                StateChanged();
            }
        }

        /// <summary>
        /// メッセージ状態が変化しました。
        /// </summary>
        public event Action StateChanged = () => { };

        public void Dispose()
        {
            this.IsDisposed = true;
            StateChanged();
        }
    }

    public class NotifyUpdatedEventArgs : EventArgs
    {
        public string NotifyString { get; set; }
        public NotifyUpdatedEventArgs(string notify)
        {
            this.NotifyString = notify;
        }
    }
}
