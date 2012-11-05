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
using CodeSharp.Core.DomainBase;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 流程调度异常/错误信息记录
    /// </summary>
    public abstract class ErrorRecord : EntityBase<long>, IAggregateRoot
    {
        private static readonly string _format = "【Message】{0}\n【Source】{1}\n【StackTrace】\n{2}\n【HelpLink】{3}\n";
        /// <summary>
        /// 获取出现异常的流程
        /// </summary>
        public virtual Process Process { get; private set; }
        /// <summary>
        /// 获取异常内容/原因
        /// </summary>
        public virtual string Reason { get; private set; }
        /// <summary>
        /// 获取错误
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }

        //是否删除
        private bool _isDeleted { get; set; }

        protected ErrorRecord()
        {
            this.CreateTime = DateTime.Now;
        }
        protected ErrorRecord(Process process, Exception reason)
            : this()
        {
            this.Process = process;
            this.Reason = this.PrepareReason(reason);

            this.Validate();
        }

        /// <summary>
        /// 标记为删除
        /// </summary>
        public virtual void MarkAsDeleted()
        {
            this._isDeleted = true;
        }

        private void Validate()
        {
            if (this.Process == null)
                throw new InvalidOperationException("Process不能为空");
            if (string.IsNullOrWhiteSpace(this.Reason))
                throw new InvalidOperationException("Reason不能为空");
        }
        private string PrepareReason(Exception e)
        {
            return e == null ? string.Empty : this.GenerateReason(e, string.Empty, 0);
        }
        private string GenerateReason(Exception e, string text, int depth)
        {
            if (depth++ >= 20)
                return text;

            text += string.Format(_format
                , e.Message
                , e.Source
                , e.StackTrace
                , e.HelpLink);
            return e.InnerException != null
                ? this.GenerateReason(e.InnerException, text, depth)
                : text;
        }
    }

    //错误书签记录
    public class FaultBookmarkRecord : ErrorRecord
    {
        /// <summary>
        /// 获取错误书签名称
        /// </summary>
        public virtual string BookmarkName { get; private set; }
        /// <summary>
        /// 获取发生错误的节点名称
        /// </summary>
        public virtual string ActivityName { get; private set; }

        protected FaultBookmarkRecord() : base() { }
        public FaultBookmarkRecord(Process process, Exception reason, string bookmarkName, string activityName)
            : base(process, reason)
        {
            this.BookmarkName = bookmarkName;
            this.ActivityName = activityName;

            this.Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.BookmarkName))
                throw new InvalidOperationException("BookmarkName不能为空");
            if (string.IsNullOrWhiteSpace(this.ActivityName))
                throw new InvalidOperationException("ActivityName不能为空");
        }
    }
    //错误调度记录
    public class FaultResumptionRecord : ErrorRecord
    {
        /// <summary>
        /// 获取错误调度项标识
        /// </summary>
        public virtual long ResumptionId { get; private set; }

        protected FaultResumptionRecord() : base() { }
        public FaultResumptionRecord(Process process, Exception reason, long resumptionId)
            : base(process, reason)
        {
            this.ResumptionId = resumptionId;

            this.Validate();
        }

        private void Validate()
        {
           
        }
    }
}