using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Services.Transaction;
using Taobao.Infrastructure;
using Taobao.Infrastructure.Services;
using Taobao.Infrastructure.RepositoryFramework;
using Taobao.Workflow.Activities.Hosting;
using Castle.Facilities.NHibernateIntegration;

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// NTFE-BPM工作流应用调度器默认实现
    /// <remarks>
    /// 用于进行面向工作流应用的调度处理，如：resumebook
    /// </remarks>
    /// </summary>
    [Transactional]
    public class Scheduler : IScheduler
    {
        private static readonly string _lockFlag = "NTFE-BPM_Scheduler";
        private static IResumptionRepository _repository;
        private object _lock = new object();
        private bool _charging;
        private bool _running;

        private ILog _log;
        //调度器标识
        private string _schedulerId;
        private double _interval;
        private int _perChargeCount;
        private System.Timers.Timer _timer;

        static Scheduler()
        {
            _repository = RepositoryFactory.GetRepository<IResumptionRepository, long, WaitingResumption>();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="processService"></param>
        /// <param name="parser">解析器</param>
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

        #region IScheduler Members

        public void Run()
        {
            this._running = true;

            if (this._timer == null)
            {
                this._timer = new System.Timers.Timer(this._interval);
                this._timer.Elapsed += (state, args) =>
                {
                    if (!this._running) return;

                    //HACK:一个调度实例同时只有一个worker线程激活
                    if (this._charging) return;

                    lock (this._lock)
                    {
                        if (this._charging) return;
                        this._charging = true;
                    }

                    //this._log.Debug("ChargeResumption");

                    try
                    {
                        this.ChargeResumption()
                            //.AsParallel()//TODO:parallel调度效果需要进一步验证
                            //.ForAll(r =>
                            //TODO:调整为按流程串行调度，避免意外并发问题和保证调度顺序
                            .ForEach(r =>
                            {
                                if (!this._running) return;

                                try
                                {
                                    //HACK:由于匿名委托共享Run的callcontext因此需要单独声明session
                                    using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
                                    {
                                        this.Resume(r);
                                        session.Flush();
                                    }
                                    //尝试将流程置为active
                                    using (var session = Taobao.Infrastructure.Castles.NHibernateRepositoryUtility.UnmanagedSession())
                                    {
                                        var processService = DependencyResolver.Resolve<IProcessService>();
                                        var resumption = _repository.FindBy(r);
                                        //if (resumption.Process.Status == ProcessStatus.Running)
                                        //{
                                        if (resumption is SubProcessCreateResumption || resumption is WorkItemCreateResumption)
                                        {
                                            processService.TrySetAsActiveAtResumption(resumption.Process, resumption);
                                            session.Flush();
                                        }
                                        //}
                                    }
                                }
                                catch (Exception e)
                                {
                                    //HACK:此处异常将被认为是意外出现的异常?
                                    this._log.Error(string.Format("Resumption Error Occur At #{0}", r), e);
                                }
                            });
                    }
                    catch (Exception e)
                    {
                        this._log.Fatal("Charging error", e);
                    }
                    finally
                    {
                        this._charging = false;
                    }
                };
                this._timer.Start();
            }

            this._log.Info("NTFE-BPM应用调度器启动");
        }

        public void Stop()
        {
            this._running = false;

            if (this._timer != null)
            {
                this._timer.Stop();
                this._timer = null;
                System.Threading.Thread.Sleep(1000);
            }

            this._log.Info("NTFE-BPM应用调度器停止");
        }

        [Transaction(TransactionMode.Requires)]
        public void Resume(long resumptionId)
        {
            this.Resume(_repository.FindBy(resumptionId));
        }

        [Transaction(TransactionMode.Requires)]
        public void Resume(WaitingResumption r)
        {
            if (r == null)
                throw new InvalidOperationException("无效的恢复请求");

            //HACK:时间调度有可能导致调度器被未满足请求占满，需要在ChargeResumption时预先进行过滤，在db处理
            if (!r.CanResumeAtNow)
            {
                if (this._log.IsDebugEnabled)
                    this._log.Debug("当前时间未满足恢复时间条件");
                return;
            }

            if (!r.CanExecuting)
            {
                r.SetInvalid();
                _repository.Update(r);
                this._log.WarnFormat("流程{0}#{1}处于{2}状态，该调度请求不允许被执行，取消该调度请求{3}#{4}"
                    , r.Process.Title
                    , r.Process.ID
                    , r.Process.Status
                    , r
                    , r.ID);
                return;
            }

            var success = false;
            //TODO:是否需要并将流程置为Error状态?
            //恢复调度中的任何异常都不能影响调度过程，同时间调度的考虑一样，无法被执行调度也有可能堆积而影响调度
            try
            {
                DependencyResolver.Resolve<IWaitingResumptionHandle>(r.Handle).Resume(r);
                success = true;
                this._log.InfoFormat("[调度记录]{0}#{1}|Priority={2}|ProcessType={3}|ProcessId={4}"
                    , r
                    , r.ID
                    , r.Priority
                    , r.Process.ProcessType.Name
                    , r.Process.ID);
            }
            catch (Exception e)
            {
                //TODO:将流程置为Error，此处异常通常是系统异常
                this._log.Error(string.Format("Resumption Error Occur At {0}#{1}", r, r.ID), e);
            }
            finally
            {
                //若允许重试，则可以继续被调度，要考虑重试堆积问题，可通过引入调度优先级来避免
                if (success || !r.EnableRetry)
                {
                    //将调度请求完成后置为失效
                    r.SetExecuted();
                    _repository.Update(r);
                }
            }
        }
        #endregion

        private List<long> ChargeResumption()
        {
            var list = new List<long>();
            //优先处理节点实例取消请求，避免对下一个节点执行造成干扰
            list.AddRange(_repository.ChargeResumption<ActivityInstanceCancelResumption>(this._schedulerId, this._perChargeCount));
            //优先处理任务创建请求
            list.AddRange(_repository.ChargeResumption<WorkItemCreateResumption>(this._schedulerId, this._perChargeCount));
            list.AddRange(_repository.ChargeResumption(this._schedulerId, this._perChargeCount));
            list = list.Distinct().ToList();
            if (this._log.IsDebugEnabled && list.Count > 0)
                this._log.DebugFormat("获得对#{0}的调度职责", string.Join("#", list));
            return list;
        }
    }
}