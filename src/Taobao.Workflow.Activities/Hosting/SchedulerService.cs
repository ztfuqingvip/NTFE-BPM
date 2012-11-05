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
using Castle.Services.Transaction;
using CodeSharp.Core.RepositoryFramework;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 调度服务接口
    /// </summary>
    public interface ISchedulerService
    {
        /// <summary>
        /// 获取合适的调度负责标识
        /// </summary>
        /// <param name="processId"></param>
        /// <returns></returns>
        string GetChargingBy(Guid processId);

        //UNDONE:resumption多数情况下以id方式获取并操作，以及直接sqlupdate，由于这种使用方式与nh的unitofwork冲突，除了该对象本身外，不应直接关联其他聚合，并且总是以stateless方式使用resumption，以避免不必要的问题
        #region WaitingResumption
        /// <summary>
        /// 增加调度项
        /// </summary>
        /// <param name="r"></param>
        void Add(WaitingResumption r);
        /// <summary>
        /// 更新调度项
        /// </summary>
        /// <param name="r"></param>
        void Update(WaitingResumption r);

        void MarkAsError(WaitingResumption r);
        /// <summary>
        /// 获取调度项
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        WaitingResumption Get(long id);
        /// <summary>
        /// 负责调度指定数量的恢复请求
        /// </summary>
        /// <param name="chargingBy">指定负责调度者标识</param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<Tuple<long, Guid>> ChargeResumption(string chargingBy, int count);
        /// <summary>
        /// 负责调度指定数量的指定类型的恢复请求
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <param name="chargingBy"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        IEnumerable<Tuple<long, Guid>> ChargeResumption<T>(string chargingBy, int count);
        /// <summary>
        /// 取消指定流程的所有调度运行时相关数据
        /// </summary>
        /// <param name="process"></param>
        void CancelAll(Process process);
        /// <summary>
        /// 取消指定节点的所有调度运行时相关数据
        /// </summary>
        /// <param name="process"></param>
        /// <param name="activityInstanceId"></param>
        /// <param name="activityName"></param>
        void CancelAll(Process process, long activityInstanceId, string activityName);
        /// <summary>
        /// 取消指定节点的所有事件升级等待调度请求
        /// </summary>
        /// <param name="process"></param>
        /// <param name="activityInstanceId"></param>
        /// <param name="activityName"></param>
        void CancelAllEscalationJob(Process process, long activityInstanceId, string activityName);
        /// <summary>
        /// 获取指定流程的调度请求（未完成的、有效的）
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        IEnumerable<WaitingResumption> GetValidWaitingResumptions(Process process);
        /// <summary>
        /// 除了指定调度项外是否有 未完成的、有效的 调度请求
        /// </summary>
        /// <param name="process"></param>
        /// <param name="except"></param>
        /// <returns></returns>
        bool AnyValidAndUnExecutedResumptions(Process process, WaitingResumption except);
        /// <summary>
        /// 除了指定时间的延迟调度外是否 未完成的、有效的、没有发生错误的 调度请求
        /// <remarks>
        /// </remarks>
        /// </summary>
        /// <param name="process"></param>
        /// <param name="at"></param>
        /// <returns></returns>
        bool AnyValidAndUnExecutedAndNoErrorResumptionsExceptDelayAt(Process process, DateTime at);
        #endregion

        #region ErrorRecord
        void AddErrorRecord(ErrorRecord record);
        IEnumerable<ErrorRecord> GetErrorRecords();
        IEnumerable<ErrorRecord> GetErrorRecords(Process process);
        /// <summary>
        /// 错误重试
        /// </summary>
        /// <param name="record"></param>
        void Retry(ErrorRecord record);
        #endregion
    }
    /// <summary>
    /// 调度服务
    /// <remarks>集成了hosting下的各类调度信息访问功能，非DomainService意图，设计不可参考</remarks>
    /// </summary>
    [Transactional]
    public class SchedulerService : ISchedulerService
    {
        private static IResumptionRepository _resumptionRepository;
        private static IErrorRecordRepository _errorRepository;
        private string[] _schedulers;
        static SchedulerService()
        {
            _resumptionRepository = RepositoryFactory.GetRepository<IResumptionRepository, long, WaitingResumption>();
            _errorRepository = RepositoryFactory.GetRepository<IErrorRecordRepository, long, ErrorRecord>();
        }
        public SchedulerService(string ntfeSchedulerIds)
        {
            this._schedulers = ntfeSchedulerIds.Split('|');
        }

        string ISchedulerService.GetChargingBy(Guid processId)
        {
            //根据流程标识返回一个调度器id作为调度标识
            return this._schedulers[Math.Abs(processId.GetHashCode()) % this._schedulers.Length];
        }

        #region WaitingResumption
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.Add(WaitingResumption r)
        {
            _resumptionRepository.Add(r);
        }
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.Update(WaitingResumption r)
        {
            _resumptionRepository.Update(r);
        }
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.MarkAsError(WaitingResumption r)
        {
            r.SetError(true);
            _resumptionRepository.SetError(r, true);

        }
        WaitingResumption ISchedulerService.Get(long id)
        {
            return _resumptionRepository.FindBy(id);
        }
        [Transaction(TransactionMode.Requires)]
        IEnumerable<Tuple<long, Guid>> ISchedulerService.ChargeResumption(string chargingBy, int count)
        {
            return _resumptionRepository.ChargeResumption(chargingBy, count);
        }
        [Transaction(TransactionMode.Requires)]
        IEnumerable<Tuple<long, Guid>> ISchedulerService.ChargeResumption<T>(string chargingBy, int count)
        {
            return _resumptionRepository.ChargeResumption<T>(chargingBy, count);
        }
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.CancelAll(Process process)
        {
            //取消调度项
            _resumptionRepository.CancelAll(process);
            //取消错误记录
            _errorRepository.CancelAll(process);
        }
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.CancelAll(Process process, long activityInstanceId, string activityName)
        {
            //取消调度项
            _resumptionRepository.CancelAll(process, activityInstanceId, activityName);
            //取消错误记录
            _errorRepository.CancelAll(process, activityName);
        }
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.CancelAllEscalationJob(Process process, long activityInstanceId, string activityName)
        {
            _resumptionRepository.CancelAllEscalationJob(process, activityInstanceId, activityName);
        }
        IEnumerable<WaitingResumption> ISchedulerService.GetValidWaitingResumptions(Process process)
        {
            return _resumptionRepository.FindValidWaitingResumptions(process);
        }

        bool ISchedulerService.AnyValidAndUnExecutedResumptions(Process process, WaitingResumption except)
        {
            return _resumptionRepository.FindAnyValidAndUnExecutedResumptions(process, except);
        }
        bool ISchedulerService.AnyValidAndUnExecutedAndNoErrorResumptionsExceptDelayAt(Process process, DateTime at)
        {
            return _resumptionRepository.FindAnyValidAndUnExecutedAndNoErrorResumptionsExceptDelayAt(process, at);
        }

        #endregion

        #region ErrorRecord
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.AddErrorRecord(ErrorRecord record)
        {
            _errorRepository.Add(record);
        }

        IEnumerable<ErrorRecord> ISchedulerService.GetErrorRecords()
        {
            return _errorRepository.FindAllValid();
        }
        IEnumerable<ErrorRecord> ISchedulerService.GetErrorRecords(Process process)
        {
            return _errorRepository.FindAllValid(process);
        }
        [Transaction(TransactionMode.Requires)]
        void ISchedulerService.Retry(ErrorRecord record)
        {
            if (record is FaultBookmarkRecord)
                this.Retry(record as FaultBookmarkRecord);
            if (record is FaultResumptionRecord)
                this.Retry(record as FaultResumptionRecord);
        }
        #endregion

        private void Retry(FaultBookmarkRecord record)
        {
            //创建书签恢复调度项即可
            (this as ISchedulerService).Add(new BookmarkResumption(record.Process
                , record.ActivityName
                , record.BookmarkName
                , string.Empty));
            record.MarkAsDeleted();
            _errorRepository.Update(record);
        }
        private void Retry(FaultResumptionRecord record)
        {
            record.MarkAsDeleted();
            _errorRepository.Update(record);
            //将对应调度项置为非错误即可被重试
            var r = (this as ISchedulerService).Get(record.ResumptionId);
            r.SetError(false);
            (this as ISchedulerService).Update(r);
        }
    }
}