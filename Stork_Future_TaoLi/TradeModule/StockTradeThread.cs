﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Threading.Tasks;
using Stork_Future_TaoLi.Variables_Type;
using Stork_Future_TaoLi.Queues;
using Stork_Future_TaoLi.Modulars;
using MCStockLib;
using Stork_Future_TaoLi;
using Newtonsoft.Json;

namespace Stork_Future_TaoLi.TradeModule
{
    public class StockTradeThread
    {
        private static LogWirter log = new LogWirter();  //主线程记录日志
        private static LogWirter sublog = new LogWirter(); //子线程记录日志
        

        public static void Main()
        {
            //股票交易主控线程名设定
            //Thread.CurrentThread.Name = "StockTradeControl";

            //初始化消息队列
            queue_stock_excuteThread.Init();

            //创建线程任务 
            Task StockTradeControl = new Task(ThreadProc);

            //启动主线程
            StockTradeControl.Start();

            //日志初始化
            log.EventSourceName = "股票交易线程控制模块";
            log.EventLogType = System.Diagnostics.EventLogEntryType.Information;
            log.EventLogID = 62302;

            sublog.EventSourceName = "股票交易线程模块";
            sublog.EventLogType = System.Diagnostics.EventLogEntryType.Information;
            sublog.EventLogID = 62303;
        }

        private static void ThreadProc()
        {
            //初始化子线程
            int stockNum = CONFIG.STOCK_TRADE_THREAD_NUM;

            DateTime lastmessage = DateTime.Now;

            List<Task> TradeThreads = new List<Task>();
            log.LogEvent("股票交易控制子线程启动： 初始化交易线程数 :" + stockNum.ToString());

            //启动心跳和交易线程
            Task.Factory.StartNew(() => HeartBeatThreadProc((object)stockNum));
            for (int i = 0; i < stockNum; i++)
            {
                TradeParaPackage tpp = new TradeParaPackage();
                tpp._threadNo = (i);
                object para = (object)tpp;
                TradeThreads.Add(Task.Factory.StartNew(() => StockTradeSubThreadProc(para)));
            }

            //此时按照配置，共初始化CONFIG.STOCK_TRADE_THREAD_NUM 数量交易线程
            // 交易线程按照方法 StockTradeSubThreadProc 执行

            //Loop 完成对于子线程的监控
            //每一秒钟执行一次自检
            //自检包含任务：
            //  1. 对每个线程，判断当前交易执行时间，超过 CONFIG.STOCK_TRADE_OVERTIME 仍未收到返回将会按照 该参数备注执行
            //  2. 判断当前线程空闲状态，整理可用线程列表
            //  3. 若当前存在可用线程，同时消息队列（queue_prdTrade_SH_tradeMonitor，queue_prdTrade_SZ_tradeMonitor)
            //      也包含新的消息送达，则安排线程处理交易
            //  4. 记录每个交易线程目前处理的交易内容，并写入数据库
            while (true)
            {
                
                Thread.Sleep(1);
                String logword = String.Empty;
                
                //获取下一笔交易
                List<TradeOrderStruct> next_trade = new List<TradeOrderStruct>();
                if (QUEUE_SH_TRADE.GetQueueNumber() > 0)
                {
                    lock (QUEUE_SH_TRADE.GetQueue().SyncRoot)
                    {
                        if (QUEUE_SH_TRADE.GetQueue().Count > 0)
                        {
                            next_trade = (List<TradeOrderStruct>)QUEUE_SH_TRADE.GetQueue().Dequeue();
                        }
                        if (next_trade.Count > 0)
                        {
                            log.LogEvent("上海交易所出队交易数量：" + next_trade.Count.ToString());
                        }
                    }
                }
                else if (QUEUE_SZ_TRADE.GetQueueNumber() > 0)
                {
                    lock (QUEUE_SZ_TRADE.GetQueue().SyncRoot)
                    {
                        if (QUEUE_SZ_TRADE.GetQueue().Count > 0)
                        {
                            next_trade = (List<TradeOrderStruct>)QUEUE_SZ_TRADE.GetQueue().Dequeue();
                        }

                        log.LogEvent("深圳交易所出队交易数量：" + next_trade.Count.ToString());
                    }
                }


                if (next_trade.Count == 0)
                {
                    if ((DateTime.Now - GlobalHeartBeat.GetGlobalTime()).TotalMinutes > 10)
                    {
                        log.LogEvent("本模块供血不足，线程即将死亡");
                        KeyValuePair<string, object> message1 = new KeyValuePair<string, object>("THREAD_STOCK_TRADE_MONITOR", (object)false);
                        queue_system_status.GetQueue().Enqueue((object)message1);
                        break;
                    }

                    if (lastmessage.Second != DateTime.Now.Second)
                    {
                        KeyValuePair<string, object> message1 = new KeyValuePair<string, object>("THREAD_STOCK_TRADE_MONITOR", (object)true);
                        try
                        {
                            queue_system_status.GetQueue().Enqueue((object)message1);
                            lastmessage = DateTime.Now;
                        }
                        catch
                        {
                            //do nothing
                        }
                    }

                    continue;
                }
                else
                {


                    //此时内存中包含了即将被进行的交易

                    //按照15个一组分割交易
                    List<TradeOrderStruct> tradeGroup = new List<TradeOrderStruct>();
                    foreach (TradeOrderStruct trade in next_trade)
                    {
                        if (tradeGroup.Count < 15)
                        {
                            tradeGroup.Add(trade);

                            if (tradeGroup.Count == 15)
                            {
                                //判断空闲的线程
                                //利用随机选择，保证线程的平均使用
                                String para = JsonConvert.SerializeObject(tradeGroup);
                                ThreadPool.QueueUserWorkItem(new WaitCallback(ChooseThread), (object)para);
                                logword += " 发送交易： " + " 时间： " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss") + " : " + DateTime.Now.Millisecond.ToString() + "\r\n";        
                                tradeGroup.Clear();
                            }
                        }
                    }
                    if (tradeGroup.Count > 0)
                    {
                        //判断空闲的线程
                        //利用随机选择，保证线程的平均使用
                        String para = JsonConvert.SerializeObject(tradeGroup);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(ChooseThread), (object)para);
                        logword += " 发送交易： " + " 时间： " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss") + " : " + DateTime.Now.Millisecond.ToString() + "\r\n";
                        tradeGroup.Clear();
                    }

                    GlobalTestLog.LogInstance.LogEvent(logword);
                }
            }

