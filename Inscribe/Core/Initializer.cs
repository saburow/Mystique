﻿using System;
using System.Threading.Tasks;
using System.Windows;
using Inscribe.Common;
using Inscribe.Communication.Streaming;
using Inscribe.Configuration;
using Inscribe.Configuration.KeyAssignment;

namespace Inscribe.Core
{
    public static class Initializer
    {
        private static bool initialized = false;

        /// <summary>
        /// ウィンドウが表示されるより前に行われる初期化処理
        /// </summary>
        public static void Init()
        {
            if (initialized)
                throw new InvalidOperationException("アプリケーションは既に初期化されています。");
            initialized = true;
            Dulcet.Network.Http.Expect100Continue = false;
            Dulcet.Network.Http.MaxConnectionLimit = Int32.MaxValue;
            Setting.Initialize();
            KeyAssign.ReloadAssign();
            UpdateReceiver.Start();
            Application.Current.Exit += new ExitEventHandler(AppExit);
        }

        private static bool standby = false;

        /// <summary>
        /// ウィンドウが表示された後に行われる初期化処理
        /// </summary>
        public static void StandbyApp()
        {
            if (standby)
                throw new InvalidOperationException("既にアプリケーションはスタンバイ状態を経ました。");
            standby = true;
            Task.Factory.StartNew(() => Inscribe.Communication.CruiseControl.AutoCruiseSchedulerManager.Begin());
            Task.Factory.StartNew(() => UserStreamsReceiverManager.RefreshReceivers());
        }

        static void AppExit(object sender, ExitEventArgs e)
        {
            Setting.Instance.Save();
        }
    }
}
