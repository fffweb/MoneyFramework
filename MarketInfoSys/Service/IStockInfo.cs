﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using TDFAPI;

namespace MarketInfoSys
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的接口名“IService1”。
    [ServiceContract(Namespace = "http://MarketInfoSys")]
    public interface IStockInfo
    {
        [OperationContract]
        int DoWork(int a, int b);

        [OperationContract]
        int DoWork2(int a, int b);

        [OperationContract]
        MarketData DeQueueInfo();
    }


    [ServiceContract(Namespace = "http://MarketInfoSys")]
    public class TestData
    {
        public Int32 t = 0;
    }

    [DataContract(Namespace = "http://MarketInfoSys")]
    public class MarketData
    {
       
        // 摘要:
        //     业务发生日(自然日)
        [DataMember]
        public int ActionDay { get; set; }
        //
        // 摘要:
        //     申卖价
        [DataMember]
        public uint[] AskPrice { get; set; }
        //
        // 摘要:
        //     申卖量
        [DataMember]
        public uint[] AskVol { get; set; }
        //
        // 摘要:
        //     申买价
        [DataMember]
        public uint[] BidPrice { get; set; }
        //
        // 摘要:
        //     申买量
        [DataMember]
        public uint[] BidVol { get; set; }
        //
        // 摘要:
        //     原始Code
        [DataMember]
        public string Code { get; set; }
        //
        // 摘要:
        //     最高价
        [DataMember]
        public uint High { get; set; }
        //
        // 摘要:
        //     涨停价
        [DataMember]
        public uint HighLimited { get; set; }
        //
        // 摘要:
        //     IOPV净值估值
        //      重新利用 0:股票 1：期货 2：指数
        [DataMember]
        public int IOPV { get; set; }
        //
        // 摘要:
        //     最低价
        [DataMember]
        public uint Low { get; set; }
        //
        // 摘要:
        //     跌停价
        [DataMember]
        public uint LowLimited { get; set; }
        //
        // 摘要:
        //     最新价
        [DataMember]
        public uint Match { get; set; }
        //
        // 摘要:
        //     成交笔数
        [DataMember]
        public uint NumTrades { get; set; }
        //
        // 摘要:
        //     开盘价
        [DataMember]
        public uint Open { get; set; }
        //
        // 摘要:
        //     前收盘价
        [DataMember]
        public uint PreClose { get; set; }
        //
        // 摘要:
        //     证券信息前缀
        [DataMember]
        public byte[] Prefix { get; set; }
        //
        // 摘要:
        //     升跌2（对比上一笔）
        [DataMember]
        public int SD2 { get; set; }
        //
        // 摘要:
        //     状态
        [DataMember]
        public int Status { get; set; }
        //
        // 摘要:
        //     市盈率1
        [DataMember]
        public int Syl1 { get; set; }
        //
        // 摘要:
        //     市盈率2
        [DataMember]
        public int Syl2 { get; set; }
        //
        // 摘要:
        //     时间(HHMMSSmmm)
        [DataMember]
        public int Time { get; set; }
        //
        // 摘要:
        //     委托卖出总量
        [DataMember]
        public long TotalAskVol { get; set; }
        //
        // 摘要:
        //     委托买入总量
        [DataMember]
        public long TotalBidVol { get; set; }
        //
        // 摘要:
        //     交易日
        [DataMember]
        public int TradingDay { get; set; }
        //
        // 摘要:
        //     成交总金额
        [DataMember]
        public long Turnover { get; set; }
        //
        // 摘要:
        //     成交总量
        [DataMember]
        public long Volume { get; set; }
        //
        // 摘要:
        //     加权平均委卖价格
        [DataMember]
        public uint WeightedAvgAskPrice { get; set; }
        //
        // 摘要:
        //     加权平均委买价格
        [DataMember]
        public uint WeightedAvgBidPrice { get; set; }
        //
        // 摘要:
        //     万得代码,600001.SH
        [DataMember]
        public string WindCode { get; set; }
        //
        // 摘要:
        //     到期收益率
        [DataMember]
        public int YieldToMaturity { get; set; }


        public MarketData(TDFMarketData _mData)
        {
            ActionDay = _mData.ActionDay;
            
            AskPrice = _mData.AskPrice;
            AskVol = _mData.AskVol;
            BidPrice = _mData.BidPrice;
            BidVol = _mData.BidVol;
            Code = _mData.Code;
            High = _mData.High;
            HighLimited = _mData.HighLimited;
            IOPV = _mData.IOPV;
            Low = _mData.Low;
            LowLimited = _mData.LowLimited;
            Match = _mData.Match;
            NumTrades = _mData.NumTrades;
            Open = _mData.Open;
            PreClose = _mData.PreClose;
            Prefix = _mData.Prefix;
            SD2 = _mData.SD2;
            WindCode = _mData.WindCode;
            Time = _mData.Time;
            Status = _mData.Status;
            IOPV = 0;
        }

        public MarketData(TDFFutureData data)
        {
            ActionDay = data.ActionDay;
            AskPrice = data.AskPrice;
            AskVol = data.AskVol;
            BidPrice = data.BidPrice;
            BidVol = data.BidVol;
            Code = data.Code;
            High = data.High;
            HighLimited = data.HighLimited;
            Low = data.Low;
            LowLimited = data.LowLimited;
            Match = data.Match;
            PreClose = data.PreClose;
            WindCode = data.WindCode;
            Time = data.Time;
            Status = data.Status;
            IOPV = 1;
        }

        public MarketData(TDFIndexData data)
        {
            ActionDay = data.ActionDay;
            Code = data.Code;
            WindCode = data.WindCode;
            Time = data.Time;
            Match = (uint)data.LastIndex;
            PreClose = (uint)data.PreCloseIndex;
            HighLimited = (uint)data.HighIndex;
            LowLimited = (uint)data.LowIndex;
            IOPV = 2;
        }
        
    }
}