            Thread.CurrentThread.Abort();

        }

        /// <summary>
        /// 执行交易线程处理逻辑
        /// 包含功能：
        ///     1. 连接股票交易所（上海，深圳）
        ///     2. 心跳包发送/接收
        ///     3. 发送交易，等待响应
        /// </summary>
        private static void StockTradeSubThreadProc(object para)
        {

            MCStockLib.managedStockClass _classTradeStock = new managedStockClass();
            MCStockLib.managedLogin login = new managedLogin(CommConfig.Stock_ServerAddr, CommConfig.Stock_Port, CommConfig.Stock_Account, CommConfig.Stock_BrokerID, CommConfig.Stock_Password, CommConfig.Stock_InvestorID);
            string ErrorMsg = string.Empty;

            bool DebugMark = false; //测试交易标志
            StockTradeTest test = new StockTradeTest();//测试类

            //令该线程为前台线程
            Thread.CurrentThread.IsBackground = true;

            TradeParaPackage _tpp = (TradeParaPackage)para;

            //当前线程编号
            int _threadNo = _tpp._threadNo;

            sublog.LogEvent("线程 ：" + _threadNo.ToString() + " 开始执行");
            //用作发送心跳包的时间标记
            DateTime _markedTime = DateTime.Now;
            DateTime lastmessagetime = DateTime.Now;

            //初始化通信
            //功能1
            _classTradeStock.Init(login, ErrorMsg);

            while (true)
            {
                Thread.Sleep(10);
                if ((DateTime.Now - GlobalHeartBeat.GetGlobalTime()).TotalMinutes > 10)
                {
                    sublog.LogEvent("线程 ：" + _threadNo.ToString() + "心跳停止 ， 最后心跳 ： " + GlobalHeartBeat.GetGlobalTime().ToString());
                    break;
                }

                //Thread.CurrentThread.stat
                if (_markedTime.Minute != DateTime.Now.Minute)
                {
                    //发送心跳包
                    //功能2
                    //_stockTradeAPI.heartBeat();
                    _classTradeStock.HeartBeat();
                    _markedTime = DateTime.Now;
                }

                if(lastmessagetime.Second != DateTime.Now.Second)
                {
                    KeyValuePair<string, object> message1 = new KeyValuePair<string, object>("THREAD_STOCK_TRADE_WORKER", (object)_threadNo);
                    try
                    {
                        queue_system_status.GetQueue().Enqueue((object)message1);
                        lastmessagetime = DateTime.Now;
                    }
                    catch
                    {
                        //do nothing
                    }
                }


                //if (!_classTradeStock.getConnectStatus())
                //{
                //    _classTradeStock.Init(login, ErrorMsg);
                //}

                if (queue_stock_excuteThread.GetQueue(_threadNo).Count < 2)
                {
                    queue_stock_excuteThread.SetThreadFree(_threadNo);
                }

                if (queue_stock_excuteThread.GetQueue(_threadNo).Count > 0)
                {
                    List<TradeOrderStruct> Queuetrades = (List<TradeOrderStruct>)queue_stock_excuteThread.StockExcuteQueues[_threadNo].Dequeue();

                    List<TradeOrderStruct> trades = new List<TradeOrderStruct>();

                    int eni = 0;
                    while (true)
                    {
                        try
                        {
                            TradeOrderStruct trade = Queuetrades[eni];
                            trades.Add(new TradeOrderStruct()
                            {
                                belongStrategy = trade.belongStrategy,
                                cExhcnageID = trade.cExhcnageID,
                                cOffsetFlag = trade.cOffsetFlag,
                                cOrderexecutedetail = trade.cOrderexecutedetail,
                                cOrderLevel = trade.cOrderLevel,
                                cOrderPriceType = trade.cOrderPriceType,
                                cSecurityCode = trade.cSecurityCode,
                                cSecurityType = trade.cSecurityType,
                                cTradeDirection = trade.cTradeDirection,
                                cUser = trade.cUser,
                                dOrderPrice = trade.dOrderPrice,
                                nSecurityAmount = trade.nSecurityAmount,
                                OrderRef = trade.OrderRef,
                                SecurityName = trade.SecurityName
                            });
                            eni++;
                        }
                        catch
                        {
                            break;
                        }
                    }


                    Guid tradeuuid = Guid.NewGuid();

                    if (trades == null || trades.Count == 0)
                    {
                        continue;
                    }

                    if(trades[0].cUser == DebugMode.TestUser)
                    {
                        DebugMark = true;
                    }
                    else
                    {
                        DebugMark = false;
                    }

                    if (trades.Count > 0)
                    {
                        sublog.LogEvent("线程 ：" + _threadNo.ToString() + " 执行交易数量 ： " + trades.Count);
                    }

                    if (!_classTradeStock.getConnectStatus())
                    {
                        _classTradeStock.Init(login, ErrorMsg);
                    }

                    //根据消息队列中发来的消息数量判断调用的接口
                    //当内容大于1 ，调用批量接口
                    //当内容等于1， 调用单笔接口
                    queue_stock_excuteThread.SetUpdateTime(_threadNo);
                    List<QueryEntrustOrderStruct_M> entrustorli = new List<QueryEntrustOrderStruct_M>();

                    if (trades.Count > 1)
                    {
                        //trades
                        Random seed = new Random();
                        sublog.LogEvent("线程 ：" + _threadNo.ToString() + "进入执行环节 ， 忙碌状态： " + queue_stock_excuteThread.GetThreadIsAvailiable(_threadNo));

                        TradeOrderStruct_M[] tradesUnit = new TradeOrderStruct_M[15];
                        int i = 0;
                        QueryEntrustOrderStruct_M[] entrustUnit = new QueryEntrustOrderStruct_M[15];
                        string s = string.Empty;
                        foreach (TradeOrderStruct unit in trades)
                        {
                            tradesUnit[i] = CreateTradeUnit(unit);
                            i++;
                        }

                        string user = trades[0].cUser;

                        //GlobalTestLog.LogInstance.LogEvent("线程 ：" + _threadNo.ToString() +  " 发送交易： " + tradeuuid.ToString() + " 时间： " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss") + " : " + DateTime.Now.Millisecond.ToString());
                        
                        if (DebugMark == true)
                        {
                            
                            test.BatchTradeTest(tradesUnit, trades.Count, out entrustUnit, out s);
                        }
                        else
                        {
                            entrustUnit = _classTradeStock.BatchTrade(tradesUnit, trades.Count, s);
                        }

                        //GlobalTestLog.LogInstance.LogEvent("线程 ：" + _threadNo.ToString() +  " 收到交易回报： " + tradeuuid.ToString() + " 时间： " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss") + " : " + DateTime.Now.Millisecond.ToString());

                        if (entrustUnit != null && entrustUnit.ToList().Count() > 0)
                        {
                            i = -1;
                            foreach (QueryEntrustOrderStruct_M unit in entrustUnit.ToList())
                            {
                                i++;
                                if (unit == null)
                                    continue;

                                if (unit.OrderSysID != null && unit.OrderSysID != String.Empty && unit.OrderSysID != "0" )
                                {
                                    entrustorli.Add(unit);
                                }
                                else
                                {
                                    //委托失败处理
                                    GlobalErrorLog.LogInstance.LogEvent("获取委托号失败！用户: " + user + ",代码：" + unit.Code);
                                }

                                

                                if (unit.Direction == 49)
                                {
                                    //清除风控冷冻金额
                                    //只有股票买入，才需要移除风控列表
                                    accountMonitor.UpdateRiskFrozonAccount(user, unit.Code, tradesUnit[i].SecurityAmount * (-1), tradesUnit[i].SecurityAmount * tradesUnit[i].OrderPrice * (-1), "S", "0");
                                }


                            }
                        }
                    }
                    else if (trades.Count == 1)
                    {
                        //trades
                        Random seed = new Random();

                        TradeOrderStruct_M tradesUnit = CreateTradeUnit(trades[0]);

                        QueryEntrustOrderStruct_M entrustUnit = new QueryEntrustOrderStruct_M();
                        string s = string.Empty;

                        string user = trades[0].cUser;

                        //GlobalTestLog.LogInstance.LogEvent("发送交易： " + tradeuuid.ToString() + " 时间： " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss") + " : " + DateTime.Now.Millisecond.ToString());

                        if (DebugMark == true)
                        {
                            test.SingleTradeTest(tradesUnit, out entrustUnit, out s);
                        }
                        else
                        {
                            _classTradeStock.SingleTrade(tradesUnit, entrustUnit, s);
                        }

                        //GlobalTestLog.LogInstance.LogEvent("收到交易回报： " + tradeuuid.ToString() + " 时间： " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss") + " : " + DateTime.Now.Millisecond.ToString());

                        if (entrustUnit.OrderSysID != null && entrustUnit.OrderSysID != String.Empty && entrustUnit.OrderSysID != "0")
                        {
                            entrustorli.Add(entrustUnit);
                        }
                        else
                        {
                            //委托失败处理
                            GlobalErrorLog.LogInstance.LogEvent("获取委托号失败！用户: " + user + ",代码：" + tradesUnit.SecurityCode);
                        }

                        if (entrustUnit.Direction.ToString() == TradeOrientationAndFlag.StockTradeDirectionBuy)
                        {
                            //清除风控冷冻金额,只有买入需要清除风控列表
                            accountMonitor.UpdateRiskFrozonAccount(user, tradesUnit.SecurityCode, tradesUnit.SecurityAmount * (-1), tradesUnit.SecurityAmount * tradesUnit.OrderPrice * (-1), "S", tradesUnit.TradeDirection.ToString());
                        }
                    }

                    //*********************************
                    //  交易成功后执行的特殊处理
                    //  交易生成委托存入数据库，并将委托送往查询成交线程
                    //*********************************
                    if (trades.Count != 0)
                    {

                        //存入数据库
                        if (entrustorli.Count() == 0) { continue; }

                        for (int i = 0; i < trades.Count; i++)
                        {
                            entrustorli[i].Code = trades[i].cSecurityCode;
                            entrustorli[i].StrategyId = trades[0].belongStrategy;
                            entrustorli[i].Direction = Convert.ToInt32(trades[i].cTradeDirection);
                            entrustorli[i].OrderRef = Convert.ToInt32(trades[i].OrderRef);
                            entrustorli[i].OrderPrice = trades[i].dOrderPrice;
                            entrustorli[i].SecurityType = (sbyte)115;
                            entrustorli[i].User = trades[i].cUser;
                            ThreadPool.QueueUserWorkItem(new WaitCallback(DBAccessLayer.CreateERRecord), (object)(entrustorli[i]));
                            queue_query_entrust.GetQueue().Enqueue((object)entrustorli[i]);
                            ERecord record = new ERecord()
                            {
                                UserName = trades[i].cUser,
                                Amount = trades[i].nSecurityAmount,
                                Code = trades[i].cSecurityCode,
                                ExchangeId = trades[i].cExhcnageID,
                                OrderPrice = trades[i].dOrderPrice,
                                OrderRef = trades[i].OrderRef,
                                StrategyNo = trades[i].belongStrategy,
                                SysOrderRef = entrustorli[i].OrderSysID,
                                Direction = trades[i].cTradeDirection
                            };

                            EntrustRecord.AddEntrustRecord(record);
                           
                        }
                    }
                }
            }

            //线程结束
            Thread.CurrentThread.Abort();

        }

