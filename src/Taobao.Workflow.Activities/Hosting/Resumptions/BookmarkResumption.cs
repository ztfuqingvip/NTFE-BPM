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
using CodeSharp.Core.Services;
using CodeSharp.Core;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 节点书签待恢复调度项
    /// </summary>
    public class BookmarkResumption : WaitingResumption
    {
        /// <summary>
        /// 获取书签
        /// </summary>
        public virtual string ActivityName { get; private set; }
        /// <summary>
        /// 获取要恢复的书签名
        /// </summary>
        public virtual string BookmarkName { get; private set; }
        /// <summary>
        /// 获取恢复的参数
        /// </summary>
        public virtual string Value { get; private set; }

        protected BookmarkResumption() : base() { }
        /// <summary>
        /// 初始化节点书签待恢复调度项
        /// </summary>
        /// <param name="process"></param>
        /// <param name="activityName"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="value"></param>
        public BookmarkResumption(Process process
            , string activityName
            , string bookmarkName
            , string value)
            : this(process
            , activityName
            , bookmarkName
            , value
            , null) { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="process"></param>
        /// <param name="activityName"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="value"></param>
        /// <param name="at"></param>
        public BookmarkResumption(Process process
            , string activityName
            , string bookmarkName
            , string value
            , DateTime? at)
            : base(process
            , process.Priority
            , at)
        {
            this.ActivityName = activityName;
            this.BookmarkName = bookmarkName;
            this.Value = value;

            this.Validate();
        }

        public override Type Handle
        {
            get { return typeof(BookmarkWaitingResumption); }
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(this.ActivityName))
                throw new InvalidOperationException("ActivityName不能为空");
            if (string.IsNullOrEmpty(this.BookmarkName))
                throw new InvalidOperationException("BookmarkName不能为空");
        }
    }
    [CodeSharp.Core.Component]
    [Transactional]
    public class BookmarkWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private IWorkflowParser _parser;
        private IProcessService _processService;
        public BookmarkWaitingResumption(ILoggerFactory factory, IWorkflowParser parser,IProcessService processService)
        {
            this._log = factory.Create(typeof(BookmarkWaitingResumption));
            this._parser = parser;
            this._processService = processService;
        }

        #region IWaitingResumption Members
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            var r = waitingResumption as BookmarkResumption;
            var persisted = this._processService.GetWorkflowInstance(r.Process).WorkflowInstance;
            var instance = WorkflowBuilder.CreateInstance(r.Process, this._parser);
            instance.Load(persisted);

            //只能尝试恢复存在的书签
            if (instance.GetBookmarks().Any(o => o.Name == r.BookmarkName))
            {
                instance.Update(r.Process.GetInputs());
                instance.ResumeBookmark(r.BookmarkName, r.Value);
            }
            else
                this._log.WarnFormat("没有在流程“{0}”#{1}中找到名为{2}的书签，取消本次恢复", r.Process.Title, r.Process.ID, r.BookmarkName);
        }
        #endregion
    }
}