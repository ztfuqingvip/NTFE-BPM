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
using System.Threading;
using Castle.Services.Transaction;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.RepositoryFramework;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 流程调度接口
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// 运行
        /// </summary>
        void Run();
        /// <summary>
        /// 停止
        /// </summary>
        void Stop();
        /// <summary>
        /// 执行调度请求
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        void Resume(WaitingResumption r);
    }
    /// <summary>
    /// NTFE-BPM工作流应用调度器默认实现
    /// <remarks>
    /// 用于进行面向工作流应用的调度处理，如：resumebook
    /// </remarks>
    /// </summary>
    [Transactional]
    public class Scheduler : IScheduler
    {
        private object _lock = new object();
        private bool _scheduling;//是否正在调度
        private bool _running;//是否运行

        private ILog _log;
        //调度器标识
        private string _schedulerId;
        private double _interval;
        private int _perChargeCount;
        private System.Timers.Timer _timer;
        private IEnumerable<IOrderedEnumerable<long>> _list;

        //由于此类运行时单例，对外部服务的依赖应实时解释
        private ISchedulerService _resumptionService { get { return DependencyResolver.Resolve<ISchedulerService>(); } }
        private IProcessService _processService { get { return DependencyResolver.Resolve<IProcessService>(); } }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="schedulerId">必须全局唯一</param>
        /// <param name="interval">调度间隔</param>
        /// <param name="perChargeCount">每次负责调度的数量</param>
        public Scheduler(ILoggerFactory factory
            , string schedulerId
            , string interval
            , string perChargeCount)
        {
            this._log = factory.Create(typeof(Scheduler));
            this._schedulerId = schedulerId;
            this._interval = double.Parse(interval);
            this._perChargeCount = int.Parse(perChargeCount);
        }

        public virtual void Run()
        {
            this.MarkAsRunning();
            if (this._timer != null) return;
            this.PrepareTimer();
            this._timer.Start();
            this._log.Info("NTFE-BPM应用调度器启动");
        }
        public virtual void Stop()
        {
            this._running = false;

            if (this._timer != null)
            {
                this._timer.Stop();
                this._timer = null;
                //TODO:利用线程池特性做安全结束等待
                System.Threading.Thread.Sleep(2000);
            }

            this._log.Info("NTFE-BPM应用调度器停止");
        }

        [Transaction(TransactionMode.Requires)]
        public virtual void Resume(WaitingResumption r)
        {
            //HACK:【重要】时间调度有可能导致调度器被未满足请求占满，必须在repository层ChargeResumption时预先进行过滤
            if (!r.CanResumeAtNow)
            {
                if (this._log.IsDebugEnabled)
                    this._log.Debug("当前时间未满足延迟调度项的时间条件");
                return;
            }
            /* 
             * HACK:【重要】将处于不可执行的调度项置为无效，用于处理由于以下原因出现的不合法的调度记录：
             * 流程状态与调度状态发生冲突，不过此类情况已经在调度重构后最大限度避免，但由于调度/服务分离的方式，还是保留此逻辑避免意外
             */
            if (!r.CanExecuting)
            {
                r.SetInvalid();
                this._resumptionService.Update(r);
                this._log.WarnFormat("流程{0}#{1}处于{2}状态，该调度请求不允许被执行，取消该调度请求{3}#{4}"
                    , r.Process.Title
                    , r.Process.ID
                    , r.Process.Status
                    , r
                    , r.ID);
                return;
            }
            //通过调度项对应的执行器执行实际调度逻辑
            DependencyResolver.Resolve<IWaitingResumptionHandle>(r.Handle).Resume(r);
            //尝试将流程置为Active状态
            this.TrySetAsActiveAtResumption(r.Process, r);//注意与Update(r)的先后顺序，避免二者逻辑（update与AnyValidAndUnExecutedResumptions）死锁
            //置为已执行
            r.SetExecuted();
            this._resumptionService.Update(r);

            this._log.InfoFormat(
                "[调度记录]{0}#{1}|Priority={2}|ProcessTitle={3}|ProcessId={4}"
                , r
                , r.ID
                , r.Priority
                , r.Process.Title
                , r.Process.ID);
        }

        //此方法不可声明事务，内部逻辑均应为独立事务，不可互相影响
        //[Transaction(TransactionMode.Requires)]
        public virtual bool Resume(long id)
        {
            var r = this._resumptionService.Get(id);
            if (r == null)
                throw new InvalidOperationException(string.Format("没有找到标识为{0}的调度项", id));

            try
            {
                this.Resume(r);
                return true;
            }
            catch (Exception e)
            {
                this._log.Error(string.Format("执行调度项{0}#{1}时发生错误，EnableAutoRetry={2}", r, r.ID, r.EnableAutoRetry), e);
                if (!r.EnableAutoRetry) this.MakeAnError(r, e);
                return false;
            }
        }
        [Transaction(TransactionMode.RequiresNew)]
        protected virtual void MakeAnError(WaitingResumption r, Exception e)
        {
            //HACK:【重要】此处必须保证更新被提交，不能被nhsession问题影响，因此必须直接sqlupdate来保证
            //r.SetError(true);
            //this._resumptionService.Update(r);
            this._resumptionService.MarkAsError(r);
            this._resumptionService.AddErrorRecord(new FaultResumptionRecord(r.Process, e, r.ID));
        }

        private void PrepareTimer()
        {
            //使用timer做工作线程定时唤醒，也可以考虑切换到job框架上
            this._timer = new System.Timers.Timer(this._interval);
            this._timer.Elapsed += (state, args) =>
            {
                //一个调度实例同时只有一个worker线程激活
                if (!this._running) return;
                if (this._scheduling) return;
                lock (this._lock)
                    if (this._scheduling)
                        return;
                    else
                        this._scheduling = true;
                this.ChargeAndResume();
                //置为非调度状态
                this._scheduling = false;
            };
        }

        internal void MarkAsRunning()
        {
            this._running = true;
        }
        internal void ChargeAndResume()
        {
            try { _list = this.ChargeResumption(); }
            catch (Exception e) { this._log.Fatal("Charging error", e); }
            //按分组并行调度
            if (_list != null)
                _list.ToList().ForEach(o =>
                //_list.AsParallel().ForAll(o =>
                {
                    if (this._running)
                        //不同流程实例之间不可互相影响
                        try { this.Resume(o); }
                        catch (Exception e) { this._log.Fatal("Resuming error", e); }
                });
        }
        //HACK:【重要】对单个流程实例的串行顺序调度（不包含仍处于延迟期的调度？），有异常则跳出
        /*
         * 关于父子流程之间的调度顺序影响问题解决
         * 在按流程串行调度前提下，按调度影响点分析
         * 1.发起子流程：无影响
         * 2.子流程结束：子流程本身正常结束即可，对父流程的唤醒排入父流程调度项序列中，按父流程的行为继续调度即可，对应SubProcessCompleteResumption设计
         * 3.并行活动中取消子流程节点：
         *      ActivityInstanceCancelResumption节点取消调度被设计为可自动重试，
         *      直到子流程处于调度项安全状态时才会被删除
         */
        internal void Resume(IOrderedEnumerable<long> list)
        {
            bool error = false;
            WaitingResumption prev = null, r = null;
            foreach (var id in list.Distinct())
            {
                r = this._resumptionService.Get(id);
                //忽略不存在的调度项
                if (r == null) continue;

                if (error
                    && !this.AreAtSameFlowNodeIndex(prev, r))
                {
                    this._log.WarnFormat("对流程“{0}”#{1}进行串行调度时异常，中断本次对该流程的调度，等待自动或人工重试"
                        , r.Process != null ? r.Process.Title : string.Empty
                        , r.Process != null ? r.Process.ID : Guid.Empty);
                    return;
                }

                error = !this.Resume(id);
                prev = r;
            }
        }

        private void TrySetAsActiveAtResumption(Process process, WaitingResumption resumption)
        {
            //只有部分调度项完成后才可进入Active
            if (!resumption.EnableActiveAfterExecuted) return;
            //HACK:【重要】只要存在未执行的有效调度项就不可进入Active状态
            if (this._resumptionService.AnyValidAndUnExecutedResumptions(process, resumption))
            {
                this._log.InfoFormat("由于流程存在未完成的调度请求，无法将其置为Active状态");
                return;
            }
            process.MarkAsActive();
            this._processService.Update(process);
            this._log.InfoFormat("流程实例“{0}”#{1}进入Active状态", process.Title, process.ID);
        }
        private IEnumerable<IOrderedEnumerable<long>> ChargeResumption()
        {
            return Scheduler.PrepareCharge(this._resumptionService.ChargeResumption(this._schedulerId, this._perChargeCount));
        }
        private bool AreAtSameFlowNodeIndex(WaitingResumption prev, WaitingResumption r)
        {
            return !prev.FlowNodeIndex.HasValue
                || !r.FlowNodeIndex.HasValue
                || (prev.FlowNodeIndex.HasValue
                && r.FlowNodeIndex.HasValue
                && r.FlowNodeIndex == prev.FlowNodeIndex);
        }
        protected internal static IEnumerable<IOrderedEnumerable<long>> PrepareCharge(IEnumerable<Tuple<long, Guid>> list)
        {
            //HACK:调度项按流程标识分组并按id正序，保证按流程以及调度项创建先后顺序串行调度
            return list.GroupBy(o => o.Item2).Select(o => o.Select(p => p.Item1).OrderBy(p => p));
        }
    }
}