        private static void HeartBeatThreadProc(object para)
        {
            int threadCount = (int)para;
            DateTime lastSubHeartBeat = DateTime.Now; //子线程最后发送心跳时间
            

            while (true)
            {
                Thread.Sleep(10);

                if ((DateTime.Now - GlobalHeartBeat.GetGlobalTime()).TotalMinutes > 10)
                {
                    log.LogEvent("心跳线程即将退出");
                    break;
                }

                //向子线程发送存活心跳，一旦心跳停止，则子线程死亡
                if (DateTime.Now.Second % 5 == 0 && DateTime.Now.Second != lastSubHeartBeat.Second)
                {
                    for (int i = 0; i < threadCount; i++)
                    {
                        List<TradeOrderStruct> o = new List<TradeOrderStruct>();
                        queue_stock_excuteThread.GetQueue(i).Enqueue((object)o);

                    }
                    lastSubHeartBeat = DateTime.Now;
                }

            }

            Thread.CurrentThread.Abort();
        }

        private static TradeOrderStruct_M CreateTradeUnit(TradeOrderStruct unit)
        {


            TradeOrderStruct_M _sorder = new TradeOrderStruct_M();

            _sorder.ExchangeID = (unit.cExhcnageID.Length != 0) ? unit.cExhcnageID : "0";
            _sorder.OffsetFlag = (unit.cOffsetFlag.Length != 0) ? Convert.ToSByte(unit.cOffsetFlag) : Convert.ToSByte("0");
            _sorder.OrderExecutedDetail = (sbyte)0;
            _sorder.OrderLevel = (unit.cOrderLevel.Length != 0) ? Convert.ToSByte(unit.cOrderLevel) : Convert.ToSByte("0");
            _sorder.OrderPrice = unit.dOrderPrice;
            _sorder.OrderPriceType = (unit.cOrderPriceType.Length != 0) ? Convert.ToSByte(unit.cOrderPriceType) : Convert.ToSByte("0");
            _sorder.SecurityAmount = (int)(unit.nSecurityAmount);
            _sorder.SecurityCode = (unit.cSecurityCode.Length != 0) ? unit.cSecurityCode : "0";
            _sorder.SecurityType = (unit.cSecurityType.Length != 0) ? (unit.cSecurityType == "115" ? (sbyte)115 : (sbyte)102) : (sbyte)115;


            if ((unit.cTradeDirection == "1") || (unit.cTradeDirection == "49"))
            {
                _sorder.TradeDirection = 49;
            }
            else
            {
                _sorder.TradeDirection = 50;
            }





            //_sorder.TradeDirection = Convert.ToSByte(Convert.ToInt16(unit.cTradeDirection));

            //if (unit.cTradeDirection == "0") { _sorder.TradeDirection = 49; }
            //else if (unit.cTradeDirection == "1") { _sorder.TradeDirection = 50; }
            //else { _sorder.TradeDirection = 49; }



            return _sorder;

        }

