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
using System.Linq;
using System.Text;
using Taobao.Activities.Hosting;
using Castle.Services.Transaction;
using CodeSharp.Core;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 流程运行请求
    /// </summary>
    public class ProcessStartResumption : WaitingResumption
    {
        protected ProcessStartResumption() : base() { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="process"></param>
        public ProcessStartResumption(Process process)
            : base(process, process.Priority) { }

        public override Type Handle
        {
            get { return typeof(ProcessStartWaitingResumption); }
        }
    }
    [CodeSharp.Core.Component]
    [Transactional]
    public class ProcessStartWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private IWorkflowParser _parser;
        public ProcessStartWaitingResumption(ILoggerFactory factory, IWorkflowParser parser)
        {
            this._log = factory.Create(typeof(ProcessStartWaitingResumption));
            this._parser = parser;
        }

        #region IWaitingResumption Members
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            var r = waitingResumption as ProcessStartResumption;
            var instance = WorkflowBuilder.CreateInstance(r.Process, this._parser);
            instance.Run();
        }

        #endregion
    }
}