﻿using System;
using System.Collections.Generic;
using SIPSorcery.GB28181.Servers;
using SIPSorcery.GB28181.Servers.SIPMessage;
using SIPSorcery.GB28181.SIP;
using SIPSorcery.GB28181.Sys.XML;
using NATS.Client;
using System.Diagnostics;
using SIPSorcery.GB28181.Sys;
using System.Text;
using Logger4Net;
using Newtonsoft.Json;

namespace GB28181Service
{
    public class MessageCenter
    {
        private static ILog logger = AppState.logger;
        private DateTime _keepaliveTime;
        private Queue<HeartBeatEndPoint> _keepAliveQueue = new Queue<HeartBeatEndPoint>();
        private Queue<Catalog> _catalogQueue = new Queue<Catalog>();
        private ISipMessageCore _sipCoreMessageService;

        public MessageCenter(ISipMessageCore sipCoreMessageService)
        {
            _sipCoreMessageService = sipCoreMessageService;
        }

        internal void OnKeepaliveReceived(SIPEndPoint remoteEP, KeepAlive keapalive, string devId)
        {
            _keepaliveTime = DateTime.Now;
            var hbPoint = new HeartBeatEndPoint()
            {
                RemoteEP = remoteEP,
                Heart = keapalive
            };
            _keepAliveQueue.Enqueue(hbPoint);
        }

        internal void OnServiceChanged(string msg, ServiceStatus state)
        {
            SetSIPService(msg, state);
        }

        /// <summary>
        /// 设置sip服务状态
        /// </summary>
        /// <param name="state">sip状态</param>
        private void SetSIPService(string msg, ServiceStatus state)
        {
            logger.Debug("SIP Service Status: " + msg + "," + state);
        }

        ///// <summary>
        ///// 目录查询回调
        ///// </summary>
        ///// <param name="cata"></param>
        //public void OnCatalogReceived(Catalog cata)
        //{
        //    _catalogQueue.Enqueue(cata);
        //}

        //设备信息查询回调函数
        private void DeviceInfoReceived(SIPEndPoint remoteEP, DeviceInfo device)
        {
        }

        //设备状态查询回调函数
        private void DeviceStatusReceived(SIPEndPoint remoteEP, DeviceStatus device)
        {
        }
        
        ///// <summary>
        ///// 录像查询回调
        ///// </summary>
        ///// <param name="record"></param>
        //internal void OnRecordInfoReceived(RecordInfo record)
        //{
        //    SetRecord(record);
        //}

        //private void SetRecord(RecordInfo record)
        //{
        //    foreach (var item in record.RecordItems.Items)
        //    {
        //    }
        //}

        //internal void OnNotifyCatalogReceived(NotifyCatalog notify)
        //{
        //    if (notify.DeviceList == null)
        //    {
        //        return;
        //    }
        //    new Action(() =>
        //    {
        //        foreach (var item in notify.DeviceList.Items)
        //        {
        //        }
        //    }).BeginInvoke(null, null);
        //}

        internal void OnAlarmReceived(Alarm alarm)
        {
            try
            {
                Event.Alarm alm = new Event.Alarm();
                alm.AlarmType = Event.Alarm.Types.AlarmType.NotEvent;
                switch (alarm.AlarmMethod)
                {
                    case "1":
                        break;
                    case "2":
                        alm.AlarmType = Event.Alarm.Types.AlarmType.AlarmOutput;
                        break;
                }
                alm.Detail = alarm.AlarmDescription ?? string.Empty;
                alm.DeviceID = alarm.DeviceID;
                alm.DeviceName = alarm.DeviceID;                
                DateTime alarttime = Convert.ToDateTime(alarm.AlarmTime ?? DateTime.Now.ToString());
                DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                UInt64 time = (UInt64)(alarttime - startTime).TotalMilliseconds;
                alm.EndTime = time;
                alm.StartTime = time;
                #region
                string subject = Event.AlarmTopic.OriginalAlarmTopic.ToString();//"OriginalAlarmTopic"
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(alm));
                Options opts = ConnectionFactory.GetDefaultOptions();
                opts.Url = EnvironmentVariables.GBNatsChannelAddress ?? Defaults.Url;
                //logger.Error("Alarming is trying to connect with nats server.");
                using (IConnection c = new ConnectionFactory().CreateConnection(opts))
                {
                    c.Publish(subject, payload);
                    c.Flush();
                    logger.Debug("Alarming created connection and published.");
                }
                #endregion

                new Action(() =>
                {
                    logger.Debug("OnAlarmReceived AlarmResponse: " + alm.ToString());

                    _sipCoreMessageService.NodeMonitorService[alarm.DeviceID].AlarmResponse(alarm);
                }).Invoke();
            }
            catch (Exception ex)
            {
                logger.Error("OnAlarmReceived Exception: " + ex.Message);
            }
        }

        //internal void OnDeviceStatusReceived(SIPEndPoint remoteEP, DeviceStatus device)
        //{
        //    var msg = "DeviceID:" + device.DeviceID +
        //         "\r\nResult:" + device.Result +
        //         "\r\nOnline:" + device.Online +
        //         "\r\nState:" + device.Status;
        //    new Action(() =>
        //    {
        //    }).Invoke();
        //}

        internal void OnDeviceInfoReceived(SIPEndPoint arg1, DeviceInfo arg2)
        {
            throw new NotImplementedException();
        }

        internal void OnMediaStatusReceived(SIPEndPoint arg1, MediaStatus arg2)
        {
            throw new NotImplementedException();
        }

        internal void OnPresetQueryReceived(SIPEndPoint arg1, PresetInfo arg2)
        {
            throw new NotImplementedException();
        }

        internal void OnDeviceConfigDownloadReceived(SIPEndPoint arg1, DeviceConfigDownload arg2)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 心跳
    /// </summary>
    public class HeartBeatEndPoint
    {
        /// <summary>
        /// 远程终结点
        /// </summary>
        public SIPEndPoint RemoteEP { get; set; }

        /// <summary>
        /// 心跳周期
        /// </summary>
        public KeepAlive Heart { get; set; }
    }
}