/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using CodeSharp.Core;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Host
{
    //TODO:NSF分布式事务完成后事件逻辑需要重构为支持事务
    //或采用事务性消息队列方式分发

    /// <summary>
    /// 提供与原有系统直接集成的NTFE-BPM事件处理
    /// </summary>
    [CodeSharp.Core.Component(LifeStyle.Singleton)]
    public class EventBus : IEventBus
    {
        private static readonly string _state = "ntfe";
        private ILog _log;
        private EventListener _sender;
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="factroy"></param>
        /// <param name="serviceCenterWebRootUrl"></param>
        /// <param name="ntfeEventReceiverService"></param>
        public EventBus(ILoggerFactory factroy, string serviceCenterWebRootUrl, string ntfeEventReceiverService)
        {
            this._log = factroy.Create(typeof(EventBus));
            this._sender = new EventListener(factroy
                , serviceCenterWebRootUrl + "/" + ntfeEventReceiverService
                , _state
                , "true"
                , "NTFE-BPM"
                , "75DC6B572D1B940E34159DCD7FF26D8D");
        }

        #region IEventBus Members

        public void RaiseWorkItemArrived(Taobao.Workflow.Activities.Hosting.WorkItemArgs args)
        {
            this._sender.SendArgs("OnTaskArrived", new
            {
                State = _state,
                Actioner = args.WorkItem.Actioner.UserName,
                FlowId = args.WorkItem.Process.ID.ToString(),
                TaskId = args.WorkItem.ID.ToString()
            }, args.WorkItem.Actioner.UserName);//传递标识作为负载依据
        }

        public void RaiseHumanActivityInstanceStarted(ActivityInstanceArgs args)
        {

        }

        public void RaiseHumanActivityInstanceCompleted(ActivityInstanceArgs args)
        {
            this._sender.SendArgs("OnActivityCompleted", new
            {
                State = _state,
                Name = args.Instance.ActivityName
            });
        }

        public void RaiseProcessCompleted(ProcessArgs args)
        {
            this._sender.SendArgs("OnFlowCompleted", new
            {
                State = _state,
                FlowId = args.Process.ID.ToString(),
                Status = ((int)args.Process.Status).ToString()
            });
        }

        #endregion
    }
    /// <summary>
    /// 移植原有事件侦听器实现 暂不做优化
    /// </summary>
    public class EventListener
    {
        private static readonly CodeSharp.Core.Utils.Serializer _serializer = new CodeSharp.Core.Utils.Serializer();
        private ILog _log;
        //事件队列
        private Queue<EventInfo> _eventQueue;
        private bool _flag = false;
        private object _lock = new object();
        //外部用于接收事件的服务
        private string _eventReceiverUrl;
        //用于标识引擎，如：k2,ntfe
        private string _state;
        private string _async;
        private string _source;
        private string _authKey;
        public EventListener(ILoggerFactory factory
            , string eventReceiverUrl
            , string state
            , string async
            , string source
            , string authKey)
        {
            this._log = factory.Create(typeof(EventListener));
            this._eventReceiverUrl = eventReceiverUrl;
            this._state = state;
            this._async = async;
            this._eventQueue = new Queue<EventInfo>();
            this._source = source;
            this._authKey = authKey;
        }

        //HACK:发送消息一律使用此方法进行队列单线程异步发送
        public void SendArgs(string eventName, object args)
        {
            this.SendArgs(eventName, args, null);
        }
        public void SendArgs(string eventName, object args, string flag)
        {
            _eventQueue.Enqueue(new EventInfo() { Name = eventName, Args = args, Flag = flag });
            DoSendArgsTask();
        }
        private void DoSendArgsTask()
        {
            if (_flag)
                return;
            lock (_lock)
            {
                if (_flag)
                    return;
                _flag = true;
            }

            System.Threading.ThreadPool.QueueUserWorkItem(o =>
            {
                EventInfo info = null;
                try
                {
                    while (_eventQueue.Count > 0)
                    {
                        info = _eventQueue.Dequeue();

                        if (string.IsNullOrEmpty(info.Flag))
                            this.CallEventService(info.Name, null, info.Args);
                        else
                            this.CallEventService(info.Name, info.Flag, info.Args);
                    }
                }
                catch (Exception e)
                {
                    _log.Error("DoSendArgsTask Error At " + info, e);
                }
                finally { _flag = false; }
            });
        }
        private void CallEventService(string method, string userId, object args)
        {
            var query = string.Empty;
            try
            {
                using (var wc = new WebClient() { Encoding = Encoding.UTF8 })
                {
                    query = "source=" + this._source
                        + "&authkey=" + this._authKey
                        + "&async=" + this._async
                        + "&state=" + _state
                        + "&userId=" + HttpUtility.UrlEncode(userId)
                        + "&args="
                        + HttpUtility.UrlEncode(_serializer.JsonSerialize(args));
                    wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    wc.UploadData(this._eventReceiverUrl + "/" + method, Encoding.ASCII.GetBytes(query));
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                    throw e;
                using (var r = new StreamReader(e.Response.GetResponseStream()))
                    throw new Exception("Query=" + query + "|Error=" + r.ReadToEnd(), e);
            }
            finally
            {
                this._log.InfoFormat("[Event]method={0}|userId={1}|data={2}", method, userId, query);
            }
        }

        class EventInfo
        {
            public object Args { get; set; }
            public string Name { get; set; }
            public string Flag { get; set; }
        }
    }
}