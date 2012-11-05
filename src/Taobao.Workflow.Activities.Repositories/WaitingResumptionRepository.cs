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
using CodeSharp.Core.Castles;
using Taobao.Workflow.Activities.Hosting;
using NHibernate.Criterion;
using Castle.Services.Transaction;

namespace Taobao.Workflow.Activities.Repositories
{
    /// <summary>
    /// 默认的调度实现
    /// </summary>
    public class WaitingResumptionRepository : NHibernateRepositoryBase<long, WaitingResumption>, Hosting.IResumptionRepository
    {
        public static readonly string Table_Resumption = "NTFE_WaitingResumption";

        #region IResumptionRepository Members
        [Transaction(TransactionMode.Requires)]
        public IEnumerable<Tuple<long, Guid>> ChargeResumption(string chargingBy, int count)
        {
            //已经在创建时完成此charging
            //            using (var session = this.GetSession())
            //            {
            //                session.CreateSQLQuery(string.Format(@"
            //update {0}
            //set chargingBy=:chargingBy
            //where id 
            //in (select top {1} id from NTFE_WaitingResumption where chargingBy is null)"
            //                    , Table_Resumption
            //                    , count))
            //                    .SetString("chargingBy", chargingBy)
            //                    .ExecuteUpdate();

            return this.GetStatelessSession()
                .CreateCriteria<WaitingResumption>()
                .SetCacheable(false)
                .SetProjection(Projections.Property("ID"), Projections.Property("Process.ID"))
                .Add(Expression.Eq("IsValid", true))
                .Add(Expression.Eq("IsExecuted", false))
                .Add(Expression.Eq("IsError", false))
                .Add(Expression.Eq("ChargingBy", chargingBy))
                .Add(Expression.Or(Expression.IsNull("At"), Expression.Le("At", DateTime.Now)))
                .AddOrder(Order.Asc("ID"))//保证调度顺序
                .List<object[]>()
                .Select(o => new Tuple<long, Guid>(Convert.ToInt64(o[0]), new Guid(o[1].ToString())));
        }
        [Transaction(TransactionMode.Requires)]
        public IEnumerable<Tuple<long, Guid>> ChargeResumption<T>(string chargingBy, int count)
        {
            //            using (var session = this.GetSession())
            //            {
            //                session.CreateSQLQuery(string.Format(@"
            //                    update {0}
            //                    set chargingBy=:chargingBy
            //                    where id
            //                    in (select top {1} id from NTFE_WaitingResumption where chargingBy is null and ResumptionType=:type)"
            //                    , Table_Resumption
            //                    , count))
            //                    .SetString("chargingBy", chargingBy)
            //                    .SetString("type", this.GetResumptionType<T>())
            //                    .ExecuteUpdate();

            return this.GetStatelessSession()
                .CreateCriteria<WaitingResumption>()
                .SetCacheable(false)
                .SetProjection(Projections.Property("ID"), Projections.Property("Process.ID"))
                .Add(Expression.Eq("IsValid", true))
                .Add(Expression.Eq("IsExecuted", false))
                .Add(Expression.Eq("IsError", false))
                .Add(Expression.Eq("ChargingBy", chargingBy))
                .Add(Expression.Or(Expression.IsNull("At"), Expression.Le("At", DateTime.Now)))
                .Add(Expression.Sql("ResumptionType=?", this.GetResumptionType<T>(), NHibernate.NHibernateUtil.String))
                .AddOrder(Order.Asc("ID"))//保证调度顺序
                .List<object[]>()
                .Select(o => new Tuple<long, Guid>(Convert.ToInt64(o[0]), new Guid(o[1].ToString()))); ;
        }
        //将流程对应的所有调度项置为无效
        [Transaction(TransactionMode.Requires)]
        public void CancelAll(Process process)
        {
            this.GetSession().CreateSQLQuery(string.Format(@"
update {0}
set IsValid=:isValid
where ProcessId=:processId", Table_Resumption))
                .SetBoolean("isValid", false)
                .SetGuid("processId", process.ID)
                .ExecuteUpdate();
        }
        //将节点实例对应的所有调度项置为无效
        [Transaction(TransactionMode.Requires)]
        public void CancelAll(Process process, long activityInstanceId, string activityName)
        {
            this.GetSession().CreateSQLQuery(string.Format(@"
                update {0}
                set IsValid=:isValid
                where ProcessId=:processId 
                and (
                HumanActivityInstanceId=:activityInstanceId 
                or SubProcessActivityInstanceId=:activityInstanceId 
                or ActivityName=:activityName
                )", Table_Resumption))
                .SetBoolean("isValid", false)
                .SetGuid("processId", process.ID)
                .SetInt64("activityInstanceId", activityInstanceId)
                .SetString("activityName", activityName)
                .ExecuteUpdate();
        }
        //将节点实例对应的所有事件升级调度项置为无效
        [Transaction(TransactionMode.Requires)]
        public void CancelAllEscalationJob(Process process, long activityInstanceId, string activityName)
        {
            this.GetSession().CreateSQLQuery(string.Format(@"
                update {0} 
                set IsValid=:isValid 
                where ProcessId=:processId 
                and ResumptionType=:resumptionType 
                and (
                HumanActivityInstanceId=:activityInstanceId 
                or ActivityName=:activityName
                )", Table_Resumption))
                .SetBoolean("isValid", false)
                .SetGuid("processId", process.ID)
                .SetString("resumptionType", this.GetResumptionType<HumanEscalationResumption>())
                .SetInt64("activityInstanceId", activityInstanceId)
                .SetString("activityName", activityName)
                .ExecuteUpdate();
        }
        public IEnumerable<WaitingResumption> FindValidWaitingResumptions(Process process)
        {
            return this.GetSession().CreateCriteria<WaitingResumption>()
                    .Add(Expression.Eq("Process", process))
                    .Add(Expression.Eq("IsValid", true))
                    .List<WaitingResumption>();
        }
        public bool FindAnyValidAndUnExecutedResumptions(Process process, WaitingResumption except)
        {
            return this.GetSession().CreateCriteria<WaitingResumption>()
                .Add(Expression.Eq("Process", process))
                .Add(Expression.Eq("IsValid", true))
                .Add(Expression.Eq("IsExecuted", false))
                .Add(Expression.Not(Expression.IdEq(except.ID)))
                .SetProjection(Projections.RowCount())
                .SetCacheable(false)
                .UniqueResult<int>() > 0;
        }
        public bool FindAnyValidAndUnExecutedAndNoErrorResumptionsExceptDelayAt(Process process, DateTime at)
        {
            return this.GetSession().CreateCriteria<WaitingResumption>()
                .Add(Expression.Eq("Process", process))
                .Add(Expression.Eq("IsValid", true))
                .Add(Expression.Eq("IsExecuted", false))
                .Add(Expression.Eq("IsError", false))
                //延迟时间需要小于指定时间
                .Add(Expression.Or(Expression.IsNull("At"), Expression.Le("At", at)))
                .SetProjection(Projections.RowCount())
                .SetCacheable(false)
                .UniqueResult<int>() > 0;
        }
        [Transaction(TransactionMode.Requires)]
        public void SetError(WaitingResumption r, bool error)
        {
            this.GetStatelessSession()
                .CreateSQLQuery(string.Format("update {0} set IsError=:error where ID=:id", Table_Resumption))
                .SetInt64("id", r.ID)
                .SetBoolean("error", error)
                .ExecuteUpdate();
        }
        #endregion

        private string GetResumptionType<T>()
        {
            //与Mapping相关
            if (typeof(T) == typeof(ProcessStartResumption))
                return "start";
            if (typeof(T) == typeof(BookmarkResumption))
                return "bookmark";
            if (typeof(T) == typeof(ErrorResumption))
                return "error";
            if (typeof(T) == typeof(WorkItemCreateResumption))
                return "w_create";
            if (typeof(T) == typeof(SubProcessCompleteResumption))
                return "sub_complete";
            if (typeof(T) == typeof(SubProcessCreateResumption))
                return "sub_create";
            if (typeof(T) == typeof(ActivityInstanceCancelResumption))
                return "a_cancel";
            if (typeof(T) == typeof(HumanEscalationResumption))
                return "escalation";
            return "";
        }
    }
}