        private static void ChooseThread(object package)
        {

            List<TradeOrderStruct> tradeGroup = JsonConvert.DeserializeObject<List<TradeOrderStruct>>(package.ToString());
            if (tradeGroup == null) return;
            int stockNum = 100;
            Random ran = new Random();
            //int stockNum = CONFIG.STOCK_TRADE_THREAD_NUM;
            int _tNo = ran.Next(0, stockNum);
           
            //bool _bSearch = true;
            //while (_bSearch)
            //{
            //    if (queue_stock_excuteThread.GetThreadIsAvailiable(_tNo))
            //    {
            //        _bSearch = false;
            //    }
            //    _tNo++;
            //    if (_tNo == stockNum)
            //    {
            //        _tNo = 0;
            //    }
            //}

            //GlobalTestLog.LogInstance.LogEvent("线程：" + _tNo.ToString() + " 发送交易： " + " 时间： " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss") + " : " + DateTime.Now.Millisecond.ToString());
            log.LogEvent("安排线程 ： " + _tNo + " 执行交易 数量： " + ((List < TradeOrderStruct >) tradeGroup).Count);
            queue_stock_excuteThread.GetQueue(_tNo).Enqueue(tradeGroup);
            ThreadPool.QueueUserWorkItem(new WaitCallback(queue_stock_excuteThread.SetThreadBusy), (object)(_tNo));

        }

    }

    public class TradeParaPackage
    {
        //当前线程的编号
        public int _threadNo { get; set; }
    }
} 