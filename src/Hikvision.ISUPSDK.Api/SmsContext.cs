﻿using Hikvision.ISUPSDK.Api.Utils;
using System;
using System.Text;
using static Hikvision.ISUPSDK.Defines;
using static Hikvision.ISUPSDK.Methods;

namespace Hikvision.ISUPSDK.Api
{
    public class SmsContext
    {
        private SmsContextOptions options;
        private int listenHandle;
        private PREVIEW_NEWLINK_CB fnNewLinkCB;
        private PREVIEW_DATA_CB fnPreviewPsDataCB;
        private PREVIEW_DATA_CB fnPreviewRtpDataCB;

        /// <summary>
        /// 新预览连接时
        /// </summary>
        public event EventHandler<SmsContextPreviewNewlinkEventArgs> PreviewNewlink;
        /// <summary>
        /// 接收到预览PS流数据时
        /// </summary>
        public event EventHandler<SmsContextPreviewDataEventArgs> PreviewPsData;
        /// <summary>
        /// 接收到预览RTP流数据时
        /// </summary>
        public event EventHandler<SmsContextPreviewDataEventArgs> PreviewRtpData;

        public SmsContext(SmsContextOptions options)
        {
            this.options = options;
            fnNewLinkCB = new PREVIEW_NEWLINK_CB(onPREVIEW_NEWLINK_CB);
            fnPreviewPsDataCB = new PREVIEW_DATA_CB(onPREVIEW_PS_DATA_CB);
            fnPreviewRtpDataCB = new PREVIEW_DATA_CB(onPREVIEW_RTP_DATA_CB);
        }

        public static void Init()
        {
            SdkManager.Init();
            Invoke(NET_ESTREAM_Init());
        }

        public void Start()
        {
            var listenParam = NET_EHOME_LISTEN_PREVIEW_CFG.NewInstance();
            StringUtils.String2ByteArray(options.ListenIPAddress, listenParam.struIPAdress.szIP);
            listenParam.struIPAdress.wPort = (ushort)options.ListenPort;
            listenParam.fnNewLinkCB = fnNewLinkCB;
            listenParam.byLinkMode = (byte)options.LinkMode;
            listenHandle = Invoke(NET_ESTREAM_StartListenPreview(ref listenParam));
        }

        public void Stop()
        {
            Invoke(NET_ESTREAM_StopListenPreview(listenHandle));
        }

        private bool onPREVIEW_NEWLINK_CB(int lLinkHandle, ref NET_EHOME_NEWLINK_CB_MSG pNewLinkCBMsg, IntPtr pUserData)
        {
            var eventArgs = new SmsContextPreviewNewlinkEventArgs();
            eventArgs.LinkHandle = lLinkHandle;
            eventArgs.SessionId = pNewLinkCBMsg.iSessionID;
            eventArgs.DeviceId = StringUtils.ByteArray2String(pNewLinkCBMsg.szDeviceID);
            eventArgs.ChannelId = pNewLinkCBMsg.dwChannelNo;
            eventArgs.StreamFormat = (SmsStreamFormat)pNewLinkCBMsg.byStreamFormat;
            eventArgs.StreamType = (SmsStreamType)pNewLinkCBMsg.byStreamType;
            eventArgs.DeviceSerial = StringUtils.ByteArray2String(pNewLinkCBMsg.sDeviceSerial);
            //触发预览新连接事件
            PreviewNewlink?.Invoke(this, eventArgs);
            if (!eventArgs.Allowed)
                return false;
            //注册接收数据回调
            var struCBParam = new NET_EHOME_PREVIEW_DATA_CB_PARAM();
            struCBParam.Init();
            struCBParam.fnPreviewDataCB = fnPreviewPsDataCB;
            NET_ESTREAM_SetPreviewDataCB(lLinkHandle, ref struCBParam);
            struCBParam.fnPreviewDataCB = fnPreviewRtpDataCB;
            NET_ESTREAM_SetPreviewDataCB(lLinkHandle, ref struCBParam);
            return true;
        }

        private void onPREVIEW_PS_DATA_CB(int iPreviewHandle, ref NET_EHOME_PREVIEW_CB_MSG pPreviewCBMsg, IntPtr pUserData)
        {
            var dataType = (SmsContextPreviewDataType)pPreviewCBMsg.byDataType;
            if (dataType != SmsContextPreviewDataType.NET_DVR_STREAMDATA)
                return;

            var eventArgs = new SmsContextPreviewDataEventArgs(
                iPreviewHandle,
                pPreviewCBMsg.pRecvdata,
                pPreviewCBMsg.dwDataLen);
            //触发接收到预览数据事件
            PreviewPsData?.Invoke(this, eventArgs);
        }

        private void onPREVIEW_RTP_DATA_CB(int iPreviewHandle, ref NET_EHOME_PREVIEW_CB_MSG pPreviewCBMsg, IntPtr pUserData)
        {
            var dataType = (SmsContextPreviewDataType)pPreviewCBMsg.byDataType;
            if (dataType != SmsContextPreviewDataType.NET_DVR_STREAMDATA)
                return;

            var eventArgs = new SmsContextPreviewDataEventArgs(
                iPreviewHandle,
                pPreviewCBMsg.pRecvdata,
                pPreviewCBMsg.dwDataLen);
            //触发接收到预览数据事件
            PreviewRtpData?.Invoke(this, eventArgs);
        }
    }
}
