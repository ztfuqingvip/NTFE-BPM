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
using CodeSharp.Core.RepositoryFramework;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 恢复记录仓储
    /// </summary>
    public interface IResumptionRepository : IRepository<long, WaitingResumption>
    {
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
        /// 取消指定流程的所有调度项
        /// </summary>
        /// <param name="process"></param>
        void CancelAll(Process process);
        /// <summary>
        /// 取消指定节点的所有调度项
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
        /// 获取指定流程的有效的调度请求 未完成的、有效的、
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        IEnumerable<WaitingResumption> FindValidWaitingResumptions(Process process);
        /// <summary>
        /// 除了指定调度项外是否有 未完成的、有效的 调度请求
        /// </summary>
        /// <param name="process"></param>
        /// <param name="except"></param>
        /// <returns></returns>
        bool FindAnyValidAndUnExecutedResumptions(Process process, WaitingResumption except);
        /// <summary>
        /// 除了指定时间的延迟调度外是否 未完成的、有效的、没有发生错误的 调度请求
        /// </summary>
        /// <param name="process"></param>
        /// <param name="at"></param>
        /// <returns></returns>
        bool FindAnyValidAndUnExecutedAndNoErrorResumptionsExceptDelayAt(Process process, DateTime at);
        /// <summary>
        /// 将调度项置为Error
        /// </summary>
        /// <param name="r"></param>
        /// <param name="error"></param>
        void SetError(WaitingResumption r, bool error);
    }
}