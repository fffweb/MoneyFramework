﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Stork_Future_TaoLi.Queues
{
    /// <summary>
    /// 行情接收数据队列
    /// </summary>
    public class Queue_Market_Data
    {
        private static Queue instance;

        /// <summary>
        /// 获取队列的实例
        /// </summary>
        /// <returns>队列实例</returns>
        public static Queue GetQueue()
        {
            if (instance == null)
            {
                instance = new Queue();
            }

            return instance;
        }

        /// <summary>
        /// 获取该队列中的消息数量
        /// </summary>
        /// <returns>
        /// -1      队列未初始化或状态异常
        /// 其他    队列包含消息数量
        /// </returns>
        public static int GetQueueNumber()
        {
            if (instance != null)
            {
                return instance.Count;
            }
            else
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// 期货接收数据队列
    /// </summary>
    public class Queue_Future_Data
    {
        private static Queue instance;

        /// <summary>
        /// 获取队列的实例
        /// </summary>
        /// <returns>队列实例</returns>
        public static Queue GetQueue()
        {
            if (instance == null)
            {
                instance = new Queue();
            }

            return instance;
        }

        /// <summary>
        /// 获取该队列中的消息数量
        /// </summary>
        /// <returns>
        /// -1      队列未初始化或状态异常
        /// 其他    队列包含消息数量
        /// </returns>
        public static int GetQueueNumber()
        {
            if (instance != null)
            {
                return instance.Count;
            }
            else
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// 指数接收数据队列
    /// </summary>
    public class Queue_Index_Data
    {
        private static Queue instance;

        /// <summary>
        /// 获取队列的实例
        /// </summary>
        /// <returns>队列实例</returns>
        public static Queue GetQueue()
        {
            if (instance == null)
            {
                instance = new Queue();
            }

            return instance;
        }

        /// <summary>
        /// 获取该队列中的消息数量
        /// </summary>
        /// <returns>
        /// -1      队列未初始化或状态异常
        /// 其他    队列包含消息数量
        /// </returns>
        public static int GetQueueNumber()
        {
            if (instance != null)
            {
                return instance.Count;
            }
            else
            {
                return -1;
            }
        }
    }
}