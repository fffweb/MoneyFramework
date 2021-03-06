﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Stork_Future_TaoLi.Database;

namespace Stork_Future_TaoLi.Hubs
{
    public class TradeMonitorHub : Hub
    {
        public void linkin()
        {
            String name = Clients.CallerState.USERNAME;
            TradeMonitor.Instance.Join(name, Context.ConnectionId);
        }
    }

    public class TradeMonitor
    {
        private IHubContext _context;
        private TradeMonitor(IHubContext context) { _context = context; }
        private readonly static TradeMonitor _instance = new TradeMonitor(GlobalHost.ConnectionManager.GetHubContext<TradeMonitorHub>());
        private static object syncRoot = new object();
        public static TradeMonitor Instance { get { return _instance; } }

        public static Dictionary<string, List<OrderViewItem>> OrderLists = new Dictionary<string, List<OrderViewItem>>();

        //用户名和链接ID的关系
        private Dictionary<String, String> UserConnectionRelation = new Dictionary<string, string>();

        /// <summary>
        /// 页面股票/期货委托信息更新
        /// 股票通过Entrust_query查询最新委托状态并更新
        /// 期货通过UpdateOrder 函数更新最新状态
        /// 该函数传入参数中的JsonString，是单只股票期货的状态，本地维护OrderList来保存之前交易的状态。
        /// </summary>
        /// <param name="name"></param>
        /// <param name="JsonString"></param>
        public void updateOrderList(String name, OrderViewItem item)
        {
            try
            {
                if (item != null)
                {
                    if (!OrderLists.Keys.Contains(name))
                    {
                        List<OrderViewItem> ss = new List<OrderViewItem>();
                        ss.Add(item);
                        OrderLists.Add(name, ss);
                    }

                    List<OrderViewItem> orders = OrderLists[name];

                    OrderViewItem order = orders.Find(
                             delegate(OrderViewItem record)
                             {
                                 return record.OrderRef == item.OrderRef;
                             }
                         );

                    if (order == null) OrderLists[name].Add(item);
                    else
                    {
                        order.MSG = item.MSG;
                        order.VolumeTotal = item.VolumeTotal;
                    }

                    if (!UserConnectionRelation.ContainsKey(name)) { return; }

                    _context.Clients.Client(UserConnectionRelation[name]).updateOrderList(JsonConvert.SerializeObject(orders));
                }
                else
                {
                    if (!UserConnectionRelation.ContainsKey(name)) { return; }

                     if (!OrderLists.Keys.Contains(name))
                     {
                         List<OrderViewItem> ss = new List<OrderViewItem>();

                         OrderLists.Add(name, ss);
                     }

                     List<OrderViewItem> orders = OrderLists[name];
                     _context.Clients.Client(UserConnectionRelation[name]).updateOrderList(JsonConvert.SerializeObject(orders));
                }
               
            }
            catch (Exception ex) { DBAccessLayer.LogSysInfo("TradeMonitorHub-updateOrderList", ex.ToString()); GlobalErrorLog.LogInstance.LogEvent(ex.ToString()); }
        }

        public void updateTradeList(String name, String JsonString)
        {
            try
            {
                if (!UserConnectionRelation.ContainsKey(name)) { return; }
                _context.Clients.Client(UserConnectionRelation[name]).updateTradeList(JsonString);
            }
            catch (Exception ex)
            {
                DBAccessLayer.LogSysInfo("TradeMonitorHub-updateTradeList", ex.ToString());
                GlobalErrorLog.LogInstance.LogEvent(ex.ToString());
            }
        }

        public void updateRiskList(String name,String JsonStringRisk, String JsonStringRiskPara)
        {
            try
            {
                if (!UserConnectionRelation.ContainsKey(name)) { return; }

                _context.Clients.Client(UserConnectionRelation[name]).updateRiskList(JsonStringRisk);
                _context.Clients.Client(UserConnectionRelation[name]).updateRiskPara(JsonStringRiskPara);
            }
            catch (Exception ex) { DBAccessLayer.LogSysInfo("TradeMonitorHub-updateRiskList", ex.ToString()); GlobalErrorLog.LogInstance.LogEvent(ex.ToString()); }
        }

        public void Join(String user,String Connectionid)
        {
            lock (syncRoot)
            {
                if (user == null || user == String.Empty) return;
                if (UserConnectionRelation.ContainsKey(user))
                {
                    UserConnectionRelation[user] = Connectionid;
                }
                else
                {
                    UserConnectionRelation.Add(user, Connectionid);
                }
            }
        }

        public void updateCCList(String name,List<AccountPosition> CList)
        {
            if (name == null) return;
            name = name.Trim();

            if (!UserConnectionRelation.ContainsKey(name)) { return; }

            _context.Clients.Client(UserConnectionRelation[name]).updateCCList(JsonConvert.SerializeObject(CList));
        }

        public void updateAuditInfo(List<AccountInfo> accounts)
        {
            List<RISK_TABLE> risks = DBAccessLayer.GetLatestRiskRecord();
            List<RISK_TABLE> show_risks = new List<RISK_TABLE>();
            if (risks == null) risks = new List<RISK_TABLE>();
            else
            {
                foreach(RISK_TABLE risk in risks)
                {
                    if(Convert.ToDateTime(risk.time).Date == DateTime.Now.Date)
                    {
                        show_risks.Add(risk);
                    }
                }
            }
            _context.Clients.All.updateauditInfo(JsonConvert.SerializeObject(accounts), JsonConvert.SerializeObject(show_risks));
        }
    }

    /// <summary>
    /// 风控信息
    /// </summary>
    public class TMRiskInfo
    {
        /// <summary>
        /// 代码
        /// </summary>
        public string code { get; set; }

        /// <summary>
        /// 手数
        /// </summary>
        public string hand { get; set; }

        /// <summary>
        /// 价格
        /// </summary>
        public string price { get; set; }

        /// <summary>
        /// 交易方向
        /// </summary>
        public string orientation { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public string time { get; set; }

        /// <summary>
        /// 用户
        /// </summary>
        public string user { get; set; }

        /// <summary>
        /// 策略号
        /// </summary>
        public string strategy { get; set; }

        /// <summary>
        /// 风控信息
        /// </summary>
        public string errinfo { get; set; }
        
    }

    /// <summary>
    /// 委托信息
    /// </summary>
    public class EntrustInfo
    {
        /// <summary>
        /// 系统号
        /// </summary>
        public string sysNo { get; set; }

        /// <summary>
        /// 报单编号
        /// </summary>
        public string entrustNo { get; set; }

        /// <summary>
        /// 合约代码
        /// </summary>
        public string contract { get; set; }

        /// <summary>
        /// 买卖
        /// </summary>
        public string direction { get; set; }

        /// <summary>
        /// 开平
        /// </summary>
        public string offsetflag { get; set; }

        /// <summary>
        /// 报单手数
        /// </summary>
        public string amount { get; set; }

        /// <summary>
        /// 未成交手数
        /// </summary>
        public string undealamount { get; set; }

        /// <summary>
        /// 报单价格
        /// </summary>
        public string price { get; set; }

        /// <summary>
        /// 报单状态
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// 报单时间
        /// </summary>
        public string time { get; set; }
    }

}