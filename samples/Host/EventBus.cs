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

namespace Host
{
    //TODO:NSF分布式事务完成后事件逻辑需要重构为支持事务
    //或采用事务性消息队列方式分发

    /// <summary>
    /// 提供与原有系统直接集成的NTFE-BPM事件处理，这是一个范例实现，可按需扩充
    /// </summary>
    [CodeSharp.Core.Component(LifeStyle.Singleton)]
    public class EventBus : IEventBus
    {
        private static readonly string _state = "ntfe";
        private ILog _log;
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="factroy"></param>
        public EventBus(ILoggerFactory factroy)
        {
            this._log = factroy.Create(typeof(EventBus));
        }

        #region IEventBus Members

        public void RaiseWorkItemArrived(Taobao.Workflow.Activities.Hosting.WorkItemArgs args)
        {
            //this._sender.SendArgs("OnTaskArrived", new
            //{
            //    State = _state,
            //    Actioner = args.WorkItem.Actioner.UserName,
            //    FlowId = args.WorkItem.Process.ID.ToString(),
            //    TaskId = args.WorkItem.ID.ToString()
            //}, args.WorkItem.Actioner.UserName);//传递标识作为负载依据
        }

        public void RaiseHumanActivityInstanceStarted(ActivityInstanceArgs args)
        {

        }

        public void RaiseHumanActivityInstanceCompleted(ActivityInstanceArgs args)
        {
            //this._sender.SendArgs("OnActivityCompleted", new
            //{
            //    State = _state,
            //    Name = args.Instance.ActivityName
            //});
        }

        public void RaiseProcessCompleted(ProcessArgs args)
        {
            //this._sender.SendArgs("OnFlowCompleted", new
            //{
            //    State = _state,
            //    FlowId = args.Process.ID.ToString(),
            //    Status = ((int)args.Process.Status).ToString()
            //});
        }

        #endregion
    }
}