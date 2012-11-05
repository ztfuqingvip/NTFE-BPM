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
    /*
     * 调度项关键状态说明：
     * IsValid=是否有效（等同是否删除标记），无效的调度项不再被使用
     * IsExecuted=是否被执行完毕
     * IsError=是否在执行时发生了错误
     * EnableAutoRetry=是否启用自动重试，在执行发生异常时调度器会自动在下一次调度时重复执行直到成功，少量可经过简单重试能恢复的调度才允许启用此属性
     * EnableActiveAfterExecuted=执行后是否允许尝试将流程置为Active状态，用于部分可以让流程进入调度安全状态的调度项，如：workitemcreate，subprocesscreate
     * 
     */

    /// <summary>
    /// 等待恢复/调度的请求
    /// <remarks>支持定时</remarks>
    /// </summary>
    public abstract class WaitingResumption : EntityBase<long>, IAggregateRoot
    {
        protected static readonly long? _emptyLong = new long?();
        public static readonly int MaxPriority = 10;
        /// <summary>
        /// 获取请求恢复的流程
        /// </summary>
        public virtual Process Process { get; protected set; }
        /// <summary>
        /// 获取优先级
        /// </summary>
        public virtual int Priority { get; protected set; }
        /// <summary>
        /// 获取请求创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }
        /// <summary>
        /// 获取期望恢复的时间点
        /// <remarks>设置DateTime.MaxValue则无限期延迟直至被主动唤醒</remarks>
        /// </summary>
        public virtual DateTime? At { get; private set; }
        
        /// <summary>
        /// 获取请求是否有效
        /// <remarks>也表示是否被删除</remarks>
        /// </summary>
        public virtual bool IsValid { get; private set; }
        /// <summary>
        /// 获取是否被执行完毕
        /// </summary>
        public virtual bool IsExecuted { get; private set; }
        /// <summary>
        /// 获取请求是否在执行时发生错误
        /// </summary>
        public virtual bool IsError { get; private set; }

        /// <summary>
        /// 获取该请求的负责者
        /// </summary>
        public virtual string ChargingBy { get; private set; }

        /// <summary>
        /// 获取是否允许在当前时间被恢复
        /// <remarks>
        /// 若设置了At则需要在满足时间条件后才返回true
        /// </remarks>
        /// </summary>
        public virtual bool CanResumeAtNow
        {
            get
            {
                return !this.At.HasValue
                    || (this.At.Value != DateTime.MaxValue && this.At.Value <= DateTime.Now);
            }
        }
        /// <summary>
        /// 获取该调度项是否允许被执行
        /// <remarks>未被执行过的且对应流程处于Running状态的调度项才可以被执行</remarks>
        /// </summary>
        public virtual bool CanExecuting
        {
            get
            {
                return !this.IsExecuted
                    && this.IsValid
                    && (this.Process.Status == ProcessStatus.Running);
            }
        }

        /// <summary>
        /// 获取调度项对应的流程图索引
        /// <remarks>
        /// 主要用于指示不同调度项实例是否属于同一个并行节点内
        /// 只有适合并行的调度项才允许设置此值
        /// 值相同的调度项之间可以并行调度
        /// </remarks>
        /// </summary>
        public virtual long? FlowNodeIndex { get; protected set; }
        /// <summary>
        /// 获取该调度项是否允许被自动重试
        /// <remarks>默认不允许</remarks>
        /// </summary>
        public virtual bool EnableAutoRetry { get; protected set; }
        /// <summary>
        /// 获取该调度项执行完成后是否允许对应流程实例尝试进入Active状态
        /// <remarks>根据调度逻辑设置此属性</remarks>
        /// </summary>
        public virtual bool EnableActiveAfterExecuted { get; protected set; }
        /// <summary>
        /// 获取处理程序类型
        /// </summary>
        public abstract Type Handle { get; }

        protected WaitingResumption()
        {
            this.CreateTime = DateTime.Now;
            this.IsValid = true;
        }
        /// <summary>
        /// 初始化调度
        /// </summary>
        /// <param name="process"></param>
        /// <param name="priority"></param>
        public WaitingResumption(Process process, int priority)
            : this()
        {
            this.Process = process;
            this.Priority = priority;

            this.Validate();
            //使用流程的charging
            this.ChargingBy = this.Process.GetChargingBy();
        }
        /// <summary>
        /// 初始化延迟调度
        /// </summary>
        /// <param name="process"></param>
        /// <param name="priority"></param>
        /// <param name="at">延迟到指定时间</param>
        public WaitingResumption(Process process, int priority, DateTime? at)
            : this(process, priority)
        {
            this.At = at;
        }

        /// <summary>
        /// 设置为无效
        /// </summary>
        public virtual void SetInvalid()
        {
            this.IsValid = false;
        }
        /// <summary>
        /// 设置为已执行完毕
        /// </summary>
        public virtual void SetExecuted()
        {
            this.IsExecuted = true;
            this.SetInvalid();
        }
        /// <summary>
        /// 设置为执行错误
        /// </summary>
        public virtual void SetError(bool isError)
        {
            this.IsError = isError;
        }
        /// <summary>
        /// 设置在指定时间唤醒
        /// </summary>
        /// <param name="time"></param>
        public virtual void WakeUpAt(DateTime time)
        {
            if (!this.At.HasValue)
                throw new InvalidOperationException("非延迟的调度请求不能设置唤醒时间");
            this.At = time;
        }

        private void Validate()
        {
            if (this.Process == null)
                throw new InvalidOperationException("Process不能为空");
            if (this.Process.Status != ProcessStatus.Running)
                throw new InvalidOperationException("只允许为处于Running状态的流程创建调度请求");
        }
    }

    /// <summary>
    /// 定义如何恢复请求
    /// </summary>
    public interface IWaitingResumptionHandle
    {
        void Resume(WaitingResumption waitingResumption);
    }
}